using UnityEngine;
using System;

/// <summary>
/// MathExercise generates arithmetic exercises (Add, Subtract, Multiply, Divide)
/// with full support for sign rules, decimal precision, and validation modes.
/// </summary>
public class MathExercise
{
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    // Powers of 10 used for decimal manipulations
    static readonly decimal[] Pow10 =
    {
        1m, 10m, 100m, 1000m, 10000m, 100000m, 1000000m
    };

    /// <summary>
    /// Constructor: generates an exercise based on operation type
    /// </summary>
    public MathExercise(int min, int max, OperationType operation)
    {
        switch (operation)
        {
            case OperationType.Add:
                GenerateAddition(min, max);
                break;

            case OperationType.Subtract:
                GenerateSubtraction(min, max);
                break;

            case OperationType.Multiply:
                GenerateMultiplication(min, max);
                break;

            case OperationType.Divide:
                GenerateSmartDivision(min, max);
                break;
        }
    }

    /// <summary>
    /// Generates a random decimal between min and max
    /// with a random number of decimals (0-5)
    /// </summary>
    decimal GenerateRandomDecimal(int min, int max)
    {
        int decimals = UnityEngine.Random.Range(0, 6);
        decimal multiplier = Pow10[decimals];

        int minInt = (int)(min * multiplier);
        int maxInt = (int)(max * multiplier);

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }

    /// <summary>
    /// Generates a valid result respecting validation mode and truncation rules
    /// </summary>
    decimal GenerateValidResult(int min, int max)
    {
        int maxDecimals = 6;
        int decimals;

        if (GameSettings.Instance != null &&
            GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            // Truncated: result must have at least (validation decimals + 1) decimals
            int minRequired = GameSettings.Instance.decimals + 1;
            decimals = UnityEngine.Random.Range(minRequired, maxDecimals + 1);
        }
        else
        {
            decimals = UnityEngine.Random.Range(1, maxDecimals + 1);
        }

        decimal multiplier = Pow10[decimals];

        int integerPart = UnityEngine.Random.Range(min, max);
        int decimalPart;

        do
        {
            decimalPart = UnityEngine.Random.Range(1, (int)multiplier);
        }
        while (decimalPart % 10 == 0); // ❗ Avoid ending in 0

        decimal result = integerPart + decimalPart / multiplier;

        // 🔥 Ensure result has max 6 decimals
        result = LimitDecimals(result, maxDecimals);

        return result;
    }

    // =========================
    // ADDITION
    // =========================
    void GenerateAddition(int min, int max)
    {
        decimal result = GenerateValidResult(min, max);

        // Limit result decimals to avoid excessive UI digits
        result = LimitDecimals(result, 6);

        // Generate first operand
        decimal a = GenerateRandomDecimal(min, max);

        // Calculate second operand based on the result
        decimal b = result - a;

        ApplySigns(ref a, ref b);

        // Limit operand decimals
        a = LimitDecimals(a, 5);
        b = LimitDecimals(b, 5);

        Number1 = a;
        Number2 = b;
        Answer = result;
    }

    // =========================
    // SUBTRACTION
    // =========================
    void GenerateSubtraction(int min, int max)
    {
        decimal result = GenerateValidResult(min, max);
        result = LimitDecimals(result, 6);

        decimal a, b;

        var signMode = GameSettings.Instance.numberSignMode;

        switch (signMode)
        {
            case NumberSignMode.PositiveOnly:
                {
                    a = GenerateRandomDecimal(min, max);
                    b = a - result;
                    if (b < 0) b = 0;
                }
                break;

            case NumberSignMode.NegativeOnly:
                {
                    // Generate 'a' negative but not degenerate (-1)
                    do
                    {
                        a = -GenerateRandomDecimal(1, 20);
                    }
                    while (Math.Abs(a) < 2);

                    // Solve a - b = result
                    b = a - result;

                    // Ensure b is also negative
                    if (b >= 0)
                    {
                        decimal offset = Math.Abs(b) + 1;
                        a -= offset;
                        b -= offset;
                    }
                }
                break;

            case NumberSignMode.Mixed:
                {
                    a = GenerateRandomDecimal(min, max);
                    b = a - result;
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        a = -a;
                        b = -b;
                    }
                }
                break;

            default:
                a = GenerateRandomDecimal(min, max);
                b = a - result;
                break;
        }

        // Limit operand decimals
        a = LimitDecimals(a, 5);
        b = LimitDecimals(b, 5);

