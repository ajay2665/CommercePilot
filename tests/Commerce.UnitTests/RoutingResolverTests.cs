using Commerce.Application.Features.Support;
using Commerce.Domain;
using Commerce.Domain.Entities;

namespace Commerce.UnitTests;

public class RoutingResolverTests
{
    private static readonly List<RoutingRule> Rules =
    [
        new() { Brand = "*", Category = TicketCategory.Technical, TargetTeam = "Tech Support" },
        new() { Brand = "*", Category = TicketCategory.Other, TargetTeam = "General Support" },
        new() { Brand = "VoltEdge", Category = TicketCategory.Technical, TargetTeam = "VoltEdge Tech Desk" },
    ];

    [Fact]
    public void BrandSpecificRuleBeatsWildcard()
    {
        var rule = RoutingResolver.Resolve(Rules, "VoltEdge", TicketCategory.Technical);
        Assert.Equal("VoltEdge Tech Desk", rule?.TargetTeam);
    }

    [Fact]
    public void FallsBackToWildcardForOtherBrands()
    {
        var rule = RoutingResolver.Resolve(Rules, "Luma Beauty", TicketCategory.Technical);
        Assert.Equal("Tech Support", rule?.TargetTeam);
    }

    [Fact]
    public void UnmappedCategoryFallsBackToOtherRule()
    {
        var rule = RoutingResolver.Resolve(Rules, "Luma Beauty", TicketCategory.Refund);
        Assert.Equal("General Support", rule?.TargetTeam);
    }

    [Fact]
    public void BrandMatchIsCaseInsensitive()
    {
        var rule = RoutingResolver.Resolve(Rules, "voltedge", TicketCategory.Technical);
        Assert.Equal("VoltEdge Tech Desk", rule?.TargetTeam);
    }

    [Fact]
    public void NoRulesMeansNull()
    {
        Assert.Null(RoutingResolver.Resolve([], "Any", TicketCategory.Refund));
    }
}
