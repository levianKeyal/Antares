using UnityEngine;
using System;

/// <summary>
/// MathExercise generates arithmetic exercises (Add, Subtract, Multiply, Divide)
/// following pedagogical constraints:
/// - operand sign rules
/// - decimal precision limits
/// - validation mode compatibility
/// - readable numeric formatting for students
/// </summary>
public class MathExercise
{
    public decimal Number1 { get; private set; }
    public decimal Number2 { get; private set; }
    public decimal Answer { get; private set; }

    /// <summary>
    /// Powers of 10 used for decimal manipulation
    /// </summary>
    static readonly decimal[] Pow10 =
    {
        1m, 10m, 100m, 1000m, 10000m, 100000m, 1000000m
    };

    /// <summary>
    /// Constructor selects generation strategy depending on operation
    /// </summary>
    public MathExercise(int min, int max, OperationType operation)
    {
        switch (operation)
        {
            case OperationType.Add:
                GenerateAddSub(min, max, OperationType.Add);
                break;

            case OperationType.Subtract:
                GenerateAddSub(min, max, OperationType.Subtract);
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
    /// with up to 5 decimal digits
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
    /// Generates a pedagogically valid result depending on validation mode
    /// </summary>
    decimal GenerateValidResult(int min, int max)
    {
        int maxDecimals = 6;
        int decimals;

        var validationMode = GameSettings.Instance.validationMode;

        bool requiresExtraDecimal =
            validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil;

        if (requiresExtraDecimal)
        {
            int minRequired = GameSettings.Instance.decimals + 1;

            decimals = UnityEngine.Random.Range(
                minRequired,
                maxDecimals + 1
            );
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

            // avoid trailing zero for truncated / ceil pedagogy
            if (requiresExtraDecimal && decimalPart % 10 == 0)
                continue;

            break;

        } while (true);

        decimal result = integerPart + decimalPart / multiplier;

        return LimitDecimals(result, maxDecimals);
    }

    // =========================================================
    // ADDITION & SUBTRACTION
    // =========================================================

    void GenerateAddSub(int min, int max, OperationType op)
    {
        decimal result;
        decimal a;
        decimal b;

        var signMode = GameSettings.Instance.numberSignMode;

        int maxAttempts = 600;
        int attempt = 0;

        bool valid = false;

        do
        {
            attempt++;

            // 1️⃣ generar resultado
            result = GenerateValidResultAddSub(min, max);

            result = ApplyResultSign(result, signMode);

            // 2️⃣ generar primer operando
            a = GenerateSignedOperand(max, signMode);

            // 3️⃣ calcular segundo operando
            b = op == OperationType.Add
                ? result - a
                : a - result;

            // 4️⃣ validar signos explícitamente
            bool signValid = ValidateOperands(a, b, signMode);

            if (!signValid)
                continue;

            // 5️⃣ validar dificultad operandos
            if (!ValidateAddSubDifficulty(a))
                continue;

            if (!ValidateAddSubDifficulty(b))
                continue;

            // 6️⃣ validar dificultad resultado
            if (!ValidateAddSubDifficulty(result))
                continue;

            // 7️⃣ validar coherencia matemática final
            decimal computed =
                op == OperationType.Add
                ? a + b
                : a - b;

            if (computed != result)
                continue;

            valid = true;

        }
        while (!valid && attempt < maxAttempts);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(op);
#endif
    }

    /// <summary>
    /// Applies sign rules to the generated result
    /// </summary>
    decimal ApplyResultSign(decimal value, NumberSignMode mode)
    {
        switch (mode)
        {
            case NumberSignMode.PositiveOnly:
                return Math.Abs(value);

            case NumberSignMode.NegativeOnly:
                return -Math.Abs(value);

            case NumberSignMode.Mixed:
                return UnityEngine.Random.value > 0.5f ? -value : value;
        }

        return value;
    }
    decimal GenerateRandomDecimalAddSub(int min, int max)
    {
        var settings = GameSettings.Instance;

        int decimals =
            UnityEngine.Random.Range(
                0,
                settings.addSubMaxDecimalDigits + 1
            );

        decimal multiplier = Pow10[decimals];

        int maxInteger =
            (int)Math.Pow(
                10,
                settings.addSubMaxIntegerDigits
            ) - 1;

        int integerPart =
            UnityEngine.Random.Range(1, maxInteger + 1);

        int decimalPart =
            UnityEngine.Random.Range(0, (int)multiplier);

        return integerPart + decimalPart / multiplier;
    }

    /// <summary>
    /// Generates an operand with the correct sign configuration
    /// </summary>
    decimal GenerateSignedOperand(int max, NumberSignMode mode)
    {
        decimal value;

        do
        {
            value = GenerateRandomDecimalAddSub(1, max);
        }
        while (value == 0);

        switch (mode)
        {
            case NumberSignMode.PositiveOnly:
                return value;

            case NumberSignMode.NegativeOnly:
                return -value;

            case NumberSignMode.Mixed:
                return UnityEngine.Random.value > 0.5f ? -value : value;
        }

        return value;
    }

    /// <summary>
    /// Ensures operands respect sign configuration
    /// </summary>
    bool ValidateOperands(decimal a, decimal b, NumberSignMode mode)
    {
        if (a == 0 || b == 0)
            return false;

        if (mode == NumberSignMode.PositiveOnly)
            return a > 0 && b > 0;

        if (mode == NumberSignMode.NegativeOnly)
            return a < 0 && b < 0;

        return true;
    }
    decimal GenerateValidResultAddSub(int min, int max)
    {
        var settings = GameSettings.Instance;

        int maxDecimals = settings.addSubMaxDecimalDigits;

        var validationMode = settings.validationMode;

        bool requiresExtraDecimal =
            validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil;

        int decimals;

        if (requiresExtraDecimal)
        {
            int minRequired = settings.decimals + 1;

            decimals = UnityEngine.Random.Range(
                minRequired,
                maxDecimals + 1
            );
        }
        else
        {
            decimals = UnityEngine.Random.Range(
                0,
                maxDecimals + 1
            );
        }

        decimal multiplier = Pow10[decimals];

        int maxIntegerValue =
            (int)Math.Pow(
                10,
                settings.addSubMaxIntegerDigits
            ) - 1;

        int integerPart =
            UnityEngine.Random.Range(
                1,
                maxIntegerValue + 1
            );

        int decimalPart;

        do
        {
            decimalPart = UnityEngine.Random.Range(
                0,
                (int)multiplier
            );

            if (requiresExtraDecimal &&
                decimalPart % 10 == 0)
                continue;

            break;

        } while (true);

        decimal result =
            integerPart + decimalPart / multiplier;

        return LimitDecimals(result, 6);
    }
    // =========================================================
    // MULTIPLICATION
    // =========================================================

    void GenerateMultiplication(int min, int max)
    {
        decimal a;
        decimal b;
        decimal result = 0;

        var signMode = GameSettings.Instance.numberSignMode;
        var validationMode = GameSettings.Instance.validationMode;

        int maxAttempts = 300;
        int attempt = 0;

        bool valid = false;

        do
        {
            attempt++;

            // 1️⃣ generate operands first (pedagogically safe)
            GenerateValidMultiplicationOperands(
                signMode, out a, out b);

            if (a == 0 || b == 0)
                continue;

            // 2️⃣ apply sign rules
            ApplyMultiplicationSigns(ref a, ref b, signMode);

            // 3️⃣ compute exact result
            result = a * b;

            if (!ValidateMultiplicationDifficulty(a))
                continue;

            if (!ValidateMultiplicationDifficulty(b))
                continue;

            // 4️⃣ enforce global decimal limit
            if (DecimalPlaces(result) > 6)
                continue;

            // 5️⃣ enforce truncated / ceil pedagogy rule
            if (validationMode == ValidationMode.Truncated ||
                validationMode == ValidationMode.Ceil)
            {
                int requiredDecimals =
                    GameSettings.Instance.decimals + 1;

                if (DecimalPlaces(result) < requiredDecimals)
                    continue;
            }

            valid = true;

        }
        while (!valid && attempt < maxAttempts);

        Number1 = a;
        Number2 = b;
        Answer = result;

#if UNITY_EDITOR
        Validate(OperationType.Multiply);
#endif
    }

    void GenerateValidMultiplicationOperands(
    NumberSignMode signMode,
    out decimal a,
    out decimal b)
    {
        var settings = GameSettings.Instance;

        int maxOperandDecimals =
            settings.multiplicationMaxDecimalDigits;

        int targetResultDecimals =
            settings.validationMode == ValidationMode.ExactOnly
            ? maxOperandDecimals
            : settings.decimals + 1;

        int decimalsA =
            UnityEngine.Random.Range(0, targetResultDecimals + 1);

        int decimalsB =
            UnityEngine.Random.Range(
                0,
                targetResultDecimals - decimalsA + 1
            );

        a = GenerateMultiplicationOperandWithDecimals(
            decimalsA,
            signMode
        );

        b = GenerateMultiplicationOperandWithDecimals(
            decimalsB,
            signMode
        );
    }

    decimal GenerateMultiplicationOperandWithDecimals(
    int decimals,
    NumberSignMode signMode)
    {
        var settings = GameSettings.Instance;

        decimal multiplier = Pow10[decimals];

        int maxIntegerValue =
            (int)Math.Pow(
                10,
                settings.multiplicationMaxIntegerDigits
            ) - 1;

        int integerPart =
            UnityEngine.Random.Range(
                1,
                maxIntegerValue + 1
            );

        int decimalPart =
            decimals == 0
            ? 0
            : UnityEngine.Random.Range(
                1,
                (int)multiplier
            );

        decimal value =
            integerPart + decimalPart / multiplier;

        switch (signMode)
        {
            case NumberSignMode.PositiveOnly:
                return value;

            case NumberSignMode.NegativeOnly:
                return -value;

            case NumberSignMode.Mixed:
                return UnityEngine.Random.value > 0.5f
                    ? -value
                    : value;
        }

        return value;
    }

    bool ValidateMultiplicationDifficulty(decimal value)
    {
        var settings = GameSettings.Instance;

        int integerDigits = IntegerDigits(value);
        int decimalDigits = DecimalPlaces(value);

        if (integerDigits > settings.multiplicationMaxIntegerDigits)
            return false;

        if (decimalDigits > settings.multiplicationMaxDecimalDigits)
            return false;

        return true;
    }

    decimal GenerateRandomDecimalMultiplication()
    {
        var settings = GameSettings.Instance;

        int decimals =
            UnityEngine.Random.Range(
                0,
                settings.multiplicationMaxDecimalDigits + 1
            );

        decimal multiplier = Pow10[decimals];

        int maxIntegerValue =
            (int)Math.Pow(
                10,
                settings.multiplicationMaxIntegerDigits
            ) - 1;

        int integerPart =
            UnityEngine.Random.Range(
                1,
                maxIntegerValue + 1
            );

        int decimalPart =
            UnityEngine.Random.Range(
                0,
                (int)multiplier
            );

        return integerPart + decimalPart / multiplier;
    }

    decimal GenerateSignedMultiplicationOperand(NumberSignMode mode)
    {
        decimal value;

        do
        {
            value = GenerateRandomDecimalMultiplication();
        }
        while (value == 0);

        switch (mode)
        {
            case NumberSignMode.PositiveOnly:
                return value;

            case NumberSignMode.NegativeOnly:
                return -value;

            case NumberSignMode.Mixed:
                return UnityEngine.Random.value > 0.5f ? -value : value;
        }

        return value;
    }

    void ApplyMultiplicationSigns(ref decimal a, ref decimal b, NumberSignMode mode)
    {
        switch (mode)
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
                if (UnityEngine.Random.value > 0.5f) a = -a;
                if (UnityEngine.Random.value > 0.5f) b = -b;
                break;
        }
    }

    // =========================================================
    // DIVISION
    // =========================================================

    void GenerateSmartDivision(int min, int max)
    {
        decimal result;
        decimal divisor = 0;
        decimal dividend = 0;

        var signMode = GameSettings.Instance.numberSignMode;
        var validationMode = GameSettings.Instance.validationMode;

        int maxOperandDecimals =
            GameSettings.Instance.maxDivisionExactOperandDecimals;

        // Prevent impossible configuration:
        // integer operands cannot produce truncated/ceil decimal answers

        if (maxOperandDecimals == 0 &&
            (validationMode == ValidationMode.Truncated ||
             validationMode == ValidationMode.Ceil))
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                "Invalid config: integer division incompatible with truncated/ceil mode."
            );
#endif

            validationMode = ValidationMode.ExactOnly;
        }

