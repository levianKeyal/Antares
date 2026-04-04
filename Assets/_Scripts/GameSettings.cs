using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    [Header("Validation Parameters")]

    public int decimals = 2;
    public ValidationMode validationMode = ValidationMode.ExactOnly;

    public NumberSignMode numberSignMode = NumberSignMode.PositiveOnly;

    [Header("Addition / Subtraction Difficulty")]

    [Range(1, 5)]
    public int addSubMaxIntegerDigits = 3;

    [Range(0, 6)]
    public int addSubMaxDecimalDigits = 2;

    [Header("Multiplication Difficulty")]

    [Range(1, 5)]
    public int multiplicationMaxIntegerDigits = 2;

    [Range(0, 6)]
    public int multiplicationMaxDecimalDigits = 2;

    [Header("Division Difficulty")]

    [Range(1, 5)]
    public int divisionMaxIntegerDigits = 2;
        
    [Range(0, 6)]
    public int maxDivisionExactOperandDecimals = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    public void CallScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ValidateAddSubDifficultyConstraints();
        ValidateMultiplicationDifficultyConstraints();
        ValidateDivisionDifficultyConstraints();
    }
#endif

#if UNITY_EDITOR
    void ValidateAddSubDifficultyConstraints()
    {
        int minDecimals = GetMinimumAddSubDecimals();

        if (addSubMaxDecimalDigits < minDecimals)
        {
            Debug.LogWarning(
                $"Add/Sub decimal difficulty too low for {validationMode}. " +
                $"Adjusting automatically to {minDecimals} decimals."
            );

            addSubMaxDecimalDigits = minDecimals;
        }

        if (addSubMaxIntegerDigits < 1)
            addSubMaxIntegerDigits = 1;

        Debug.Log(
    $"Minimum allowed Add/Sub decimals for {validationMode}: {minDecimals}"
);
    }
#endif
    int GetMinimumAddSubDecimals()
    {
        if (validationMode == ValidationMode.ExactOnly)
            return 0;

        if (validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil ||
            validationMode == ValidationMode.All)
        {
            return decimals + 1;
        }

        return 0;
    }

#if UNITY_EDITOR
    void ValidateMultiplicationDifficultyConstraints()
    {
        if (validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil ||
            validationMode == ValidationMode.All)
        {
            int minRequired = decimals + 1;

            if (multiplicationMaxDecimalDigits < minRequired)
            {
                Debug.LogWarning(
                    $"Multiplication decimal difficulty too low for {validationMode}. " +
                    $"Adjusting automatically to {minRequired} decimals."
                );

                multiplicationMaxDecimalDigits = minRequired;
            }
        }
    }
#endif

#if UNITY_EDITOR
    void ValidateDivisionDifficultyConstraints()
    {
        if (validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil ||
            validationMode == ValidationMode.All)
        {
            int minRequired = decimals + 1;

            if (maxDivisionExactOperandDecimals < minRequired)
            {
                Debug.LogWarning(
                    $"Division decimal difficulty too low for {validationMode}. " +
                    $"Adjusting automatically to {minRequired} decimals."
                );

                maxDivisionExactOperandDecimals = minRequired;
            }
        }

        if (divisionMaxIntegerDigits < 1)
            divisionMaxIntegerDigits = 1;
    }
#endif
}