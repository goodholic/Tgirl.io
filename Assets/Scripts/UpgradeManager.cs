using UnityEngine;

public static class UpgradeManager
{
    // 골드
    public static int Gold;

    // 각 업그레이드 레벨
    public static int DamageLevel;
    public static int MoveSpeedLevel;
    //public static int AttackSpeedLevel;
    public static int GoldGainLevel;
    public static int MaxHealthLevel;

    // 업그레이드 당 증가량(임의 예시 값)
    private static float damageIncrement = 2f;       // 데미지 1레벨당 +5
    private static float moveSpeedIncrement = 0.05f;  // 이동속도 1레벨당 +0.2
    //private static float attackSpeedIncrement = 0.05f;// 공격속도 1레벨당 0.1초 감소
    private static float goldGainIncrement = 1f;     // 골드 획득량 증가
    private static float maxHealthIncrement = 10f;   // 최대체력 1레벨당 +10

    // --------------- 불러오기 / 저장 ---------------
    public static void LoadData()
    {
        Gold = PlayerPrefs.GetInt("Gold", 0);
        DamageLevel = PlayerPrefs.GetInt("DamageLevel", 0);
        MoveSpeedLevel = PlayerPrefs.GetInt("MoveSpeedLevel", 0);
        //AttackSpeedLevel = PlayerPrefs.GetInt("AttackSpeedLevel", 0);
        GoldGainLevel = PlayerPrefs.GetInt("GoldGainLevel", 0);
        MaxHealthLevel = PlayerPrefs.GetInt("MaxHealthLevel", 0);
    }

    public static void SaveData()
    {
        PlayerPrefs.SetInt("Gold", Gold);
        PlayerPrefs.SetInt("DamageLevel", DamageLevel);
        PlayerPrefs.SetInt("MoveSpeedLevel", MoveSpeedLevel);
        //PlayerPrefs.SetInt("AttackSpeedLevel", AttackSpeedLevel);
        PlayerPrefs.SetInt("GoldGainLevel", GoldGainLevel);
        PlayerPrefs.SetInt("MaxHealthLevel", MaxHealthLevel);
        PlayerPrefs.Save();
    }

    // --------------- 보너스 계산 함수 ---------------
    public static float GetDamageBonus()
    {
        return DamageLevel * damageIncrement;
    }

    public static float GetMoveSpeedBonus()
    {
        return MoveSpeedLevel * moveSpeedIncrement;
    }

    // 공격 속도는 PlayerController에서 _attackInterval를 감소시키는 방식 예시
    /*public static float GetAttackIntervalReduction()
    {
        return AttackSpeedLevel * attackSpeedIncrement;
    }*/

    public static float GetGoldGainBonus()
    {
        return GoldGainLevel * goldGainIncrement;
    }

    public static float GetMaxHealthBonus()
    {
        return MaxHealthLevel * maxHealthIncrement;
    }
}
