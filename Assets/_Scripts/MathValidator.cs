using UnityEngine;
using System;

public class MathValidator : MonoBehaviour
{
    public static bool Validate(
        decimal correctAnswer,
        decimal userValue,
        int decimals,
        ValidationMode mode)
    {
        decimal multiplier = (decimal)Math.Pow(10, decimals);

        decimal truncated = Math.Floor(correctAnswer * multiplier) / multiplier;
        decimal ceil = Math.Ceiling(correctAnswer * multiplier) / multiplier;

        bool exact = userValue == correctAnswer;
        bool truncMatch = userValue == truncated;
        bool ceilMatch = userValue == ceil;

        switch (mode)
        {
            case ValidationMode.ExactOnly:
                return exact;

            case ValidationMode.TruncatedAndCeil:
                return truncMatch || ceilMatch;

            case ValidationMode.All:
                return exact || truncMatch || ceilMatch;
        }

        return false;
    }
}
