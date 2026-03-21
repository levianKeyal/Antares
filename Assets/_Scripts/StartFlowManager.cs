using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StartFlowManager : MonoBehaviour
{
    [SerializeField] Button _additionScene;
    [SerializeField] Button _subtractScene;
    [SerializeField] Button _multiplyScene;
    [SerializeField] Button _divideScene;

    [SerializeField] TMP_Dropdown decimalsDropdown;
    [SerializeField] TMP_Dropdown validationDropdown;
    [SerializeField] TMP_Dropdown signsDropdown;

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
        // Scene navigation buttons
        _additionScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Add"); });
        _subtractScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Subtract"); });
        _multiplyScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Multiply"); });
        _divideScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Divide"); });
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
    }

    public void OnDecimalsChanged(int value)
    {
        GameSettings.Instance.decimals = value;
    }

    // NEW: Handle sign mode change
    public void OnSignsModeChanged(int index)
    {
        GameSettings.Instance.numberSignMode = (NumberSignMode)index;
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