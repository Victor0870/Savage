using CBS.Models;
using CBS.Scriptable;
using CBS.Utils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace CBS.UI
{
    public class ProfileIcon : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text NickNameLabel;
        [SerializeField]
        private TMP_Text LevelLabel;
        [SerializeField]
        private TMP_Text ExpLabel;
        [SerializeField]
        private Slider ExpSlider;
        [SerializeField]
        private AvatarDrawer Avatar;

        private IProfile CBSProfile { get; set; }
        private ProfilePrefabs Prefabs { get; set; }
        private AuthData AuthData { get; set; }
        private ProfileConfigData ProfileConfig { get; set; }
        private AvatarDisplayOptions DisplayOption { get; set; }
        private int lastLevel = 0;

        private void Start()
        {
            Prefabs = CBSScriptable.Get<ProfilePrefabs>();
            CBSProfile = CBSModule.Get<CBSProfileModule>();
            AuthData = CBSScriptable.Get<AuthData>();
            ProfileConfig = CBSScriptable.Get<ProfileConfigData>();
            DisplayOption = ProfileConfig.AvatarDisplay;
            // subscribe to events
            CBSProfile.OnDisplayNameUpdated += OnDisplayNameUpdated;
            CBSProfile.OnPlayerExperienceUpdated += OnPlayerExperienceUpdated;
            CBSProfile.OnAvatarUpdated += OnAvatarImageUpdated;
            lastLevel = CBSProfile.CachedLevelInfo.CurrentLevel;
            // try display cache value
            DisplayName();
            DisplayLevelData();


            // get actual data from DB
            CBSProfile.GetAccountInfo(OnAccountInfoGetted);
            if (AuthData.PreloadLevelData)
            {
                DisplayLevelData();
            }
            else
            {
                CBSProfile.GetProfileLevelDetail(OnGetLevelData);
            }

            DrawAvatar();
        }

        private void OnDestroy()
        {
            CBSProfile.OnDisplayNameUpdated -= OnDisplayNameUpdated;
            CBSProfile.OnPlayerExperienceUpdated -= OnPlayerExperienceUpdated;
            CBSProfile.OnAvatarUpdated -= OnAvatarImageUpdated;
        }

        private void DisplayName()
        {
            NickNameLabel.text = CBSProfile.DisplayName;
        }

        private void DrawAvatar()
        {
            // draw avatar
            var avatarUrl = CBSProfile.Avatar.AvatarURL;
            var avatarID = CBSProfile.Avatar.AvatarID;
            var profileID = CBSProfile.ProfileID;
            if (DisplayOption == AvatarDisplayOptions.ONLY_DEFAULT)
                Avatar.DisplayDefaultAvatar();
            else if (DisplayOption == AvatarDisplayOptions.LOAD_AVATAR_URL)
                Avatar.LoadAvatarFromUrl(avatarUrl, profileID);
            else if (DisplayOption == AvatarDisplayOptions.LOAD_AVATAR_SPRITE)
                Avatar.DisplaySpriteAvatar(avatarID);
        }

        private void DisplayLevelData()
        {
            var levelData = CBSProfile.CachedLevelInfo;
            LevelLabel.text = levelData.CurrentLevel.ToString();

            if (levelData.CurrentLevel > lastLevel)
                    {
                        // Level đã tăng, gọi hàm xử lý Level Up
                        ShowLevelUpReward(levelData.CurrentLevel);
                        // Cập nhật cấp độ cuối cùng
                        lastLevel = levelData.CurrentLevel;
                    }

            int curExp = levelData.CurrentExp;
            int nextExp = levelData.NextLevelExp;
            int prevExp = levelData.PrevLevelExp;
            float expVal = (float)(curExp - prevExp) / (float)(nextExp - prevExp);
            ExpLabel.text = curExp.ToString() + "/" + nextExp.ToString();
            ExpSlider.value = expVal;


        }

        private void ShowLevelUpReward(int newLevel)
            {

                new PopupViewer().ShowSimplePopup(new PopupRequest
                {
                    Title = "Chúc mừng Level Up!",
                    Body = $"Bạn đã đạt cấp độ {newLevel}. Hãy kiểm tra phần thưởng trong hòm thư!"
                });

                // var prefabs = CBSScriptable.Get<LeaderboardPrefabs>();
               //  var leaderboardsPrefab = prefabs.LeaderboardsWindow;
                // UIView.ShowWindow(leaderboardsPrefab);



            }

        // button click
        public void ShowAccountInfo()
        {
            var windowsPrefab = Prefabs.AccountForm;
            UIView.ShowWindow(windowsPrefab);
        }

        // events
        private void OnPlayerExperienceUpdated(CBSLevelDataResult result)
        {
            DisplayLevelData();
        }

        private void OnDisplayNameUpdated(CBSUpdateDisplayNameResult result)
        {
            DisplayName();
        }

        private void OnAccountInfoGetted(CBSGetAccountInfoResult result)
        {
            DisplayName();
        }

        private void OnGetLevelData(CBSLevelDataResult result)
        {
            DisplayLevelData();
        }

        private void OnAvatarImageUpdated(AvatarInfo obj)
        {
            DrawAvatar();
        }


    }
}
