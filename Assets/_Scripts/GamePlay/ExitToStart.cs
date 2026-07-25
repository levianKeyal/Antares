using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToStart : MonoBehaviour
{
    [SerializeField] string startSceneName = "StartFlowPirates";

    public void ExitToStartScene()
    {
        StartGamePlay startGamePlay = StartGamePlay.Instance;

        if (startGamePlay != null && startGamePlay.encounterActive)
        {
            startGamePlay.ClearEncounter();
            return;
        }

        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.CallScene(startSceneName);
            return;
        }

        SceneManager.LoadScene(startSceneName);
    }
}
