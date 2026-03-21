using UnityEngine;
using System;

public class MathExercise
{
    // Public read-only properties for operands and result
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    // Precomputed powers of 10 for decimal handling
    static readonly decimal[] Pow10 =
    {
        1m, 10m, 100m, 1000m, 10000m, 100000m, 1000000m
    };

    // Constructor selects operation type
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

    // Generates a random decimal number with up to 5 decimal places
    decimal GenerateRandomDecimal(int min, int max)
    {
        int decimals = UnityEngine.Random.Range(0, 6);
        decimal multiplier = Pow10[decimals];

        int minInt = (int)(min * multiplier);
        int maxInt = (int)(max * multiplier);

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }

    // Generates a valid result:
    // - Enough decimals for truncation mode
    // - Does NOT end in zero
    decimal GenerateValidResult(int min, int max)
    {
        int decimals = UnityEngine.Random.Range(1, 6);

        if (GameSettings.Instance != null &&
            GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            int minRequired = GameSettings.Instance.decimals + 1;
            decimals = UnityEngine.Random.Range(minRequired, 7);
        }

        decimal multiplier = Pow10[decimals];

        int integerPart = UnityEngine.Random.Range(min, max);

        int decimalPart;
        do
        {
            decimalPart = UnityEngine.Random.Range(1, (int)multiplier);
        }
        while (decimalPart % 10 == 0); // Avoid trailing zero

        return integerPart + decimalPart / multiplier;
    }

    // Applies sign rules without breaking math correctness
    void ApplySigns(ref decimal a, ref decimal b, decimal result, OperationType op)
    {
        if (GameSettings.Instance == null)
            return;

        switch (GameSettings.Instance.numberSignMode)
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

                bool makeNegativeResult = UnityEngine.Random.value > 0.5f;

                if (op == OperationType.Add || op == OperationType.Subtract)
                {
                    // Recalculate safely
                    a = UnityEngine.Random.value > 0.5f ? -Math.Abs(a) : Math.Abs(a);
                    b = (op == OperationType.Add) ? result - a : a - result;
                }
                else
                {
                    if (makeNegativeResult)
                    {
                        if (UnityEngine.Random.value > 0.5f)
                            a = -a;
                        else
                            b = -b;
                    }
                    else
                    {
                        bool bothNegative = UnityEngine.Random.value > 0.5f;

                        if (bothNegative)
                        {
                            a = -a;
                            b = -b;
                        }
                    }
                }
                break;
        }
    }

    // ADDITION
    void GenerateAddition(int min, int max)
    {
        decimal result = GenerateValidResult(min, max);

        decimal a = GenerateRandomDecimal(min, max);
        decimal b = result - a;

        ApplySigns(ref a, ref b, result, OperationType.Add);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Add);
#endif
    }

    // SUBTRACTION
    void GenerateSubtraction(int min, int max)
    {
        decimal result = GenerateValidResult(min, max);

        decimal a = GenerateRandomDecimal(min, max);
        decimal b = a - result;

        ApplySigns(ref a, ref b, result, OperationType.Subtract);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Subtract);
#endif
    }

    // MULTIPLICATION
    void GenerateMultiplication(int min, int max)
    {
        decimal result = GenerateValidResult(1, 100);

        decimal b = GenerateRandomDecimal(1, 20);

        if (b == 0)
            b = 1;

        decimal a = result / b;
        a = LimitDecimals(a, 5);

        ApplySigns(ref a, ref b, result, OperationType.Multiply);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Multiply);
#endif
    }

    // DIVISION
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
        }
        while (decimalPart % 10 == 0);

        decimal result = integerPart + decimalPart / multiplier;

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

        decimal a = dividend;
        decimal b = divisor;

        ApplySigns(ref a, ref b, result, OperationType.Divide);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Divide);
#endif

        Debug.Log($"Generated Division: {Number1} ÷ {Number2} = {Answer}");
    }

    // Limits decimal places for UI readability
    decimal LimitDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = Pow10[maxDecimals];
        return Math.Round(value * multiplier) / multiplier;
    }

#if UNITY_EDITOR

    // VALIDATION SYSTEM (DEBUG ONLY)
    void Validate(OperationType op)
    {
        decimal a = Number1;
        decimal b = Number2;
        decimal result = Answer;

        // 1. Check math correctness
        bool valid = true;

        switch (op)
        {
            case OperationType.Add:
                valid = (a + b == result);
                break;
            case OperationType.Subtract:
                valid = (a - b == result);
                break;
            case OperationType.Multiply:
                valid = (a * b == result);
                break;
            case OperationType.Divide:
                if (b != 0)
                    valid = (a / b == result);
                break;
        }

        if (!valid)
            Debug.LogError($"❌ Math error: {a} op {b} != {result}");

        // 2. Check truncation rule
        if (GameSettings.Instance != null &&
            GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            int decimals = CountDecimals(result);

            if (decimals <= GameSettings.Instance.decimals)
                Debug.LogError($"❌ Truncation error: {result}");
        }

        // 3. Check trailing zero
        if (HasTrailingZero(result))
            Debug.LogError($"❌ Trailing zero: {result}");

        // 4. Check sign rules
        if (GameSettings.Instance != null)
        {
            switch (GameSettings.Instance.numberSignMode)
            {
                case NumberSignMode.PositiveOnly:
                    if (a < 0 || b < 0)
                        Debug.LogError($"❌ Sign error (PositiveOnly): {a}, {b}");
                    break;

                case NumberSignMode.NegativeOnly:
                    if (a > 0 || b > 0)
                        Debug.LogError($"❌ Sign error (NegativeOnly): {a}, {b}");
                    break;
            }
        }
    }

    // Counts decimal places
    int CountDecimals(decimal value)
    {
        value = Math.Abs(value);
        int count = 0;

        while (value != Math.Floor(value) && count < 10)
        {
            value *= 10;
            count++;
        }

        return count;
    }

    // Detects trailing zero
    bool HasTrailingZero(decimal value)
    {
        value = Math.Abs(value);
        decimal scaled = value * 10;
        return scaled % 10 == 0;
    }

#endif
}