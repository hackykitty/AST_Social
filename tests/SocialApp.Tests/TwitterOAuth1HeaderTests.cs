using SocialApp.Api.Infrastructure;

namespace SocialApp.Tests;

public sealed class TwitterOAuth1HeaderTests
{
    [Fact]
    public void ForGet_IncludesOAuthVersionAndSignature()
    {
        var uri = new Uri("https://api.twitter.com/2/users/me?user.fields=id");
        var header = TwitterOAuth1Header.ForGet(
            uri,
            consumerKey: "ck",
            consumerSecret: "cs",
            accessToken: "at",
            accessTokenSecret: "ats");

        Assert.StartsWith("OAuth ", header, StringComparison.Ordinal);
        Assert.Contains("oauth_consumer_key=\"ck\"", header, StringComparison.Ordinal);
        Assert.Contains("oauth_token=\"at\"", header, StringComparison.Ordinal);
        Assert.Contains("oauth_signature_method=\"HMAC-SHA1\"", header, StringComparison.Ordinal);
        Assert.Contains("oauth_version=\"1.0\"", header, StringComparison.Ordinal);
        Assert.Contains("oauth_signature=\"", header, StringComparison.Ordinal);
    }
}
