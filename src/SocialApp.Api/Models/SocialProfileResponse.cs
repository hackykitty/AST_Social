namespace SocialApp.Api.Models;

public sealed class SocialProfileResponse
{
    public string Provider { get; init; } = "";
    public string Name { get; init; } = "";
    public string? ProfilePictureUrl { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<SocialPostItem> RecentPosts { get; init; } = Array.Empty<SocialPostItem>();
    public string? PostsNote { get; init; }
}
