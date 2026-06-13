//using Unity.VisualScripting;
using UnityEngine;

public class EncounterUIManager : MonoBehaviour
{
    StartGamePlay startGamePlay;
    public RectTransform formulaUI;
    public RectTransform formulaSustituidaUI;
    public RectTransform cannonDialUI;

    [Header("Fungus Elements")]
    public RectTransform fungusPanel;
    public RectTransform firstMateImage; 


    private void Start()
    {
        startGamePlay = FindAnyObjectByType<StartGamePlay>();
        UpdateUIElements();
    }

    public void UpdateUIElements()
    {
        if (GameSettings.Instance.isPortrait)
        {
            formulaUI.anchoredPosition = new Vector2(0, -200f);
            formulaSustituidaUI.anchoredPosition = new Vector2(-70f, -400f);
            cannonDialUI.anchoredPosition = new Vector2(0, 375f);
            fungusPanel.anchoredPosition = new Vector2(-500f, 60f);
            firstMateImage.anchoredPosition = new Vector2(-317f, 724f);

            if(startGamePlay.encounterActive)
            {
                startGamePlay.ActivateBattleCamera();
            }
        }
        else if (GameSettings.Instance.isLandscape)
        {
            formulaUI.anchoredPosition = new Vector2(0, -75f);
            formulaSustituidaUI.anchoredPosition = new Vector2(-70f, -250f);
            cannonDialUI.anchoredPosition = new Vector2(0, 150f);
            fungusPanel.anchoredPosition = new Vector2(-500, 0f);
            firstMateImage.anchoredPosition = new Vector2(-500f, 724f);

            if (startGamePlay.encounterActive)
            {
                startGamePlay.ActivateBattleCamera();
            }
        }
    }
}
