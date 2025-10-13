using UnityEngine;

public static class JSONLoader
{
    public static T LoadFromResources<T>(string fileName) where T : class
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null)
        {
            Debug.LogError($"❌ Không tìm thấy file JSON trong Resources: {fileName}");
            return null;
        }

        return JsonUtility.FromJson<T>(jsonFile.text);
    }
}
