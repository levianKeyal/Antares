using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Globalization;

public class RoundUpController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text number1Text;
    [SerializeField] TMP_Text number2Text;
    [SerializeField] TMP_Text signText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] Button _exitScreen;

    [SerializeField] RectTransform answerInputTransform;
    [SerializeField] CanvasGroup feedbackPanel;
    [SerializeField] Image feedbackImage;

    [Header("Feedback")]
    [SerializeField] float flashDuration = 0.35f;
    [SerializeField] float shakeDuration = 0.25f;
    [SerializeField] float shakeStrength = 8f;

    [Header("Exercise Settings")]
    public OperationType operationType;

    MathExercise currentExercise;
    private void Awake()
    {
        _exitScreen.onClick.AddListener(delegate { GameSettings.Instance.CallScene("StartFlow"); });
    }
    void Start()
    {
        GenerateExercise();
    }

    public void GenerateExercise()
    {
        Debug.Log("Generating exercise");

        OperationType exerciseOperation = operationType;

        if (operationType == OperationType.Random)
        {
            exerciseOperation = (OperationType)Random.Range(1, System.Enum.GetValues(typeof(OperationType)).Length);
        }

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

        if (GameSettings.Instance != null)
        {
            Debug.Log(
            $"Answer = {currentExercise.Answer} | Mode = {GameSettings.Instance.validationMode} | Decimals = {GameSettings.Instance.decimals}"
            );
        }
    }

    public void CheckAnswer()
    {
        string normalized = answerInput.text.Replace(",", ".");

        if (!decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal userValue))
        {
            messageText.text = "Numero invalido";
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
}