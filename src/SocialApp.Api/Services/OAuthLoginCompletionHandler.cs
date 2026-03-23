using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SocialApp.Api.Models;
using SocialApp.Api.Options;

namespace SocialApp.Api.Services;

public sealed class OAuthLoginCompletionHandler
{
    private readonly ISocialSessionStore _sessions;
    private readonly IJwtTokenIssuer _jwt;
    private readonly IOptions<FrontendOptions> _frontend;
    private readonly ILogger<OAuthLoginCompletionHandler> _logger;

    public OAuthLoginCompletionHandler(
        ISocialSessionStore sessions,
        IJwtTokenIssuer jwt,
        IOptions<FrontendOptions> frontend,
        ILogger<OAuthLoginCompletionHandler> logger)
    {
        _sessions = sessions;
        _jwt = jwt;
        _frontend = frontend;
        _logger = logger;
    }

    /// <summary>
    /// Runs after the remote handler has built a ticket and stored tokens on <see cref="AuthenticationProperties"/>.
    /// Uses <see cref="TicketReceivedContext.HandleResponse"/> so the default cookie sign-in and redirect are skipped.
    /// </summary>
    public Task CompleteFromTicketReceivedAsync(TicketReceivedContext context, SocialProvider provider)
    {
        var props = context.Properties;
        if (props is null)
        {
            RedirectTicketError(context, "missing_properties");
            return Task.CompletedTask;
        }

        var accessToken = props.GetTokenValue("access_token");
        var tokenSecret = props.GetTokenValue("access_token_secret");
        var refreshToken = props.GetTokenValue("refresh_token");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("OAuth login for {Provider} completed without an access token.", provider);
            RedirectTicketError(context, "missing_access_token");
            return Task.CompletedTask;
        }

        var principal = context.Principal ?? new ClaimsPrincipal();
        var session = new SocialSession(provider, accessToken, tokenSecret, refreshToken);
        var sessionId = _sessions.Create(session);

        var extraClaims = principal.Claims.Where(c =>
            c.Type is ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Email);

        var jwt = _jwt.CreateToken(sessionId, extraClaims);
        var baseUrl = _frontend.Value.BaseUrl.TrimEnd('/');
        var target = $"{baseUrl}/auth/callback?token={Uri.EscapeDataString(jwt)}";

        context.HandleResponse();
        context.Response.Redirect(target);
        return Task.CompletedTask;
    }

    public void RedirectWithRemoteFailure(RemoteFailureContext context, string? message)
    {
        var baseUrl = _frontend.Value.BaseUrl.TrimEnd('/');
        context.HandleResponse();
        var code = string.IsNullOrWhiteSpace(message) ? "remote_failure" : message;
        context.Response.Redirect($"{baseUrl}/auth/callback?error={Uri.EscapeDataString(code)}");
    }

    private void RedirectTicketError(TicketReceivedContext context, string code)
    {
        var baseUrl = _frontend.Value.BaseUrl.TrimEnd('/');
        context.HandleResponse();
        context.Response.Redirect($"{baseUrl}/auth/callback?error={Uri.EscapeDataString(code)}");
    }
}
