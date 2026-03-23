using System.Text.Json;
using SocialApp.Api.Models;

namespace SocialApp.Api.Services;

/// <summary>
/// Parses elements from LinkedIn <c>/v2/shares</c> style list responses.
/// </summary>
public static class LinkedInSharesResponseParser
{
    public static IReadOnlyList<SocialPostItem> ParseElements(JsonElement root, int maxCount)
    {
        var posts = new List<SocialPostItem>();
        if (maxCount <= 0)
            return posts;

        if (!root.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
            return posts;

        foreach (var share in elements.EnumerateArray())
        {
            if (posts.Count >= maxCount)
                break;

            var text = "";
            if (share.TryGetProperty("text", out var textEl) &&
                textEl.TryGetProperty("text", out var inner))
                text = inner.GetString() ?? "";

            string? img = null;
            if (share.TryGetProperty("content", out var content) &&
                content.TryGetProperty("contentEntities", out var entities) &&
                entities.ValueKind == JsonValueKind.Array)
            {
                foreach (var entity in entities.EnumerateArray())
                {
                    if (!entity.TryGetProperty("entityLocation", out var loc))
                        continue;
                    img ??= loc.GetString();
                }
            }

            DateTimeOffset? created = null;
            if (share.TryGetProperty("lastModified", out var lm) &&
                lm.ValueKind == JsonValueKind.Object &&
                lm.TryGetProperty("time", out var t) &&
                t.TryGetInt64(out var ms))
                created = DateTimeOffset.FromUnixTimeMilliseconds(ms);

            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(img))
                continue;

            posts.Add(new SocialPostItem { Text = text, ImageUrl = img, CreatedAt = created });
        }

        return posts;
    }

    public static IReadOnlyList<SocialPostItem> ParseElements(string json, int maxCount)
    {
        using var doc = JsonDocument.Parse(json);
        return ParseElements(doc.RootElement, maxCount);
    }
}
