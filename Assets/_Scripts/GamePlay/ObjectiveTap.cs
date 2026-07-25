using Fungus;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectiveTap : MonoBehaviour
{
    PlayerMovement player;
    FireCanonManager fireManager;
    TutorialManager tutorialManager;
    Collider objectiveCollider;
    Collider childCollider;
    Renderer objectiveRenderer;
    Renderer childRenderer;

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
        CacheObjectiveCenterReferences();
    }

    void CacheObjectiveCenterReferences()
    {
        objectiveCollider = GetComponent<Collider>();
        if (objectiveCollider == null)
        {
            childCollider = GetComponentInChildren<Collider>();
        }

        objectiveRenderer = GetComponent<Renderer>();
        if (objectiveRenderer == null)
        {
            childRenderer = GetComponentInChildren<Renderer>();
        }
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
        if (objectiveCollider != null)
        {
            return objectiveCollider.bounds.center;
        }

        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }

        if (objectiveRenderer != null)
        {
            return objectiveRenderer.bounds.center;
        }

        if (childRenderer != null)
        {
            return childRenderer.bounds.center;
        }

        return transform.position;
    }

    public void OnObjectiveTapped()
    {
        StartGamePlay startGamePlay = StartGamePlay.Instance;
        GameSettings settings = GameSettings.Instance;

        if (startGamePlay != null && startGamePlay.encounterActive)
            return;

        if (settings != null && settings.cinematicPause)
            return;

        if (settings != null && settings.interactionBlocked)
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

        Vector3 targetCenter = GetObjectiveCenter();
        Vector3 cannonMuzzlePosition = fireManager.cannonMuzzle.position;

        cannonMuzzlePosition.y = 0f;
        targetCenter.y = 0f;

        distanceToTarget =
            Vector3.Distance(targetCenter, cannonMuzzlePosition);

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

            if (startGamePlay != null)
            {
                startGamePlay.StartPhase1(
                gameObject,
                rotateTowardsPlayer
            );
            }
            return;
        }

        if (fungusWarning != null)
        {
            fungusWarning.SetActive(true);
        }

        Debug.Log("out of reach of cannon");
    }
}
