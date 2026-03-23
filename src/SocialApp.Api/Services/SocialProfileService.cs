using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialApp.Api.Infrastructure;
using SocialApp.Api.Models;

namespace SocialApp.Api.Services;

public sealed class SocialProfileService : ISocialProfileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SocialProfileService> _logger;

    public SocialProfileService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SocialProfileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SocialProfileResponse> BuildProfileAsync(SocialSession session, CancellationToken cancellationToken = default)
    {
        return session.Provider switch
        {
            SocialProvider.Facebook => await BuildFacebookAsync(session.AccessToken, cancellationToken),
            SocialProvider.Twitter => await BuildTwitterAsync(session, cancellationToken),
            SocialProvider.LinkedIn => await BuildLinkedInAsync(session.AccessToken, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(session), session.Provider, "Unknown provider")
        };
    }

    private async Task<SocialProfileResponse> BuildFacebookAsync(string accessToken, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var fields =
            "name,email,mobile_phone,picture.type(large),feed.limit(5){message,story,full_picture,created_time}";
        var url =
            $"https://graph.facebook.com/v21.0/me?fields={Uri.EscapeDataString(fields)}&access_token={Uri.EscapeDataString(accessToken)}";

        using var response = await client.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook Graph error {Status}: {Body}", (int)response.StatusCode, body);
            throw new SocialApiException(
                "Facebook returned an error while loading your profile. Ensure the app has the required permissions and your token is valid.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "Facebook user" : "Facebook user";
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        var phone = root.TryGetProperty("mobile_phone", out var p) ? p.GetString() : null;
        var picture = TryGetFacebookPicture(root);

        var posts = new List<SocialPostItem>();
        if (root.TryGetProperty("feed", out var feed) && feed.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (posts.Count >= 3)
                    break;

                var text = item.TryGetProperty("message", out var m)
                    ? m.GetString()
                    : item.TryGetProperty("story", out var s)
                        ? s.GetString()
                        : null;

                var image = item.TryGetProperty("full_picture", out var fp) ? fp.GetString() : null;

                var created = item.TryGetProperty("created_time", out var c) && DateTimeOffset.TryParse(c.GetString(), out var dto)
                    ? dto
                    : (DateTimeOffset?)null;

                if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(image))
                    continue;

                posts.Add(new SocialPostItem
                {
                    Text = text ?? "",
                    ImageUrl = image,
                    CreatedAt = created
                });
            }
        }

        return new SocialProfileResponse
        {
            Provider = "Facebook",
            Name = name,
            ProfilePictureUrl = picture,
            Email = email,
            Phone = phone,
            RecentPosts = posts,
            PostsNote = posts.Count == 0
                ? "No recent feed items returned. Grant `email` / feed-related permissions in the Facebook app, or your account may have no public feed entries."
                : null
        };
    }

    private static string? TryGetFacebookPicture(JsonElement root)
    {
        if (!root.TryGetProperty("picture", out var pic))
            return null;
        if (pic.TryGetProperty("data", out var d) && d.TryGetProperty("url", out var u))
            return u.GetString();
        return null;
    }

    private async Task<SocialProfileResponse> BuildTwitterAsync(SocialSession session, CancellationToken ct)
    {
        var consumerKey = _configuration["Authentication:Twitter:ConsumerKey"];
        var consumerSecret = _configuration["Authentication:Twitter:ConsumerSecret"];
        if (string.IsNullOrWhiteSpace(consumerKey) || string.IsNullOrWhiteSpace(consumerSecret))
            throw new InvalidOperationException("Twitter consumer key/secret are not configured.");

        if (string.IsNullOrWhiteSpace(session.AccessTokenSecret))
            throw new SocialApiException("Twitter login did not return a token secret; cannot call the Twitter API.");

        var client = _httpClientFactory.CreateClient();

        var meUri = new Uri(
            "https://api.twitter.com/2/users/me?user.fields=name,username,profile_image_url,description,confirmed_email");
        var meAuth = TwitterOAuth1Header.ForGet(meUri, consumerKey, consumerSecret, session.AccessToken, session.AccessTokenSecret);
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, meUri);
        meRequest.Headers.TryAddWithoutValidation("Authorization", meAuth);

        using var meResponse = await client.SendAsync(meRequest, ct);
        var meBody = await meResponse.Content.ReadAsStringAsync(ct);
        if (!meResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Twitter /2/users/me error {Status}: {Body}", (int)meResponse.StatusCode, meBody);
            throw new SocialApiException("Twitter rejected the profile request. Check API access tier and app permissions.");
        }

        using var meDoc = JsonDocument.Parse(meBody);
        if (!meDoc.RootElement.TryGetProperty("data", out var user))
            throw new SocialApiException("Unexpected Twitter profile response.");

        var name = user.TryGetProperty("name", out var n) ? n.GetString() ?? "Twitter user" : "Twitter user";
        var handle = user.TryGetProperty("username", out var u) ? u.GetString() : null;
        var picture = user.TryGetProperty("profile_image_url", out var pi) ? pi.GetString() : null;
        var email = user.TryGetProperty("confirmed_email", out var em) ? em.GetString() : null;

        var id = user.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var posts = new List<SocialPostItem>();
        if (!string.IsNullOrEmpty(id))
        {
            var timelineUri = new Uri(
                $"https://api.twitter.com/2/users/{id}/tweets?max_results=5&tweet.fields=created_at,attachments&expansions=attachments.media_keys&media.fields=preview_image_url,url,type");
            var tlAuth = TwitterOAuth1Header.ForGet(
                timelineUri,
                consumerKey,
                consumerSecret,
                session.AccessToken,
                session.AccessTokenSecret);
            using var tlRequest = new HttpRequestMessage(HttpMethod.Get, timelineUri);
            tlRequest.Headers.TryAddWithoutValidation("Authorization", tlAuth);

            using var tlResponse = await client.SendAsync(tlRequest, ct);
            var tlBody = await tlResponse.Content.ReadAsStringAsync(ct);
            if (tlResponse.IsSuccessStatusCode)
            {
                using var tlDoc = JsonDocument.Parse(tlBody);
                var mediaByKey = new Dictionary<string, string>();
                if (tlDoc.RootElement.TryGetProperty("includes", out var inc) &&
                    inc.TryGetProperty("media", out var mediaArr) &&
                    mediaArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in mediaArr.EnumerateArray())
                    {
                        var key = m.TryGetProperty("media_key", out var mk) ? mk.GetString() : null;
                        var url = m.TryGetProperty("preview_image_url", out var prev)
                            ? prev.GetString()
                            : m.TryGetProperty("url", out var urlEl)
                                ? urlEl.GetString()
                                : null;
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(url))
                            mediaByKey[key] = url;
                    }
                }

                if (tlDoc.RootElement.TryGetProperty("data", out var tweets) && tweets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tweets.EnumerateArray())
                    {
                        if (posts.Count >= 3)
                            break;

                        var text = t.TryGetProperty("text", out var te) ? te.GetString() ?? "" : "";
                        string? img = null;
                        if (t.TryGetProperty("attachments", out var att) &&
                            att.TryGetProperty("media_keys", out var keys) &&
                            keys.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var k in keys.EnumerateArray())
                            {
                                var ks = k.GetString();
                                if (ks is not null && mediaByKey.TryGetValue(ks, out var found))
                                {
                                    img = found;
                                    break;
                                }
                            }
                        }

                        var created = t.TryGetProperty("created_at", out var ca) &&
                                      DateTimeOffset.TryParse(ca.GetString(), out var c)
                            ? c
                            : (DateTimeOffset?)null;

                        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(img))
                            continue;

                        posts.Add(new SocialPostItem { Text = text, ImageUrl = img, CreatedAt = created });
                    }
                }
            }
            else
            {
                _logger.LogInformation("Twitter timeline not available: {Status} {Body}", (int)tlResponse.StatusCode, tlBody);
            }
        }

        return new SocialProfileResponse
        {
            Provider = "Twitter / X",
            Name = string.IsNullOrEmpty(handle) ? name : $"{name} (@{handle})",
            ProfilePictureUrl = picture,
            Email = email,
            Phone = null,
            RecentPosts = posts,
            PostsNote = posts.Count == 0
                ? "No tweets returned. Your app may need Elevated/Pro access, or the account has no recent tweets."
                : null
        };
    }

    private async Task<SocialProfileResponse> BuildLinkedInAsync(string accessToken, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        // OpenID Connect tokens (openid/profile/email) work with userinfo — not /v2/me (me.GET.NO_VERSION).
        using var userinfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
        userinfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userinfoResponse = await client.SendAsync(userinfoRequest, ct);
        var userinfoBody = await userinfoResponse.Content.ReadAsStringAsync(ct);
        if (!userinfoResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("LinkedIn /v2/userinfo error {Status}: {Body}", (int)userinfoResponse.StatusCode, userinfoBody);
            throw new SocialApiException("LinkedIn returned an error while loading your profile.");
        }

        using var profileDoc = JsonDocument.Parse(userinfoBody);
        var profile = LinkedInOpenIdUserInfoParser.Parse(profileDoc.RootElement);

        var posts = new List<SocialPostItem>();
        var personId = profile.Subject;
        if (!string.IsNullOrEmpty(personId))
        {
            var sharesUrl =
                $"https://api.linkedin.com/v2/shares?q=owners&owners=urn:li:person:{personId}&sortBy=LAST_MODIFIED&count=5";
            using var sharesRequest = new HttpRequestMessage(HttpMethod.Get, sharesUrl);
            sharesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var sharesResponse = await client.SendAsync(sharesRequest, ct);
            var sharesBody = await sharesResponse.Content.ReadAsStringAsync(ct);
            if (sharesResponse.IsSuccessStatusCode)
            {
                using var sharesDoc = JsonDocument.Parse(sharesBody);
                foreach (var item in LinkedInSharesResponseParser.ParseElements(sharesDoc.RootElement, maxCount: 3))
                    posts.Add(item);
            }
            else
            {
                _logger.LogInformation("LinkedIn shares not available: {Status} {Body}", (int)sharesResponse.StatusCode, sharesBody);
            }
        }

        return new SocialProfileResponse
        {
            Provider = "LinkedIn",
            Name = profile.DisplayName,
            ProfilePictureUrl = profile.PictureUrl,
            Email = profile.Email,
            Phone = null,
            RecentPosts = posts,
            PostsNote = posts.Count == 0
                ? "Recent shares are not available with Sign In with LinkedIn (OpenID) alone; the shares API needs additional LinkedIn products/permissions."
                : null
        };
    }
}

public sealed class SocialApiException : Exception
{
    public SocialApiException(string message) : base(message)
    {
    }
}
