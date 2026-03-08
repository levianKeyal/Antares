using UnityEngine;
using System;
using System.Globalization;
using TMPro;
using UnityEngine.UI;

public class RoundUpTestDecimals : MonoBehaviour
{
    [SerializeField] TMP_Text m_number1;
    [SerializeField] TMP_Text m_number2;
    [SerializeField] TMP_InputField m_answer;
    [SerializeField] TMP_Text m_Messages;
    public TMP_Dropdown decimalsDropdown;

    public Button exitScene;

    decimal number1;
    decimal number2;
    decimal answer;

    int decimalSelected = 0;

    private void Awake()
    {
        exitScene.onClick.AddListener(ExitScene);
    }
    void Start()
    {
        UpdateDecimals();
        GenerateRandomNumbers();
    }

    public void UpdateDecimals()
    {
        decimalSelected = decimalsDropdown.value;
    }

    public decimal GenerateRandomDecimal(int minValue, int maxValue, int decimals)
    {
        decimal multiplier = (decimal)Math.Pow(10, decimals);

        int minInt = minValue * (int)multiplier;
        int maxInt = maxValue * (int)multiplier;

        int randomInt = UnityEngine.Random.Range(minInt, maxInt);

        return randomInt / multiplier;
    }

    void GenerateRandomNumbers()
    {
        number1 = GenerateRandomDecimal(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 5));
        number2 = GenerateRandomDecimal(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 5));

        answer = number1 + number2;

        m_number1.text = number1.ToString(CultureInfo.InvariantCulture);
        m_number2.text = number2.ToString(CultureInfo.InvariantCulture);

        Debug.Log($"Answer = {answer}");
    }

    public void CheckForCorrectAnswer()
    {
        string normalized = m_answer.text.Replace(",", ".");

        decimal userValue;
        if (!decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out userValue))
        {
            m_Messages.text = "Número inválido";
            return;
        }

        decimal multiplier = (decimal)Math.Pow(10, decimalSelected);

        // Real exact value
        decimal realValue = answer;

        // Truncated value
        decimal truncated = Math.Floor(answer * multiplier) / multiplier;

        // Rounded up value
        decimal expectedCeil = Math.Ceiling(answer * multiplier) / multiplier;

        bool isExact = userValue == realValue;
        bool isTruncatedMatch = userValue == truncated;
        bool isCeilMatch = userValue == expectedCeil;

        if (isExact || isTruncatedMatch || isCeilMatch)
            m_Messages.text = "Respuesta Correcta!";
        else
            m_Messages.text = "Respuesta Incorrecta!";
    }

    public void ResetExercise()
    {
        UpdateDecimals();
        GenerateRandomNumbers();
        m_answer.text = "";
        m_Messages.text = "Esperando Respuesta";
    }
    public void ExitScene()
    {
        GameSettings.Instance.CallScene("StartFlow");
    }
}