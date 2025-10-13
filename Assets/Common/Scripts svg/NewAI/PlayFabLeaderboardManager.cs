using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Collections.Generic;

public class PlayFabLeaderboardManager : MonoBehaviour
{
    public static PlayFabLeaderboardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy bảng xếp hạng từ PlayFab theo tên chỉ số
    /// </summary>
    public void GetLeaderboard(string statName, int maxResults = 10)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("❌ Không có kết nối mạng! Không thể lấy bảng xếp hạng.");
            return;
        }

        var request = new GetLeaderboardRequest
        {
            StatisticName = statName,
            StartPosition = 0,
            MaxResultsCount = maxResults
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardSuccess, OnLeaderboardError);
    }

    private void OnLeaderboardSuccess(GetLeaderboardResult result)
    {
        Debug.Log($"✅ Leaderboard lấy thành công!");

        foreach (var entry in result.Leaderboard)
        {
            string playerName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.PlayFabId;
            Debug.Log($"🏆 Rank {entry.Position + 1}: {playerName} - {entry.StatValue}");
        }
    }

    private void OnLeaderboardError(PlayFabError error)
    {
        Debug.LogError("❌ Lỗi khi lấy leaderboard: " + error.GenerateErrorReport());
    }

    /// <summary>
    /// Lấy bảng xếp hạng EXP
    /// </summary>
    public void GetGlobalEXPLeaderboard()
    {
        GetLeaderboard("GlobalEXP");
    }

    /// <summary>
    /// Lấy bảng xếp hạng Level
    /// </summary>
    public void GetLevelLeaderboard()
    {
        GetLeaderboard("GlobalLevel");
    }

    /// <summary>
    /// Gửi EXP của người chơi lên PlayFab
    /// </summary>
    public void UpdateEXPLeaderboard(int exp)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("❌ Không có kết nối mạng! Không thể cập nhật EXP.");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = "GlobalEXP", Value = exp }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => Debug.Log($"✅ Cập nhật EXP {exp} lên PlayFab thành công!"),
            error => Debug.LogError("❌ Lỗi khi cập nhật EXP: " + error.GenerateErrorReport()));
    }

    /// <summary>
    /// Gửi Level của người chơi lên PlayFab
    /// </summary>
    public void UpdateLevelLeaderboard(int level)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("❌ Không có kết nối mạng! Không thể cập nhật Level.");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = "GlobalLevel", Value = level }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            result => Debug.Log($"✅ Cập nhật Level {level} lên PlayFab thành công!"),
            error => Debug.LogError("❌ Lỗi khi cập nhật Level: " + error.GenerateErrorReport()));
    }
}
