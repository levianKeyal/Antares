using UnityEngine;
using UnityEngine.SceneManagement;

public class Overlord : MonoBehaviour
{
    public static Overlord overlord;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (!overlord)
        {
            overlord = this;
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