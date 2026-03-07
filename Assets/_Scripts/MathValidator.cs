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