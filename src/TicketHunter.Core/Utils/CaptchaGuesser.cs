using System.Text.RegularExpressions;

namespace TicketHunter.Core.Utils;

/// <summary>
/// Auto-guess answers for Tixcraft text-based CAPTCHA questions.
/// Ported from tickets_hunter's util.guess_tixcraft_question() logic.
/// </summary>
public static class CaptchaGuesser
{
    /// <summary>
    /// Try to guess the answer for a Tixcraft text question.
    /// Returns null if unable to guess.
    /// </summary>
    public static string? Guess(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return null;

        // Pattern 1: "輸入YES" + "已詳閱/請詳閱" + "同意"
        if (question.Contains("YES", StringComparison.OrdinalIgnoreCase) &&
            (question.Contains("已詳閱") || question.Contains("請詳閱")) &&
            question.Contains("同意"))
        {
            return "YES";
        }

        // Pattern 2: "輸入【同意】" + "已詳閱/請詳閱"
        if ((question.Contains("已詳閱") || question.Contains("請詳閱")) &&
            question.Contains("同意"))
        {
            return "同意";
        }

        // Pattern 3: Extract text between brackets 【】「」
        var bracketAnswer = ExtractBracketText(question);
        if (bracketAnswer != null) return bracketAnswer;

        // Pattern 4: Extract text between quotes ""''
        var quoteAnswer = ExtractQuotedText(question);
        if (quoteAnswer != null) return quoteAnswer;

        // Pattern 5: (ans:XXXX) format
        var ansMatch = Regex.Match(question, @"\(ans[:\s]*([^)]+)\)", RegexOptions.IgnoreCase);
        if (ansMatch.Success) return ansMatch.Groups[1].Value.Trim();

        // Pattern 6: Simple math questions (e.g., "2+3=?", "請問 5 加 3 等於多少")
        var mathAnswer = SolveMath(question);
        if (mathAnswer != null) return mathAnswer;

        // Pattern 7: Chinese numeral conversion in brackets
        var chineseNumAnswer = ExtractChineseNumeral(question);
        if (chineseNumAnswer != null) return chineseNumAnswer;

        return null;
    }

    private static string? ExtractBracketText(string question)
    {
        // Normalize all bracket types to 【】
        var normalized = question
            .Replace('「', '【').Replace('」', '】')
            .Replace('『', '【').Replace('』', '】')
            .Replace('〔', '【').Replace('〕', '】')
            .Replace('﹝', '【').Replace('﹞', '】')
            .Replace('〈', '【').Replace('〉', '】')
            .Replace('《', '【').Replace('》', '】')
            .Replace('〖', '【').Replace('〗', '】')
            .Replace('［', '【').Replace('］', '】');

        // Must contain instruction keywords
        bool hasInstruction = normalized.Contains("輸入") || normalized.Contains("填入") ||
                              normalized.Contains("請問") || normalized.Contains("答案");
        if (!hasInstruction) return null;

        var matches = Regex.Matches(normalized, @"【([^】]+)】");
        if (matches.Count == 0) return null;

        // If question says "引號內文字" or "括號內", return the bracketed content
        if (normalized.Contains("引號內") || normalized.Contains("括號內") ||
            normalized.Contains("框框內") || normalized.Contains("內的文字") ||
            normalized.Contains("內文字"))
        {
            var answer = matches[0].Groups[1].Value.Trim();

            // If it's Chinese numerals and question asks for 數字/阿拉伯
            if (normalized.Contains("數字") || normalized.Contains("阿拉伯"))
            {
                var num = ConvertChineseDigits(answer);
                if (num != null) return num;
            }

            return answer;
        }

        // Return last bracket content as answer (common pattern)
        var lastAnswer = matches[^1].Groups[1].Value.Trim();

        // Check if conversion to Arabic numerals is needed
        if (normalized.Contains("數字") || normalized.Contains("阿拉伯"))
        {
            var num = ConvertChineseDigits(lastAnswer);
            if (num != null) return num;
        }

        return lastAnswer;
    }

