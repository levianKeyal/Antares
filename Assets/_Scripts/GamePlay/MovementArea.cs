using UnityEngine;

public class MovementArea : MonoBehaviour
{
    [Header("Area")]
    public float radius = 20f;

    public Vector3 GetClosestPoint(Vector3 position)
    {
        Vector3 center = transform.position;

        Vector3 offset =
            position - center;

        offset.y = 0;

        if (offset.magnitude > radius)
        {
            offset =
                offset.normalized * radius;
        }

        return center + offset;
    }

    public bool IsInsideArea(Vector3 position)
    {
        Vector3 flatPosition = position;
        flatPosition.y = transform.position.y;

        return
            Vector3.Distance(
                flatPosition,
                transform.position
            ) <= radius;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}