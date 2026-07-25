using TMPro;
using UnityEngine;

public class InGameAnswerKeyboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] FireCanonManager fireCanonManager;
    [SerializeField] TMP_InputField answerInputField;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Keyboard Options")]
    [SerializeField] bool allowDecimalPoint = true;
    [SerializeField] float fadeDuration = 1f;

    Coroutine fadeRoutine;

    void Awake()
    {
        CacheReferences();
        HideMobileKeyboard();
        SetKeyboardStateImmediate(false);
    }

    void OnEnable()
    {
        CacheReferences();
        HideMobileKeyboard();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        SetKeyboardStateImmediate(false);
    }

    void CacheReferences()
    {
        if (fireCanonManager == null)
        {
            fireCanonManager = FindFirstObjectByType<FireCanonManager>();
        }

        if (answerInputField == null && fireCanonManager != null)
        {
            answerInputField = fireCanonManager.answerInputField;
        }
    }

    void HideMobileKeyboard()
    {
        if (answerInputField != null)
        {
            answerInputField.shouldHideMobileInput = true;
        }

        TouchScreenKeyboard.hideInput = true;
    }

    public void ShowKeyboard()
    {
        CacheReferences();
        HideMobileKeyboard();

        if (fireCanonManager != null && !fireCanonManager.CanOpenAnswerKeyboard())
        {
            return;
        }

        FadeTo(true);
    }

    public void HideKeyboard()
    {
        FadeTo(false);
    }

    public void PressDigit(string digit)
    {
        if (string.IsNullOrEmpty(digit))
        {
            return;
        }

        AppendText(digit);
    }

    public void PressDecimalPoint()
    {
        if (!allowDecimalPoint)
        {
            return;
        }

        if (answerInputField == null)
        {
            return;
        }

        if (answerInputField.text.Contains("."))
        {
            return;
        }

        if (string.IsNullOrEmpty(answerInputField.text))
        {
            AppendText("0.");
            return;
        }

        AppendText(".");
    }

    public void PressBackspace()
    {
        if (fireCanonManager == null)
        {
            return;
        }

        fireCanonManager.BackspaceAnswerInputText();
    }

    public void PressClear()
    {
        if (fireCanonManager == null)
        {
            return;
        }

        fireCanonManager.ClearAnswerInputText();
    }

    public void PressEnter()
    {
        if (fireCanonManager == null)
        {
            return;
        }

        fireCanonManager.SyncUserAnswerFromInputField();
        fireCanonManager.FireButtonAfterDelay(1f);
    }

    void FadeTo(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (visible && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(visible));
    }

    void SetKeyboardStateImmediate(bool visible)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    System.Collections.IEnumerator FadeRoutine(bool visible)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        fadeRoutine = null;
    }

    void AppendText(string value)
    {
        if (fireCanonManager == null)
        {
            return;
        }

        if (answerInputField == null)
        {
            return;
        }

        fireCanonManager.AppendAnswerInputText(value);
    }
}