        int maxAttempts = 300;
        int attempt = 0;

        bool valid = false;

        do
        {
            attempt++;

            // 1️⃣ generate pedagogically valid result
            if (maxOperandDecimals == 0)
            {
                result = UnityEngine.Random.Range(min, max + 1);
            }
            else
            {
                result = GenerateValidResult(min, max);
            }

            if (result == 0)
                continue;

            // 2️⃣ enforce truncated / ceil pedagogy rule
            if (validationMode == ValidationMode.Truncated ||
                validationMode == ValidationMode.Ceil)
            {
                int requiredDecimals =
                    GameSettings.Instance.decimals + 1;

                if (DecimalPlaces(result) < requiredDecimals)
                    continue;
            }

            // 3️⃣ generate divisor
            if (maxOperandDecimals == 0)
            {
                divisor = UnityEngine.Random.Range(1, max + 1);
            }
            else
            {
                divisor = GenerateRandomDecimal(1, max);
            }

            if (divisor == 0)
                continue;

            if (!ValidateDivisionDifficulty(divisor))
                continue;

            // 4️⃣ apply divisor sign
            ApplyDivisionDivisorSign(ref divisor, signMode);

            // 5️⃣ compute dividend exactly
            dividend = result * divisor;

            if (!ValidateDivisionDifficulty(dividend))
                continue;

            // 🔵 Special rule: enforce integer-only division when decimals = 0
            if (maxOperandDecimals == 0)
            {
                if (DecimalPlaces(result) != 0 ||
                    DecimalPlaces(divisor) != 0 ||
                    DecimalPlaces(dividend) != 0)
                    continue;
            }

            if (DecimalPlaces(dividend) > 6)
                continue;

            // 6️⃣ ExactOnly readability constraints
            if (validationMode == ValidationMode.ExactOnly)
            {
                if (DecimalPlaces(dividend) > maxOperandDecimals ||
                    DecimalPlaces(divisor) > maxOperandDecimals)
                    continue;

                if (EndsWithZeroDecimal(dividend) ||
                    EndsWithZeroDecimal(divisor))
                    continue;
            }

            // 7️⃣ apply dividend sign
            ApplyDivisionDividendSign(ref dividend, signMode);

            valid = true;

        }
        while (!valid && attempt < maxAttempts);

