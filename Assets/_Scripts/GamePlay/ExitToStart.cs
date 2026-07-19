using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToStart : MonoBehaviour
{
    public void ExitToStartScene()
    {        
        SceneManager.LoadScene("StartFlowPirates");
    }
}
