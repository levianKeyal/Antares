using UnityEngine;

public class CannonBallTapArea : MonoBehaviour
{
    CannonBall cannonBall;

    public void SetOwner(CannonBall owner)
    {
        cannonBall = owner;
    }

    void Awake()
    {
        if (cannonBall == null)
        {
            cannonBall = GetComponentInParent<CannonBall>();
        }
    }

    void OnMouseDown()
    {
        if (cannonBall == null)
        {
            cannonBall = GetComponentInParent<CannonBall>();
        }

        if (cannonBall != null)
        {
            cannonBall.HandleTapRequested();
        }
    }
}
