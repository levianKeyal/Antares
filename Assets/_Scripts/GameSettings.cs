using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    [Header("Game State")]

    public bool cinematicPause;
    public bool cannonBallViewActive;

    [Header("Interaction")]

    public bool interactionBlocked;

    [Header("Screen Orientation")]

    public bool isLandscape;

    public bool isPortrait;

    int lastScreenWidth;

    int lastScreenHeight;

    EncounterUIManager encounterUIManager;

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
    void Start()
    {
        UpdateScreenOrientation();
        ResolveEncounterUIManager();
        UpdateUIElements();

        lastScreenWidth =
            Screen.width;

        lastScreenHeight =
            Screen.height;
    }
    public void CallScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void StartCinematicPause()
    {
        cinematicPause = true;

        BoatMover[] boats =
            FindObjectsByType<BoatMover>(
                FindObjectsSortMode.None
            );

        foreach (BoatMover boat in boats)
        {
            boat.StopMovementImmediately();
        }
    }

    public void EndCinematicPause()
    {
        cinematicPause = false;

        BoatMover[] boats =
            FindObjectsByType<BoatMover>(
                FindObjectsSortMode.None
            );

        foreach (BoatMover boat in boats)
        {
            boat.ResumeMovement();
        }
    }

    public void StartCannonBallViewPause()
    {
        cinematicPause = true;

        BoatMover[] boats =
            FindObjectsByType<BoatMover>(
                FindObjectsSortMode.None
            );

        foreach (BoatMover boat in boats)
        {
            boat.StopMovementImmediately();
        }
    }

    public void EndCannonBallViewPause()
    {
        cinematicPause = false;
        cannonBallViewActive = false;

        ResumeBoatsAfterCannonBallView();
    }

    void ResumeBoatsAfterCannonBallView()
    {
        BoatMover battleBoat = null;

        if (StartGamePlay.Instance != null)
        {
            battleBoat = StartGamePlay.Instance.currentBoatMover;
        }

        BoatMover[] boats =
            FindObjectsByType<BoatMover>(
                FindObjectsSortMode.None
            );

        foreach (BoatMover boat in boats)
        {
            if (boat == null)
            {
                continue;
            }

            if (battleBoat != null && boat == battleBoat)
            {
                continue;
            }

            boat.ResumeMovement();
        }
    }

    void Update()
    {
        // ====================================
        // SCREEN CHANGED
        // ====================================

        if (
            Screen.width != lastScreenWidth
            ||
            Screen.height != lastScreenHeight
        )
        {
            lastScreenWidth =
                Screen.width;

            lastScreenHeight =
                Screen.height;

            UpdateScreenOrientation();
            UpdateUIElements();
        }
    }

    void UpdateScreenOrientation()
    {
        isLandscape =
            Screen.width >
            Screen.height;

        isPortrait =
            Screen.height >
            Screen.width;
    }

    void UpdateUIElements()
    {
        if (encounterUIManager == null)
        {
            ResolveEncounterUIManager();
        }

        if (encounterUIManager == null)
        {
            return;
        }

        encounterUIManager.UpdateUIElements();
    }

    public void RegisterEncounterUIManager(EncounterUIManager manager)
    {
        encounterUIManager = manager;
    }

    void ResolveEncounterUIManager()
    {
        if (encounterUIManager == null)
        {
            encounterUIManager = FindFirstObjectByType<EncounterUIManager>();
        }
    }

    void OnValidate()
    {
        ValidateAddSubDifficultyConstraints();
        ValidateMultiplicationDifficultyConstraints();
        ValidateDivisionDifficultyConstraints();
    }

    public void ValidateAddSubDifficultyConstraints()
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
            $"Minimum allowed Add/Sub decimals for {validationMode}: {minDecimals}");
    }
    public int GetMinimumAddSubDecimals()
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

    public int GetMinimumMultiplicationDecimals()
    {
        if (validationMode == ValidationMode.ExactOnly)
            return 0;

        return decimals + 1;
    }

    public int GetMinimumDivisionDecimals()
    {
        if (validationMode == ValidationMode.ExactOnly)
            return 0;

        return decimals + 1;
    }

    public void ValidateMultiplicationDifficultyConstraints()
    {
        if (validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil ||
            validationMode == ValidationMode.All)
        {
            int minRequired = decimals + 1;

            if (multiplicationMaxDecimalDigits < minRequired)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"Multiplication decimal difficulty too low for {validationMode}. " +
                    $"Adjusting automatically to {minRequired} decimals."
                );
#endif

                multiplicationMaxDecimalDigits = minRequired;
            }
        }

        if (multiplicationMaxIntegerDigits < 1)
            multiplicationMaxIntegerDigits = 1;
    }

    public void ValidateDivisionDifficultyConstraints()
    {
        if (validationMode == ValidationMode.Truncated ||
            validationMode == ValidationMode.Ceil ||
            validationMode == ValidationMode.All)
        {
            int minRequired = decimals + 1;

            if (maxDivisionExactOperandDecimals < minRequired)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"Division decimal difficulty too low for {validationMode}. " +
                    $"Adjusting automatically to {minRequired} decimals."
                );
#endif

                maxDivisionExactOperandDecimals = minRequired;
            }
        }

        if (divisionMaxIntegerDigits < 1)
            divisionMaxIntegerDigits = 1;
    }
}
