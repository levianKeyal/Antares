using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public LevelManager levelManager;
    public int enemyLife;
    public GameObject destroyedFx;

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
    }
    public void OnCannonBallHit()
    {
        enemyLife -= 1;
        CheckForLife();
    }

    void CheckForLife()
    {
        if (enemyLife == 0)
        {
            Debug.Log("Enemy Defeated!");

            if(levelManager.enemies.Contains(this.gameObject))
            {
                levelManager.enemies.Remove(this.gameObject);
            }

            levelManager.UpdateUI();

            Instantiate(destroyedFx);

            if (FindFirstObjectByType<TutorialManager>() != null)
            {
                FindFirstObjectByType<TutorialManager>().CallTutoBlock();
            }

            this.gameObject.SetActive(false);

        }
    }
}
