using TicketHunter.Core.Utils;

namespace TicketHunter.Tests;

public class ChineseNumeralsTests
{
    [Theory]
    [InlineData("一", 1)]
    [InlineData("十", 10)]
    [InlineData("十二", 12)]
    [InlineData("二十", 20)]
    [InlineData("二十五", 25)]
    [InlineData("一百", 100)]
    [InlineData("三百六十五", 365)]
    [InlineData("一千二百", 1200)]
    public void ToInt_ShouldConvert(string chinese, int expected)
    {
        Assert.Equal(expected, ChineseNumerals.ToInt(chinese));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    public void ToInt_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(ChineseNumerals.ToInt(input!));
    }
}
