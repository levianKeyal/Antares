using UnityEngine;
using TMPro;

public class RoundUpTest : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_number1;
    private float number1;
    [SerializeField]
    private TMP_Text m_number2;
    private float number2;
    [SerializeField]
    private TMP_InputField m_answer;
    private float answer;
    [SerializeField]
    private TMP_Text m_Messages;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateRandomNumbers();
    }

    void GenerateRandomNumbers()
    {
        number1 = Random.Range(0f, 100f);
        float truncated1 = Mathf.Floor(number1 * 100f) / 100f;
        m_number1.text = truncated1.ToString();

        number2 = Random.Range(0f, 100f);
        float truncated2 = Mathf.Floor(number2 * 100f) / 100f;
        m_number2.text = truncated2.ToString();

        answer = truncated1 + truncated2;
        Debug.Log(answer);
    }

    public void CheckForCorrectAnswer()
    {
        float value;

        if(float.TryParse (m_answer.text, out value))
        {
            Debug.Log("Valor ingresado: " + value);
        }

        if(value == answer)
        {
            m_Messages.text = ("Respuesta Correcta!");
            Debug.Log("Correct answer");
        }
        else
        {
            m_Messages.text = ("Respuesta Incorrecta!");
            Debug.Log("Incorrect answer");
        }
    }
    public void Reset()
    {
        GenerateRandomNumbers();
        m_answer.text = null;
        m_Messages.text = ("Esperando Respuesta");
    }
}
