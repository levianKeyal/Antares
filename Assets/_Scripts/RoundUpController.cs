using UnityEngine;
using TMPro;
using System.Globalization;
using System.Collections.Generic;

public class RoundUpController : MonoBehaviour
{
    [SerializeField] TMP_Text number1Text;
    [SerializeField] TMP_Text number2Text;
    [SerializeField] TMP_Text signText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_InputField answerInput;

    [SerializeField] RectTransform answerInputTransform;
    [SerializeField] CanvasGroup feedbackPanel;

    [SerializeField] float flashDuration = 0.35f;
    [SerializeField] float shakeDuration = 0.25f;
    [SerializeField] float shakeStrength = 8f;

    [SerializeField] TMP_Dropdown decimalsDropdown;
    [SerializeField] TMP_Dropdown operationDropdown;
    [SerializeField] TMP_Dropdown validationDropdown;

    public OperationType operationType;
    public ValidationMode validationMode;

    MathExercise currentExercise;

    int Decimals => decimalsDropdown.value;

    void Start()
    {
        PopulateOperationDropdown();
        PopulateValidationDropdown();

        UpdateDecimalDropdownState();

        GenerateExercise();
    }

    void PopulateOperationDropdown()
    {
        operationDropdown.ClearOptions();

        var options = new List<string>();

        foreach (OperationType op in System.Enum.GetValues(typeof(OperationType)))
        {
            options.Add(op.ToString());
        }

        operationDropdown.AddOptions(options);
        operationDropdown.onValueChanged.AddListener(OnOperationChanged);
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
        validationDropdown.onValueChanged.AddListener(OnValidationModeChanged);
    }

    void OnOperationChanged(int index)
    {
        operationType = (OperationType)index;
        GenerateExercise();
    }

    void OnValidationModeChanged(int index)
    {
        validationMode = (ValidationMode)index;

        UpdateDecimalDropdownState();
        GenerateExercise();
    }

    void UpdateDecimalDropdownState()
    {
        decimalsDropdown.interactable = validationMode != ValidationMode.ExactOnly;
    }

    public void GenerateExercise()
    {
        Debug.Log("Generating exercise");

        currentExercise = new MathExercise(
            min: 0,
            max: 100,
            operation: operationType
        );

        number1Text.text = currentExercise.Number1.ToString(CultureInfo.InvariantCulture);
        number2Text.text = currentExercise.Number2.ToString(CultureInfo.InvariantCulture);

        signText.text = GetOperationSymbol(operationType);

        messageText.text = "Esperando respuesta";

        answerInput.text = "";
        answerInput.ActivateInputField();

        Debug.Log($"Answer = {currentExercise.Answer} | Mode = {validationMode} | Decimals = {Decimals}");
    }

    public void CheckAnswer()
    {
        string normalized = answerInput.text.Replace(",", ".");

        if (!decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal userValue))
        {
            messageText.text = "Número inválido";
            return;
        }

        bool correct = MathValidator.Validate(
            currentExercise.Answer,
            userValue,
            Decimals,
            validationMode
        );

        if (correct)
            OnCorrectAnswer();
        else
            OnWrongAnswer();
    }

    void OnCorrectAnswer()
    {
        messageText.text = "Respuesta Correcta!";

        Debug.Log("Correct Answer");

        StartCoroutine(FlashFeedback(Color.green));

        Invoke(nameof(GenerateExercise), 1.2f);
    }

    void OnWrongAnswer()
    {
        messageText.text = "Respuesta Incorrecta!";

        Debug.Log("Wrong Answer");

        StartCoroutine(FlashFeedback(Color.red));
        StartCoroutine(ShakeInput());

    }

    System.Collections.IEnumerator FlashFeedback(Color color)
    {
        feedbackPanel.alpha = 0.6f;
        feedbackPanel.GetComponent<UnityEngine.UI.Image>().color = color;

        yield return new WaitForSeconds(flashDuration);

        feedbackPanel.alpha = 0f;
    }

    System.Collections.IEnumerator ShakeInput()
    {
        Vector3 originalPos = answerInputTransform.localPosition;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeStrength;
            float y = Random.Range(-1f, 1f) * shakeStrength;

            answerInputTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        answerInputTransform.localPosition = originalPos;
    }

    string GetOperationSymbol(OperationType operation)
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