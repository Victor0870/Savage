using System;
using System.Collections.Generic;
using OctoberStudio;
using PlayFab.ClientModels;
using UnityEngine;
using OctoberStudio;
using OctoberStudio.Bossfight;

public class PlayerData
{
    private static PlayerData _instance;
    public static PlayerData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PlayerData();
            }
            return _instance;
        }
    }

    public string PlayerName { get; private set; }
    public float GlobalEXP { get; private set; }
    public int GlobalLevel { get; private set; }
    public List<KLevelGroup> KLevelGroups { get; set; } = new List<KLevelGroup>();
    public List<LevelData> LevelThresholds { get; set; } = new List<LevelData>();

    // ✅ Giữ nguyên logic game khi tải dữ liệu từ PlayFab
    public void SetPlayerData(Dictionary<string, UserDataRecord> data)
    {
        // 🛠 Xử lý tên người chơi
        if (data.TryGetValue("PlayerName", out UserDataRecord nameRecord) && !string.IsNullOrEmpty(nameRecord.Value))
        {
            PlayerName = nameRecord.Value;
        }
        else
        {
            PlayerName = GenerateRandomPlayerName();
            PlayFabLoginManager.Instance.SaveUserData(); // 🔄 Lưu lại tên mới lên PlayFab
        }

        // 🛠 Xử lý EXP
        if (data.TryGetValue("GlobalEXP", out UserDataRecord expRecord) && float.TryParse(expRecord.Value, out float parsedEXP))
        {
            GlobalEXP = parsedEXP;
        }
        else
        {
            GlobalEXP = 0;
        }

        // 🛠 Xử lý Level
        if (data.TryGetValue("GlobalLevel", out UserDataRecord levelRecord) && int.TryParse(levelRecord.Value, out int parsedLevel))
        {
            GlobalLevel = parsedLevel;
        }
        else
        {
            GlobalLevel = 1;
        }

        Debug.Log($"📥 Dữ liệu tải từ PlayFab: PlayerName = {PlayerName}, EXP = {GlobalEXP}, Level = {GlobalLevel}");
    }

    // ✅ Giữ nguyên logic lưu dữ liệu lên PlayFab
    public Dictionary<string, string> GetPlayerDataAsDictionary()
    {
        return new Dictionary<string, string>
        {
            { "PlayerName", PlayerName },
            { "GlobalEXP", GlobalEXP.ToString() },
            { "GlobalLevel", GlobalLevel.ToString() }
        };
    }

    // ✅ Giữ nguyên logic thiết lập dữ liệu mặc định
    public void SetDefaultData()
    {
        PlayerName = GenerateRandomPlayerName();
        GlobalEXP = 0;
        GlobalLevel = 1;
    }

    public void SetPlayerName(string name)
    {
        PlayerName = name;
        PlayFabLoginManager.Instance.SaveUserData();
    }

    // ✅ Tạo tên người chơi với 5 chữ cái + 5 số
    public string GenerateRandomPlayerName()
    {
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        System.Text.StringBuilder randomLetters = new System.Text.StringBuilder();

        // 🛠 Sinh 5 ký tự chữ cái ngẫu nhiên
        for (int i = 0; i < 5; i++)
        {
            char randomChar = letters[UnityEngine.Random.Range(0, letters.Length)];
            randomLetters.Append(randomChar);
        }

        // 🛠 Sinh 5 số ngẫu nhiên từ 10000 đến 99999
        int randomNumber = UnityEngine.Random.Range(10000, 99999);

        return $"Player{randomLetters}{randomNumber}"; // Ví dụ: PlayerXDKTN58231
    }

    // ✅ Giữ nguyên logic tăng EXP và kiểm tra lên cấp
    public void IncreaseEXP()
    {
        float kValue = GetKForLevel(GlobalLevel);
        float expGain = 100 * kValue;
        GlobalEXP += expGain;

        Debug.Log($"🌟 Tăng {expGain} EXP (KValue: {kValue}) - Tổng EXP: {GlobalEXP}");

        CheckLevelUp();

        // 🔄 Lưu lại dữ liệu sau khi tăng EXP
        PlayFabLoginManager.Instance.SaveUserData();
    }

    private void CheckLevelUp()
    {
        while (true)
        {
            LevelData nextLevel = LevelThresholds.Find(l => l.level == GlobalLevel + 1);

            if (nextLevel != null && GlobalEXP >= nextLevel.globalExp)
            {
                GlobalEXP -= nextLevel.globalExp; // Trừ đi lượng EXP yêu cầu để lên cấp
                GlobalLevel = nextLevel.level; // Tăng cấp độ
                Debug.Log($"🎉 Level Up! New Level: {GlobalLevel}, EXP còn lại: {GlobalEXP}");
            }
            else
            {
                break; // Thoát vòng lặp nếu không thể lên cấp tiếp
            }
        }

        PlayFabLoginManager.Instance.SaveUserData(); // 🔄 Lưu lại dữ liệu
    }

    private float GetKForLevel(int level)
    {
        foreach (var group in KLevelGroups)
        {
            if (level >= group.min && level <= group.max)
            {
                return group.K;
            }
        }
        return 1.0f;
    }

    public MapDataList CurrentMapData { get; private set; } // Biến lưu MapData

    public void SetMapData(MapDataList mapDataList)
    {
        if (mapDataList != null && (mapDataList.mapData != null ))
        {
            CurrentMapData = mapDataList;

            UpdateBossList();
            UpdateEnemyList();
            UpdateEnemiesOnMap();
            UpdateBossOnMap();
           // Debug.Log("✅ MapData đã được lưu vào PlayerData!");
        }
        else
        {
            Debug.LogWarning("Dữ liệu MapData nhận được là null hoặc rỗng!");
        }
    }

    public List<MapData> GetMapData()
    {
        if (CurrentMapData == null)
            return new List<MapData>();

        List<MapData> combinedData = new List<MapData>();
        combinedData.AddRange(CurrentMapData.mapData ?? new List<MapData>());
       

        return combinedData;
    }
    public List<EnemyType> listEnemies { get; private set; } = new List<EnemyType>();
    public List<BossType> listBoss { get; private set; } = new List<BossType>();
    public void UpdateBossList()
    {
        listBoss.Clear();

        if (CurrentMapData != null)
        {
            foreach (var map in CurrentMapData.mapData)
            {
                BossType boss = ParseBossType(map.enemyType);
                if (!listBoss.Contains(boss))
                {
                    listBoss.Add(boss);
                }
            }
        }

       // Debug.Log("🔄 Danh sách Boss được cập nhật: " + string.Join(", ", listBoss));
    }

    public void UpdateEnemyList()
    {
        listEnemies.Clear();

        if (CurrentMapData != null)
        {
            foreach (var map in CurrentMapData.mapData)
            {
                EnemyType enemy = ParseEnemyType(map.enemyType);
                if (!listEnemies.Contains(enemy))
                {
                    listEnemies.Add(enemy);
                }
            }

            
        }


      //  Debug.Log("🔄 Danh sách Enemy được cập nhật: " + string.Join(", ", listEnemies));
    }

    public static EnemyType ParseEnemyType(string enemyTypeStr)
    {
        if (System.Enum.TryParse(enemyTypeStr, true, out EnemyType enemyType))
        {
            return enemyType;
        }
      //  Debug.LogWarning($"⚠ Không tìm thấy EnemyType tương ứng với: {enemyTypeStr}, trả về EnemyType.Pumpkin mặc định!");
        return EnemyType.Pumpkin; // Giá trị mặc định nếu không tìm thấy
    }

    public static BossType ParseBossType(string enemyTypeStr)
    {
        if (System.Enum.TryParse(enemyTypeStr, true, out BossType enemyType))
        {
            return enemyType;
        }
      //  Debug.LogWarning($"⚠ Không tìm thấy EnemyType tương ứng với: {enemyTypeStr}, trả về EnemyType.Pumpkin mặc định!");
        return BossType.Void; // Giá trị mặc định nếu không tìm thấy
    }

    public Dictionary<EnemyType, int> enemiesOnMap { get; private set; } = new Dictionary<EnemyType, int>();

    public void UpdateEnemiesOnMap()
    {
        enemiesOnMap.Clear();

        if (CurrentMapData != null)
        {
            foreach (var map in CurrentMapData.mapData)
            {
                EnemyType enemy = ParseEnemyType(map.enemyType);
                if (enemiesOnMap.ContainsKey(enemy))
                {
                    enemiesOnMap[enemy] += map.amount; // Cộng dồn số lượng enemy
                }
                else
                {
                    enemiesOnMap[enemy] = map.amount; // Thêm enemy mới vào Dictionary
                }
            }
           
        }

      //  Debug.Log("🔄 Tổng số lượng kẻ địch trên bản đồ: ");
        foreach (var enemy in enemiesOnMap)
        {
        //    Debug.Log($"🛡 {enemy.Key}: {enemy.Value}");
        }
    }

    public Dictionary<BossType, int> bossOnMap { get; private set; } = new Dictionary<BossType, int>();
    public void UpdateBossOnMap()
    {
        bossOnMap.Clear();

        if (CurrentMapData != null)
        {
            foreach (var map in CurrentMapData.mapData)
            {
                BossType boss = ParseBossType(map.enemyType);
                if (bossOnMap.ContainsKey(boss))
                {
                    bossOnMap[boss] += map.amount; // Cộng dồn số lượng enemy
                }
                else
                {
                    bossOnMap[boss] = map.amount; // Thêm enemy mới vào Dictionary
                }
            }

        }

       // Debug.Log("🔄 Tổng số lượng kẻ địch trên bản đồ: ");
        foreach (var enemy in bossOnMap)
        {
       //     Debug.Log($"🛡 {enemy.Key}: {enemy.Value}");
        }
    }

}
