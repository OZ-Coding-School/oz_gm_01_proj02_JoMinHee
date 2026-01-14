using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DungeonLog.Character;
using DungeonLog.Combat;

namespace DungeonLog.StatusEffects
{
    /// <summary>
    /// 캐릭터별 상태 이상을 관리하는 MonoBehaviour입니다.
    /// Character 컴포넌트로 추가되어 각 캐릭터의 상태 이상을 독립적으로 관리합니다.
    ///
    /// 리팩토링 v2: IStatusEffectManager 인터페이스 구현
    /// - 결합도 감소
    /// - 테스트 가능성 향상
    /// - 의존성 주입 용이
    /// </summary>
    [DefaultExecutionOrder(-35)] // CharacterStats(-50)와 Health(-30) 사이에서 실행
    public class StatusEffectManager : MonoBehaviour, IStatusEffectManager
    {
        #region 필드
        
        // ============ 관리 컬렉션 ============
        
        /// <summary>활성 상태 이상 리스트 (순회용)</summary>
        private List<StatusEffectInstance> _activeEffects = new();
        
        /// <summary>효과 타입별 인덱스 (빠른 조회용 O(1))</summary>
        private Dictionary<StatusEffectType, List<StatusEffectInstance>> _effectsByType = new();
        
        /// <summary>효과 ID별 인덱스 (유니크 ID 조회용)</summary>
        private Dictionary<int, StatusEffectInstance> _effectsById = new();
        
        // ============ 캐싱 ============
        
        private Character.Character _character;
        private CharacterStats _stats;
        private Health _health;
        
        // ============ 캐시 (성능 최적화) ============
        
        private List<StatusEffectInstance> _toRemoveCache = new();
        private bool _isCacheDirty = true;
        
        #endregion
        
        #region 속성
        
        /// <summary>활성 상태 이상 리스트 (읽기 전용)</summary>
        public IReadOnlyList<StatusEffectInstance> ActiveEffects => _activeEffects.AsReadOnly();
        
        /// <summary>활성 상태 이상 수</summary>
        public int ActiveEffectCount => _activeEffects.Count;
        
        /// <summary>기절 상태인지 확인</summary>
        public bool IsStunned => HasEffectType(StatusEffectType.Stun) || HasEffectType(StatusEffectType.Freeze);
        
        /// <summary>침묵 상태인지 확인</summary>
        public bool IsSilenced => HasEffectType(StatusEffectType.Silence);
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void OnEnable()
        {
            SubscribeToEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        private void OnDestroy()
        {
            ClearAllEffects();
        }
        
        #endregion
        
        #region 초기화
        
        /// <summary>
        /// 컴포넌트를 캐싱합니다.
        /// </summary>
        private void InitializeComponents()
        {
            _character = GetComponent<Character.Character>();
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            
            if (_character == null)
            {
                Debug.LogError("[StatusEffectManager] Character 컴포넌트를 찾을 수 없습니다.", this);
            }
        }
        
        /// <summary>
        /// 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_health != null)
            {
                // HP가 0이 되면 모든 상태 이상 정리
            }
        }
        
        /// <summary>
        /// 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // 이벤트 구독 해제
        }
        
        #endregion
        
        #region 상태 이상 적용
        
        /// <summary>
        /// 상태 이상을 적용합니다.
        /// </summary>
        /// <param name="effectData">상태 이상 데이터</param>
        /// <param name="sourceCharacter">시전자 (null 가능)</param>
        /// <param name="source">출처 (SkillData, ItemData 등)</param>
        /// <param name="intensityMultiplier">강도 계수</param>
        /// <param name="customDuration">커스텀 지속 시간 (0이면 기본값 사용)</param>
        /// <returns>적용 성공 여부</returns>
        public bool ApplyEffect(StatusEffectData effectData, Character.Character sourceCharacter, object source = null, float intensityMultiplier = 1f, int customDuration = 0)
        {
            if (effectData == null)
            {
                Debug.LogError("[StatusEffectManager] 효과 데이터가 null입니다.");
                return false;
            }
            
            if (_character == null)
            {
                Debug.LogError("[StatusEffectManager] Character가 없습니다.");
                return false;
            }
            
            // 저항 체크
            float resistance = CalculateResistance(effectData);
            if (UnityEngine.Random.Range(0f, 100f) < resistance)
            {
                StatusEffectEvents.NotifyEffectResisted(gameObject, effectData);
                return false;
            }
            
            // 기존 효과 확인
            var existing = FindEffectByType(effectData.EffectType);

            // null 체크 후 Data 프로퍼티 접근 (FindEffectByType은 null을 반환할 수 있음)
            if (existing != null && existing.Data != null)
            {
                return HandleExistingEffect(existing, effectData, sourceCharacter, source, intensityMultiplier, customDuration);
            }
            
            // 새로운 효과 적용
            return ApplyNewEffect(effectData, sourceCharacter, source, intensityMultiplier, customDuration);
        }
        
