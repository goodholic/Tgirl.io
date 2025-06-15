using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용시

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // 업그레이드 팝업 관련
    [Header("Upgrade Popup")]
    public GameObject upgradePopup;        // 업그레이드 팝업(전체)
    public Text goldText;       // 현재 골드 표시용
    public Text damageText;
    public Text damageGoldText;
    public Text moveSpeedText;
    public Text moveSpeedGoldText;
    //public Text attackSpeedText;
    //public Text attackSpeedGoldText;
    public Text goldGainText;
    public Text goldGainGoldText;
    public Text maxHealthText;
    public Text maxHealthGoldText;
    
    // 업그레이드 버튼들
    public Button damageUpButton;
    public Button moveSpeedUpButton;
    //public Button attackSpeedUpButton;
    public Button goldGainUpButton;
    public Button maxHealthUpButton;
    
    [Header("Attack Mode UI")]
    public GameObject attackModePanel;      // 공격 모드 표시 패널
    public TextMeshProUGUI attackModeText;  // 공격 모드 텍스트

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 처음 골드 표시 갱신
        RefreshGoldUI();
        RefreshUpgradeUI();

        // 버튼 이벤트 연결
        damageUpButton.onClick.AddListener(OnClickDamageUp);
        moveSpeedUpButton.onClick.AddListener(OnClickMoveSpeedUp);
        //attackSpeedUpButton.onClick.AddListener(OnClickAttackSpeedUp);
        goldGainUpButton.onClick.AddListener(OnClickGoldGainUp);
        maxHealthUpButton.onClick.AddListener(OnClickMaxHealthUp);
        
        // 공격 모드 UI 설정
        SetupAttackModeUI();
    }
    
    /// <summary>
    /// 공격 모드 UI 설정
    /// </summary>
    private void SetupAttackModeUI()
    {
        if (attackModePanel != null)
        {
            attackModePanel.SetActive(true);
        }
        
        if (attackModeText != null && PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            // PlayerController의 공격 모드 텍스트 참조 설정
            PlayerController player = PlayerManager.Instance.curPlayer;
            System.Reflection.FieldInfo field = player.GetType().GetField("_attackModeText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(player, attackModeText);
            }
        }
    }

    public void ShowUpgradePopup()
    {
        // 게임 멈춘 상태에서 팝업 띄우기
        upgradePopup.SetActive(true);
        RefreshGoldUI();
        RefreshUpgradeUI();
    }

    // --- 각 업그레이드 버튼 ---
    private void OnClickDamageUp()
    {
        if (UpgradeManager.Gold >= UpgradeManager.DamageLevel + 1)
        {
            UpgradeManager.Gold -= UpgradeManager.DamageLevel + 1;
            UpgradeManager.DamageLevel++;
            UpgradeManager.SaveData();
            RefreshGoldUI();
            RefreshUpgradeUI();
        }
    }
    private void OnClickMoveSpeedUp()
    {
        if (UpgradeManager.Gold >= UpgradeManager.MoveSpeedLevel + 1)
        {
            UpgradeManager.Gold -= UpgradeManager.MoveSpeedLevel + 1;
            UpgradeManager.MoveSpeedLevel++;
            UpgradeManager.SaveData();
            RefreshGoldUI();
            RefreshUpgradeUI();
        }
    }
    /*private void OnClickAttackSpeedUp()
    {
        if (UpgradeManager.Gold >= UpgradeManager.AttackSpeedLevel + 1)
        {
            UpgradeManager.Gold -= UpgradeManager.AttackSpeedLevel + 1;
            UpgradeManager.AttackSpeedLevel++;
            UpgradeManager.SaveData();
            RefreshGoldUI();
        }
    }*/
    private void OnClickGoldGainUp()
    {
        if (UpgradeManager.Gold >= UpgradeManager.GoldGainLevel + 1)
        {
            UpgradeManager.Gold -= UpgradeManager.GoldGainLevel + 1;
            UpgradeManager.GoldGainLevel++;
            UpgradeManager.SaveData();
            RefreshGoldUI();
            RefreshUpgradeUI();
        }
    }
    private void OnClickMaxHealthUp()
    {
        if (UpgradeManager.Gold >= UpgradeManager.MaxHealthLevel + 1)
        {
            UpgradeManager.Gold -= UpgradeManager.MaxHealthLevel + 1;
            UpgradeManager.MaxHealthLevel++;
            UpgradeManager.SaveData();
            RefreshGoldUI();
            RefreshUpgradeUI();
        }
    }

    // 골드 텍스트 갱신
    public void RefreshGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"{UpgradeManager.Gold}";
        }
    }
    
    public void RefreshUpgradeUI()
    {
        damageText.text = "공격력 Lv." + UpgradeManager.DamageLevel + " - 추가 공격력 : " + UpgradeManager.GetDamageBonus();
        damageGoldText.text = (UpgradeManager.DamageLevel + 1).ToString();
        
        moveSpeedText.text = "이동속도 Lv." + UpgradeManager.MoveSpeedLevel + " - 추가 이동속도 : " + UpgradeManager.GetMoveSpeedBonus();
        moveSpeedGoldText.text = (UpgradeManager.MoveSpeedLevel + 1).ToString();
        
        goldGainText.text = "골드획득량 Lv." + UpgradeManager.GoldGainLevel + " - 추가 골드 : " + UpgradeManager.GetGoldGainBonus();
        goldGainGoldText.text = (UpgradeManager.GoldGainLevel + 1).ToString();
        
        maxHealthText.text = "최대체력 Lv." + UpgradeManager.MaxHealthLevel + " - 추가 체력 : " + UpgradeManager.GetMaxHealthBonus();
        maxHealthGoldText.text = (UpgradeManager.MaxHealthLevel + 1).ToString();
    }

    // ---- 팝업 닫기: 광고 보고 성공하면 환생 (씬 재시작 or 이어서 진행) ----
    public void OnClickCloseUpgradePopup()
    {
        // 리워드 광고 로직 (여기서는 가짜 함수로 처리)
        ShowRewardedAd(() =>
        {
            // 광고 성공 콜백
            upgradePopup.SetActive(false);
            Time.timeScale = 1f;

            // 업그레이드 적용된 상태로 씬 재시작 or PlayerManager의 RestartScene()
            PlayerManager.Instance.RestartScene();
        });
    }

    // 실제 AdMob 연동 대신 가짜 함수: 광고 성공 시 콜백 실행
    private void ShowRewardedAd(System.Action onSuccess)
    {
        AdMobManager.Instance.ShowRewardedInterstitialAd(onSuccess);
    }
}