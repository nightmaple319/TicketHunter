using TicketHunter.Core.Utils;

namespace TicketHunter.Tests;

public class CaptchaGuesserTests
{
    [Theory]
    [InlineData("請詳閱注意事項，如已詳閱，請輸入「YES」表示同意", "YES")]
    [InlineData("已詳閱注意事項並同意，請輸入YES", "YES")]
    [InlineData("請詳閱以上注意事項，如果您同意，請輸入【同意】", "同意")]
    [InlineData("已詳閱注意事項，如同意請輸入引號內文字【同意】", "同意")]
    public void Guess_AgreementPatterns(string question, string expected)
    {
        Assert.Equal(expected, CaptchaGuesser.Guess(question));
    }

    [Theory]
    [InlineData("請輸入引號內文字【拓元】", "拓元")]
    [InlineData("請輸入括號內文字「ITZY」", "ITZY")]
    [InlineData("請在下方空白處輸入引號內文字【演唱會】", "演唱會")]
    public void Guess_BracketTextExtraction(string question, string expected)
    {
        Assert.Equal(expected, CaptchaGuesser.Guess(question));
    }

    [Theory]
    [InlineData("請輸入括號內的數字【三五二】", "352")]
    [InlineData("請輸入引號內文字轉為阿拉伯數字【一九八七】", "1987")]
    [InlineData("請輸入括號內的數字【三十五】", "35")]
    [InlineData("請輸入阿拉伯數字【二百一十三】", "213")]
    [InlineData("請輸入括號內的數字【一千零二十四】", "1024")]
    public void Guess_ChineseNumeralConversion(string question, string expected)
    {
        Assert.Equal(expected, CaptchaGuesser.Guess(question));
    }

    [Theory]
    [InlineData("請問 2+3 等於多少？", "5")]
    [InlineData("驗證：5＋8=?", "13")]
    [InlineData("10-3=?", "7")]
    [InlineData("4×5=?", "20")]
    [InlineData("20÷4=?", "5")]
    [InlineData("請問 15／3 等於多少？", "5")]
    public void Guess_MathQuestions(string question, string expected)
    {
        Assert.Equal(expected, CaptchaGuesser.Guess(question));
    }

    [Theory]
    [InlineData("(ans:HELLO)", "HELLO")]
    [InlineData("答案是 (ans: 12345)", "12345")]
    public void Guess_AnsPattern(string question, string expected)
    {
        Assert.Equal(expected, CaptchaGuesser.Guess(question));
    }

    [Fact]
    public void Guess_UnknownQuestion_ReturnsNull()
    {
        Assert.Null(CaptchaGuesser.Guess("這是一個無法猜測的問題"));
        Assert.Null(CaptchaGuesser.Guess(""));
        Assert.Null(CaptchaGuesser.Guess(null!));
    }
}
