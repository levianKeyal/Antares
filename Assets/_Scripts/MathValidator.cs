using System;

public static class MathValidator
{
    public static bool Validate(
        decimal correctAnswer,
        decimal userValue,
        int decimals,
        ValidationMode mode)
    {
        decimal multiplier = (decimal)Math.Pow(10, decimals);

        decimal truncated = Math.Truncate(correctAnswer * multiplier) / multiplier;

        decimal ceil;

        if (correctAnswer >= 0)
            ceil = Math.Ceiling(correctAnswer * multiplier) / multiplier;
        else
            ceil = Math.Floor(correctAnswer * multiplier) / multiplier;

        bool exactMatch = userValue == correctAnswer;
        bool truncMatch = userValue == truncated;
        bool ceilMatch = userValue == ceil;

        switch (mode)
        {
            case ValidationMode.ExactOnly:
                return exactMatch;

            case ValidationMode.Truncated:
                return truncMatch;

            case ValidationMode.Ceil:
                return ceilMatch;

            case ValidationMode.All:
                return exactMatch || truncMatch || ceilMatch;
        }

        return false;
    }
}