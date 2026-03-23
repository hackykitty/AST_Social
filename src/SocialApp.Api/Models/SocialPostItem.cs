namespace SocialApp.Api.Models;

public sealed class SocialPostItem
{
    public string Text { get; init; } = "";
    public string? ImageUrl { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}
