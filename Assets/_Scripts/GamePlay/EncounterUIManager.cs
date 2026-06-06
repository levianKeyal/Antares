using UnityEngine;

public class EncounterUIManager : MonoBehaviour
{
    StartGamePlay startGamePlay;
    public RectTransform formulaUI;
    public RectTransform formulaSustituidaUI;
    public RectTransform cannonDialUI;

    private void Start()
    {
        startGamePlay = FindAnyObjectByType<StartGamePlay>();
    }

    public void UpdateUIElements()
    {
        if (GameSettings.Instance.isPortrait)
        {
            formulaUI.anchoredPosition = new Vector2(0, -200f);
            formulaSustituidaUI.anchoredPosition = new Vector2(-70f, -400f);
            cannonDialUI.anchoredPosition = new Vector2(0, 375f);

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

            if (startGamePlay.encounterActive)
            {
                startGamePlay.ActivateBattleCamera();
            }
        }
    }
}
