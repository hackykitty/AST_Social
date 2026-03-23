namespace SocialApp.Api.Services;

public interface ISocialSessionStore
{
    string Create(SocialSession session);
    SocialSession? Get(string sessionId);
}
