using System.Text;
using TMPro;
using UnityEngine;

public class PirateCombatDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] StartGamePlay startGamePlay;
    [SerializeField] FireCanonManager fireCanonManager;

    [Header("UI")]
    [SerializeField] TMP_Text debugText;
    [SerializeField] bool showOnScreenDebug = true;
    [SerializeField] Vector2 guiSize = new Vector2(420f, 220f);

    [Header("Drawing")]
    [SerializeField] bool drawWorldGizmos = true;
    [SerializeField] Color enemyLineColor = Color.red;
    [SerializeField] Color rangeColor = Color.cyan;
    [SerializeField] float rangeCircleHeightOffset = 0.15f;

    readonly StringBuilder debugBuilder = new StringBuilder(256);
    string cachedDebugText = string.Empty;

    void Start()
    {
        CacheReferences();
    }

    void Update()
    {
        CacheReferences();
        RebuildDebugData();
    }

    void OnGUI()
    {
        if (!showOnScreenDebug || string.IsNullOrEmpty(cachedDebugText))
        {
            return;
        }

        GUI.Box(new Rect(10f, 10f, guiSize.x, guiSize.y), cachedDebugText);
    }

    void OnDrawGizmos()
    {
        if (!drawWorldGizmos)
        {
            return;
        }

        CacheReferences();

        Transform rangeOriginTransform = GetRangeOriginTransform();
        Transform selectedEnemyTransform = GetSelectedEnemyTransform();

        if (rangeOriginTransform == null || selectedEnemyTransform == null)
        {
            return;
        }

        Vector3 rangeOriginPosition = rangeOriginTransform.position;
        Vector3 enemyCenter = GetEnemyCenter(selectedEnemyTransform);

        float maxRange = fireCanonManager != null
            ? fireCanonManager.maxRange
            : 0f;

        Vector3 originDebugPosition = rangeOriginPosition + Vector3.up * rangeCircleHeightOffset;
        Vector3 enemyDebugPosition = enemyCenter + Vector3.up * rangeCircleHeightOffset;

        Gizmos.color = rangeColor;
        DrawRangeCircle(rangeOriginPosition, maxRange);

        Gizmos.color = enemyLineColor;
        Gizmos.DrawLine(originDebugPosition, enemyDebugPosition);
        Gizmos.DrawSphere((originDebugPosition + enemyDebugPosition) * 0.5f, 0.12f);
    }

    void CacheReferences()
    {
        if (startGamePlay == null)
        {
            startGamePlay = FindFirstObjectByType<StartGamePlay>();
        }

        if (fireCanonManager == null)
        {
            fireCanonManager = FindFirstObjectByType<FireCanonManager>();
        }
    }

    void RebuildDebugData()
    {
        Transform rangeOriginTransform = GetRangeOriginTransform();
        Transform selectedEnemyTransform = GetSelectedEnemyTransform();

        if (rangeOriginTransform == null)
        {
            cachedDebugText = "Pirate combat debug: missing CannonMuzzle reference.";
            PushTextToUI();
            return;
        }

        if (selectedEnemyTransform == null)
        {
            cachedDebugText = "Pirate combat debug: missing selected enemy reference.";
            PushTextToUI();
            return;
        }

        if (fireCanonManager == null)
        {
            cachedDebugText = "Pirate combat debug: missing cannon reference.";
            PushTextToUI();
            return;
        }

        Vector3 enemyCenter = GetEnemyCenter(selectedEnemyTransform);
        float distance = Vector3.Distance(rangeOriginTransform.position, enemyCenter);
        float maxRange = fireCanonManager.maxRange;

        debugBuilder.Clear();
        debugBuilder.AppendLine("Pirate Combat Debug");
        debugBuilder.Append("Origin: CannonMuzzle");
        debugBuilder.AppendLine();
        debugBuilder.Append("Selected Enemy Distance: ");
        debugBuilder.Append(distance.ToString("F2"));
        debugBuilder.Append(distance <= maxRange ? " m (in range)" : " m (out of range)");
        debugBuilder.AppendLine();
        debugBuilder.Append("Max Range: ");
        debugBuilder.Append(maxRange.ToString("F2"));
        debugBuilder.AppendLine(" m");

        cachedDebugText = debugBuilder.ToString();
        PushTextToUI();
    }

    void PushTextToUI()
    {
        if (showOnScreenDebug && debugText != null)
        {
            debugText.text = cachedDebugText;
        }
    }

    Transform GetSelectedEnemyTransform()
    {
        if (startGamePlay != null && startGamePlay.currentObjective != null)
        {
            return startGamePlay.currentObjective.transform;
        }

        return null;
    }

    Transform GetRangeOriginTransform()
    {
        if (fireCanonManager != null && fireCanonManager.cannonMuzzle != null)
        {
            return fireCanonManager.cannonMuzzle;
        }

        return null;
    }

    Vector3 GetEnemyCenter(Transform enemy)
    {
        if (enemy == null)
        {
            return Vector3.zero;
        }

        Collider enemyCollider = enemy.GetComponent<Collider>();
        if (enemyCollider != null)
        {
            return enemyCollider.bounds.center;
        }

        Collider childCollider = enemy.GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }

        Renderer enemyRenderer = enemy.GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            return enemyRenderer.bounds.center;
        }

        Renderer childRenderer = enemy.GetComponentInChildren<Renderer>();
        if (childRenderer != null)
        {
            return childRenderer.bounds.center;
        }

        return enemy.position;
    }

    void DrawRangeCircle(Vector3 center, float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        const int segments = 48;
        Vector3 previousPoint = center + new Vector3(radius, rangeCircleHeightOffset, 0f);
        float angleStep = 360f / segments;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                rangeCircleHeightOffset,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