        // 🛑 If no valid combination found, retry generation
        if (!valid)
        {
#if UNITY_EDITOR
            Debug.LogError(
                "Division generation failed: incompatible configuration detected."
            );
#endif

            // fallback seguro pedagógico
            Number1 = 10;
            Number2 = 2;
            Answer = 5;

            return;
        }

        Number1 = dividend;
        Number2 = divisor;
        Answer = dividend / divisor;

#if UNITY_EDITOR
        Validate(OperationType.Divide);
#endif
    }

    void ApplyDivisionDivisorSign(ref decimal divisor, NumberSignMode mode)
    {
        if (mode == NumberSignMode.NegativeOnly)
            divisor = -Math.Abs(divisor);
        else if (mode == NumberSignMode.Mixed && UnityEngine.Random.value > 0.5f)
            divisor = -divisor;
    }

    void ApplyDivisionDividendSign(ref decimal dividend, NumberSignMode mode)
    {
        if (mode == NumberSignMode.NegativeOnly)
            dividend = -Math.Abs(dividend);
        else if (mode == NumberSignMode.Mixed && UnityEngine.Random.value > 0.5f)
            dividend = -dividend;
    }

    // =========================================================
    // UTILITIES
    // =========================================================
    bool ValidateAddSubDifficulty(decimal value)
    {
        var settings = GameSettings.Instance;

        int integerDigits = IntegerDigits(value);
        int decimalDigits = DecimalPlaces(value);

        if (integerDigits > settings.addSubMaxIntegerDigits)
            return false;

        if (decimalDigits > settings.addSubMaxDecimalDigits)
            return false;

        return true;
    }

    bool ValidateDivisionDifficulty(decimal value)
    {
        var settings = GameSettings.Instance;

        int integerDigits = IntegerDigits(value);
        int decimalDigits = DecimalPlaces(value);

        if (integerDigits > settings.divisionMaxIntegerDigits)
            return false;

        if (decimalDigits > settings.maxDivisionExactOperandDecimals)
            return false;

        return true;
    }

    bool EndsWithZeroDecimal(decimal value)
    {
        value = Math.Abs(value);

        int decimals = DecimalPlaces(value);

        if (decimals == 0)
            return false;

        decimal multiplier = Pow10[decimals];

        decimal decimalPart = (value * multiplier) % 10;

        return decimalPart == 0;
    }

    decimal TruncateDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = Pow10[maxDecimals];
        return Math.Truncate(value * multiplier) / multiplier;
    }

    int DecimalPlaces(decimal value)
    {
        value = Math.Abs(value);

        int[] bits = decimal.GetBits(value);

        return (bits[3] >> 16) & 31;
    }

    int IntegerDigits(decimal value)
    {
        value = Math.Abs(value);

        if (value < 1)
            return 1;

        return (int)Math.Floor(Math.Log10((double)value)) + 1;
    }
    decimal LimitDecimals(decimal value, int maxDecimals)
    {
        decimal multiplier = Pow10[maxDecimals];
        return Math.Round(value * multiplier) / multiplier;
    }

#if UNITY_EDITOR
    void Validate(OperationType op)
    {
        decimal computed = op switch
        {
            OperationType.Add => Number1 + Number2,
            OperationType.Subtract => Number1 - Number2,
            OperationType.Multiply => Number1 * Number2,
            OperationType.Divide => Number1 / Number2,
            _ => 0
        };

        if (Math.Round(computed, 5) != Math.Round(Answer, 5))
        {
            Debug.LogError($"Math error: {Number1} op {Number2} != {Answer}");
        }
    }
#endif
}