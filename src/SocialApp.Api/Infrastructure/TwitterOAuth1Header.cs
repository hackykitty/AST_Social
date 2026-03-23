using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace SocialApp.Api.Infrastructure;

/// <summary>
/// Builds an OAuth 1.0a Authorization header for Twitter REST calls (user context).
/// </summary>
public static class TwitterOAuth1Header
{
    public static string ForGet(
        Uri requestUri,
        string consumerKey,
        string consumerSecret,
        string accessToken,
        string accessTokenSecret)
    {
        var method = "GET";
        var normalizedUrl = NormalizeUrl(requestUri);
        var queryParams = ParseQuery(requestUri.Query);

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        var all = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
            all[kv.Key] = kv.Value;

        all["oauth_consumer_key"] = consumerKey;
        all["oauth_nonce"] = nonce;
        all["oauth_signature_method"] = "HMAC-SHA1";
        all["oauth_timestamp"] = timestamp;
        all["oauth_token"] = accessToken;
        all["oauth_version"] = "1.0";

        var parameterString = string.Join(
            "&",
            all.Select(p => $"{Rfc3986Encode(p.Key)}={Rfc3986Encode(p.Value)}"));

        var signatureBase = string.Join(
            "&",
            new[] { method, Rfc3986Encode(normalizedUrl), Rfc3986Encode(parameterString) });

        var signingKey = $"{Rfc3986Encode(consumerSecret)}&{Rfc3986Encode(accessTokenSecret)}";
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase)));

        var headerParams = new Dictionary<string, string>(all) { ["oauth_signature"] = signature };

        return "OAuth " + string.Join(
            ", ",
            headerParams.Select(p => $"{Rfc3986Encode(p.Key)}=\"{Rfc3986Encode(p.Value)}\""));
    }

    private static string NormalizeUrl(Uri uri)
    {
        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath;
        var defaultPort = scheme == "https" ? 443 : 80;
        var authority = uri.Port == defaultPort ? host : $"{host}:{uri.Port}";
        return $"{scheme}://{authority}{path}";
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query) || query == "?")
            return result;

        var trimmed = query.StartsWith('?') ? query[1..] : query;
        foreach (var kv in QueryHelpers.ParseQuery(trimmed))
            result[kv.Key] = kv.Value.ToString();
        return result;
    }

    private static string Rfc3986Encode(string value)
    {
        return Uri.EscapeDataString(value).Replace("%7E", "~", StringComparison.Ordinal);
    }
}