        /// <summary>
        /// 저항률을 계산합니다.
        /// </summary>
        private float CalculateResistance(StatusEffectData effectData)
        {
            // 기본 저항
            float resistance = effectData.BaseResistance;
            
            // 스탯 기반 저항 (예: 방어력이 높을수록 기절 저항)
            if (_stats != null && effectData.EffectType.IsCrowdControl())
            {
                int defense = _stats.GetFinalStat(StatType.Defense);
                resistance += Mathf.Min(50f, defense * 0.1f); // 방어력 100당 10% 최대 50%
            }
            
            return Mathf.Clamp(resistance, 0f, 100f);
        }
        
        /// <summary>
        /// 기존 효과를 처리합니다 (중첩 또는 갱신).
        /// </summary>
        private bool HandleExistingEffect(StatusEffectInstance existing, StatusEffectData newData, Character.Character sourceCharacter, object source, float intensityMultiplier, int customDuration)
        {
            switch (newData.StackBehavior)
            {
                case StackBehavior.Ignore:
                    Debug.Log($"[StatusEffectManager] {newData.DisplayName}: 중첩 불가로 무시");
                    return false;
                    
                case StackBehavior.RefreshDuration:
                    existing.RefreshDuration();
                    StatusEffectEvents.NotifyEffectRefreshed(gameObject, existing);
                    return true;
                    
                case StackBehavior.AdditiveDuration:
                    existing.RemainingTurns += customDuration > 0 ? customDuration : newData.BaseDuration;
                    StatusEffectEvents.NotifyEffectRefreshed(gameObject, existing);
                    return true;
                    
                case StackBehavior.MaxDuration:
                    existing.RemainingTurns = Mathf.Max(existing.RemainingTurns, customDuration > 0 ? customDuration : newData.BaseDuration);
                    StatusEffectEvents.NotifyEffectRefreshed(gameObject, existing);
                    return true;
                    
                case StackBehavior.AddStack:
                    if (!newData.IsStackable || existing.IsMaxStacked)
                    {
                        existing.RefreshDuration();
                        StatusEffectEvents.NotifyEffectRefreshed(gameObject, existing);
                        return true;
                    }
                    
                    if (existing.AddStack())
                    {
                        StatusEffectEvents.NotifyEffectStacked(gameObject, existing);
                        return true;
                    }
                    return false;
                    
                case StackBehavior.IndependentStacks:
                    return ApplyNewEffect(newData, sourceCharacter, source, intensityMultiplier, customDuration);
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 새로운 효과를 적용합니다.
        /// </summary>
        private bool ApplyNewEffect(StatusEffectData effectData, Character.Character sourceCharacter, object source, float intensityMultiplier, int customDuration)
        {
            var instance = new StatusEffectInstance(effectData, _character, sourceCharacter, source, intensityMultiplier, customDuration);
            
            _activeEffects.Add(instance);
            
            // 타입별 인덱싱
            if (!_effectsByType.ContainsKey(effectData.EffectType))
            {
                _effectsByType[effectData.EffectType] = new List<StatusEffectInstance>();
            }
            _effectsByType[effectData.EffectType].Add(instance);
            
            // ID 인덱싱
            _effectsById[instance.InstanceId] = instance;
            
            // 즉시 효과 적용 (스탯 수정자)
            ApplyImmediateEffects(instance);
            
            StatusEffectEvents.NotifyEffectApplied(gameObject, instance);
            return true;
        }
        
        /// <summary>
        /// 즉시 효과를 적용합니다 (스탯 수정자 등).
        /// </summary>
        private void ApplyImmediateEffects(StatusEffectInstance instance)
        {
            if (!instance.Data.ModifiesStat || _stats == null) return;
            
            var modifier = instance.Data.CreateStatModifier(instance.ActualIntensity, instance.RemainingTurns);
            if (modifier.Value == 0) return;
            
            _stats.AddModifier(instance.Data.TargetStat, modifier);
            Debug.Log($"[StatusEffectManager] {instance.Data.DisplayName}: {instance.Data.TargetStat} 스탯 수정자 추가 ({modifier.Value})");
        }
        
        #endregion
        
        #region 턴 처리
        
        /// <summary>
        /// 턴 시작 시 상태 이상을 처리합니다.
        /// </summary>
        public void ProcessTurnStart()
        {
            // GC 최적화: ToArray() 제거, 역순회로 안전한 제거
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                StatusEffectEvents.NotifyEffectTurnStart(gameObject, effect);

                // 기절 체크 등 턴 시작 처리
                if (effect.Data.EffectType == StatusEffectType.Stun || effect.Data.EffectType == StatusEffectType.Freeze)
                {
                    StatusEffectEvents.NotifyStunAttempt(gameObject);
                }
            }
        }
        
        /// <summary>
        /// 턴 종료 시 상태 이상을 처리합니다.
        /// </summary>
        public void ProcessTurnEnd()
        {
            _toRemoveCache.Clear();

            // GC 최적화: ToArray() 제거, 캐시된 리스트 사용
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];

                // 지속 효과 처리 (독, 재생 등)
                ProcessTickEffect(effect);

                // 턴 경과
                effect.Tick();

                StatusEffectEvents.NotifyEffectTurnEnd(gameObject, effect);

                // 만료 체크
                if (effect.IsExpired)
                {
                    _toRemoveCache.Add(effect);
                }
            }

