using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    public static LevelProgressManager Instance;

    [SerializeField]
    private int correctAnswers = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void RegisterCorrectAnswer(DifficultyProfile profile)
    {
        if (profile == null)
            return;

        correctAnswers++;

        int required =
            profile.correctAnswersToUnlockNext;

        if (correctAnswers >= required)
        {
            UnlockNextLevel(profile);

            correctAnswers = 0;
        }
    }


    void UnlockNextLevel(DifficultyProfile profile)
    {
        if (!profile.unlockNextScene)
            return;

        if (string.IsNullOrEmpty(profile.nextSceneToUnlock))
            return;


        PlayerPrefs.SetInt(
            profile.nextSceneToUnlock + "_Unlocked",
            1
        );

#if UNITY_EDITOR
        Debug.Log(
            "Unlocked level: " +
            profile.nextSceneToUnlock
        );
#endif
    }


    public bool IsLevelUnlocked(string sceneName)
    {
        return PlayerPrefs.GetInt(sceneName + "_Unlocked", 0) == 1;
    }
}