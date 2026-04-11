using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

using System.Globalization;

public class RoundUpController : MonoBehaviour
{
    [Header("Scene Difficulty Profile")]
    [SerializeField]
    DifficultyProfile difficultyProfile;

    [Header("UI")]
    [SerializeField] TMP_Text _validationType;
    [SerializeField] TMP_Text _decimalNumbers;
    [SerializeField] TMP_Text number1Text;
    [SerializeField] TMP_Text number2Text;
    [SerializeField] TMP_Text signText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button _exitScreen;

    [SerializeField] RectTransform answerInputTransform;
    [SerializeField] CanvasGroup feedbackPanel;
    [SerializeField] Image feedbackImage;

    [SerializeField] GameObject _drawingBoard;

    [Header("Feedback")]
    [SerializeField] float flashDuration = 0.35f;
    [SerializeField] float shakeDuration = 0.25f;
    [SerializeField] float shakeStrength = 8f;

    [Header("Exercise Settings")]
    public OperationType operationType;

    MathExercise currentExercise;
    private void Awake()
    {
        _exitScreen.onClick.AddListener(
            delegate { GameSettings.Instance.CallScene("StartFlow"); }
        );

        // Apply scene difficulty if profile exists
        ApplyDifficultyProfile();


        ValidationMode validation =
            GameSettings.Instance.validationMode;

        _validationType.text =
            GetValidationType(validation);


        if (validation == ValidationMode.ExactOnly ||
           validation == ValidationMode.All)
        {
            _decimalNumbers.text = null;
        }
        else
        {
            _decimalNumbers.text =
                GameSettings.Instance.decimals +
                " Decimales";
        }


        _drawingBoard.SetActive(false);
    }

    void Start()
    {
        GenerateExercise();
    }

    void ApplyDifficultyProfile()
    {
        if (difficultyProfile == null)
            return;

#if UNITY_EDITOR
        Debug.Log(
            $"Applying DifficultyProfile: {difficultyProfile.name}"
        );
#endif

        DifficultyManager.Instance.ApplyProfile(
            difficultyProfile
        );
    }

    OperationType ResolveOperation(OperationType fallbackOperation)
    {
        // Si no hay DifficultyProfile → comportamiento original
        if (difficultyProfile == null)
            return fallbackOperation;


        List<OperationType> allowedOperations =
            new List<OperationType>();


        if (difficultyProfile.allowAddition)
            allowedOperations.Add(OperationType.Add);

        if (difficultyProfile.allowSubtraction)
            allowedOperations.Add(OperationType.Subtract);

        if (difficultyProfile.allowMultiplication)
            allowedOperations.Add(OperationType.Multiply);

        if (difficultyProfile.allowDivision)
            allowedOperations.Add(OperationType.Divide);


        // Si el profile no definió operaciones válidas
        if (allowedOperations.Count == 0)
            return fallbackOperation;


        // Solo una operación
        if (allowedOperations.Count == 1)
            return allowedOperations[0];


        // Varias operaciones → random curricular
        return allowedOperations[
            Random.Range(0, allowedOperations.Count)
        ];
    }

    public void BlackboardIO()
    {
        if(_drawingBoard.activeInHierarchy == true)
        {
            _drawingBoard.SetActive(false);
        }
        else if (_drawingBoard.activeInHierarchy == false)
        {
            _drawingBoard.SetActive(true);
        }
    }
    public void GenerateExercise()
    {
        Debug.Log("Generating exercise");

        OperationType exerciseOperation = operationType;


        // comportamiento original Random
        if (operationType == OperationType.Random)
        {
            exerciseOperation = (OperationType)Random.Range(
                1,
                System.Enum.GetValues(typeof(OperationType)).Length
            );
        }

        // aplicar selector curricular si existe profile
        exerciseOperation =
        ResolveOperation(exerciseOperation);

        currentExercise = new MathExercise(
            min: 0,
            max: 100,
            operation: exerciseOperation
        );

        number1Text.text = currentExercise.Number1.ToString(CultureInfo.InvariantCulture);
        number2Text.text = currentExercise.Number2.ToString(CultureInfo.InvariantCulture);

        signText.text = GetOperationSymbol(exerciseOperation);

        messageText.text = "Esperando respuesta";

        answerInput.text = "";
        answerInput.ActivateInputField();

        decimal expectedAnswer =
            CalculateExpectedAnswer(
                currentExercise.Answer,
                GameSettings.Instance.decimals,
                GameSettings.Instance.validationMode
    );

        if (GameSettings.Instance != null)
        {
            string decimalsInfo =
    GameSettings.Instance.validationMode == ValidationMode.ExactOnly
    ? "notRequired"
    : GameSettings.Instance.decimals.ToString();

            Debug.Log(
            $"Answer(real) = {currentExercise.Answer} | " +
            $"Expected(student) = {expectedAnswer} | " +
            $"Mode = {GameSettings.Instance.validationMode} | " +
            $"Decimals = {decimalsInfo}"
            );
        }
    }

    public void CheckAnswer()
    {
        string normalized = answerInput.text.Replace(",", ".");

        if (!decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal userValue))
        {
            messageText.text = "Esperando respuesta";
            return;
        }

        if (GameSettings.Instance == null)
        {
            Debug.LogError("GameSettings Instance no encontrado");
            return;
        }

        bool correct = MathValidator.Validate(
            currentExercise.Answer,
            userValue,
            GameSettings.Instance.decimals,
            GameSettings.Instance.validationMode
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

        RegisterCampaignProgressIfNeeded();

        Invoke(nameof(GenerateExercise), 1.2f);
    }

    void RegisterCampaignProgressIfNeeded()
    {
        if (difficultyProfile == null)
            return;

        if (LevelProgressManager.Instance == null)
            return;

        LevelProgressManager.Instance.RegisterCorrectAnswer(
            difficultyProfile
        );
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

        if (feedbackImage != null)
            feedbackImage.color = color;

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
    string GetValidationType(ValidationMode validationTypeString)
    {
        switch (validationTypeString)
        {
            case ValidationMode.ExactOnly : return "Resultado Exacto";
            case ValidationMode.Truncated: return "Resultado Truncado";
            case ValidationMode.Ceil: return "Resultado Redondeado";
            case ValidationMode.All: return "Cualquier Tipo De Resultado";
        }

        return "?";
    }
    decimal CalculateExpectedAnswer(
     decimal answer,
     int decimals,
     ValidationMode mode)
    {
        decimal multiplier =
            (decimal)System.Math.Pow(10, decimals);

        return mode switch
        {
            ValidationMode.ExactOnly =>
                answer,

            ValidationMode.Truncated =>
                System.Math.Truncate(answer * multiplier) / multiplier,

            ValidationMode.Ceil =>
                System.Math.Ceiling(answer * multiplier) / multiplier,

            _ => answer
        };
    }
}