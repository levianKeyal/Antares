using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public int tutoBlockNum = 0;

    public GameObject tutoBlock1;
    public GameObject tutoBlock2;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CallTutoBlock();
    }

    public void AddToTutoBlock()
    {
        tutoBlockNum++;
    }

    void CallTutoBlock()
    {
        switch(tutoBlockNum)
        {
            case 0:
                tutoBlock1.SetActive(true);
                break;

            case 1:
                tutoBlock2.SetActive(true);
                break;
        }
    }
}
