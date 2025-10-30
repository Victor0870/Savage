using CBS.Scriptable;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using CBS.UI;

namespace CBS.Example
{
    public class FunctionRequestExample1 : ExecuteFunctionProfileArgs
    {
        public string EventID;
        public int TestInt;

        public void ShowStore()
                {
                    var prefabs = CBSScriptable.Get<LeaderboardPrefabs>();
                                var leaderboardsPrefab = prefabs.LeaderboardsWindow;
                                UIView.ShowWindow(leaderboardsPrefab);
                }
    }
}
