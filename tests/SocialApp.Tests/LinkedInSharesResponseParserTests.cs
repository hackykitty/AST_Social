using SocialApp.Api.Services;

namespace SocialApp.Tests;

public sealed class LinkedInSharesResponseParserTests
{
    [Fact]
    public void ParseElements_ReturnsEmpty_WhenNoElementsArray()
    {
        const string json = """{"paging":{"start":0}}""";

        var items = LinkedInSharesResponseParser.ParseElements(json, maxCount: 3);

        Assert.Empty(items);
    }

    [Fact]
    public void ParseElements_ReturnsEmpty_WhenElementsEmpty()
    {
        const string json = """{"elements":[]}""";

        var items = LinkedInSharesResponseParser.ParseElements(json, maxCount: 3);

        Assert.Empty(items);
    }

    [Fact]
    public void ParseElements_SkipsSharesWithNoTextAndNoImage()
    {
        const string json = """
            {
              "elements":[
                {"id":"1"},
                {
                  "text":{"text":"Hello network"},
                  "lastModified":{"time":1700000000000}
                }
              ]
            }
            """;

        var items = LinkedInSharesResponseParser.ParseElements(json, maxCount: 3);

        Assert.Single(items);
        Assert.Equal("Hello network", items[0].Text);
        Assert.Null(items[0].ImageUrl);
        Assert.NotNull(items[0].CreatedAt);
        Assert.Equal(1700000000000L, items[0].CreatedAt!.Value.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void ParseElements_RespectsMaxCount()
    {
        const string json = """
            {
              "elements":[
                {"text":{"text":"One"}},
                {"text":{"text":"Two"}},
                {"text":{"text":"Three"}},
                {"text":{"text":"Four"}}
              ]
            }
            """;

        var items = LinkedInSharesResponseParser.ParseElements(json, maxCount: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal("One", items[0].Text);
        Assert.Equal("Two", items[1].Text);
    }

    [Fact]
    public void ParseElements_PicksFirstEntityLocation_AsImage()
    {
        const string json = """
            {
              "elements":[{
                "text":{"text":"With media"},
                "content":{
                  "contentEntities":[
                    {"entityLocation":"https://example.com/a.png"},
                    {"entityLocation":"https://example.com/b.png"}
                  ]
                }
              }]
            }
            """;

        var items = LinkedInSharesResponseParser.ParseElements(json, maxCount: 3);

        Assert.Single(items);
        Assert.Equal("https://example.com/a.png", items[0].ImageUrl);
    }
}
