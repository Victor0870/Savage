
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StageFieldData
{
    public string background;
    public List<string> props;

    public static StageFieldData LoadFromJSON(string jsonPath)
    {
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            return JsonUtility.FromJson<StageFieldData>(json);
        }
        Debug.LogError("❌ Không tìm thấy file JSON: " + jsonPath);
        return null;
    }
}
