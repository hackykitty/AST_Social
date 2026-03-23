using SocialApp.Api.Services;

namespace SocialApp.Tests;

public sealed class LinkedInOpenIdUserInfoParserTests
{
    [Fact]
    public void Parse_UsesName_WhenPresent()
    {
        const string json = """
            {
              "sub": "abc123",
              "name": "Jamie Doe",
              "email": "jamie@example.com",
              "picture": "https://media.licdn.com/p.jpg"
            }
            """;

        var p = LinkedInOpenIdUserInfoParser.Parse(json);

        Assert.Equal("Jamie Doe", p.DisplayName);
        Assert.Equal("jamie@example.com", p.Email);
        Assert.Equal("https://media.licdn.com/p.jpg", p.PictureUrl);
        Assert.Equal("abc123", p.Subject);
    }

    [Fact]
    public void Parse_BuildsName_FromGivenAndFamily_WhenNameMissing()
    {
        const string json = """
            {
              "sub": "x",
              "given_name": "Pat",
              "family_name": "Lee"
            }
            """;

        var p = LinkedInOpenIdUserInfoParser.Parse(json);

        Assert.Equal("Pat Lee", p.DisplayName);
        Assert.Null(p.Email);
        Assert.Null(p.PictureUrl);
        Assert.Equal("x", p.Subject);
    }

    [Fact]
    public void Parse_FallsBackToLinkedInMember_WhenNoNameFields()
    {
        const string json = """{"sub":"only-sub"}""";

        var p = LinkedInOpenIdUserInfoParser.Parse(json);

        Assert.Equal("LinkedIn member", p.DisplayName);
        Assert.Equal("only-sub", p.Subject);
    }

    [Fact]
    public void Parse_PrefersTopLevelName_OverGivenAndFamily()
    {
        const string json = """
            {
              "name": "Display Name",
              "given_name": "Ignore",
              "family_name": "This"
            }
            """;

        var p = LinkedInOpenIdUserInfoParser.Parse(json);

        Assert.Equal("Display Name", p.DisplayName);
    }

    [Fact]
    public void Parse_HandlesOnlyGivenName()
    {
        const string json = """{"given_name":"Madonna"}""";

        var p = LinkedInOpenIdUserInfoParser.Parse(json);

        Assert.Equal("Madonna", p.DisplayName);
    }
}
