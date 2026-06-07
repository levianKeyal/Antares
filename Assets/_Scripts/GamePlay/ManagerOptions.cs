using UnityEngine;

public class ManagerOptions : MonoBehaviour
{
    public void PauseGame()
    {
        GameSettings.Instance.StartCinematicPause();
    }

    public void UnpauseGame()
    {
        GameSettings.Instance.EndCinematicPause();
    }

    public void CantTouchThis()
    {
        GameSettings.Instance.interactionBlocked = true;
    }
    public void TouchThis()
    {
        GameSettings.Instance.interactionBlocked = false;
    }
}
