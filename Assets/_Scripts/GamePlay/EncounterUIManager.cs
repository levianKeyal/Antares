using UnityEngine;

public class EncounterUIManager : MonoBehaviour
{
    StartGamePlay startGamePlay;

    [Header("Gameplay UI Elements")]
    public RectTransform formulaUI;
    public RectTransform formulaSustituidaUI;
    public RectTransform cannonDialUI;

    [Header("Solve Mode Elements")]
    public RectTransform firstMatePictureSolveModeUI;
    public RectTransform answerPromptUI;
    public RectTransform answerInputFieldUI;
    public RectTransform angleUI;
    public RectTransform velocityUI;
    public RectTransform rangeUI;
    public RectTransform firebuttonCopyUI;

    [Header("Fungus Elements")]
    public RectTransform fungusPanel;
    public RectTransform firstMateImage;

    [Header("Tutorial Elements")]
    public RectTransform dialArrow;
    public RectTransform velocityArrow;
    public RectTransform shootArrow;

    private void Start()
    {
        startGamePlay = StartGamePlay.Instance;

        if (startGamePlay == null)
        {
            startGamePlay = FindAnyObjectByType<StartGamePlay>();
        }

        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.RegisterEncounterUIManager(this);
        }

        UpdateUIElements();
    }

    public void UpdateUIElements()
    {
        GameSettings settings = GameSettings.Instance;
        if (settings == null)
        {
            return;
        }

        if (settings.isPortrait)
        {
            formulaUI.anchoredPosition = new Vector2(0, -200f);
            formulaSustituidaUI.anchoredPosition = new Vector2(-70f, -400f);
            cannonDialUI.anchoredPosition = new Vector2(0, 375f);
            fungusPanel.anchoredPosition = new Vector2(-500f, 60f);
            firstMateImage.anchoredPosition = new Vector2(-317f, 724f);

            //Solve Mode Elements
            firstMatePictureSolveModeUI.anchoredPosition = new Vector2(351f,63f);
            answerPromptUI.anchoredPosition = new Vector2(-193.7747f,-211.9437f);
            answerInputFieldUI.anchoredPosition = new Vector2(305f, -211f);
            angleUI.anchoredPosition = new Vector2(-426.23f, 165f);
            velocityUI.anchoredPosition = new Vector2(-408.01f, 60.08698f);
            rangeUI.anchoredPosition = new Vector2(-408.01f, -56.17502f);
            firebuttonCopyUI.anchoredPosition = new Vector2(0f, 43f);

            //Tutorial Elements

            dialArrow.anchoredPosition = new Vector2(0f, -294f);
            velocityArrow.anchoredPosition = new Vector2(172f, -853f);
            shootArrow.anchoredPosition = new Vector2(172f, -853f);
        }
        else if (settings.isLandscape)
        {
            formulaUI.anchoredPosition = new Vector2(0, -75f);
            formulaSustituidaUI.anchoredPosition = new Vector2(-70f, -250f);
            cannonDialUI.anchoredPosition = new Vector2(0, 150f);
            fungusPanel.anchoredPosition = new Vector2(-500, 0f);
            firstMateImage.anchoredPosition = new Vector2(-500f, 724f);

            //Solve Mode Elements
            firstMatePictureSolveModeUI.anchoredPosition = new Vector2(700f,245f);
            answerPromptUI.anchoredPosition = new Vector2(-193.7747f,130f);
            answerInputFieldUI.anchoredPosition = new Vector2(305f, 130.9437f);
            angleUI.anchoredPosition = new Vector2(-841f, 306f);
            velocityUI.anchoredPosition = new Vector2(-821f, 200f);
            rangeUI.anchoredPosition = new Vector2(-821f, 85f);
            firebuttonCopyUI.anchoredPosition = new Vector2(0f, 342f);

            //Tutorial Elements

            dialArrow.anchoredPosition = new Vector2(0f, -220f);
            velocityArrow.anchoredPosition = new Vector2(-235f, -430f);
            shootArrow.anchoredPosition = new Vector2(600f, -430f);
        }

        if (startGamePlay != null && startGamePlay.encounterActive)
        {
            startGamePlay.ActivateBattleCamera();
        }
    }
}
