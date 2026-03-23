using System.Collections.Concurrent;

namespace SocialApp.Api.Services;

public sealed class MemorySocialSessionStore : ISocialSessionStore
{
    private readonly ConcurrentDictionary<string, (SocialSession Session, DateTimeOffset Expires)> _sessions = new();
    private readonly TimeSpan _ttl;

    public MemorySocialSessionStore(IConfiguration configuration)
    {
        var minutes = configuration.GetValue("Jwt:SessionMinutes", 60);
        _ttl = TimeSpan.FromMinutes(Math.Clamp(minutes, 5, 24 * 60));
    }

    public string Create(SocialSession session)
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = (session, DateTimeOffset.UtcNow.Add(_ttl));
        return id;
    }

    public SocialSession? Get(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry))
            return null;

        if (entry.Expires < DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return entry.Session;
    }
}
