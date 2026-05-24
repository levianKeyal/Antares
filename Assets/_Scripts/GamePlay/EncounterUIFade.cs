using UnityEngine;

public class EncounterUIFade : MonoBehaviour
{
    [Header("Canvas Group")]
    public CanvasGroup canvasGroup;

    [Header("Fade")]
    public float fadeSpeed = 5f;

    [Header("Interaction")]
    public bool disableInteractionWhenHidden = true;

    bool previousEncounterState;

    // ====================================
    // UNITY
    // ====================================

    void Start()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        // START HIDDEN
        canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (StartGamePlay.Instance == null)
            return;

        bool encounterActive =
            StartGamePlay.Instance
                .encounterActive;

        // ====================================
        // ENTER ENCOUNTER
        // ====================================

        if (
            encounterActive
            &&
            !previousEncounterState
        )
        {
            gameObject.SetActive(true);
        }

        // ====================================
        // UPDATE FADE
        // ====================================

        float targetAlpha =
            encounterActive ? 1f : 0f;

        canvasGroup.alpha =
            Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                fadeSpeed *
                Time.deltaTime
            );

        // ====================================
        // INTERACTION
        // ====================================

        bool visible =
            canvasGroup.alpha > 0.95f;

        SetInteraction(visible);

        // ====================================
        // EXIT ENCOUNTER
        // ====================================

        if (
            !encounterActive
            &&
            canvasGroup.alpha <= 0f
        )
        {
            gameObject.SetActive(false);
        }

        previousEncounterState =
            encounterActive;
    }

    // ====================================
    // INTERACTION
    // ====================================

    void SetInteraction(bool value)
    {
        if (!disableInteractionWhenHidden)
            return;

        canvasGroup.interactable =
            value;

        canvasGroup.blocksRaycasts =
            value;
    }
}