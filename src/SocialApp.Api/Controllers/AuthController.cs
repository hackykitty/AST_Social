using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Mvc;
using SocialApp.Api.Models;

namespace SocialApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("facebook")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Facebook()
    {
        if (!IsConfigured("Authentication:Facebook:AppId", "Authentication:Facebook:AppSecret"))
            return ProviderNotConfigured("Facebook");

        return Challenge(
            new AuthenticationProperties { Items = { ["provider"] = "Facebook" } },
            FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("twitter")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Twitter()
    {
        if (!IsConfigured("Authentication:Twitter:ConsumerKey", "Authentication:Twitter:ConsumerSecret"))
            return ProviderNotConfigured("Twitter / X");

        return Challenge(new AuthenticationProperties { Items = { ["provider"] = "Twitter" } }, TwitterDefaults.AuthenticationScheme);
    }

    [HttpGet("linkedin")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public IActionResult LinkedIn()
    {
        if (!IsConfigured("Authentication:LinkedIn:ClientId", "Authentication:LinkedIn:ClientSecret"))
            return ProviderNotConfigured("LinkedIn");

        return Challenge(
            new AuthenticationProperties { Items = { ["provider"] = "LinkedIn" } },
            LinkedInAuthenticationDefaults.AuthenticationScheme);
    }

    private bool IsConfigured(params string[] keys) =>
        keys.All(k => !string.IsNullOrWhiteSpace(_configuration[k]));

    private ObjectResult ProviderNotConfigured(string name)
    {
        _logger.LogWarning("{Provider} OAuth is not configured (missing app id/secret in configuration).", name);
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse
        {
            Code = "provider_not_configured",
            Message = $"{name} sign-in is not configured on this server. Add credentials to appsettings or user secrets."
        });
    }
}
