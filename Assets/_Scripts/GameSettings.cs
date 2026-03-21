using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    public int decimals = 2;
    public ValidationMode validationMode = ValidationMode.ExactOnly;

    public NumberSignMode numberSignMode = NumberSignMode.PositiveOnly;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    public void CallScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}