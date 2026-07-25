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

    public float distanceToTarget;

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
        if (fireManager == null || fireManager.cannonMuzzle == null)
        {
            distanceToTarget = float.MaxValue;
            return;
        }

        Vector3 cannonMuzzlePosition = fireManager.cannonMuzzle.position;
        Vector3 objectivePosition = GetObjectiveCenter();

        cannonMuzzlePosition.y = 0f;
        objectivePosition.y = 0f;

        distanceToTarget = Vector3.Distance(objectivePosition, cannonMuzzlePosition);
    }

    Vector3 GetObjectiveCenter()
    {
        Collider objectiveCollider = GetComponent<Collider>();
        if (objectiveCollider != null)
        {
            return objectiveCollider.bounds.center;
        }

        Collider childCollider = GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }

        Renderer objectiveRenderer = GetComponent<Renderer>();
        if (objectiveRenderer != null)
        {
            return objectiveRenderer.bounds.center;
        }

        Renderer childRenderer = GetComponentInChildren<Renderer>();
        if (childRenderer != null)
        {
            return childRenderer.bounds.center;
        }

        return transform.position;
    }

    public void OnObjectiveTapped()
    {
        if (StartGamePlay.Instance != null && StartGamePlay.Instance.encounterActive)
            return;

        if (GameSettings.Instance.cinematicPause)
            return;

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

        Vector3 targetCenter = GetObjectiveCenter();

        fireManager.PrepareChallengeFromEnemy(
            player.transform.position,
            targetCenter
        );

        bool inRange = distanceToTarget <= fireManager.maxRange;

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
