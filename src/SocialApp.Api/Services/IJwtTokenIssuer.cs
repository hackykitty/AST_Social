using System.Security.Claims;

namespace SocialApp.Api.Services;

public interface IJwtTokenIssuer
{
    string CreateToken(string sessionId, IEnumerable<Claim> additionalClaims);
}
