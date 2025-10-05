using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;

public class ChangeNameUI : MonoBehaviour
{
    public TMP_InputField nameInputField; // Ô nhập tên
    public Button changeNameButton; // Nút xác nhận
    public TMP_Text playerNameText; // Hiển thị tên hiện tại

    void Start()
    {
        UpdateUI(); // Hiển thị tên người chơi khi mở UI

        changeNameButton.onClick.AddListener(ChangePlayerName);
    }

    void ChangePlayerName()
    {
        string newName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName) || newName.Length < 3)
        {
            Debug.LogWarning("⚠ Tên quá ngắn! Phải từ 3 ký tự trở lên.");
            return;
        }

        // Cập nhật trong bộ nhớ game
        PlayerData.Instance.SetPlayerName(newName);

        // Gửi lên PlayFab
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "PlayerName", newName } }
        },
        result =>
        {
            Debug.Log($"✅ Tên mới đã lưu: {newName}");
            UpdateUI();
        },
        error => Debug.LogError("❌ Lỗi khi đổi tên: " + error.ErrorMessage));
    }

    void UpdateUI()
    {
        playerNameText.text = "Tên của bạn: " + PlayerData.Instance.PlayerName;
        nameInputField.text = ""; // Reset ô nhập
    }
}
