using UnityEngine;
using System;
using System.Globalization;
using TMPro;
using UnityEngine.UI;

public class RoundUpTest : MonoBehaviour
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

    void GenerateRandomNumbers()
    {
        decimal multiplier = (decimal)Math.Pow(10, decimalSelected);

        number1 = Math.Floor((decimal)UnityEngine.Random.Range(0f, 100f) * multiplier) / multiplier;
        number2 = Math.Floor((decimal)UnityEngine.Random.Range(0f, 100f) * multiplier) / multiplier;

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
        decimal expectedCeil = Math.Ceiling(answer * multiplier) / multiplier;

        bool isExact = userValue == answer;
        bool isRoundedUp = userValue == expectedCeil;

        m_Messages.text = (isExact || isRoundedUp) ? "Respuesta Correcta!" : "Respuesta Incorrecta!";
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
        Overlord.overlord.CallScene("StartFlow");
    }
}