    private static string? ExtractQuotedText(string question)
    {
        if (!question.Contains("輸入") && !question.Contains("填入")) return null;

        // Try double quotes (ASCII " and fullwidth \u201C \u201D)
        var match = Regex.Match(question, "[\"\u201C\u201D]([^\"\u201C\u201D]+)[\"\u201C\u201D]");
        if (match.Success) return match.Groups[1].Value.Trim();

        // Try single quotes (ASCII ' and fullwidth \u2018 \u2019)
        match = Regex.Match(question, "[\'\u2018\u2019]([^\'\u2018\u2019]+)[\'\u2018\u2019]");
        if (match.Success) return match.Groups[1].Value.Trim();

        return null;
    }

    private static string? SolveMath(string question)
    {
        // Pattern: "X + Y = ?" or "X加Y等於"
        var match = Regex.Match(question, @"(\d+)\s*[\+\＋加]\s*(\d+)");
        if (match.Success)
        {
            var a = int.Parse(match.Groups[1].Value);
            var b = int.Parse(match.Groups[2].Value);
            return (a + b).ToString();
        }

        match = Regex.Match(question, @"(\d+)\s*[\-\－減]\s*(\d+)");
        if (match.Success)
        {
            var a = int.Parse(match.Groups[1].Value);
            var b = int.Parse(match.Groups[2].Value);
            return (a - b).ToString();
        }

        match = Regex.Match(question, @"(\d+)\s*[\*\×\＊乘]\s*(\d+)");
        if (match.Success)
        {
            var a = int.Parse(match.Groups[1].Value);
            var b = int.Parse(match.Groups[2].Value);
            return (a * b).ToString();
        }

        match = Regex.Match(question, @"(\d+)\s*[\/\÷\／除]\s*(\d+)");
        if (match.Success)
        {
            var a = int.Parse(match.Groups[1].Value);
            var b = int.Parse(match.Groups[2].Value);
            if (b != 0) return (a / b).ToString();
        }

        return null;
    }

    private static string? ExtractChineseNumeral(string question)
    {
        if (!question.Contains("數字") && !question.Contains("阿拉伯")) return null;

        var match = Regex.Match(question, @"【([一二三四五六七八九十百千萬零〇兩貳參肆伍陸柒捌玖拾佰仟]+)】");
        if (!match.Success) return null;

        return ConvertChineseDigits(match.Groups[1].Value);
    }

    /// <summary>
    /// Convert Chinese digit characters to Arabic number string.
    /// First tries one-by-one mapping (e.g., "三五二" → "352").
    /// Falls back to ChineseNumerals.ToInt() for positional numerals (e.g., "三十五" → "35").
    /// </summary>
    private static string? ConvertChineseDigits(string chinese)
    {
        var digitMap = new Dictionary<char, char>
        {
            ['零'] = '0', ['〇'] = '0',
            ['一'] = '1', ['壹'] = '1',
            ['二'] = '2', ['兩'] = '2', ['貳'] = '2',
            ['三'] = '3', ['參'] = '3',
            ['四'] = '4', ['肆'] = '4',
            ['五'] = '5', ['伍'] = '5',
            ['六'] = '6', ['陸'] = '6',
            ['七'] = '7', ['柒'] = '7',
            ['八'] = '8', ['捌'] = '8',
            ['九'] = '9', ['玖'] = '9',
        };

        // Try one-by-one digit mapping first (e.g. "三五二" → "352")
        var result = new char[chinese.Length];
        bool allSimpleDigits = true;
        for (int i = 0; i < chinese.Length; i++)
        {
            if (digitMap.TryGetValue(chinese[i], out var digit))
                result[i] = digit;
            else
            {
                allSimpleDigits = false;
                break;
            }
        }

        if (allSimpleDigits) return new string(result);

        // Fallback: positional Chinese numerals (e.g. "三十五" → 35)
        var num = ChineseNumerals.ToInt(chinese);
        return num?.ToString();
    }
}
