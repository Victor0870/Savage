using OctoberStudio;
using UnityEngine;
using UnityEngine.Playables;

public class StageLoadManager : MonoBehaviour
{
    public StageData stageData; // Dữ liệu màn chơi từ ScriptableObject
    public int loadMode = 0; // 0 = Playable Timeline, 1 = JSON

    void Start()
    {
        if (stageData == null)
        {
            Debug.LogError("❌ StageData is missing in StageLoadManager!");
            return;
        }

        if (loadMode == 1)
        {
            LoadUsingJSON();
            
        }
        else
        {
            LoadUsingPlayable();
            //Debug.LogError("❌ StageData is missing in StageLoadManager!             aaaaaaaaaaaaaaaaa");
        }
    }

    void LoadUsingPlayable()
    {
        if (stageData.Timeline != null)
        {
            Debug.Log("🎬 Loading stage using Playable Timeline...");
            PlayableDirector director = gameObject.AddComponent<PlayableDirector>();
            director.playableAsset = stageData.Timeline;
            director.Play();
        }
        else
        {
            Debug.LogError("❌ Playable Timeline is missing!");
        }
    }

    void LoadUsingJSON()
    {
        
    }

    void LoadBackground(string bgName)
    {
        Debug.Log($"🌲 Loading Background: {bgName}");
    }

    void SpawnEnemy()
    {
        Debug.Log("👹 Spawning an enemy");
    }

    void SpawnBoss()
    {
        Debug.Log("👑 Boss Appeared!");
    }

    public void SetLoadMode(int mode)
    {
        loadMode = mode;
        Debug.Log($"🔄 Load Mode changed to: {(loadMode == 0 ? "Playable Timeline" : "JSON")}");
    }
}
