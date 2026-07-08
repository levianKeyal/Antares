using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public int playerLife = 3;

    public GameObject selfWarning1;
    public GameObject selfWarning2;
    public GameObject selfWarning3;

    [Header("Player Models")]
    public GameObject boat;
    public GameObject cannon;

    public void CheckForPlayerLife()
    {
        if(playerLife == 0)
        {
            boat.SetActive(false);
            cannon.SetActive(false);
            FindFirstObjectByType<FireCanonManager>().showTrajectory = false;
        }
    }

    public void SelfHarm()
    {
        Debug.Log("It hurts itself in its confusion!");
        

        switch(playerLife)
        {
            case 3:
                selfWarning1.SetActive(true);
                playerLife--;
                CheckForPlayerLife();
                break;

            case 2:
                selfWarning2.SetActive(true);
                playerLife--;
                CheckForPlayerLife();
                break;

            case 1:
                selfWarning3.SetActive(true);
                playerLife--;
                CheckForPlayerLife();
                break;
        }

    }
}
