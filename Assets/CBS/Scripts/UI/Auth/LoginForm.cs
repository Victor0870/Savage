using CBS.Models;
using CBS.Scriptable;
using CBS.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CBS.UI
{
    // Lớp này giờ đây chịu trách nhiệm Khởi tạo Auth và chờ AdManager sẵn sàng.
    public class LoginForm : MonoBehaviour
    {
        // =====================================================================
        // UI TIẾN TRÌNH (PROGRESS BAR)
        // =====================================================================
        [Header("Progress Bar UI")]
        [SerializeField]
        private Slider progressBar; // Dùng cho thanh tiến trình
        [SerializeField]
        private TextMeshProUGUI progressText;   // Sử dụng TextMeshProUGUI

        [Header("Loading Visual")]
        [SerializeField]
        private RectTransform loadingCircle; // THAM CHIẾU ĐẾN HÌNH ẢNH XOAY
        [SerializeField]
        private float rotationSpeed = 200f; // Tốc độ xoay (độ/giây)

        private const int MAX_PROGRESS = 100;
        private int currentProgress = 0;

        // Trạng thái khởi tạo
        private bool isAdMobInitialized = false;
        private bool isAuthAttempted = false;
        private bool isFinished = false; // Cờ báo hiệu quá trình tải đã hoàn tất

        // =====================================================================
        // PHẦN ĐĂNG NHẬP CBS
        // =====================================================================

        public event Action<CBSLoginResult> OnLogined;

        private IAuth Auth { get; set; }
        private AuthPrefabs AuthUIData { get; set; }

        private void Start()
        {
            // Thiết lập tiến trình ban đầu
            SetProgress(0, "Đang khởi tạo...");

            // 1. Khởi tạo CBS Auth Module (10% progress)
            Auth = CBSModule.Get<CBSAuthModule>();
            AuthUIData = CBSScriptable.Get<AuthPrefabs>();
            SetProgress(10, "Khởi tạo hệ thống Auth...");

            // 2. Lắng nghe sự kiện từ AdManager (20% progress)
            if (AdManager.instance != null)
            {
                // *** ĐÃ SỬA LỖI CS0176 ***
                // Truy cập thuộc tính static IsInitialized bằng tên Class: AdManager.IsInitialized
                if (AdManager.IsInitialized)
                {
                    Debug.Log("AdMob đã sẵn sàng từ trước. Bắt đầu đăng nhập ngay.");
                    SetProgress(50, "Đã sẵn sàng quảng cáo. Đang đăng nhập...");
                    OnLoginWithdeviceID();
                }
                else
                {
                    // Nếu AdMob chưa khởi tạo, đăng ký lắng nghe như bình thường
                    Debug.Log("LoginForm đang chờ AdManager hoàn tất khởi tạo AdMob...");
                    SetProgress(20, "Đang tải AdMob SDK...");
                    AdManager.instance.OnAdMobInitialized += OnAdMobReady;
                }
            }
            else
            {
                Debug.LogError("AdManager.instance không tìm thấy. Bắt đầu đăng nhập ngay lập tức.");
                OnLoginWithdeviceID();
            }
        }

        // HÀM UPDATE ĐỂ XỬ LÝ VIỆC XOAY
        private void Update()
        {
            // Chỉ xoay khi quá trình chưa hoàn tất
            if (!isFinished && loadingCircle != null)
            {
                // Xoay hình ảnh quanh trục Z
                loadingCircle.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            }
        }

        private void SetProgress(int value, string message)
        {
            currentProgress = value;
            if (progressBar != null)
            {
                progressBar.value = (float)currentProgress / MAX_PROGRESS;
            }
            if (progressText != null)
            {
                progressText.text = message;
            }

            // Nếu tiến trình đạt 100%, đánh dấu là đã hoàn tất (dừng xoay)
            if (currentProgress >= MAX_PROGRESS)
            {
                isFinished = true;
                // Optional: Ẩn vòng tròn xoay khi hoàn tất
                if (loadingCircle != null)
                {
                    loadingCircle.gameObject.SetActive(false);
                }
            }
        }

        // Phương thức được gọi sau khi AdMob Initialization hoàn tất
        private void OnAdMobReady()
        {
            // Hủy đăng ký ngay lập tức
            if (AdManager.instance != null)
            {
                 AdManager.instance.OnAdMobInitialized -= OnAdMobReady;
            }

            isAdMobInitialized = true;
            SetProgress(50, "Đã sẵn sàng quảng cáo. Đang đăng nhập...");

            OnLoginWithdeviceID();
        }

        // LOGIC ĐĂNG NHẬP BẰNG DEVICE ID
        public void OnLoginWithdeviceID()
        {
            if (Auth != null && !isAuthAttempted)
            {
                isAuthAttempted = true;
                SetProgress(60, "Gửi yêu cầu đăng nhập...");
                Auth.LoginWithDevice(OnUserLogined);
            }
            else
            {
                Debug.LogError("CBSAuthModule chưa được khởi tạo hoặc đã cố gắng đăng nhập!");
            }
        }

        private void OnUserLogined(CBSLoginResult result)
        {
            if (result.IsSuccess)
            {
                SetProgress(MAX_PROGRESS, "Đăng nhập thành công!");
                gameObject.SetActive(false);
                OnLogined?.Invoke(result);
            }
            else
            {
                SetProgress(100, "Lỗi đăng nhập.");
                new PopupViewer().ShowFabError(result.Error);
            }
        }

        // =====================================================================
        // CÁC PHƯƠNG THỨC UI KHÁC
        // =====================================================================

        public bool IsSubscribeToLogin()
        {
            return OnLogined != null && OnLogined.GetInvocationList().Length != 0;
        }

        public void OnRegistration()
        {
            var registrationPrefab = AuthUIData.RegisterForm;
            UIView.ShowWindow(registrationPrefab);
            HideWindow();
        }

        public void OnFogotPassword()
        {
            var recoveryPrefab = AuthUIData.RecoveryForm;
            UIView.ShowWindow(recoveryPrefab);
            HideWindow();
        }

        private void HideWindow()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (AdManager.instance != null)
            {
                 AdManager.instance.OnAdMobInitialized -= OnAdMobReady;
            }
        }
    }
}
