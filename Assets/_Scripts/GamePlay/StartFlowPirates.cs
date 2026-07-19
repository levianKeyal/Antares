using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StartFlowPirates : MonoBehaviour
{
    [SerializeField] Button _tutorialScene;
    [SerializeField] Button _angleScene;
    [SerializeField] Button _velocityScene;
    [SerializeField] Button _rangeScene;

    [SerializeField] TMP_Dropdown decimalsDropdown;
    [SerializeField] TMP_Dropdown validationDropdown;
    [SerializeField] TMP_Dropdown signsDropdown;

    [SerializeField] Slider intAddSubSlider;
    [SerializeField] TMP_Text intAddSubValue;

    [SerializeField] Slider decAddSubSlider;
    [SerializeField] TMP_Text decAddSubValue;

    [SerializeField] Slider intMultiplicationSlider;
    [SerializeField] TMP_Text intMultiplicationValue;

    [SerializeField] Slider decMultiplicationSlider;
    [SerializeField] TMP_Text decMultiplicationValue;

    [SerializeField] Slider intDivisionSlider;
    [SerializeField] TMP_Text intDivisionValue;

    [SerializeField] Slider decDivisionSlider;
    [SerializeField] TMP_Text decDivisionValue;

    Dictionary<ValidationMode, string> validationLabels = new Dictionary<ValidationMode, string>()
    {
    { ValidationMode.ExactOnly, "Exacto" },
    { ValidationMode.Truncated, "Truncado" },
    { ValidationMode.Ceil, "Redondeo" }, // o "Hacia arriba" si quieres ser más específico
    { ValidationMode.All, "Todos" }
    };

    [SerializeField] Animator _settingsAnim;

    private static bool hasInitialized = false;

    private void Awake()
    {       
        //Pirate Scene buttons
        _tutorialScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("TutoScene"); });
        _angleScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("SolveAngle"); });
        _velocityScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("SolveVelocity"); });
        _rangeScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("SolveRange"); });

        //Operands integer and decimals values

        //ADDITION AND SUBTRACTION
        intAddSubSlider.value = GameSettings.Instance.addSubMaxIntegerDigits;
        intAddSubValue.text = intAddSubSlider.value.ToString();

        decAddSubSlider.value = GameSettings.Instance.addSubMaxDecimalDigits;
        decAddSubValue.text = decAddSubSlider.value.ToString();

        intAddSubSlider.onValueChanged.AddListener(OnIntAddSubSliderChanged);
        decAddSubSlider.onValueChanged.AddListener(OnDecAddSubSliderChanged);

        // MULTIPLICATION
        intMultiplicationSlider.value =
        GameSettings.Instance.multiplicationMaxIntegerDigits;

        intMultiplicationValue.text =
        intMultiplicationSlider.value.ToString();

        decMultiplicationSlider.value =
        GameSettings.Instance.multiplicationMaxDecimalDigits;

        decMultiplicationValue.text =
        decMultiplicationSlider.value.ToString();

        intMultiplicationSlider.onValueChanged
            .AddListener(OnIntMultiplicationSliderChanged);

        decMultiplicationSlider.onValueChanged
            .AddListener(OnDecMultiplicationSliderChanged);


        // DIVISION
        intDivisionSlider.value =
        GameSettings.Instance.divisionMaxIntegerDigits;

        intDivisionValue.text =
        intDivisionSlider.value.ToString();

        decDivisionSlider.value =
        GameSettings.Instance.maxDivisionExactOperandDecimals;

        decDivisionValue.text =
        decDivisionSlider.value.ToString();

        intDivisionSlider.onValueChanged
            .AddListener(OnIntDivisionSliderChanged);

        decDivisionSlider.onValueChanged
            .AddListener(OnDecDivisionSliderChanged);

        UpdateAddSubDecimalSliderLimits();
    }

    private void Start()
    {
        // Solo se ejecuta la primera vez que la app inicia
        if (!hasInitialized)
        {
            // Force default only if needed (first launch or no persistence)
            GameSettings.Instance.validationMode = ValidationMode.ExactOnly;
            hasInitialized = true;
            }

        PopulateValidationDropdown();
        PopulateDecimalsDropdown();
        PopulateSignsDropdown(); // 👈 NEW

        UpdateDecimalsDropdownState();
    }

    void RefreshAddSubDifficultyUI()
    {
        GameSettings.Instance.ValidateAddSubDifficultyConstraints();

        int correctedDecimals =
            GameSettings.Instance.addSubMaxDecimalDigits;

        decAddSubSlider.value = correctedDecimals;
        decAddSubValue.text = correctedDecimals.ToString();

        int correctedIntegers =
            GameSettings.Instance.addSubMaxIntegerDigits;

        intAddSubSlider.value = correctedIntegers;
        intAddSubValue.text = correctedIntegers.ToString();
    }

    void UpdateAddSubDecimalSliderLimits()
    {
        int minAllowed =
            GameSettings.Instance.GetMinimumAddSubDecimals();

        decAddSubSlider.minValue = minAllowed;

        // Si el valor actual es menor que el mínimo permitido
        if (decAddSubSlider.value < minAllowed)
        {
            decAddSubSlider.value = minAllowed;

            GameSettings.Instance.addSubMaxDecimalDigits = minAllowed;
        }

        decAddSubValue.text =
            decAddSubSlider.value.ToString();
    }

    void UpdateMultiplicationDecimalSliderLimits()
    {
        int minAllowed =
        GameSettings.Instance.GetMinimumMultiplicationDecimals();

        decMultiplicationSlider.minValue = minAllowed;

        if (decMultiplicationSlider.value < minAllowed)
        {
            decMultiplicationSlider.value = minAllowed;

            GameSettings.Instance.multiplicationMaxDecimalDigits =
            minAllowed;
        }

        decMultiplicationValue.text =
        decMultiplicationSlider.value.ToString();
    }

    void UpdateDivisionDecimalSliderLimits()
    {
        int minAllowed =
        GameSettings.Instance.GetMinimumDivisionDecimals();

        decDivisionSlider.minValue = minAllowed;

        if (decDivisionSlider.value < minAllowed)
        {
            decDivisionSlider.value = minAllowed;

            GameSettings.Instance.maxDivisionExactOperandDecimals =
            minAllowed;
        }

        decDivisionValue.text =
        decDivisionSlider.value.ToString();
    }

    // =========================
    // VALIDATION DROPDOWN
    // =========================
    void PopulateValidationDropdown()
    {
        validationDropdown.ClearOptions();

        var options = new List<string>();

        // Iterate in enum order (respects your defined order)
        foreach (ValidationMode mode in System.Enum.GetValues(typeof(ValidationMode)))
        {
            options.Add(validationLabels[mode]);
        }

        validationDropdown.AddOptions(options);

        // Sync UI with current setting
        validationDropdown.value = (int)GameSettings.Instance.validationMode;

        validationDropdown.onValueChanged.AddListener(OnValidationModeChanged);
    }

    // =========================
    // DECIMALS DROPDOWN
    // =========================
    void PopulateDecimalsDropdown()
    {
        decimalsDropdown.ClearOptions();

        var options = new List<string>();

        for (int i = 0; i <= 5; i++)
        {
            options.Add(i.ToString());
        }

        decimalsDropdown.AddOptions(options);

        decimalsDropdown.value = GameSettings.Instance.decimals;

        decimalsDropdown.onValueChanged.AddListener(OnDecimalsChanged);
    }

    // =========================
    // SIGNS DROPDOWN (NEW)
    // =========================
    void PopulateSignsDropdown()
    {
        signsDropdown.ClearOptions();

        var options = new List<string>();

        // Friendly names for UI
        options.Add("Solo positivos");
        options.Add("Solo negativos");
        options.Add("Mezclados");

        signsDropdown.AddOptions(options);

        // Sync with current setting
        signsDropdown.value = (int)GameSettings.Instance.numberSignMode;

        signsDropdown.onValueChanged.AddListener(OnSignsModeChanged);
    }

    // =========================
    // EVENT HANDLERS
    // =========================

    public void OnValidationModeChanged(int index)
    {
        GameSettings.Instance.validationMode = (ValidationMode)index;

        UpdateDecimalsDropdownState();

        UpdateAddSubDecimalSliderLimits();
        RefreshAddSubDifficultyUI();

        UpdateMultiplicationDecimalSliderLimits();
        UpdateDivisionDecimalSliderLimits();
    }

    public void OnDecimalsChanged(int value)
    {
        GameSettings.Instance.decimals = value;

        UpdateAddSubDecimalSliderLimits();
        RefreshAddSubDifficultyUI();

        UpdateMultiplicationDecimalSliderLimits();
        UpdateDivisionDecimalSliderLimits();
    }
    
    // NEW: Handle sign mode change
    public void OnSignsModeChanged(int index)
    {
        GameSettings.Instance.numberSignMode = (NumberSignMode)index;
    }

    void OnIntAddSubSliderChanged(float value)
    {
        GameSettings.Instance.addSubMaxIntegerDigits = (int)value;

        GameSettings.Instance.ValidateAddSubDifficultyConstraints();

        int correctedValue =
            GameSettings.Instance.addSubMaxIntegerDigits;

        intAddSubSlider.value = correctedValue;

        intAddSubValue.text = correctedValue.ToString();
    }

    void OnDecAddSubSliderChanged(float value)
    {
        int intValue = (int)value;

        GameSettings.Instance.addSubMaxDecimalDigits = intValue;

        GameSettings.Instance.ValidateAddSubDifficultyConstraints();

        UpdateAddSubDecimalSliderLimits();

        decAddSubValue.text =
            GameSettings.Instance.addSubMaxDecimalDigits.ToString();
    }

    void OnIntMultiplicationSliderChanged(float value)
    {
        GameSettings.Instance.multiplicationMaxIntegerDigits =
        (int)value;

        GameSettings.Instance.ValidateMultiplicationDifficultyConstraints();

        int corrected =
        GameSettings.Instance.multiplicationMaxIntegerDigits;

        intMultiplicationSlider.value = corrected;

        intMultiplicationValue.text = corrected.ToString();
    }

    void OnDecMultiplicationSliderChanged(float value)
    {
        GameSettings.Instance.multiplicationMaxDecimalDigits =
        (int)value;

        GameSettings.Instance.ValidateMultiplicationDifficultyConstraints();

        UpdateMultiplicationDecimalSliderLimits();

        decMultiplicationValue.text =
        GameSettings.Instance.multiplicationMaxDecimalDigits
        .ToString();
    }

    void OnIntDivisionSliderChanged(float value)
    {
        GameSettings.Instance.divisionMaxIntegerDigits =
        (int)value;

        GameSettings.Instance.ValidateDivisionDifficultyConstraints();

        int corrected =
        GameSettings.Instance.divisionMaxIntegerDigits;

        intDivisionSlider.value = corrected;

        intDivisionValue.text = corrected.ToString();
    }

    void OnDecDivisionSliderChanged(float value)
    {
        GameSettings.Instance.maxDivisionExactOperandDecimals =
        (int)value;

        GameSettings.Instance.ValidateDivisionDifficultyConstraints();

        UpdateDivisionDecimalSliderLimits();

        decDivisionValue.text =
        GameSettings.Instance.maxDivisionExactOperandDecimals
        .ToString();
    }



    // =========================
    // UI STATE
    // =========================

    void UpdateDecimalsDropdownState()
    {
        // Disable decimals if ExactOnly mode is selected
        decimalsDropdown.interactable =
            GameSettings.Instance.validationMode != ValidationMode.ExactOnly;
    }

    // Toggle settings panel animation
    public void SettingsPanelActive()
    {
        _settingsAnim.SetBool("Config", !_settingsAnim.GetBool("Config"));
    }
}