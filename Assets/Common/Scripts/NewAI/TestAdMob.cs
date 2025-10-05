using GoogleMobileAds.Api;
using UnityEngine;

public class TestAdMob : MonoBehaviour
{
    void Start()
    {
        MobileAds.Initialize(initStatus => {
            Debug.Log(" Google Mobile Ads SDK da khoi tao thanh cong!");
        });
    }
}