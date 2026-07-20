using StorePilot.Application.Features.Support;
using StorePilot.Domain;

namespace StorePilot.UnitTests;

public class ClassificationParserTests
{
    private const string Clean = """
        {"brand":"VoltEdge","category":"Technical","urgency":"High","sentiment":"Negative","confidence":0.87,"summary":"Drill battery not charging."}
        """;

    [Fact]
    public void ParsesCleanJson()
    {
        Assert.True(ClassificationParser.TryParse(Clean, out var c));
        Assert.Equal("VoltEdge", c.Brand);
        Assert.Equal(TicketCategory.Technical, c.Category);
        Assert.Equal(TicketUrgency.High, c.Urgency);
        Assert.Equal(TicketSentiment.Negative, c.Sentiment);
        Assert.Equal(0.87, c.Confidence, 3);
    }

    [Fact]
    public void StripsMarkdownFencesAndProse()
    {
        string wrapped = $"Here is the classification:\n```json\n{Clean}\n```\nHope that helps!";
        Assert.True(ClassificationParser.TryParse(wrapped, out var c));
        Assert.Equal(TicketCategory.Technical, c.Category);
    }

    [Fact]
    public void UnknownEnumValuesFallBackToDefaults()
    {
        string odd = """{"brand":"X","category":"Billing?","urgency":"URGENT!!","sentiment":"angry","confidence":0.5,"summary":"s"}""";
        Assert.True(ClassificationParser.TryParse(odd, out var c));
        Assert.Equal(TicketCategory.Other, c.Category);
        Assert.Equal(TicketUrgency.Medium, c.Urgency);
        Assert.Equal(TicketSentiment.Neutral, c.Sentiment);
    }

    [Fact]
    public void ConfidenceIsClamped()
    {
        string over = """{"brand":"X","category":"Refund","urgency":"Low","sentiment":"Neutral","confidence":7.5,"summary":"s"}""";
        Assert.True(ClassificationParser.TryParse(over, out var c));
        Assert.Equal(1.0, c.Confidence);
    }

    [Fact]
    public void GarbageReturnsFalse()
    {
        Assert.False(ClassificationParser.TryParse("the model rambled with no json at all", out _));
        Assert.False(ClassificationParser.TryParse("", out _));
        Assert.False(ClassificationParser.TryParse("{broken json", out _));
    }
}
