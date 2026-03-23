namespace SocialApp.Api.Models;

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }
}
