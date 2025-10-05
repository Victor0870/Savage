using System;
using System.Collections.Generic;

[Serializable]
public class MapData
{
    public float time;
    public string enemyType;
    public int amount;
    public string spawnMethod;
    public float spawnRate;
    public float spawnDuration;
}

[Serializable]
public class MapDataList
{
    public List<MapData> mapData;
    
}
