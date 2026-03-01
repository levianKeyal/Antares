using UnityEngine;
using TMPro;
using System.Globalization;

public class RoundUpController : MonoBehaviour
{
    [SerializeField] TMP_Text number1Text;
    [SerializeField] TMP_Text number2Text;
    [SerializeField] TMP_Text signText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_InputField answerInput;
    [SerializeField] TMP_Dropdown decimalsDropdown;

    public OperationType operationType;
    public ValidationMode validationMode;

    MathExercise currentExercise;

    void Start()
    {
        GenerateExercise();
    }

    public void GenerateExercise()
    {
        Debug.Log("Start");
        int decimals = decimalsDropdown.value;

        currentExercise = new MathExercise(
            min: 0,
            max: 100,
            operation: operationType
        );

        number1Text.text = currentExercise.Number1.ToString(CultureInfo.InvariantCulture);
        number2Text.text = currentExercise.Number2.ToString(CultureInfo.InvariantCulture);

        Debug.Log(operationType);

        switch(operationType)
        {
            case OperationType.Add:
                signText.text = "+";
                break;
            case OperationType.Subtract:
                signText.text = "-";
                break;
            case OperationType.Multiply:
                signText.text = "X";
                break;
            case OperationType.Divide:
                signText.text = "÷";
                break;
        }

        messageText.text = "Esperando respuesta";
        answerInput.text = "";
    }

    public void CheckAnswer()
    {
        string normalized = answerInput.text.Replace(",", ".");

        decimal userValue;
        if (!decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out userValue))
        {
            messageText.text = "Número inválido";
            return;
        }

        bool correct = MathValidator.Validate(
            currentExercise.Answer,
            userValue,
            decimalsDropdown.value,
            validationMode
        );

        messageText.text = correct ? "Respuesta Correcta!" : "Respuesta Incorrecta!";
    }
}
