using BestStories.Api.Contracts;
using Shouldly;

namespace BestStories.Api.UnitTests;

public class RequestedStoryCountTests
{
    [Theory]
    [InlineData(null)]                       // n omitted; there is no default page size
    [InlineData("")]
    [InlineData("0")]                        // asking for zero rows is a mistake, not a request
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("99999999999999999999")]     // parses as a number, but not as an int
    [InlineData("1.5")]
    [InlineData("1,000")]
    public void Parse_RejectsEveryInvalidCount(string? n)
    {
        RequestedStoryCount.Parse(n).ShouldBeNull();
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("3", 3)]
    [InlineData("500", 500)]
    [InlineData("1000", 1000)]               // more than upstream is likely to offer is still a count
    [InlineData("2147483647", int.MaxValue)]
    public void Parse_AcceptsAnyPositiveInteger(string n, int expected)
    {
        RequestedStoryCount.Parse(n).ShouldBe(expected);
    }

    [Fact]
    public void Parse_IsNotAffectedByTheServerCulture()
    {
        var thousandsSeparatedInGerman = "1.000";

        RequestedStoryCount.Parse(thousandsSeparatedInGerman).ShouldBeNull();
    }
}
