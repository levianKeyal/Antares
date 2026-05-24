using UnityEngine;

public class ObjectiveTap : MonoBehaviour
{
    [Header("Rotation")]
    public bool rotateTowardsPlayer = true;

    public void OnObjectiveTapped()
    {
        StartGamePlay.Instance.StartPhase1(
            gameObject,
            rotateTowardsPlayer
        );
    }
}