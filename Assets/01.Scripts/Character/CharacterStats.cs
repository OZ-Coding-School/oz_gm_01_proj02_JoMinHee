using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DungeonLog.Data;

namespace DungeonLog.Character
{
    /// <summary>
    /// 캐릭터의 스탯을 관리하고 계산하는 클래스입니다.
    /// 기본 스탯과 StatModifier를 조합하여 최종 스탯을 계산합니다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CharacterStats : MonoBehaviour
    {
        [Header("Data Reference")]
        [SerializeField, Tooltip("캐릭터 데이터 (ScriptableObject)")]
        private CharacterData characterData;

        // 스탯 수정자 저장 (StatType -> List<StatModifier>)
        private Dictionary<StatType, List<StatModifier>> _modifiers;

        // 초기화 완료 플래그
        private bool _isInitialized = false;

        // 이벤트
        public event Action<StatType, int, int> OnStatChanged; // (statType, oldValue, newValue)

        // ========================================================================
        // 초기화
        // ========================================================================

        private void Awake()
        {
            _modifiers = new Dictionary<StatType, List<StatModifier>>();
        }

        /// <summary>
        /// 캐릭터 데이터로 초기화합니다.
        /// </summary>
        public void Initialize(CharacterData data)
        {
            if (data == null)
            {
                Debug.LogError($"[CharacterStats] CharacterData가 null입니다.", this);
                return;
            }

            characterData = data;
            _isInitialized = true;

            Debug.Log($"[CharacterStats] {characterData.ID} 초기화 완료");
        }

        /// <summary>
        /// 초기화되었는지 확인합니다.
        /// </summary>
        public bool IsInitialized => _isInitialized && characterData != null;

        // ========================================================================
        // 스탯 계산
        // ========================================================================

        /// <summary>
        /// 지정된 스탯 타입의 최종 값을 계산합니다.
        /// 기본 스탯 + 모든 수정자를 순서대로 적용합니다.
        /// </summary>
        public int GetFinalStat(StatType statType)
        {
            if (!_isInitialized || characterData == null)
            {
                Debug.LogWarning($"[CharacterStats] 초기화되지 않았습니다.");
                return 0;
            }

            // 1. 기본 스탯 가져오기
            float baseValue = GetBaseStat(statType);

            // 2. 수정자가 없으면 기본값 반환
            if (!_modifiers.ContainsKey(statType) || _modifiers[statType].Count == 0)
            {
                return Mathf.RoundToInt(baseValue);
            }

            // 3. Operation 순서대로 정렬 후 적용
            // BaseAddition (0) → PercentageBonus (1) → Multiplicative (2) → FinalOverride (3)
            var sortedModifiers = _modifiers[statType]
                .OrderBy(m => (int)m.Operation)
                .ThenBy(m => m.Priority);

            float result = baseValue;

            foreach (var modifier in sortedModifiers)
            {
                switch (modifier.Operation)
                {
                    case StatModifierOperation.BaseAddition:
                        result += modifier.Value;
                        break;

                    case StatModifierOperation.PercentageBonus:
                        // 현재 값(result)을 기준으로 퍼센트 증가 (BaseAddition과의 중첩 문제 해결)
                        float multiplier = 1f + (modifier.Value / 100f);
                        result = Mathf.RoundToInt(result * multiplier);
                        break;

                    case StatModifierOperation.Multiplicative:
                        result *= modifier.Value;
                        break;

                    case StatModifierOperation.FinalOverride:
                        result = modifier.Value;
                        break;
                }
            }

            return Mathf.RoundToInt(result);
        }

        /// <summary>
        /// 기본 스탯 값을 가져옵니다.
        /// </summary>
        private float GetBaseStat(StatType statType)
        {
            if (characterData == null) return 0;

            return statType switch
            {
                StatType.MaxHP => characterData.BaseHP,
                StatType.Attack => characterData.BaseAttack,
                StatType.Defense => characterData.BaseDefense,
                StatType.CriticalChance => characterData.BaseCriticalChance * 100f, // 퍼센트로 변환
                StatType.Speed => 100f, // 기본 속도 (향후 확장 가능)
                _ => 0
            };
        }

        // ========================================================================
        // 수정자 관리
        // ========================================================================

        /// <summary>
        /// 스탯 수정자를 추가합니다.
        /// </summary>
        public void AddModifier(StatType statType, StatModifier modifier)
        {
            if (!_modifiers.ContainsKey(statType))
            {
                _modifiers[statType] = new List<StatModifier>();
            }

            int oldValue = GetFinalStat(statType);
            _modifiers[statType].Add(modifier);
            int newValue = GetFinalStat(statType);

            Debug.Log($"[CharacterStats] {statType}에 수정자 추가: {modifier}");
            OnStatChanged?.Invoke(statType, oldValue, newValue);
        }

        /// <summary>
        /// 특정 출처의 모든 수정자를 제거합니다.
        /// </summary>
        public void RemoveModifier(object source)
        {
            if (source == null) return;

            foreach (var statType in _modifiers.Keys.ToList())
            {
                var toRemove = _modifiers[statType]
                    .Where(m => ReferenceEquals(m.Source, source))
                    .ToList();

                foreach (var modifier in toRemove)
                {
                    int oldValue = GetFinalStat(statType);
                    _modifiers[statType].Remove(modifier);
                    int newValue = GetFinalStat(statType);

                    Debug.Log($"[CharacterStats] {statType}에서 수정자 제거: {modifier}");
                    OnStatChanged?.Invoke(statType, oldValue, newValue);
                }
            }
        }

        /// <summary>
        /// 모든 수정자를 제거합니다.
        /// </summary>
        public void ClearAllModifiers()
        {
            _modifiers.Clear();
            Debug.Log($"[CharacterStats] 모든 수정자가 제거되었습니다.");
        }

        // ========================================================================
        // 프로퍼티 (편의성 접근자)
        // ========================================================================

        /// <summary>최대 HP</summary>
        public int MaxHP => GetFinalStat(StatType.MaxHP);

        /// <summary>공격력</summary>
        public int Attack => GetFinalStat(StatType.Attack);

        /// <summary>방어력</summary>
        public int Defense => GetFinalStat(StatType.Defense);

        /// <summary>치명타 확률 (0.0 ~ 1.0)</summary>
        public float CriticalChance => GetFinalStat(StatType.CriticalChance) / 100f;

        /// <summary>이동 속도</summary>
        public int Speed => GetFinalStat(StatType.Speed);

        // ========================================================================
        // 생명주기 정리
        // ========================================================================

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (OnStatChanged != null)
            {
                foreach (Delegate subscriber in OnStatChanged.GetInvocationList())
                {
                    OnStatChanged -= (Action<StatType, int, int>)subscriber;
                }
            }
        }
    }
}
