using TicketHunter.Core.Utils;

namespace TicketHunter.Tests;

public class KeywordMatcherTests
{
    [Theory]
    [InlineData("2024/12/25 台北場", "12/25", true)]
    [InlineData("2024/12/25 台北場", "12/26", false)]
    [InlineData("VIP 搖滾區 A", "VIP;搖滾", true)]
    [InlineData("一般座位區 B", "VIP;搖滾", false)]
    [InlineData("VIP 搖滾區 A", "VIP 搖滾", true)]     // AND: both must match
    [InlineData("VIP 座位區 B", "VIP 搖滾", false)]     // AND: "搖滾" missing
    [InlineData("Any text", "", true)]                   // Empty keyword matches all
    [InlineData("", "keyword", false)]                   // Empty text matches nothing
    public void IsMatch_ShouldWork(string text, string keyword, bool expected)
    {
        Assert.Equal(expected, KeywordMatcher.IsMatch(text, keyword));
    }

    [Fact]
    public void FindFirstMatch_ShouldReturnFirstOrGroupMatch()
    {
        var options = new[] { "A區 $1000", "B區 $800", "VIP區 $2000" };

        // First OR group "VIP" matches "VIP區"
        Assert.Equal("VIP區 $2000", KeywordMatcher.FindFirstMatch(options, "VIP;A區"));

        // First OR group "C區" fails, second "B區" matches
        Assert.Equal("B區 $800", KeywordMatcher.FindFirstMatch(options, "C區;B區"));
    }

    [Fact]
    public void FindFirstMatch_EmptyKeyword_ReturnsFirst()
    {
        var options = new[] { "A", "B", "C" };
        Assert.Equal("A", KeywordMatcher.FindFirstMatch(options, ""));
    }

    [Fact]
    public void FindAllMatches_WithExclude()
    {
        var options = new[] { "A區 $1000", "輪椅區", "B區 $800", "視線不良區" };
        var result = KeywordMatcher.FindAllMatches(options, "", "輪椅;視線不良");

        Assert.Equal(2, result.Count);
        Assert.Contains("A區 $1000", result);
        Assert.Contains("B區 $800", result);
    }

    [Theory]
    [InlineData("from_top", "A")]
    [InlineData("from_bottom", "C")]
    [InlineData("center", "B")]
    public void SelectByMode_ShouldReturnCorrectItem(string mode, string expected)
    {
        var options = new List<string> { "A", "B", "C" };
        Assert.Equal(expected, KeywordMatcher.SelectByMode(options, mode));
    }

    [Fact]
    public void SelectByMode_Random_ShouldReturnItem()
    {
        var options = new List<string> { "A", "B", "C" };
        var result = KeywordMatcher.SelectByMode(options, "random");
        Assert.Contains(result, options);
    }

    [Fact]
    public void SelectByMode_EmptyList_ReturnsNull()
    {
        Assert.Null(KeywordMatcher.SelectByMode(new List<string>(), "from_top"));
    }
}
