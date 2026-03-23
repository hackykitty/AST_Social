namespace SocialApp.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = "";
    public string Issuer { get; set; } = "SocialApp.Api";
    public string Audience { get; set; } = "SocialApp.Spa";
    public int SessionMinutes { get; set; } = 60;
}
