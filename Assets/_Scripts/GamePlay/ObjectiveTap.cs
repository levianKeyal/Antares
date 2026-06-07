using Fungus;
using UnityEngine;

public class ObjectiveTap : MonoBehaviour
{
    [Header("Rotation")]
    public bool rotateTowardsPlayer = true;

    public void OnObjectiveTapped()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        if (GameSettings.Instance.cinematicPause)
            return;

        // ====================================
        // INTERACTION BLOCKED
        // ====================================

        if (GameSettings.Instance.interactionBlocked)
            return;

        StartGamePlay.Instance.StartPhase1(
            gameObject,
            rotateTowardsPlayer
        );
    }
}