        Number1 = a;
        Number2 = b;
        Answer = result;
    }

    // =========================
    // MULTIPLICATION
    // =========================
    void GenerateMultiplication(int min, int max)
    {
        decimal result = GenerateValidResult(1, 100);

        decimal b = GenerateRandomDecimal(1, 20);
        if (b == 0) b = 1;

        decimal a = result / b;
        a = LimitDecimals(a, 5);

        var signMode = GameSettings.Instance.numberSignMode;

        switch (signMode)
        {
            case NumberSignMode.PositiveOnly:
                a = Math.Abs(a);
                b = Math.Abs(b);
                break;

            case NumberSignMode.NegativeOnly:
                a = -Math.Abs(a);
                b = -Math.Abs(b);
                break;

            case NumberSignMode.Mixed:
                {
                    bool makeNegative = UnityEngine.Random.value > 0.5f;
                    if (makeNegative)
                    {
                        if (UnityEngine.Random.value > 0.5f)
                            a = -Math.Abs(a);
                        else
                            b = -Math.Abs(b);

                        result = -Math.Abs(result);
                    }
                    else
                    {
                        a = Math.Abs(a);
                        b = Math.Abs(b);
                        result = Math.Abs(result);
                    }
                }
                break;
        }

        // Ensure result matches operands
        result = LimitDecimals(a * b, 6);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Multiply);
#endif
    }

    // =========================
    // DIVISION
    // =========================
    void GenerateSmartDivision(int min, int max)
    {
        int resultDecimals = UnityEngine.Random.Range(1, 6);

        if (GameSettings.Instance != null &&
            GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            int minRequired = GameSettings.Instance.decimals + 1;
            resultDecimals = UnityEngine.Random.Range(minRequired, 7);
        }

        decimal multiplier = Pow10[resultDecimals];

        int integerPart = UnityEngine.Random.Range(0, 100);
        int decimalPart;
        do
        {
            decimalPart = UnityEngine.Random.Range(1, (int)multiplier);
        } while (decimalPart % 10 == 0);

        decimal result = integerPart + decimalPart / multiplier;
        result = LimitDecimals(result, 6);

        decimal divisor;

        int divisorType = UnityEngine.Random.Range(0, 3);

        switch (divisorType)
        {
            case 0:
                divisor = UnityEngine.Random.Range(2, 10);
                break;

            case 1:
                divisor = UnityEngine.Random.Range(10, 50);
                break;

            default:
                int divisorDecimals = UnityEngine.Random.Range(1, 3);
                decimal divMultiplier = Pow10[divisorDecimals];
                divisor = UnityEngine.Random.Range(20, 200) / divMultiplier;
                break;
        }

        decimal dividend = result * divisor;
        dividend = LimitDecimals(dividend, 5);

        Number1 = dividend;
        Number2 = divisor;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Divide);
#endif
    }

    /// <summary>
    /// Apply sign rules to operands according to global NumberSignMode
    /// </summary>
    void ApplySigns(ref decimal a, ref decimal b)
    {
        var signMode = GameSettings.Instance.numberSignMode;

        switch (signMode)
        {
            case NumberSignMode.PositiveOnly:
                a = Math.Abs(a);
                b = Math.Abs(b);
                break;

            case NumberSignMode.NegativeOnly:
                a = -Math.Abs(a);
                b = -Math.Abs(b);
                break;

            case NumberSignMode.Mixed:
                if (UnityEngine.Random.value > 0.5f)
                    a = -a;
                if (UnityEngine.Random.value > 0.5f)
                    b = -b;
                break;
        }
    }

    /// <summary>
    /// Limit the number of decimals of a decimal value
    /// </summary>
    decimal LimitDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = Pow10[maxDecimals];
        return Math.Round(value * multiplier) / multiplier;
    }

    /// <summary>
    /// Optional: validate the exercise in editor for debugging
    /// </summary>
    void Validate(OperationType op)
    {
#if UNITY_EDITOR
        decimal computed = 0;
        switch (op)
        {
            case OperationType.Add: computed = Number1 + Number2; break;
            case OperationType.Subtract: computed = Number1 - Number2; break;
            case OperationType.Multiply: computed = Number1 * Number2; break;
            case OperationType.Divide: computed = Number1 / Number2; break;
        }

        if (Math.Round(computed, 5) != Math.Round(Answer, 5))
        {
            Debug.LogError($"Math error: {Number1} op {Number2} != {Answer}");
        }

        // Optional: check sign rules
        if (GameSettings.Instance.numberSignMode == NumberSignMode.PositiveOnly &&
            (Number1 < 0 || Number2 < 0))
        {
            Debug.LogError($"Sign error (PositiveOnly): {Number1}, {Number2}");
        }
#endif
    }
}