            // 만료된 효과 제거
            foreach (var effect in _toRemoveCache)
            {
                RemoveEffect(effect);
            }
        }
        
        /// <summary>
        /// 지속 효과를 처리합니다.
        /// </summary>
        private void ProcessTickEffect(StatusEffectInstance effect)
        {
            int value = Mathf.RoundToInt(effect.GetTotalValue());
            
            switch (effect.Data.EffectType)
            {
                case StatusEffectType.Poison:
                case StatusEffectType.Burn:
                case StatusEffectType.Bleed:
                    if (_health != null && value > 0)
                    {
                        _health.TakeDamage(value);
                        StatusEffectEvents.NotifyEffectDamage(gameObject, value, effect);
                    }
                    break;
                    
                case StatusEffectType.Regeneration:
                    if (_health != null && value > 0)
                    {
                        _health.Heal(value);
                        StatusEffectEvents.NotifyEffectHeal(gameObject, value, effect);
                    }
                    break;
            }
        }
        
        #endregion
        
        #region 상태 이상 제거
        
        /// <summary>
        /// 특정 효과를 제거합니다.
        /// </summary>
        public void RemoveEffect(StatusEffectInstance instance)
        {
            if (instance.Data == null) return;
            
            _activeEffects.Remove(instance);
            _effectsByType[instance.Data.EffectType].Remove(instance);
            _effectsById.Remove(instance.InstanceId);
            
            // 스탯 수정자 제거
            if (instance.Data.ModifiesStat && _stats != null)
            {
                _stats.RemoveModifier(instance.Source);
            }
            
            StatusEffectEvents.NotifyEffectRemoved(gameObject, instance);
        }
        
        /// <summary>
        /// 특정 타입의 모든 효과를 제거합니다.
        /// </summary>
        public void RemoveEffectsByType(StatusEffectType type)
        {
            if (!_effectsByType.ContainsKey(type)) return;
            
            foreach (var effect in _effectsByType[type].ToList())
            {
                RemoveEffect(effect);
            }
        }
        
        /// <summary>
        /// 모든 상태 이상을 제거합니다.
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects.ToList())
            {
                if (effect.Data.PersistsThroughDeath) continue;
                
                _activeEffects.Remove(effect);
                _effectsByType[effect.Data.EffectType].Remove(effect);
                _effectsById.Remove(effect.InstanceId);
                
                if (effect.Data.ModifiesStat && _stats != null)
                {
                    _stats.RemoveModifier(effect.Source);
                }
                
                StatusEffectEvents.NotifyEffectRemoved(gameObject, effect);
            }
            
            Debug.Log($"[StatusEffectManager] 모든 상태 이상 제거 ({_activeEffects.Count}개 남음)");
        }
        
        /// <summary>
        /// 정화 가능한 모든 효과를 제거합니다.
        /// </summary>
        public void DispelAllEffects()
        {
            foreach (var effect in _activeEffects.ToList())
            {
                if (!effect.Data.IsDispellable) continue;
                
                StatusEffectEvents.NotifyEffectDispelled(gameObject, effect);
                RemoveEffect(effect);
            }
        }
        
        #endregion
        
        #region 조회
        
        /// <summary>
        /// 특정 타입의 효과를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasEffectType(StatusEffectType type)
        {
            return _effectsByType.ContainsKey(type) && _effectsByType[type].Count > 0;
        }
        
        /// <summary>
        /// 특정 카테고리의 효과를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasEffectCategory(StatusEffectCategory category)
        {
            return _activeEffects.Any(e => e.Data.EffectType.GetCategory() == category);
        }
        
        /// <summary>
        /// 특정 타입의 효과를 찾습니다.
        /// </summary>
        public StatusEffectInstance FindEffectByType(StatusEffectType type)
        {
            if (!_effectsByType.ContainsKey(type) || _effectsByType[type].Count == 0)
            {
                return default;
            }
            
            return _effectsByType[type][0]; // 첫 번째 효과 반환
        }
        
        /// <summary>
        /// 특정 타입의 중첩 수를 반환합니다.
        /// </summary>
        public int GetStackCount(StatusEffectType type)
        {
            if (!HasEffectType(type)) return 0;
            
            return _effectsByType[type].Sum(e => e.CurrentStacks);
        }
        
        #endregion
        
        #region 데미지 계산 연동
        
        /// <summary>
        /// 주는 데미지를 수정합니다 (공격자 측).
        /// </summary>
        public int ModifyOutgoingDamage(int baseDamage, DamageContext context)
        {
            int modified = baseDamage;

            // GC 최적화: ToArray() 제거
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];

                switch (effect.Data.EffectType)
                {
                    case StatusEffectType.AttackBoost:
                    case StatusEffectType.Adrenaline:
                        modified = Mathf.RoundToInt(modified * (1f + effect.Data.StatValue * effect.ActualIntensity * effect.CurrentStacks));
                        break;

                    case StatusEffectType.Weakness:
                        modified = Mathf.RoundToInt(modified * (1f - effect.Data.StatValue * effect.ActualIntensity * effect.CurrentStacks));
                        break;

                    case StatusEffectType.Berserk:
                        modified = Mathf.RoundToInt(modified * 1.5f); // 광폭화: 50% 증가
                        break;
                }
            }

            return modified;
        }
        
        /// <summary>
        /// 받는 데미지를 수정합니다 (대상 측).
        /// </summary>
        public int ModifyIncomingDamage(int baseDamage, DamageContext context)
        {
            int modified = baseDamage;
            
            foreach (var effect in _activeEffects)
            {
                switch (effect.Data.EffectType)
                {
                    case StatusEffectType.DefenseBoost:
                        modified = Mathf.RoundToInt(modified * (1f - effect.Data.StatValue * effect.ActualIntensity * effect.CurrentStacks));
                        break;
                        
                    case StatusEffectType.DefenseReduction:
                    case StatusEffectType.Burn:
                    case StatusEffectType.Vulnerability:
                        modified = Mathf.RoundToInt(modified * (1f + effect.Data.StatValue * effect.ActualIntensity * effect.CurrentStacks));
                        break;
                        
                    case StatusEffectType.Shield:
                        int shieldValue = Mathf.RoundToInt(effect.GetTotalValue());
                        if (shieldValue > 0)
                        {
                            int absorbed = Mathf.Min(shieldValue, modified);
                            modified -= absorbed;

                            // 보호막 소진 로직 구현
                            if (effect.Data.IsStackable)
                            {
                                // 스택 기반 보호막: 스택으로 감소
                                int stacksToConsume = Mathf.CeilToInt((float)absorbed / effect.Data.BaseValue);
                                effect.CurrentStacks = Mathf.Max(0, effect.CurrentStacks - stacksToConsume);
                                if (effect.CurrentStacks == 0)
                                {
                                    _toRemoveCache.Add(effect);
                                }
                            }
                            else
                            {
                                // 단일 보호막: 즉시 소진
                                effect.RemainingTurns = 0;
                                _toRemoveCache.Add(effect);
                            }

                            StatusEffectEvents.NotifyEffectShieldBroken(gameObject, absorbed, effect);
                        }
                        break;
                        
                    case StatusEffectType.Invulnerability:
                        modified = 0;
                        break;
                        
                    case StatusEffectType.Freeze:
                        modified = Mathf.RoundToInt(modified * 1.3f); // 빙면: 받는 데미지 30% 증가
                        break;
                }
            }
            
            return Mathf.Max(0, modified);
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 현재 활성 상태 이상을 로그로 출력합니다.
        /// </summary>
        public void LogActiveEffects()
        {
            if (_activeEffects.Count == 0)
            {
                Debug.Log($"[StatusEffectManager] {_character?.CharacterName}: 활성 상태 이상 없음");
                return;
            }
            
            Debug.Log($"[StatusEffectManager] {_character?.CharacterName} 활성 상태 이상 ({_activeEffects.Count}개):");
            foreach (var effect in _activeEffects)
            {
                Debug.Log($"  - {effect}");
            }
        }
        
        #endregion
    }
}