using GoogleMobileAds.Api;
using UnityEngine;
using System;

public class AdManager : MonoBehaviour
{
    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    // ✅ Sử dụng ID TEST của AdMob
    private string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; // ID test Interstitial
    private string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // ID test Banner

    
    public static AdManager instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; 
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Xóa bản trùng lặp khi load Scene mới
        }
    }

    void Start()
    {
        // Khởi tạo Google Mobile Ads
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("Google Mobile Ads SDK đã khởi tạo!");
            RequestInterstitialAd();
            RequestBannerAd();
        });
    }

    // Tạo và load quảng cáo Interstitial
    public void RequestInterstitialAd()
    {
        AdRequest adRequest = new AdRequest();

        InterstitialAd.Load(interstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.Log("Lỗi khi load quảng cáo Interstitial: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("Quảng cáo Interstitial đã load thành công!");
        });
    }

    // Hiển thị quảng cáo Interstitial trước khi vào game
    public void ShowAdBeforeGameStart(Action callback)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Quảng cáo đã đóng, bắt đầu game!");
                callback.Invoke(); // Gọi callback để bắt đầu game
                RequestInterstitialAd(); // Load quảng cáo mới
            };
        }
        else
        {
            Debug.Log("Quảng cáo chưa sẵn sàng, vào game ngay!");
            callback.Invoke(); // Nếu quảng cáo chưa sẵn sàng, bắt đầu game luôn
            RequestInterstitialAd();
        }
    }

    // Tạo và load quảng cáo Banner
    public void RequestBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        AdRequest adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
        //bannerView.Show();

        Debug.Log("Quảng cáo Banner đã được tải và hiển thị!");
    }

    // Ẩn quảng cáo Banner
    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
            Debug.Log("Quảng cáo Banner đã ẩn!");
        }
         
            
    }

    // Hiển thị lại quảng cáo Banner
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
    }
}
