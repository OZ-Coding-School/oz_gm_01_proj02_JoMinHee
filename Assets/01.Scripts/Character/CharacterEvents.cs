using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonLog.Character
{
    /// <summary>
    /// 캐릭터 관련 이벤트를 중앙에서 관리하는 정적 클래스입니다.
    /// Observer 패턴을 사용하여 결합도를 최소화합니다.
    /// 메모리 누수 수정: 씬 전환 시 자동으로 이벤트를 정리합니다.
    ///
    /// 사용 예시:
    /// <code>
    /// // 이벤트 구독
    /// private void OnEnable()
    /// {
    ///     CharacterEvents.OnHealthChanged += HandleHealthChanged;
    ///     CharacterEvents.OnCharacterDeath += HandleDeath;
    /// }
    ///
    /// // 이벤트 해제
    /// private void OnDisable()
    /// {
    ///     CharacterEvents.OnHealthChanged -= HandleHealthChanged;
    ///     CharacterEvents.OnCharacterDeath -= HandleDeath;
    /// }
    /// </code>
    /// </summary>
    public static class CharacterEvents
    {
        // 정적 생성자에서 씬 전환 이벤트 구독
        static CharacterEvents()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        // ========================================================================
        // HP 관련 이벤트
        // ========================================================================

        /// <summary>
        /// HP가 변경되었을 때 발생합니다.
        /// 파라미터: (characterGameObject, oldHP, newHP)
        /// </summary>
        public static event Action<GameObject, int, int> OnHealthChanged;

        /// <summary>
        /// 데미지를 입었을 때 발생합니다.
        /// 파라미터: (characterGameObject, damage, isCritical)
        /// </summary>
        public static event Action<GameObject, int, bool> OnDamaged;

        /// <summary>
        /// 데미지를 입었을 때 발생합니다 (상세 정보 포함).
        /// Phase 5: UI 표시를 위한 이벤트
        /// 파라미터: (position, damageInfo)
        /// </summary>
        public static event Action<Vector3, DungeonLog.Combat.DamageInfo> OnDamagedWithInfo;

        /// <summary>
        /// 회복했을 때 발생합니다.
        /// 파라미터: (characterGameObject, healAmount)
        /// </summary>
        public static event Action<GameObject, int> OnHealed;

        /// <summary>
        /// 회복했을 때 발생합니다 (위치 정보 포함).
        /// Phase 5: UI 표시를 위한 이벤트
        /// 파라미터: (position, healAmount)
        /// </summary>
        public static event Action<Vector3, int> OnHealedWithPosition;

        /// <summary>
        /// 사망했을 때 발생합니다.
        /// 파라미터: (characterGameObject)
        /// </summary>
        public static event Action<GameObject> OnCharacterDeath;

        // ========================================================================
        // 스킬 관련 이벤트
        // ========================================================================

        /// <summary>
        /// 스킬을 사용했을 때 발생합니다.
        /// 파라미터: (casterGameObject, skillData, targetGameObjects)
        /// </summary>
        public static event Action<GameObject, Data.SkillData, GameObject[]> OnSkillUsed;

        /// <summary>
        /// 스킬 추첨이 완료되었을 때 발생합니다.
        /// 파라미터: (characterGameObject, skillData)
        /// </summary>
        public static event Action<GameObject, Data.SkillData> OnSkillDrawn;

        // ========================================================================
        // 스탯 관련 이벤트
        // ========================================================================

        /// <summary>
        /// 스탯이 변경되었을 때 발생합니다.
        /// 파라미터: (characterGameObject, statType, oldValue, newValue)
        /// </summary>
        public static event Action<GameObject, StatType, int, int> OnStatChanged;

        // ========================================================================
        // 헬퍼 메서드 (이벤트 발생)
        // ========================================================================

        /// <summary>
        /// HP 변경 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyHealthChanged(GameObject character, int oldHP, int newHP)
        {
            OnHealthChanged?.Invoke(character, oldHP, newHP);
        }

        /// <summary>
        /// 데미지 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyDamaged(GameObject character, int damage, bool isCritical)
        {
            OnDamaged?.Invoke(character, damage, isCritical);
        }

        /// <summary>
        /// 데미지 이벤트를 발생시킵니다 (상세 정보 포함).
        /// Phase 5: UI 표시를 위한 이벤트
        /// </summary>
        public static void NotifyDamagedWithInfo(Vector3 position, DungeonLog.Combat.DamageInfo damageInfo)
        {
            OnDamagedWithInfo?.Invoke(position, damageInfo);
        }

        /// <summary>
        /// 회복 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyHealed(GameObject character, int healAmount)
        {
            OnHealed?.Invoke(character, healAmount);
        }

        /// <summary>
        /// 회복 이벤트를 발생시킵니다 (위치 정보 포함).
        /// Phase 5: UI 표시를 위한 이벤트
        /// </summary>
        public static void NotifyHealedWithPosition(Vector3 position, int healAmount)
        {
            OnHealedWithPosition?.Invoke(position, healAmount);
        }

        /// <summary>
        /// 사망 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyDeath(GameObject character)
        {
            OnCharacterDeath?.Invoke(character);
        }

        /// <summary>
        /// 스킬 사용 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifySkillUsed(GameObject caster, Data.SkillData skill, GameObject[] targets)
        {
            OnSkillUsed?.Invoke(caster, skill, targets);
        }

        /// <summary>
        /// 스킬 추첨 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifySkillDrawn(GameObject character, Data.SkillData skill)
        {
            OnSkillDrawn?.Invoke(character, skill);
        }

        /// <summary>
        /// 스탯 변경 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyStatChanged(GameObject character, StatType statType, int oldValue, int newValue)
        {
            OnStatChanged?.Invoke(character, statType, oldValue, newValue);
        }

        // ========================================================================
        // 이벤트 정리 (메모리 누수 방지)
        // ========================================================================

        /// <summary>
        /// 씬 전환 시 자동으로 호출됩니다. 모든 null 구독자를 제거합니다.
        /// </summary>
        private static void OnSceneUnloaded(Scene scene)
        {
            RemoveNullListeners();
            Debug.Log($"[CharacterEvents] 씬 전환 ({scene.name}) - null 구독자 제거 완료.");
        }

        /// <summary>
        /// 모든 이벤트에서 null 구독자를 제거합니다 (Destroy된 객체).
        /// </summary>
        private static void RemoveNullListeners()
        {
            OnHealthChanged = RemoveNullListeners(OnHealthChanged);
            OnDamaged = RemoveNullListeners(OnDamaged);
            OnDamagedWithInfo = RemoveNullListeners(OnDamagedWithInfo);
            OnHealed = RemoveNullListeners(OnHealed);
            OnHealedWithPosition = RemoveNullListeners(OnHealedWithPosition);
            OnCharacterDeath = RemoveNullListeners(OnCharacterDeath);
            OnSkillUsed = RemoveNullListeners(OnSkillUsed);
            OnSkillDrawn = RemoveNullListeners(OnSkillDrawn);
            OnStatChanged = RemoveNullListeners(OnStatChanged);
        }

        /// <summary>
        /// 이벤트에서 null 구독자를 제거하는 제네릭 헬퍼 메서드 (4개 파라미터).
        /// </summary>
        private static Action<T1, T2, T3, T4> RemoveNullListeners<T1, T2, T3, T4>(Action<T1, T2, T3, T4> evt)
        {
            if (evt == null) return null;
            return (Action<T1, T2, T3, T4>)Delegate.Combine(
                evt.GetInvocationList()
                    .Where(d => !(d.Target == null || (d.Target is UnityEngine.Object o && o == null)))
                    .ToArray());
        }

        /// <summary>
        /// 이벤트에서 null 구독자를 제거하는 제네릭 헬퍼 메서드 (3개 파라미터).
        /// </summary>
        private static Action<T1, T2, T3> RemoveNullListeners<T1, T2, T3>(Action<T1, T2, T3> evt)
        {
            if (evt == null) return null;
            return (Action<T1, T2, T3>)Delegate.Combine(
                evt.GetInvocationList()
                    .Where(d => !(d.Target == null || (d.Target is UnityEngine.Object o && o == null)))
                    .ToArray());
        }

        /// <summary>
        /// 이벤트에서 null 구독자를 제거하는 제네릭 헬퍼 메서드 (2개 파라미터).
        /// </summary>
        private static Action<T1, T2> RemoveNullListeners<T1, T2>(Action<T1, T2> evt)
        {
            if (evt == null) return null;
            return (Action<T1, T2>)Delegate.Combine(
                evt.GetInvocationList()
                    .Where(d => !(d.Target == null || (d.Target is UnityEngine.Object o && o == null)))
                    .ToArray());
        }

        /// <summary>
        /// 이벤트에서 null 구독자를 제거하는 제네릭 헬퍼 메서드 (1개 파라미터).
        /// </summary>
        private static Action<T1> RemoveNullListeners<T1>(Action<T1> evt)
        {
            if (evt == null) return null;
            return (Action<T1>)Delegate.Combine(
                evt.GetInvocationList()
                    .Where(d => !(d.Target == null || (d.Target is UnityEngine.Object o && o == null)))
                    .ToArray());
        }

        /// <summary>
        /// 모든 이벤트 구독자를 수동으로 해제합니다.
        /// 씬 전환 시 자동으로 호출되지만, 필요시 수동으로 호출할 수 있습니다.
        /// </summary>
        public static void ClearAllListeners()
        {
            OnHealthChanged = null;
            OnDamaged = null;
            OnDamagedWithInfo = null;
            OnHealed = null;
            OnHealedWithPosition = null;
            OnCharacterDeath = null;
            OnSkillUsed = null;
            OnSkillDrawn = null;
            OnStatChanged = null;

            Debug.Log("[CharacterEvents] 모든 이벤트 구독자가 해제되었습니다.");
        }
    }
}
