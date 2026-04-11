using UnityEngine;

[CreateAssetMenu(
    fileName = "DifficultyProfile",
    menuName = "Math Generator/Difficulty Profile"
)]
public class DifficultyProfile : ScriptableObject
{
    [Header("Curriculum Metadata (Future Use)")]

    public DifficultyPreset presetTier;

    [Range(1, 6)]
    public int presetLevel = 1;

    [Header("Campaign Progression")]

    [Tooltip("Correct answers required to unlock next level")]
    public int correctAnswersToUnlockNext = 10;

    [Tooltip("Next scene name to unlock")]
    public string nextSceneToUnlock;
    public bool unlockNextScene = true;

    [Header("Allowed Operations")]

    public bool allowAddition = true;

    public bool allowSubtraction = false;

    public bool allowMultiplication = false;

    public bool allowDivision = false;

    [Header("Validation Settings")]
    public ValidationMode validationMode;

    [Range(0, 6)]
    public int validationDecimals;

    public NumberSignMode signMode;


    [Header("Addition/Subtraction")]
    [Range(1, 5)]
    public int addSubIntegerDigits;

    [Range(0, 6)]
    public int addSubDecimalDigits;


    [Header("Multiplication")]
    [Range(1, 5)]
    public int multiplicationIntegerDigits;

    [Range(0, 6)]
    public int multiplicationDecimalDigits;


    [Header("Division")]
    [Range(1, 5)]
    public int divisionIntegerDigits;

    [Range(0, 6)]
    public int divisionDecimalDigits;
}