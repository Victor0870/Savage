using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : MonoBehaviour
{
    // =====================================================================
    // SINGLETON VÀ SỰ KIỆN KHỞI TẠO (ĐÃ SỬA ĐỔI)
    // =====================================================================

    // THUỘC TÍNH MỚI: Báo hiệu AdMob đã khởi tạo xong (khắc phục lỗi CS1061)
    public static bool IsInitialized { get; private set; } = false;

    // Sự kiện được phát ra khi MobileAds.Initialize() hoàn tất.
    public event Action OnAdMobInitialized;

    public static AdManager instance { get; private set; }

    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    // ID TEST của AdMob
    private readonly string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    private readonly string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

    private void Awake()
    {
        // Thiết lập Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // QUAN TRỌNG: Tồn tại qua các Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeAdMob();
    }

    private void InitializeAdMob()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads SDK đã khởi tạo! Trạng thái: " + initStatus.ToString());

            // ĐẶT ISINITIALIZED = TRUE SAU KHI KHỞI TẠO XONG
            IsInitialized = true;

            // Yêu cầu tải quảng cáo
            RequestInterstitialAd();
            RequestBannerAd();

            // Phát sự kiện thông báo cho LoginForm
            OnAdMobInitialized?.Invoke();
        });
    }

    // =====================================================================
    // LOGIC QUẢNG CÁO (GIỮ NGUYÊN)
    // =====================================================================

    // Tạo và load quảng cáo Interstitial
    public void RequestInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        AdRequest adRequest = new AdRequest();

        InterstitialAd.Load(interstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Lỗi khi load quảng cáo Interstitial: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("Quảng cáo Interstitial đã load thành công!");
        });
    }

    // Tạo và load quảng cáo Banner (Không hiển thị ngay)
    public void RequestBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        AdRequest adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
        // KHÔNG hiển thị (Show) ở đây

        Debug.Log("Quảng cáo Banner đã được tải (chưa hiển thị)!");
    }

    // Các phương thức Show/Hide...
    public void ShowAdBeforeGameStart(Action callback)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Quảng cáo đã đóng, tiếp tục game/màn hình!");
                callback.Invoke();
                RequestInterstitialAd();
            };
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("Quảng cáo Interstitial chưa sẵn sàng, tiếp tục ngay!");
            callback.Invoke();
            RequestInterstitialAd();
        }
    }

    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
            Debug.Log("Quảng cáo Banner đã ẩn!");
        }
    }

    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Show();
            Debug.Log("Quảng cáo Banner đã hiển thị lại!");
        }
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }
    }
}