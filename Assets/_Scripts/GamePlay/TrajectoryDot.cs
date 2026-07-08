using UnityEngine;

public class TrajectoryDot : MonoBehaviour
{
    Camera _camera;
    [HideInInspector]
    public FireCanonManager manager;

    SpriteRenderer spriteRenderer;

    Vector3 initialScale;

    int hierarchyIndex;

    // ====================================
    // UNITY
    // ====================================

    void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        initialScale =
            transform.localScale;
    }

    private void Start()
    {
        _camera = Camera.main;
    }

    void OnEnable()
    {
        hierarchyIndex =
            transform.GetSiblingIndex();
    }

    void Update()
    {
        AnimateScale();

        AnimateSequentialFade();

        transform.LookAt(_camera.transform);
    }

    void AnimateScale()
    {
        if (
            manager == null
            || !manager.enableDotPulse
        )
        {
            return;
        }

        float wave =
            Mathf.Sin(
                Time.time *
                manager.dotPulseSpeed
            );

        wave =
            Mathf.InverseLerp(
                -1f,
                1f,
                wave
            );

        float scale =
            Mathf.Lerp(
                manager.dotMinScale,
                manager.dotMaxScale,
                wave
            );

        transform.localScale =
            initialScale * scale;
    }

    void AnimateSequentialFade()
    {
        if (
            manager == null
            || !manager.enableDotFade
            || spriteRenderer == null
        )
        {
            return;
        }

        float wave =
            Mathf.Sin(
                (
                    Time.time *
                    manager.dotFadeSpeed
                )
                -
                (
                    hierarchyIndex *
                    manager.dotFadeOffset
                )
            );

        wave =
            Mathf.InverseLerp(
                -1f,
                1f,
                wave
            );

        float alpha =
            Mathf.Lerp(
                manager.dotMinAlpha,
                manager.dotMaxAlpha,
                wave
            );

        Color color =
            spriteRenderer.color;

        color.a = alpha;

        spriteRenderer.color =
            color;
    }
}