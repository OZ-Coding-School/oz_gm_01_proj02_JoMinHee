using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonLog.Data;
using DungeonLog.StatusEffects;

namespace DungeonLog.Character
{
    /// <summary>
    /// 캐릭터를 나타내는 MonoBehaviour 클래스입니다.
    /// 모든 하위 시스템(Stats, Health, SkillManager, StatusEffectManager)을 조립하고 관리합니다.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(SkillManager))]
    [RequireComponent(typeof(StatusEffectManager))]
    [RequireComponent(typeof(AI.AIController))]
    public class Character : MonoBehaviour
    {
        [Header("Data Reference")]
        [SerializeField, Tooltip("캐릭터 데이터 (ScriptableObject)")]
        private CharacterData characterData;

        // 컴포넌트 캐싱
        private CharacterStats _stats;
        private Health _health;
        private SkillManager _skillManager;
        private StatusEffectManager _statusEffectManager;

        // 초기화 완료 플래그
        private bool _isInitialized = false;

        // ========================================================================
        // 프로퍼티 (편의성 접근자)
        // ========================================================================

        /// <summary>캐릭터 데이터</summary>
        public CharacterData Data => characterData;

        /// <summary>스탯 시스템</summary>
        public CharacterStats Stats => _stats;

        /// <summary>HP 시스템</summary>
        public Health Health => _health;

        /// <summary>스킬 관리자</summary>
        public SkillManager SkillManager => _skillManager;

        /// <summary>상태 이상 관리자</summary>
        public StatusEffectManager StatusEffects => _statusEffectManager;

        /// <summary>캐릭터 ID</summary>
        public string CharacterID => characterData != null ? characterData.ID : "Unknown";

        /// <summary>캐릭터 이름</summary>
        public string CharacterName => characterData != null ? characterData.DisplayName : "Unknown";

        /// <summary>생존 여부</summary>
        public bool IsAlive => _health != null && _health.IsAlive;

        /// <summary>사망 여부</summary>
        public bool IsDead => _health != null && _health.IsDead;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        private bool _isDataAssigned = false;

        private void Awake()
        {
            // 컴포넌트 캐싱 수행 (characterData 없어도 진행)
            InitializeComponents();
        }

        private void Start()
        {
            // characterData가 할당되었을 때만 초기화
            if (characterData != null && _isDataAssigned)
            {
                InitializeAllSystems();
            }
        }

        /// <summary>
        /// CharacterData를 설정하고 초기화를 수행합니다.
        /// Factory 패턴에서 사용하기 위해 제공됩니다.
        /// EnemyData도 BaseData를 상속하므로 BaseData로 받습니다.
        /// </summary>
        public void SetCharacterData(BaseData data)
        {
            if (data == null)
            {
                Debug.LogError($"[Character] 데이터 설정 실패: data가 null입니다.", this);
                return;
            }

            // CharacterData 형변환 시도
            if (data is CharacterData characterDataTyped)
            {
                characterData = characterDataTyped;
                _isDataAssigned = true;
            }
            else if (data is EnemyData enemyData)
            {
                // EnemyData는 별도 처리 (현재는 CharacterData가 필요하므로 경고)
                Debug.LogWarning($"[Character] {enemyData.ID}: EnemyData는 지원되지 않습니다. CharacterData만 사용 가능합니다.", this);
                return;
            }
            else
            {
                Debug.LogError($"[Character] 지원되지 않는 데이터 타입: {data.GetType().Name}", this);
                return;
            }

            // 이미 Start()가 지나갔으면 수동으로 초기화
            if (_isInitialized)
            {
                Debug.LogWarning($"[Character] {data.ID}: 이미 초기화되었습니다. 데이터만 설정합니다.");
                return;
            }

            // 컴포넌트가 캐싱되었는지 확인
            if (_stats == null || _health == null || _skillManager == null)
            {
                InitializeComponents();
            }

            // 즉시 초기화 수행
            InitializeAllSystems();
        }

        /// <summary>
        /// 모든 시스템을 초기화합니다.
        /// </summary>
        private void InitializeAllSystems()
        {
            if (_isInitialized) return;

            // 하위 시스템 초기화
            InitializeSystems();

            // 이벤트 구독
            SubscribeToEvents();

            _isInitialized = true;
            Debug.Log($"[Character] {CharacterID} ({CharacterName}) 초기화 완료");
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            UnsubscribeFromEvents();
        }

        // ========================================================================
        // 초기화
        // ========================================================================

        /// <summary>
        /// 컴포넌트를 초기화하고 캐싱합니다.
        /// </summary>
        private void InitializeComponents()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _skillManager = GetComponent<SkillManager>();
            _statusEffectManager = GetComponent<StatusEffectManager>();

            // 필수 컴포넌트 확인
            if (_stats == null)
            {
                Debug.LogError($"[Character] CharacterStats 컴포넌트가 없습니다.", this);
            }
            if (_health == null)
            {
                Debug.LogError($"[Character] Health 컴포넌트가 없습니다.", this);
            }
            if (_skillManager == null)
            {
                Debug.LogError($"[Character] SkillManager 컴포넌트가 없습니다.", this);
            }
            if (_statusEffectManager == null)
            {
                Debug.LogError($"[Character] StatusEffectManager 컴포넌트가 없습니다.", this);
            }
        }

