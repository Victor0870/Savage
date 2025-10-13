using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameUI : MonoBehaviour
{
    public TMP_Text playerNameText; // UI Text để hiển thị tên người chơi

    void Start()
    {
        if (PlayerData.Instance != null)
        {
            playerNameText.text = " " + PlayerData.Instance.PlayerName;
        }
    }
}