using SocialApp.Api.Models;

namespace SocialApp.Api.Services;

public sealed record SocialSession(
    SocialProvider Provider,
    string AccessToken,
    string? AccessTokenSecret,
    string? RefreshToken);
