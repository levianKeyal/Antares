using UnityEngine;
using System;

public class MathExercise
{
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    static readonly decimal[] Pow10 =
    {
        1m,
        10m,
        100m,
        1000m,
        10000m,
        100000m,
        1000000m
    };

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

    decimal GenerateRandomDecimal(int min, int max)
    {
        int decimals = UnityEngine.Random.Range(0, 6);
        decimal multiplier = Pow10[decimals];

        int minInt = (int)(min * multiplier);
        int maxInt = (int)(max * multiplier);

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }

    void GenerateAddition(int min, int max)
    {
        Number1 = GenerateRandomDecimal(min, max);
        Number2 = GenerateRandomDecimal(min, max);
        Answer = Number1 + Number2;
    }

    void GenerateSubtraction(int min, int max)
    {
        Number1 = GenerateRandomDecimal(min, max);
        Number2 = GenerateRandomDecimal(min, max);
        Answer = Number1 - Number2;
    }

    void GenerateMultiplication(int min, int max)
    {
        Number1 = GenerateRandomDecimal(1, 20);
        Number2 = GenerateRandomDecimal(1, 20);
        Answer = Number1 * Number2;
    }

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

        Number1 = dividend;
        Number2 = divisor;
        Answer = result;

        Debug.Log($"Generated Division: {Number1} ÷ {Number2} = {Answer}");
    }

    decimal LimitDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = Pow10[maxDecimals];
        return Math.Round(value * multiplier) / multiplier;
    }
}