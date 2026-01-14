using System;

namespace DungeonLog.StatusEffects
{
    /// <summary>
    /// 상태 이상 타입을 정의하는 enum입니다.
    /// 로그라이크 장르의 특성을 고려하여 다양한 효과를 포함합니다.
    /// </summary>
    [Serializable]
    public enum StatusEffectType
    {
        // ============ 지속 데미지 (DoT - Damage over Time) ============
        /// <summary>독: 턴마다 최대 HP 비율 데미지</summary>
        Poison,
        
        /// <summary>화상: 턴마다 고정 데미지 + 회복 감소</summary>
        Burn,
        
        /// <summary>출혈: 턴마다 데미지 + 힐 불가</summary>
        Bleed,
        
        // ============ 지속 회복 (HoT - Heal over Time) ============
        /// <summary>재생: 턴마다 HP 회복</summary>
        Regeneration,
        
        // ============ 행동 방해 (Crowd Control) ============
        /// <summary>기절: 행동 불가, 방어력 감소</summary>
        Stun,
        
        /// <summary>침묵: 스킬 사용 불가</summary>
        Silence,
        
        /// <summary>빙면: 행동 불가 + 받는 데미지 증가</summary>
        Freeze,
        
        /// <summary>혼란: 50% 확률로 아군 공격</summary>
        Confusion,
        
        // ============ 스탯 버프 (Stat Buffs) ============
        /// <summary>공격력 증가</summary>
        AttackBoost,
        
        /// <summary>방어력 증가</summary>
        DefenseBoost,
        
        /// <summary>속도 증가 (턴 순서)</summary>
        SpeedBoost,
        
        // ============ 스탯 디버프 (Stat Debuffs) ============
        /// <summary>공격력 감소</summary>
        AttackReduction,
        
        /// <summary>방어력 감소</summary>
        DefenseReduction,
        
        /// <summary>취약상태: 받는 데미지 증가</summary>
        Vulnerability,
        
        /// <summary>약화: 주는 데미지 감소</summary>
        Weakness,
        
        // ============ 보호 (Protection) ============
        /// <summary>보호막: 데미지 흡수</summary>
        Shield,
        
        /// <summary>무적: 모든 데미지 무효</summary>
        Invulnerability,
        
        // ============ 로그라이크 특수 효과 ============
        /// <summary>표시: 해당 대상 공격 시 추가 데미지</summary>
        Mark,
        
        /// <summary>에코: 다음 턴에 사용 스킬 재사용</summary>
        Echo,
        
        /// <summary>아드레날린: AP +1, 공격 +20%, 받는 데미지 +20%</summary>
        Adrenaline,
        
        /// <summary>광폭화: 공격 +50%, 방어 -50%, 무작표 공격</summary>
        Berserk,
        
        /// <summary>저주: 받는 치료 효과가 데미지로 변환</summary>
        Curse
    }
    
    /// <summary>
    /// 상태 이상 분류를 정의합니다.
    /// </summary>
    [Serializable]
    public enum StatusEffectCategory
    {
        /// <summary>버프: 아군에게 걸리는 긍정적 효과</summary>
        Buff,
        
        /// <summary>디버프: 적에게 걸리는 부정적 효과</summary>
        Debuff,
        
        /// <summary>행동 방해: 기절, 침묵 등</summary>
        CrowdControl,
        
        /// <summary>지속 효과: 독, 재생 등</summary>
        DamageOverTime,
        
        /// <summary>보호: 보호막, 무적 등</summary>
        Protection,
        
        /// <summary>특수: 로그라이크 특유 효과</summary>
        Special
    }
    
    /// <summary>
    /// 상태 이상 적용 타이밍을 정의합니다.
    /// </summary>
    [Serializable]
    public enum StatusEffectTiming
    {
        /// <summary>즉시: 적용 즉시 효과</summary>
        Instant,
        
        /// <summary>턴 시작: 턴 시작 시 효과</summary>
        TurnStart,
        
        /// <summary>행동 전: 행동하기 전에 효과</summary>
        BeforeAction,
        
        /// <summary>행동 후: 행동한 후에 효과</summary>
        AfterAction,
        
