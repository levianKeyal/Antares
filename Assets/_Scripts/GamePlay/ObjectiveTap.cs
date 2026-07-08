using Fungus;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectiveTap : MonoBehaviour
{
    PlayerMovement player;
    FireCanonManager fireManager;
    TutorialManager tutorialManager;

    [Header("Rotation")]
    public bool rotateTowardsPlayer = true;

    public float distanceToPlayer;

    [Header("Story Elements")]
    public GameObject fungusWarning;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        fireManager = FindFirstObjectByType<FireCanonManager>();
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void CalculateDistanceToPlayer()
    {
        if (player == null)
        {
            distanceToPlayer = float.MaxValue;
            return;
        }

        Vector3 playerPosition = player.transform.position;
        Vector3 objectivePosition = transform.position;

        playerPosition.y = 0f;
        objectivePosition.y = 0f;

        distanceToPlayer = Vector3.Distance(objectivePosition, playerPosition);
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

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
        }

        if (fireManager == null)
        {
            fireManager = FindFirstObjectByType<FireCanonManager>();
        }

        if (tutorialManager == null)
        {
            tutorialManager = FindFirstObjectByType<TutorialManager>();
        }

        if (player == null || fireManager == null)
        {
            Debug.LogWarning("ObjectiveTap: missing player or cannon manager reference.");
            return;
        }

        CalculateDistanceToPlayer();

        bool inRange = distanceToPlayer <= fireManager.maxRange;

        if (inRange)
        {
            Debug.Log("in reach of cannon");

            if (fungusWarning != null)
            {
                fungusWarning.SetActive(false);
            }

            if (tutorialManager != null && tutorialManager.tutoBlockNum == 2)
            {
                Debug.Log("Tuto 3 active");
                tutorialManager.CallTutoBlock();
            }

            StartGamePlay.Instance.StartPhase1(
                gameObject,
                rotateTowardsPlayer
            );
            return;
        }

        if (fungusWarning != null)
        {
            fungusWarning.SetActive(true);
        }

        Debug.Log("out of reach of cannon");
    }
}