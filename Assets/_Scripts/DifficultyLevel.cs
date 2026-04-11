
[System.Serializable]
public struct DifficultyLevel
{
    public DifficultyPreset tier;
    public int level;

    public DifficultyLevel(DifficultyPreset tier, int level)
    {
        this.tier = tier;
        this.level = level;
    }
}