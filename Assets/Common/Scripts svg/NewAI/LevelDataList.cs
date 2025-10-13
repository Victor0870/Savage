using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    public int level;
    public int globalExp;
}

[System.Serializable]
public class LevelDataList
{
    public List<LevelData> levels;
}
