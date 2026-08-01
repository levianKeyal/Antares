using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitToStart : MonoBehaviour
{
    [SerializeField] string startSceneName = "StartFlowPirates";
    [SerializeField] Button exitButton;

    bool isCameraTransitionLocked;

    void Awake()
    {
        if (exitButton == null)
        {
            exitButton = GetComponent<Button>();
        }
    }

    public void ExitToStartScene()
    {
        if (isCameraTransitionLocked)
        {
            return;
        }

        StartGamePlay startGamePlay = StartGamePlay.Instance;

        if (startGamePlay != null && startGamePlay.battleTransitionActive)
        {
            return;
        }

        GameSettings settings = GameSettings.Instance;

        if (settings != null && settings.cannonBallViewActive)
        {
            CannonBall cannonBall =
                FindFirstObjectByType<CannonBall>();

            if (cannonBall != null)
            {
                cannonBall.ExitCannonBallView();
                return;
            }

            settings.cannonBallViewActive = false;
        }

        if (startGamePlay != null && startGamePlay.encounterActive)
        {
            startGamePlay.ClearEncounter();
            return;
        }

        if (settings != null)
        {
            settings.CallScene(startSceneName);
            return;
        }

        SceneManager.LoadScene(startSceneName);
    }

    public void SetTransitionLocked(bool locked)
    {
        SetButtonInteractable(!locked);
    }

    void SetButtonInteractable(bool interactable)
    {
        isCameraTransitionLocked = !interactable;

        if (exitButton != null)
        {
            exitButton.interactable = interactable;
        }
    }

    void OnDisable()
    {
        isCameraTransitionLocked = false;

        if (exitButton != null)
        {
            exitButton.interactable = true;
        }
    }
}
