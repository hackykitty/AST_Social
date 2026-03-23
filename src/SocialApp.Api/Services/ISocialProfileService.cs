using SocialApp.Api.Models;

namespace SocialApp.Api.Services;

public interface ISocialProfileService
{
    Task<SocialProfileResponse> BuildProfileAsync(SocialSession session, CancellationToken cancellationToken = default);
}
