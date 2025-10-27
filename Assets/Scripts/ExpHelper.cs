using System;
using CBS;
using CBS.Models;
using UnityEngine;

namespace OctoberStudio
{
    public static class ExpHelper
    {
        // Bien luu tru module profile, dam bao chi lay 1 lan
        private static IProfile profileModule;

        // Ham khoi tao module neu chua co
        private static void EnsureProfileModule()
        {
            if (profileModule == null)
                profileModule = CBSModule.Get<CBSProfileModule>();
        }

        // Ham tang EXP (mac dinh khong can callback)
        public static void Add(int expToAdd)
        {
            Add(expToAdd, null);
        }

        // Ham tang EXP co callback neu can
        public static void Add(int expToAdd, Action<CBSLevelDataResult> onComplete)
        {
            EnsureProfileModule();

            if (profileModule == null)
            {
                Debug.LogError("ExpHelper: ProfileModule not found!");
                return;
            }

            // Goi ham tang EXP cua CBS
            profileModule.AddExpirienceToProfile(expToAdd, result =>
            {
                if (result.IsSuccess)
                {
                    Debug.Log($"[EXP] +{expToAdd} EXP thanh cong (Level: {result.LevelInfo?.CurrentLevel ?? -1})");
                }
                else
                {
                    Debug.LogError($"[EXP] Loi khi tang EXP: {result.Error?.Message}");
                }

                onComplete?.Invoke(result);
            });
        }
    }
}
