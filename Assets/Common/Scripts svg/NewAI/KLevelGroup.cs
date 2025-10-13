using System.Collections.Generic;

[System.Serializable]
public class KLevelGroup
{
    public int min;
    public int max;
    public float K;
}

[System.Serializable]
public class KLevelGroupList
{
    public List<KLevelGroup> KLevelGroups;
}