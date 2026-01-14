using UnityEngine;
using DungeonLog.Character;
using DungeonLog.Data;

namespace DungeonLog.StatusEffects
{
    /// <summary>
    /// 상태 이상 데이터를 정의하는 ScriptableObject입니다.
    /// BaseData를 상속하며, 에디터에서 데이터를 설정할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Dungeon Log/Status Effects/Status Effect Data")]
    public class StatusEffectData : BaseData
    {
        [Header("효과 설정")]
        [SerializeField, Tooltip("상태 이상 타입")]
        private StatusEffectType effectType;
        
        [SerializeField, Tooltip("기본 지속 시간 (턴)")]
        private int baseDuration = 3;
        
        [SerializeField, Tooltip("중첩 가능 여부")]
        private bool isStackable = false;
        
        [SerializeField, Tooltip("최대 중첩 수 (IsStackable=true일 때만 사용)")]
        private int maxStacks = 1;
        
        [SerializeField, Tooltip("중첩 동작 방식")]
        private StackBehavior stackBehavior = StackBehavior.RefreshDuration;
        
        [Header("강도 설정")]
        [SerializeField, Tooltip("효과 강도 계수 (스킬 등에서 오버라이드 가능)")]
        private float intensity = 1.0f;
        
        [SerializeField, Tooltip("기본 값 (독 데미지, 보호막량 등)")]
        private int baseValue = 0;
        
        [Header("스탯 수정 (버프/디버프용)")]
        [SerializeField, Tooltip("스탯을 수정하는지 여부")]
        private bool modifiesStat = false;
        
        [SerializeField, Tooltip("대상 스탯")]
        private StatType targetStat = StatType.Attack;
        
        [SerializeField, Tooltip("스탯 수정 연산 방식")]
        private StatModifierOperation statOperation = StatModifierOperation.PercentageBonus;
        
        [SerializeField, Tooltip("스탯 수정 값")]
        private float statValue = 0f;
        
        [Header("저항 및 해제")]
        [SerializeField, Tooltip("기본 저항률 (0-100%)")]
        private float baseResistance = 0f;
        
        [SerializeField, Tooltip("정화 가능 여부 (치료 스킬 등으로 제거 가능)")]
        private bool isDispellable = true;
        
        [SerializeField, Tooltip("데미지를 받으면 해제되는지 (예: 은신)")]
        private bool removedByDamage = false;
        
        [SerializeField, Tooltip("사망 후에도 유지되는지 (부활 시 효과 유지)")]
        private bool persistsThroughDeath = false;
        
        [Header("시각 효과")]
        [SerializeField, Tooltip("상태 이상 아이콘")]
        private Sprite icon;
        
        [SerializeField, Tooltip("VFX 프리팹")]
        private GameObject vfxPrefab;
        
        [SerializeField, Tooltip("효과 색상 (UI 등에서 사용)")]
        private Color effectColor = Color.white;
        
        [Header("상세 정보")]
        [SerializeField, TextArea(3, 6)]
        [Tooltip("상태 이상 설명")]
        protected string effectDescription;
        
        // ============ 속성 접근자 ============
        
        /// <summary>상태 이상 타입</summary>
        public StatusEffectType EffectType => effectType;
        
        /// <summary>기본 지속 시간 (턴)</summary>
        public int BaseDuration => baseDuration;
        
        /// <summary>중첩 가능 여부</summary>
        public bool IsStackable => isStackable;
        
        /// <summary>최대 중첩 수</summary>
        public int MaxStacks => maxStacks;
        
        /// <summary>중첩 동작 방식</summary>
        public StackBehavior StackBehavior => stackBehavior;
        
        /// <summary>효과 강도 계수</summary>
        public float Intensity => intensity;
        
        /// <summary>기본 값 (독 데미지, 보호막량 등)</summary>
        public int BaseValue => baseValue;
        
        /// <summary>스탯을 수정하는지 여부</summary>
        public bool ModifiesStat => modifiesStat;
        
        /// <summary>대상 스탯</summary>
        public StatType TargetStat => targetStat;
        
        /// <summary>스탯 수정 연산 방식</summary>
        public StatModifierOperation StatOperation => statOperation;
        
        /// <summary>스탯 수정 값</summary>
        public float StatValue => statValue;
        
        /// <summary>기본 저항률</summary>
        public float BaseResistance => baseResistance;
        
        /// <summary>정화 가능 여부</summary>
        public bool IsDispellable => isDispellable;
        
        /// <summary>데미지를 받으면 해제되는지</summary>
        public bool RemovedByDamage => removedByDamage;
        
        /// <summary>사망 후에도 유지되는지</summary>
        public bool PersistsThroughDeath => persistsThroughDeath;
        
        /// <summary>상태 이상 아이콘</summary>
        public Sprite Icon => icon;
        
        /// <summary>VFX 프리팹</summary>
        public GameObject VFXPrefab => vfxPrefab;
        
        /// <summary>효과 색상</summary>
        public Color EffectColor => effectColor;
        
        /// <summary>상태 이상 설명</summary>
        public string EffectDescription => effectDescription;
        
        /// <summary>
        /// StatModifier 기반 효과인지 확인합니다.
        /// 단순 스탯 버프/디버프는 기존 StatModifier 시스템을 활용합니다.
        /// </summary>
        public virtual bool IsStatModifierBased => modifiesStat;
        
        /// <summary>
        /// StatModifier를 생성합니다.
        /// </summary>
        public virtual StatModifier CreateStatModifier(float intensityMultiplier, int duration)
        {
            if (!modifiesStat)
            {
                Debug.LogWarning($"[StatusEffectData] {ID}: 스탯 수정 효과가 아닙니다.");
                return default;
            }
            
            return new StatModifier
            {
                Value = statValue * intensityMultiplier,
                Operation = statOperation,
                Priority = 0,
                Source = this,
                DurationTurns = duration > 0 ? duration : baseDuration
            };
        }
        
        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate()) return false;
            
            if (baseDuration <= 0)
            {
                Debug.LogWarning($"[StatusEffectData] {ID}: 지속 시간은 1 턴 이상이어야 합니다. (현재: {baseDuration})");
                return false;
            }
            
            if (isStackable && maxStacks <= 1)
            {
                Debug.LogWarning($"[StatusEffectData] {ID}: 중첩 가능 효과는 최대 2스택 이상이어야 합니다. (현재: {maxStacks})");
                return false;
            }
            
            if (modifiesStat && statValue == 0f)
            {
                Debug.LogWarning($"[StatusEffectData] {ID}: 스탯 수정 효과지만 수치가 0입니다.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 디버그용 정보를 반환합니다.
        /// </summary>
        public override string ToString()
        {
            return $"[StatusEffectData] {DisplayName} ({effectType}) - {baseDuration}턴, 강도: {intensity}";
        }
    }
}