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

    [SerializeField] Animator _settingsAnim;

    private void Awake()
    {
        _additionScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Add"); });
        _subtractScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Subtract"); });
        _multiplyScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Multiply"); });
        _divideScene.onClick.AddListener(delegate { GameSettings.Instance.CallScene("Divide"); });
    }

    private void Start()
    {
        PopulateValidationDropdown();
        PopulateDecimalsDropdown();

        UpdateDecimalsDropdownState();
    }

    void PopulateValidationDropdown()
    {
        validationDropdown.ClearOptions();

        var options = new List<string>();

        foreach (ValidationMode mode in System.Enum.GetValues(typeof(ValidationMode)))
        {
            options.Add(mode.ToString());
        }

        validationDropdown.AddOptions(options);

        validationDropdown.value = (int)GameSettings.Instance.validationMode;

        validationDropdown.onValueChanged.AddListener(OnValidationModeChanged);
    }

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

    public void OnValidationModeChanged(int index)
    {
        GameSettings.Instance.validationMode = (ValidationMode)index;

        UpdateDecimalsDropdownState();
    }

    public void OnDecimalsChanged(int value)
    {
        GameSettings.Instance.decimals = value;
    }

    void UpdateDecimalsDropdownState()
    {
        decimalsDropdown.interactable =
            GameSettings.Instance.validationMode != ValidationMode.ExactOnly;
    }

    public void SettingsPanelActive()
    {
        _settingsAnim.SetBool("Config", !_settingsAnim.GetBool("Config"));
    }
}