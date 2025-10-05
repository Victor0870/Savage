using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Collections;
using OctoberStudio;
using UnityEngine.UI;
using TMPro;

public class PlayFabLoginManager : MonoBehaviour
{
    public static PlayFabLoginManager Instance { get; private set; }
    public static event System.Action onDataLoaded;

    private bool isShowingErrorMessage = false;
    private bool isRetrying = false;

    public Slider loadingBar; // đây là code bổ sung - Thêm thanh tiến trình
    public TMP_Text loadingText;  // đây là code bổ sung - Thêm text hiển thị phần trăm tải

    private float progress = 0f; // đây là code bổ sung - Biến lưu trạng thái tiến trình


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        progress = 0.1f; // đây là code bổ sung - Bắt đầu tiến trình
        UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
         
        LoginWithDeviceID();

    }

    void LoginWithDeviceID()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("✅ Đăng nhập PlayFab thành công! Player ID: " + result.PlayFabId);
        progress = 0.3f; // đây là code bổ sung - Cập nhật tiến trình
        UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
        LoadUserData();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("❌ Đăng nhập PlayFab thất bại: " + error.GenerateErrorReport());
        progress = 1f; // đây là code bổ sung - Đánh dấu tải xong dù có lỗi
        UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
        ShowNetworkErrorMessage();
    }

    void LoadUserData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            progress = 0.5f; // đây là code bổ sung - Cập nhật tiến trình
            UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading

            if (result.Data == null || result.Data.Count == 0)
            {
                Debug.LogWarning("⚠ User Data rỗng! Đang khởi tạo dữ liệu mặc định...");
                InitializeDefaultUserData();
                StartCoroutine(WaitAndLoadMainMenu());
                return;
            }

            PlayerData.Instance.SetPlayerData(result.Data);

            // 🚀 Nếu người chơi chưa có tên, tạo mới
            if (string.IsNullOrEmpty(PlayerData.Instance.PlayerName) || PlayerData.Instance.PlayerName == "Player")
            {
                string newName = PlayerData.Instance.GenerateRandomPlayerName(); // Gọi qua Instance
                PlayerData.Instance.SetPlayerName(newName);

                // 🔄 Lưu lại tên mới lên PlayFab
                PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string> { { "PlayerName", newName } }
                },
                result => Debug.Log($"✅ Tên mới được cập nhật: {newName}"),
                error => Debug.LogError("❌ Lỗi khi cập nhật tên mới: " + error.ErrorMessage));
            }

            Debug.Log($"✅ LoadUserData: PlayerName = {PlayerData.Instance.PlayerName}, GlobalEXP = {PlayerData.Instance.GlobalEXP}, GlobalLevel = {PlayerData.Instance.GlobalLevel}");
            LoadTitleData();
        },
        error =>
        {
            Debug.LogError("❌ Lỗi khi tải User Data từ PlayFab: " + error.ErrorMessage);
            progress = 1f; // đây là code bổ sung - Đánh dấu tải xong dù có lỗi
            UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
            ShowNetworkErrorMessage();
        });
    }

    void InitializeDefaultUserData()
    {
        PlayerData.Instance.SetDefaultData();
        SaveUserData();
        
    }

    public void SaveUserData()
    {
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = PlayerData.Instance.GetPlayerDataAsDictionary() },
            result => Debug.Log("✅ Dữ liệu đã được lưu lên PlayFab!"),
            error => Debug.LogError("❌ Lỗi khi lưu dữ liệu: " + error.ErrorMessage));
    }

    void LoadTitleData()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), result =>
        {
            progress = 0.7f; // đây là code bổ sung - Cập nhật tiến trình
            UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading

            if (result.Data != null)
            {
                if (result.Data.ContainsKey("KLevelGroups"))
                {
                    string jsonData = result.Data["KLevelGroups"];
                    KLevelGroupList kDataList = JsonUtility.FromJson<KLevelGroupList>(jsonData);
                    PlayerData.Instance.KLevelGroups = kDataList?.KLevelGroups ?? new List<KLevelGroup>();
                   // Debug.Log("✅ KLevelGroups đã tải thành công.");
                }

                if (result.Data.ContainsKey("LevelData"))
                {
                    string levelJson = result.Data["LevelData"];
                    LevelDataList levelDataList = JsonUtility.FromJson<LevelDataList>(levelJson);
                    PlayerData.Instance.LevelThresholds = levelDataList?.levels ?? new List<LevelData>();
                   // Debug.Log("✅ LevelData đã tải thành công.");
                }

                if (result.Data.ContainsKey("MapData"))
                {
                    string mapJson = result.Data["MapData"];
                    MapDataList mapDataList = JsonUtility.FromJson<MapDataList>(mapJson);
                    PlayerData.Instance.SetMapData(mapDataList);
                    // Debug.Log("✅ MapData đã tải thành công.");
                }
                else
                {
                    Debug.LogWarning("⚠ Không tìm thấy MapData trong Title Data.");
                }
            }

            StartCoroutine(WaitAndLoadMainMenu());
        },
        error =>
        {
            Debug.LogError("❌ Lỗi khi tải Title Data từ PlayFab: " + error.ErrorMessage);
            progress = 1f; // đây là code bổ sung - Đánh dấu tải xong dù có lỗi
            UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
            ShowNetworkErrorMessage();
        });
    }

    void ShowNetworkErrorMessage()
    {
        if (isShowingErrorMessage) return;
        isShowingErrorMessage = true;
        Debug.LogError("❌ Không thể kết nối đến PlayFab. Vui lòng kiểm tra kết nối mạng và thử lại.");
    }

    IEnumerator WaitAndLoadMainMenu()
    {
        yield return new WaitForSeconds(1);
        progress = 1f; // đây là code bổ sung - Cập nhật tiến trình hoàn tất
        UpdateLoadingBar(); // đây là code bổ sung - Cập nhật thanh loading
        SceneManager.LoadScene("Main Menu");
    }

    private void UpdateLoadingBar() // đây là code bổ sung - Hàm cập nhật thanh tiến trình
    {
        if (loadingBar != null)
        {
            loadingBar.value = progress;
            loadingText.text = "Loading... " + (progress * 100).ToString("F0") + "%";
        }
    }
}
