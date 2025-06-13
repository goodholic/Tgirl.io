using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance { get; private set; }

    // 플랫폼별 보상형 전면 광고 테스트 유닛 ID (Google 제공)
#if UNITY_ANDROID
    private readonly string _adUnitId = "ca-app-pub-3940256099942544/5354046379";
#elif UNITY_IPHONE
    private readonly string _adUnitId = "ca-app-pub-3940256099942544/6978759866";
#else
    private readonly string _adUnitId = "unused";
#endif

    // 로드된 보상형 전면 광고를 저장할 변수
    private RewardedInterstitialAd _rewardedInterstitialAd;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Google Mobile Ads SDK 초기화
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("Google Mobile Ads SDK 초기화 완료");
            // 초기화 완료 후 광고 로드
            LoadRewardedInterstitialAd();
        });
    }

    /// <summary>
    /// 보상형 전면 광고를 로드합니다.
    /// </summary>
    public void LoadRewardedInterstitialAd()
    {
        // 이전에 로드된 광고 객체가 있다면 정리
        if (_rewardedInterstitialAd != null)
        {
            _rewardedInterstitialAd.Destroy();
            _rewardedInterstitialAd = null;
        }

        Debug.Log("보상형 전면 광고 로드 시작");

        // 광고 요청(Builder 패턴 권장)
        AdRequest request = new AdRequest();

        // 광고 로드
        RewardedInterstitialAd.Load(_adUnitId, request, (ad, loadError) =>
        {
            if (loadError != null || ad == null)
            {
                Debug.LogError($"보상형 전면 광고 로드 실패: {loadError}");
                return;
            }

            Debug.Log("보상형 전면 광고 로드 성공");
            _rewardedInterstitialAd = ad;

            // 광고 이벤트 핸들러 등록
            RegisterEventHandlers(ad);
            // 광고가 닫히거나 실패했을 때 자동 재로드
            RegisterReloadHandler(ad);
        });
    }

    /// <summary>
    /// 로드된 보상형 전면 광고가 준비되었으면 즉시 표시합니다.
    /// </summary>
    /// <param name="onRewardEarned">광고 시청 완료(보상 획득) 시 호출할 콜백</param>
    public void ShowRewardedInterstitialAd(Action onRewardEarned)
    {
        if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd())
        {
            _rewardedInterstitialAd.Show((Reward reward) =>
            {
                Debug.Log($"사용자 광고 시청 완료. 보상 타입: {reward.Type}, 보상 수량: {reward.Amount}");
                onRewardEarned?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("보상형 전면 광고가 준비되지 않았습니다. 로드가 필요합니다.");
        }
    }

    /// <summary>
    /// 전체화면 광고 이벤트 핸들러 등록
    /// </summary>
    private void RegisterEventHandlers(RewardedInterstitialAd ad)
    {
        // 광고비 지불 이벤트(ECPM 등)
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"[OnAdPaid] 보상형 전면 광고 수익 발생: {adValue.Value} {adValue.CurrencyCode}");
        };

        // 광고 노출(임프레션)
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[OnAdImpressionRecorded] 보상형 전면 광고 노출");
        };

        // 광고 클릭
        ad.OnAdClicked += () =>
        {
            Debug.Log("[OnAdClicked] 보상형 전면 광고 클릭");
        };

        // 전체화면 열림
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[OnAdFullScreenContentOpened] 보상형 전면 광고 열림");
        };

        // 전체화면 닫힘
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[OnAdFullScreenContentClosed] 보상형 전면 광고 닫힘");
        };

        // 전체화면 표시 실패
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[OnAdFullScreenContentFailed] 보상형 전면 광고 표시 실패: {error}");
        };
    }

    /// <summary>
    /// 광고가 닫히거나 표시 실패 시 다음 광고를 다시 로드하도록 설정
    /// </summary>
    private void RegisterReloadHandler(RewardedInterstitialAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("광고 닫힘 -> 다음 광고 재로드");
            LoadRewardedInterstitialAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("광고 전체화면 표시 실패 -> 다음 광고 재로드");
            LoadRewardedInterstitialAd();
        };
    }
}
