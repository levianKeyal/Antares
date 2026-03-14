using System;
using System.Globalization;
using UnityEngine;

public class MathExercise
{
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    OperationType operation;

    const int MAX_ATTEMPTS = 40;

    public MathExercise(int min, int max, OperationType operation)
    {
        this.operation = operation;

        if (operation == OperationType.Divide)
            GenerateSmartDivision(min, max);
        else
            GenerateSmartExercise(min, max);
    }

    void GenerateSmartExercise(int min, int max)
    {
        int attempts = 0;

        while (attempts < MAX_ATTEMPTS)
        {
            decimal n1 = GenerateRandomDecimal(min, max);
            decimal n2 = GenerateRandomDecimal(min, max);

            decimal result = Calculate(n1, n2);

            if (IsValidResult(result))
            {
                Number1 = n1;
                Number2 = n2;
                Answer = result;

                Debug.Log($"Generated: {Number1} {GetSymbol()} {Number2} = {Answer}");
                return;
            }

            attempts++;
        }

        GenerateFallback(min, max);
    }

    void GenerateSmartDivision(int min, int max)
    {
        int resultDecimals = 3;

        if (GameSettings.Instance != null &&
            GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            resultDecimals = GameSettings.Instance.decimals + 1;
        }

        decimal resultMultiplier = (decimal)Math.Pow(10, resultDecimals);

        // Generate target result
        decimal result = UnityEngine.Random.Range(100, 9000) / resultMultiplier;

        // Decide divisor type
        int divisorType = UnityEngine.Random.Range(0, 3);

        decimal divisor;

        switch (divisorType)
        {
            // small integer
            case 0:
                divisor = UnityEngine.Random.Range(2, 10);
                break;

            // large integer
            case 1:
                divisor = UnityEngine.Random.Range(10, 50);
                break;

            // decimal divisor
            default:
                int decimals = UnityEngine.Random.Range(1, 3);
                decimal multiplier = (decimal)Math.Pow(10, decimals);

                divisor = UnityEngine.Random.Range(20, 200) / multiplier;
                break;
        }

        decimal dividend = result * divisor;

        // Limit dividend decimals (keep numbers clean)
        dividend = LimitDecimals(dividend, 4);

        Number1 = dividend;
        Number2 = divisor;
        Answer = result;

        Debug.Log($"Generated Division: {Number1} ÷ {Number2} = {Answer}");
    }
    decimal LimitDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = (decimal)Math.Pow(10, maxDecimals);

        return Math.Round(value * multiplier) / multiplier;
    }
    void GenerateFallback(int min, int max)
    {
        decimal n1 = UnityEngine.Random.Range(min, max);
        decimal n2 = UnityEngine.Random.Range(min, max);

        if (operation == OperationType.Divide && n2 == 0)
            n2 = 1;

        Number1 = n1;
        Number2 = n2;
        Answer = Calculate(n1, n2);

        Debug.Log("Fallback exercise generated");
    }

    decimal Calculate(decimal n1, decimal n2)
    {
        switch (operation)
        {
            case OperationType.Add:
                return n1 + n2;

            case OperationType.Subtract:
                return n1 - n2;

            case OperationType.Multiply:
                return n1 * n2;

            case OperationType.Divide:
                return n1 / n2;
        }

        return 0;
    }

    bool IsValidResult(decimal result)
    {
        if (GameSettings.Instance == null)
            return true;

        int decimals = GetDecimalCount(result);

        if (decimals > 6)
            return false;

        if (Math.Abs(result) > 10000)
            return false;

        if (GameSettings.Instance.validationMode == ValidationMode.Truncated)
        {
            int requiredDecimals = GameSettings.Instance.decimals + 1;

            if (decimals < requiredDecimals)
                return false;
        }

        return true;
    }

    decimal GenerateRandomDecimal(int minValue, int maxValue)
    {
        int decimals = UnityEngine.Random.Range(0, 6);

        decimal multiplier = (decimal)Math.Pow(10, decimals);

        int minInt = minValue * (int)multiplier;
        int maxInt = maxValue * (int)multiplier;

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }

    int GetDecimalCount(decimal number)
    {
        string text = number.ToString(CultureInfo.InvariantCulture);

        if (!text.Contains("."))
            return 0;

        return text.Split('.')[1].Length;
    }

    string GetSymbol()
    {
        switch (operation)
        {
            case OperationType.Add: return "+";
            case OperationType.Subtract: return "-";
            case OperationType.Multiply: return "×";
            case OperationType.Divide: return "÷";
        }

        return "?";
    }
}