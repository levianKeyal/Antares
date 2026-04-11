using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    private void Awake()
    {
        // Singleton seguro
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    public void ApplyProfile(DifficultyProfile profile)
    {
        if (profile == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("ApplyProfile called with NULL profile.");
#endif
            return;
        }

        var settings = GameSettings.Instance;


        // ========================
        // VALIDATION SETTINGS
        // ========================

        settings.validationMode =
            profile.validationMode;

        settings.decimals =
            profile.validationDecimals;

        settings.numberSignMode =
            profile.signMode;


        // ========================
        // ADD / SUB SETTINGS
        // ========================

        settings.addSubMaxIntegerDigits =
            profile.addSubIntegerDigits;

        settings.addSubMaxDecimalDigits =
            profile.addSubDecimalDigits;


        // ========================
        // MULTIPLICATION SETTINGS
        // ========================

        settings.multiplicationMaxIntegerDigits =
            profile.multiplicationIntegerDigits;

        settings.multiplicationMaxDecimalDigits =
            profile.multiplicationDecimalDigits;


        // ========================
        // DIVISION SETTINGS
        // ========================

        settings.divisionMaxIntegerDigits =
            profile.divisionIntegerDigits;

        settings.maxDivisionExactOperandDecimals =
            profile.divisionDecimalDigits;


        // ========================
        // APPLY CONSTRAINT GUARDS
        // ========================

        settings.ValidateAddSubDifficultyConstraints();

        settings.ValidateMultiplicationDifficultyConstraints();

        settings.ValidateDivisionDifficultyConstraints();


#if UNITY_EDITOR
        Debug.Log(
            $"DifficultyProfile applied: {profile.name}"
        );
#endif
    }
}