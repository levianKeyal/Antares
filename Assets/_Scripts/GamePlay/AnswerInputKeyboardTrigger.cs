using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class AnswerInputKeyboardTrigger : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] InGameAnswerKeyboard inGameAnswerKeyboard;
    [SerializeField] FireCanonManager fireCanonManager;
    [SerializeField] TMP_InputField answerInputField;

    void Awake()
    {
        CacheReferences();
        HookInputFieldEvents();
    }

    void CacheReferences()
    {
        if (fireCanonManager == null)
        {
            fireCanonManager = FindFirstObjectByType<FireCanonManager>();
        }

        if (inGameAnswerKeyboard == null && fireCanonManager != null)
        {
            inGameAnswerKeyboard = fireCanonManager.answerKeyboard;
        }

        if (answerInputField == null && fireCanonManager != null)
        {
            answerInputField = fireCanonManager.answerInputField;
        }
    }

    void HookInputFieldEvents()
    {
        if (answerInputField == null)
        {
            return;
        }

        answerInputField.onSelect.RemoveListener(HandleInputFieldSelected);
        answerInputField.onSelect.AddListener(HandleInputFieldSelected);
    }

    void HandleInputFieldSelected(string _)
    {
        ShowKeyboardIfAllowed();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ShowKeyboardIfAllowed();
    }

    void ShowKeyboardIfAllowed()
    {
        CacheReferences();

        if (fireCanonManager == null)
        {
            return;
        }

        if (fireCanonManager.physicsMode == CannonPhysicsMode.Tutorial)
        {
            return;
        }

        if (!fireCanonManager.CanOpenAnswerKeyboard())
        {
            return;
        }

        if (inGameAnswerKeyboard != null)
        {
            inGameAnswerKeyboard.ShowKeyboard();
            return;
        }

        fireCanonManager.ShowAnswerKeyboard();
    }
}
