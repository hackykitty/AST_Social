using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialApp.Api.Models;
using SocialApp.Api.Services;

namespace SocialApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private readonly ISocialSessionStore _sessions;
    private readonly ISocialProfileService _profiles;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ISocialSessionStore sessions,
        ISocialProfileService profiles,
        ILogger<ProfileController> logger)
    {
        _sessions = sessions;
        _profiles = profiles;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(SocialProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SocialProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("session_id");
        if (string.IsNullOrEmpty(sessionId))
        {
            return Unauthorized(new ApiErrorResponse
            {
                Code = "invalid_token",
                Message = "The access token is missing the session claim."
            });
        }

        var session = _sessions.Get(sessionId);
        if (session is null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Code = "session_expired",
                Message = "Your session has expired. Please sign in again."
            });
        }

        try
        {
            var profile = await _profiles.BuildProfileAsync(session, cancellationToken);
            return Ok(profile);
        }
        catch (SocialApiException ex)
        {
            _logger.LogWarning(ex, "Social network API error for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse
            {
                Code = "upstream_error",
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Profile configuration error for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse
            {
                Code = "misconfigured_provider",
                Message = ex.Message
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error building profile for session {SessionId}", sessionId);
            return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse
            {
                Code = "profile_error",
                Message = "Something went wrong while loading your profile. Please try again later."
            });
        }
    }
}
