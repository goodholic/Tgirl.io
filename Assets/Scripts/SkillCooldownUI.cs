using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [System.Serializable]
    public class SkillButton
    {
        public Button button;
        public Image cooldownImage;  // 쿨타임 표시용 이미지 (Fill Method: Radial 360)
        public Text cooldownText;    // 쿨타임 숫자 표시용 텍스트
    }
    
    [Header("Skill Buttons")]
    public SkillButton skill1Button;    // 이동 스킬
    public SkillButton skill2Button;    // 이동 공격
    public SkillButton ultimateButton;  // 궁극기
    
    private PlayerController currentPlayer;
    
    private void Start()
    {
        // 각 버튼에 이벤트 연결
        if (skill1Button.button != null)
            skill1Button.button.onClick.AddListener(() => PlayerManager.Instance.OnSkill1Button());
            
        if (skill2Button.button != null)
            skill2Button.button.onClick.AddListener(() => PlayerManager.Instance.OnSkill2Button());
            
        if (ultimateButton.button != null)
            ultimateButton.button.onClick.AddListener(() => PlayerManager.Instance.OnUltimateButton());
        
        // 초기 상태 설정
        ResetCooldownUI(skill1Button);
        ResetCooldownUI(skill2Button);
        ResetCooldownUI(ultimateButton);
    }
    
    private void Update()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.curPlayer != null)
        {
            currentPlayer = PlayerManager.Instance.curPlayer;
            
            // 각 스킬의 쿨타임 업데이트
            UpdateSkillCooldown(skill1Button, currentPlayer.GetSkill1CooldownRemaining(), currentPlayer.GetSkill1Cooldown());
            UpdateSkillCooldown(skill2Button, currentPlayer.GetSkill2CooldownRemaining(), currentPlayer.GetSkill2Cooldown());
            UpdateSkillCooldown(ultimateButton, currentPlayer.GetUltimateCooldownRemaining(), currentPlayer.GetUltimateCooldown());
        }
    }
    
    private void UpdateSkillCooldown(SkillButton skillButton, float remainingTime, float totalCooldown)
    {
        if (skillButton.cooldownImage == null) return;
        
        if (remainingTime > 0)
        {
            // 쿨타임 중
            skillButton.cooldownImage.gameObject.SetActive(true);
            skillButton.cooldownImage.fillAmount = remainingTime / totalCooldown;
            
            if (skillButton.cooldownText != null)
            {
                skillButton.cooldownText.gameObject.SetActive(true);
                skillButton.cooldownText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
            
            // 버튼 비활성화 상태로 표시
            if (skillButton.button != null)
                skillButton.button.interactable = false;
        }
        else
        {
            // 쿨타임 완료
            ResetCooldownUI(skillButton);
        }
    }
    
    private void ResetCooldownUI(SkillButton skillButton)
    {
        if (skillButton.cooldownImage != null)
        {
            skillButton.cooldownImage.gameObject.SetActive(false);
            skillButton.cooldownImage.fillAmount = 0;
        }
        
        if (skillButton.cooldownText != null)
        {
            skillButton.cooldownText.gameObject.SetActive(false);
        }
        
        if (skillButton.button != null)
        {
            skillButton.button.interactable = true;
        }
    }
}