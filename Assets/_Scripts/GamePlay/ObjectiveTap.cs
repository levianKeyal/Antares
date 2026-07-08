using Fungus;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectiveTap : MonoBehaviour
{
    PlayerMovement player;
    FireCanonManager fireManager;

    [Header("Rotation")]
    public bool rotateTowardsPlayer = true;

    public float distanceToPlayer;

    [Header("Story Elements")]
    public GameObject fungusWarning;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        fireManager = FindFirstObjectByType<FireCanonManager>();
    }
    public void CalculateDistanceToPlayer()
    {
        distanceToPlayer = Vector3.Distance (transform.position, player.transform.position);
    }

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

        CalculateDistanceToPlayer();

        if (FindFirstObjectByType<TutorialManager>() != null)
        {
            if (distanceToPlayer <= fireManager.maxRange)
            {
                if (FindFirstObjectByType<TutorialManager>().tutoBlockNum == 2)
                {
                    Debug.Log("Tuto 3 active");
                    FindFirstObjectByType<TutorialManager>().CallTutoBlock();
                }
            }
            else if (distanceToPlayer > fireManager.maxRange)
            {
                fungusWarning.SetActive(true);
                Debug.Log(fireManager.maxRange);
                Debug.Log("Out of reach of Cannon");
            }
        }
        else if (FindFirstObjectByType<TutorialManager>() == null)
        {
            if (distanceToPlayer <= fireManager.maxRange)
            {
                Debug.Log(fireManager.maxRange);
                Debug.Log("In reach of Cannon");
            }
            else
            {
                fungusWarning.SetActive(true);
                Debug.Log(fireManager.maxRange);
                Debug.Log("Out of reach of Cannon");
            }
        }

        StartGamePlay.Instance.StartPhase1(
                gameObject,
                rotateTowardsPlayer
            );
    }
}