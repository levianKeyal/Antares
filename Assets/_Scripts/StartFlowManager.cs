using UnityEngine;
using UnityEngine.UI;

public class StartFlowManager : MonoBehaviour
{
    public Button scene1;
    public Button scene2;

    private void Awake()
    {
        scene1.onClick.AddListener(delegate { Overlord.overlord.CallScene("RoundUpTests"); });
        scene2.onClick.AddListener(delegate { Overlord.overlord.CallScene("RoundUpDecimal"); });
    }
}
