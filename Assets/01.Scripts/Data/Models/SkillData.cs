using System.Collections.Generic;
using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 스킬 타입을 정의합니다.
    /// </summary>
    public enum SkillType
    {
        Attack,     // 공격
        Buff,       // 버프
        Debuff,     // 디버프
        Heal        // 회복
    }

    /// <summary>
    /// 타겟 타입을 정의합니다.
    /// </summary>
    public enum TargetType
    {
        Single,     // 단일
        AoE,        // 전체 (범위)
        Self,       // 자신
        AllAllies,  // 아군 전체
        Random      // 랜덤
    }

    /// <summary>
    /// 스킬 효과 타입을 정의합니다.
    /// </summary>
    public enum SkillEffectType
    {
        Damage,             // 데미지
        Heal,               // 회복
        DefenseUp,          // 방어력 증가
        AttackUp,           // 공격력 증가
        DefenseDown,        // 방어력 감소
        AttackDown,         // 공격력 감소
        Poison,             // 독
        Stun,               // 기절
        HighCrit,           // 높은 치명타율
        MultiHit            // 다중 타격
    }

    /// <summary>
    /// 스킬 데이터를 정의하는 ScriptableObject입니다.
    /// 스킬의 기본 정보, AP 비용, 타입, 효과 등을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Dungeon Log/Skill Data")]
    public class SkillData : BaseData, IDataLoadable
    {
        [Header("스킬 정보")]
        [SerializeField, Tooltip("스킬 타입")]
        private SkillType skillType;

        [SerializeField, Tooltip("AP 소모량")]
        private int apCost;

        [SerializeField, Tooltip("타겟 타입")]
        private TargetType targetType;

        [Header("데미지/회복")]
        [SerializeField, Tooltip("기본 데미지")]
        private int baseDamage = 0;

        [SerializeField, Tooltip("데미지 배율 (공격력에 곱할 계수)")]
        private float damageMultiplier = 1.0f;

        [SerializeField, Tooltip("치명타 확률 보정 (0.0 ~ 1.0)")]
        private float criticalBonus = 0f;

        [Header("효과")]
        [SerializeField, Tooltip("스킬 효과 목록")]
        private List<SkillEffectType> effects;

        [Header("시각 정보")]
        [SerializeField, Tooltip("스킬 아이콘")]
        private Sprite icon;

        [SerializeField, Tooltip("스킬 이펙트 프리팹")]
        private GameObject effectPrefab;

        [Header("상태 이상")]
        [SerializeField, Tooltip("부여할 상태 이상 지속 시간 (턴)")]
        private int statusEffectDuration = 0;

        [SerializeField, Tooltip("상태 이상 강도")]
        private float statusEffectIntensity = 1.0f;

        // 속성 접근자
        public SkillType SkillType => skillType;
        public int APCost => apCost;
        public TargetType TargetType => targetType;
        public int BaseDamage => baseDamage;
        public float DamageMultiplier => damageMultiplier;
        public float CriticalBonus => criticalBonus;
        public List<SkillEffectType> Effects => effects;
        public Sprite Icon => icon;
        public GameObject EffectPrefab => effectPrefab;
        public int StatusEffectDuration => statusEffectDuration;
        public float StatusEffectIntensity => statusEffectIntensity;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            if (apCost <= 0)
            {
                Debug.LogWarning($"[SkillData] {ID}: AP 비용은 0보다 커야 합니다.");
                return false;
            }

            if (effects == null || effects.Count == 0)
            {
                Debug.LogWarning($"[SkillData] {ID}: 최소 1개의 효과가 필요합니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// CSV 데이터를 로드합니다.
        /// </summary>
        public void LoadFromCSV(Dictionary<string, string> csvData)
        {
            if (csvData.ContainsKey("ID"))
                SetID(csvData["ID"]);

            if (csvData.ContainsKey("Name"))
                displayName = csvData["Name"];

            if (csvData.ContainsKey("Description"))
                description = csvData["Description"];

            if (csvData.ContainsKey("APCost") && int.TryParse(csvData["APCost"], out int ap))
                apCost = ap;

            if (csvData.ContainsKey("SkillType") && System.Enum.TryParse<SkillType>(csvData["SkillType"], out SkillType type))
                skillType = type;

            if (csvData.ContainsKey("TargetType") && System.Enum.TryParse<TargetType>(csvData["TargetType"], out TargetType target))
                targetType = target;

            if (csvData.ContainsKey("DamageMultiplier") && float.TryParse(csvData["DamageMultiplier"], out float dmg))
                damageMultiplier = dmg;

            if (csvData.ContainsKey("CriticalBonus") && float.TryParse(csvData["CriticalBonus"], out float crit))
                criticalBonus = crit;

            // 효과 목록 파싱 (세미콜론으로 구분)
            if (csvData.ContainsKey("Effects"))
            {
                effects = new List<SkillEffectType>();
                string[] effectArray = csvData["Effects"].Split(';');
                foreach (string effectStr in effectArray)
                {
                    if (System.Enum.TryParse<SkillEffectType>(effectStr.Trim(), out SkillEffectType effect))
                    {
                        effects.Add(effect);
                    }
                }
            }

            if (csvData.ContainsKey("StatusDuration") && int.TryParse(csvData["StatusDuration"], out int duration))
                statusEffectDuration = duration;

            if (csvData.ContainsKey("StatusIntensity") && float.TryParse(csvData["StatusIntensity"], out float intensity))
                statusEffectIntensity = intensity;
        }
    }
}
