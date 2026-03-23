using System.Text.Json;
using SocialApp.Api.Models;

namespace SocialApp.Api.Services;

/// <summary>
/// Parses LinkedIn OpenID Connect <c>GET /v2/userinfo</c> JSON.
/// </summary>
public static class LinkedInOpenIdUserInfoParser
{
    public static LinkedInOpenIdUserProfile Parse(JsonElement root)
    {
        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name))
        {
            var first = root.TryGetProperty("given_name", out var f) ? f.GetString() ?? "" : "";
            var last = root.TryGetProperty("family_name", out var l) ? l.GetString() ?? "" : "";
            name = $"{first} {last}".Trim();
        }

        if (string.IsNullOrEmpty(name))
            name = "LinkedIn member";

        var picture = root.TryGetProperty("picture", out var picEl) ? picEl.GetString() : null;
        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        var sub = root.TryGetProperty("sub", out var subEl) ? subEl.GetString() : null;

        return new LinkedInOpenIdUserProfile(name, email, picture, sub);
    }

    public static LinkedInOpenIdUserProfile Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement);
    }
}

public sealed record LinkedInOpenIdUserProfile(
    string DisplayName,
    string? Email,
    string? PictureUrl,
    string? Subject);
