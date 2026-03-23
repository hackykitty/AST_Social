using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialApp.Api.Options;

namespace SocialApp.Api.Services;

public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(string sessionId, IEnumerable<Claim> additionalClaims)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("session_id", sessionId)
        };
        claims.AddRange(additionalClaims);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(Math.Clamp(_options.SessionMinutes, 5, 24 * 60)),
            signingCredentials: _credentials);

        return _handler.WriteToken(token);
    }
}
