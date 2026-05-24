using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject>();

    [Header("UI")]
    public TMP_Text enemiesLeft;    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        UpdateUI();
    }

    // Update is called once per frame
    public void UpdateUI()
    {
        enemiesLeft.text = enemies.Count.ToString();
    }
}
