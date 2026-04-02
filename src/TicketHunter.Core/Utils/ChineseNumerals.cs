namespace TicketHunter.Core.Utils;

public static class ChineseNumerals
{
    private static readonly Dictionary<char, int> NumeralMap = new()
    {
        ['零'] = 0, ['一'] = 1, ['二'] = 2, ['三'] = 3, ['四'] = 4,
        ['五'] = 5, ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9,
        ['十'] = 10, ['百'] = 100, ['千'] = 1000, ['萬'] = 10000,
        ['〇'] = 0, ['兩'] = 2, ['貳'] = 2, ['參'] = 3, ['肆'] = 4,
        ['伍'] = 5, ['陸'] = 6, ['柒'] = 7, ['捌'] = 8, ['玖'] = 9,
        ['拾'] = 10, ['佰'] = 100, ['仟'] = 1000
    };

    public static int? ToInt(string chinese)
    {
        if (string.IsNullOrWhiteSpace(chinese)) return null;

        int result = 0;
        int current = 0;

        foreach (var ch in chinese)
        {
            if (!NumeralMap.TryGetValue(ch, out var val))
                return null;

            if (val >= 10)
            {
                if (current == 0) current = 1;
                result += current * val;
                current = 0;
            }
            else
            {
                current = val;
            }
        }

        return result + current;
    }
}
