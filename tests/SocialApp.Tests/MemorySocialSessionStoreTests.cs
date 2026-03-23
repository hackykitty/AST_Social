using Microsoft.Extensions.Configuration;
using SocialApp.Api.Models;
using SocialApp.Api.Services;

namespace SocialApp.Tests;

public sealed class MemorySocialSessionStoreTests
{
    [Fact]
    public void Get_ReturnsNull_WhenSessionUnknown()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SessionMinutes"] = "60"
        }).Build();
        var store = new MemorySocialSessionStore(config);

        Assert.Null(store.Get("missing"));
    }

    [Fact]
    public void Create_ThenGet_RoundTripsSession()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SessionMinutes"] = "60"
        }).Build();
        var store = new MemorySocialSessionStore(config);
        var session = new SocialSession(SocialProvider.Facebook, "token", null, null);

        var id = store.Create(session);
        var loaded = store.Get(id);

        Assert.NotNull(loaded);
        Assert.Equal(SocialProvider.Facebook, loaded.Provider);
        Assert.Equal("token", loaded.AccessToken);
    }
}
