using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PirateCombatDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] StartGamePlay startGamePlay;
    [SerializeField] FireCanonManager fireCanonManager;
    [SerializeField] LevelManager levelManager;

    [Header("UI")]
    [SerializeField] TMP_Text debugText;
    [SerializeField] bool showOnScreenDebug = true;
    [SerializeField] Vector2 guiSize = new Vector2(420f, 220f);

    [Header("Drawing")]
    [SerializeField] bool drawWorldGizmos = true;
    [SerializeField] Color enemyLineColor = Color.red;
    [SerializeField] Color rangeColor = Color.cyan;
    [SerializeField] float rangeCircleHeightOffset = 0.15f;
    [SerializeField] int rangeCircleSegments = 48;

    readonly StringBuilder debugBuilder = new StringBuilder(256);
    string cachedDebugText = string.Empty;
    readonly List<GameObject> currentEnemies = new List<GameObject>(3);

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
        RebuildDebugData();

        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        float maxRange = fireCanonManager != null
            ? fireCanonManager.maxRange
            : 0f;

        Vector3 playerPosition = playerTransform.position;
        Vector3 playerDebugPosition = playerPosition + Vector3.up * rangeCircleHeightOffset;

        Gizmos.color = rangeColor;
        DrawRangeCircle(playerPosition, maxRange);

        Vector3 flatPlayerPosition = Flatten(playerPosition);
        int visibleEnemyIndex = 0;

        for (int i = 0; i < currentEnemies.Count && visibleEnemyIndex < 3; i++)
        {
            GameObject enemy = currentEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                continue;
            }

            Vector3 enemyPosition = enemy.transform.position;
            Vector3 enemyDebugPosition = enemyPosition + Vector3.up * rangeCircleHeightOffset;

            float distance = Vector3.Distance(flatPlayerPosition, Flatten(enemyPosition));
            Gizmos.color = enemyLineColor;
            Gizmos.DrawLine(playerDebugPosition, enemyDebugPosition);

            Vector3 midpoint = (playerDebugPosition + enemyDebugPosition) * 0.5f;
            Gizmos.DrawSphere(midpoint, 0.12f);

            visibleEnemyIndex++;
        }
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

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
    }

    void RebuildDebugData()
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            cachedDebugText = "Pirate combat debug: missing player reference.";
            PushTextToUI();
            return;
        }

        currentEnemies.Clear();
        currentEnemies.AddRange(GetEnemies());

        if (fireCanonManager == null)
        {
            cachedDebugText = "Pirate combat debug: missing cannon reference.";
            PushTextToUI();
            return;
        }

        float maxRange = fireCanonManager.maxRange;
        Vector3 flatPlayerPosition = Flatten(playerTransform.position);

        debugBuilder.Clear();
        debugBuilder.AppendLine("Pirate Combat Debug");
        debugBuilder.Append("Max Range: ");
        debugBuilder.Append(maxRange.ToString("F2"));
        debugBuilder.AppendLine(" m");

        int visibleEnemyIndex = 0;
        for (int i = 0; i < currentEnemies.Count && visibleEnemyIndex < 3; i++)
        {
            GameObject enemy = currentEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.Distance(
                flatPlayerPosition,
                Flatten(enemy.transform.position)
            );

            debugBuilder.Append("Enemy ");
            debugBuilder.Append(visibleEnemyIndex + 1);
            debugBuilder.Append(": ");
            debugBuilder.Append(distance.ToString("F2"));
            debugBuilder.Append(distance <= maxRange ? " m (in range)" : " m (out of range)");
            debugBuilder.AppendLine();

            visibleEnemyIndex++;
        }

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

    Transform GetPlayerTransform()
    {
        if (startGamePlay != null && startGamePlay.player != null)
        {
            return startGamePlay.player.transform;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            return playerObject.transform;
        }

        return null;
    }

    List<GameObject> GetEnemies()
    {
        if (levelManager != null && levelManager.enemies != null && levelManager.enemies.Count > 0)
        {
            return levelManager.enemies;
        }

        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        return new List<GameObject>(enemyObjects);
    }

    Vector3 Flatten(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    void DrawRangeCircle(Vector3 center, float radius)
    {
        if (radius <= 0f || rangeCircleSegments < 3)
        {
            return;
        }

        Vector3 previousPoint = center + new Vector3(radius, rangeCircleHeightOffset, 0f);
        float angleStep = 360f / rangeCircleSegments;

        for (int i = 1; i <= rangeCircleSegments; i++)
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