        /// <summary>턴 종료: 턴 끝에 효과</summary>
        TurnEnd,
        
        /// <summary>데미지 받을 때: 피격 시 효과</summary>
        OnDamaged,
        
        /// <summary>데미지 줄 때: 공격 시 효과</summary>
        OnDealDamage,
        
        /// <summary>사망 시: 죽을 때 효과</summary>
        OnDeath
    }
    
    /// <summary>
    /// 상태 이상 중첩 동작을 정의합니다.
    /// </summary>
    [Serializable]
    public enum StackBehavior
    {
        /// <summary>무시: 새로운 효과 적용 안 함</summary>
        Ignore,
        
        /// <summary>갱신: 지속 시간만 초기화</summary>
        RefreshDuration,
        
        /// <summary>누적: 지속 시간 추가</summary>
        AdditiveDuration,
        
        /// <summary>최대: 더 긴 지속 시간 유지</summary>
        MaxDuration,
        
        /// <summary>독립: 별도 인스턴스로 관리</summary>
        IndependentStacks,
        
        /// <summary>스택: 중첩 수 증가</summary>
        AddStack
    }
    
    /// <summary>
    /// 상태 이상 타입에 대한 확장 메서드를 제공합니다.
    /// </summary>
    public static class StatusEffectTypeExtensions
    {
        /// <summary>
        /// 상태 이상이 디버프인지 확인합니다.
        /// </summary>
        public static bool IsDebuff(this StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Poison or StatusEffectType.Burn or StatusEffectType.Bleed or
                StatusEffectType.Stun or StatusEffectType.Silence or StatusEffectType.Freeze or StatusEffectType.Confusion or
                StatusEffectType.AttackReduction or StatusEffectType.DefenseReduction or 
                StatusEffectType.Vulnerability or StatusEffectType.Weakness or StatusEffectType.Curse => true,
                _ => false
            };
        }
        
        /// <summary>
        /// 상태 이상이 버프인지 확인합니다.
        /// </summary>
        public static bool IsBuff(this StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Regeneration or StatusEffectType.AttackBoost or 
                StatusEffectType.DefenseBoost or StatusEffectType.SpeedBoost or
                StatusEffectType.Shield or StatusEffectType.Invulnerability or
                StatusEffectType.Adrenaline => true,
                _ => false
            };
        }
        
        /// <summary>
        /// 상태 이상이 군중 제어(CC)인지 확인합니다.
        /// </summary>
        public static bool IsCrowdControl(this StatusEffectType type)
        {
            return type is StatusEffectType.Stun or StatusEffectType.Silence or StatusEffectType.Freeze or StatusEffectType.Confusion;
        }
        
        /// <summary>
        /// 상태 이상이 지속 데미지/회복 효과인지 확인합니다.
        /// </summary>
        public static bool IsOverTimeEffect(this StatusEffectType type)
        {
            return type is StatusEffectType.Poison or StatusEffectType.Burn or StatusEffectType.Bleed or StatusEffectType.Regeneration;
        }
        
        /// <summary>
        /// 상태 이상의 카테고리를 반환합니다.
        /// </summary>
        public static StatusEffectCategory GetCategory(this StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.AttackBoost or StatusEffectType.DefenseBoost or StatusEffectType.SpeedBoost or StatusEffectType.Adrenaline => StatusEffectCategory.Buff,
                StatusEffectType.AttackReduction or StatusEffectType.DefenseReduction or StatusEffectType.Vulnerability or StatusEffectType.Weakness or StatusEffectType.Curse => StatusEffectCategory.Debuff,
                StatusEffectType.Stun or StatusEffectType.Silence or StatusEffectType.Freeze or StatusEffectType.Confusion => StatusEffectCategory.CrowdControl,
                StatusEffectType.Poison or StatusEffectType.Burn or StatusEffectType.Bleed or StatusEffectType.Regeneration => StatusEffectCategory.DamageOverTime,
                StatusEffectType.Shield or StatusEffectType.Invulnerability => StatusEffectCategory.Protection,
                StatusEffectType.Mark or StatusEffectType.Echo or StatusEffectType.Berserk => StatusEffectCategory.Special,
                _ => StatusEffectCategory.Debuff
            };
        }
    }
}