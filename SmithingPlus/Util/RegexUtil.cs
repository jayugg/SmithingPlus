namespace SmithingPlus.Util;

public static class RegexUtil
{
    public static string CombineWithOr(string pattern1, string pattern2)
    {
        // Remove '@' from the start of the patterns if present
        pattern1 = pattern1.StartsWith("@") ? pattern1[1..] : pattern1;
        pattern2 = pattern2.StartsWith("@") ? pattern2[1..] : pattern2;
        return $"@({pattern1}|{pattern2})";
    }
}