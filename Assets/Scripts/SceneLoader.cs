using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // ⭐️ Singleton Instance
    public static SceneLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Giữ đối tượng này không bị hủy khi tải Scene mới
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ⭐️ Hàm công khai để gọi từ các script khác (Ví dụ: IconsPanel)
    public static void LoadGameScene(string gameSceneName)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.GameLoadingCoroutine(gameSceneName));
        }
    }

    // ⭐️ Coroutine chính: Chứa logic tải cảnh
    private IEnumerator GameLoadingCoroutine(string gameSceneName)
    {
        // 1. Tải màn hình Loading Screen (Additive)
        yield return LoadAsyncScene("Loading Screen", LoadSceneMode.Additive);

        // 2. Dỡ cảnh Lobby/Menu hiện tại
        // Đảm bảo tên cảnh "Lobby" là chính xác
        yield return UnloadAsyncScene("Lobby");

        // 3. Tải cảnh GameScene (Single - thay thế tất cả)
        yield return LoadAsyncScene(gameSceneName, LoadSceneMode.Single);
    }

    // Hàm Wrapper cho tải cảnh bất đồng bộ
    private IEnumerator LoadAsyncScene(string sceneName, LoadSceneMode mode)
    {
        // Sử dụng SceneManager.LoadSceneAsync và chờ nó hoàn thành
        yield return SceneManager.LoadSceneAsync(sceneName, mode);
    }

    // Hàm Wrapper cho dỡ cảnh bất đồng bộ
    private IEnumerator UnloadAsyncScene(string sceneName)
    {
        // Sử dụng SceneManager.UnloadSceneAsync và chờ nó hoàn thành
        yield return SceneManager.UnloadSceneAsync(sceneName);
    }
}