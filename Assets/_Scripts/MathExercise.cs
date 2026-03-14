using System;
using System.Diagnostics;
using UnityEngine;

public class MathExercise
{
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    OperationType operation;

    public MathExercise(int min, int max, OperationType operation)
    {
        this.operation = operation;

        Number1 = GenerateRandomDecimalWithRandomPrecision(min, max);
        Number2 = GenerateRandomDecimalWithRandomPrecision(min, max);

        CalculateAnswer();
    }

    void CalculateAnswer()
    {
        switch (operation)
        {
            case OperationType.Add:
                Answer = Number1 + Number2;
                break;

            case OperationType.Subtract:
                Answer = Number1 - Number2;
                break;

            case OperationType.Multiply:
                Answer = Number1 * Number2;
                break;

            case OperationType.Divide:
                Answer = Number2 != 0 ? Number1 / Number2 : 0;
                break;
        }
        UnityEngine.Debug.Log(Answer);
    }

    decimal GenerateRandomDecimalWithRandomPrecision(int minValue, int maxValue)
    {
        int minDecimals = 0;
        int maxDecimals = 5;

        if (GameSettings.Instance != null)
        {
            // If using truncated validation, ensure enough decimals exist
            if (GameSettings.Instance.validationMode == ValidationMode.Truncated)
            {
                minDecimals = GameSettings.Instance.decimals;
            }
        }

        int decimals = UnityEngine.Random.Range(minDecimals, maxDecimals + 1);

        decimal multiplier = (decimal)Math.Pow(10, decimals);

        int minInt = minValue * (int)multiplier;
        int maxInt = maxValue * (int)multiplier;

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }
}