        /// <summary>
        /// 하위 시스템을 초기화합니다.
        /// </summary>
        private void InitializeSystems()
        {
            // Stats 초기화 (먼저 실행)
            if (_stats != null)
            {
                _stats.Initialize(characterData);
            }

            // Health는 Start()에서 자체 초기화하므로 여기서 호출 불필요

            // SkillManager 초기화
            if (_skillManager != null)
            {
                // 기본 스킬 ID 리스트 가져오기
                List<string> defaultSkillIds = characterData?.DefaultSkillIds;
                if (defaultSkillIds != null && defaultSkillIds.Count > 0)
                {
                    _skillManager.Initialize(defaultSkillIds);
                }
            }
        }

        // ========================================================================
        // 이벤트 구독
        // ========================================================================

        private void SubscribeToEvents()
        {
            if (_stats != null)
            {
                _stats.OnStatChanged += HandleStatChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_stats != null)
            {
                _stats.OnStatChanged -= HandleStatChanged;
            }
        }

        // ========================================================================
        // 이벤트 핸들러
        // ========================================================================

        private void HandleStatChanged(StatType statType, int oldValue, int newValue)
        {
            // 스탯 변경 로그
            Debug.Log($"[Character] {CharacterID} 스탯 변경: {statType} {oldValue} → {newValue}");

            // MaxHP가 변경되면 현재 HP 비율 유지
            if (statType == StatType.MaxHP && _health != null)
            {
                float hpPercent = _health.HPPercent;
                int newMaxHP = newValue;
                int newCurrentHP = Mathf.RoundToInt(newMaxHP * hpPercent);
                _health.SetHP(newCurrentHP);
            }

            // CharacterEvents로 전파
            CharacterEvents.NotifyStatChanged(gameObject, statType, oldValue, newValue);
        }

        // ========================================================================
        // 전투 관련
        // ========================================================================

        /// <summary>
        /// 전투 시작 준비를 합니다.
        /// </summary>
        public void PrepareForBattle()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[Character] 초기화되지 않았습니다.");
                return;
            }

            _health?.ResetForBattle();
            _skillManager?.ResetForBattle();
            _statusEffectManager?.ClearAllEffects(); // 전투 시작 시 모든 상태 이상 정리

            Debug.Log($"[Character] {CharacterID} 전투 준비 완료");
        }

        /// <summary>
        /// 데미지를 입습니다.
        /// </summary>
        public void TakeDamage(int damage, bool isCritical = false)
        {
            _health?.TakeDamage(damage, isCritical);
        }

        /// <summary>
        /// 회복합니다.
        /// </summary>
        public void Heal(int amount)
        {
            _health?.Heal(amount);
        }

        /// <summary>
        /// 현재 스킬을 사용합니다.
        /// </summary>
        public void UseCurrentSkill(List<Character> targets)
        {
            if (_skillManager == null)
            {
                Debug.LogWarning($"[Character] SkillManager가 없습니다.");
                return;
            }

            // 기절 상태 확인
            if (_statusEffectManager != null && _statusEffectManager.IsStunned)
            {
                Debug.LogWarning($"[Character] {CharacterName}은(는) 기절 상태로 스킬 사용 불가!");
                StatusEffectEvents.NotifyStunAttempt(gameObject);
                return;
            }

            // 침묵 상태 확인
            if (_statusEffectManager != null && _statusEffectManager.IsSilenced)
            {
                Debug.LogWarning($"[Character] {CharacterName}은(는) 침묵 상태로 스킬 사용 불가!");
                return;
            }

            SkillData currentSkill = _skillManager.GetCurrentSkillData();
            if (currentSkill == null)
            {
                Debug.LogWarning($"[Character] 사용할 스킬이 없습니다.");
                return;
            }

            // TODO: SkillExecutor를 통한 스킬 실행 (Phase 5)
            Debug.Log($"[Character] {CharacterName}이(가) {currentSkill.DisplayName} 스킬 사용!");

            // 이벤트 발생
            GameObject[] targetObjects = new GameObject[targets.Count];
            for (int i = 0; i < targets.Count; i++)
            {
                targetObjects[i] = targets[i].gameObject;
            }
            CharacterEvents.NotifySkillUsed(gameObject, currentSkill, targetObjects);
        }

        // ========================================================================
        // 스탯 수정자
        // ========================================================================

        /// <summary>
        /// 스탯 수정자를 추가합니다.
        /// </summary>
        public void AddStatModifier(StatType statType, StatModifier modifier)
        {
            _stats?.AddModifier(statType, modifier);
        }

        /// <summary>
        /// 특정 출처의 모든 수정자를 제거합니다.
        /// </summary>
        public void RemoveStatModifier(object source)
        {
            _stats?.RemoveModifier(source);
        }

        // ========================================================================
        // 디버그
        // ========================================================================

        /// <summary>
        /// 캐릭터 정보를 로그로 출력합니다.
        /// </summary>
        public void LogCharacterInfo()
        {
            if (!_isInitialized)
            {
                Debug.Log($"[Character] 초기화되지 않음");
                return;
            }

            Debug.Log($"=== 캐릭터 정보: {CharacterID} ({CharacterName}) ===");
            Debug.Log($"HP: {_health?.CurrentHP ?? 0}/{_health?.MaxHP ?? 0}");
            Debug.Log($"공격력: {_stats?.Attack ?? 0}");
            Debug.Log($"방어력: {_stats?.Defense ?? 0}");
            Debug.Log($"치명타 확률: {_stats?.CriticalChance ?? 0:P0}");
            Debug.Log($"스킬 풀: {_skillManager?.TotalSkillPoolCount ?? 0}개");
            Debug.Log($"생존 여부: {IsAlive}");
            Debug.Log($"========================================");
        }
    }
}
