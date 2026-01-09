using System;
using UnityEngine;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 전투 관련 이벤트를 중앙에서 관리하는 정적 클래스입니다.
    /// CharacterEvents와 분리하여 전투 흐름 제어용으로 사용합니다.
    /// 씬 전환 시 자동으로 이벤트를 정리합니다.
    /// </summary>
    public static class BattleEvents
    {
        // ========================================================================
        // 전투 흐름 이벤트
        // ========================================================================

        /// <summary>
        /// 전투 상태가 변경되었을 때 발생합니다.
        /// </summary>
        public static event Action<BattleState> OnBattleStateChanged;

        /// <summary>
        /// 턴이 변경되었을 때 발생합니다.
        /// </summary>
        public static event Action<int> OnTurnChanged;

        /// <summary>
        /// AP가 변경되었을 때 발생합니다.
        /// </summary>
        public static event Action<int, int> OnAPChanged; // (current, max)

        // ========================================================================
        // 전투 결과 이벤트
        // ========================================================================

        /// <summary>
        /// 전투가 종료되었을 때 발생합니다.
        /// </summary>
        public static event Action<bool> OnBattleEnded; // (victory)

        // ========================================================================
        // 스킬 관련 이벤트
        // ========================================================================

        /// <summary>
        /// 스킬 추첨이 완료되었을 때 발생합니다.
        /// </summary>
        public static event Action<DungeonLog.Character.Character> OnSkillDrawn;

        /// <summary>
        /// 스킬 리롤이 실행되었을 때 발생합니다.
        /// </summary>
        public static event Action<DungeonLog.Character.Character> OnSkillRerolled;

        /// <summary>
        /// 스킬 사용이 시도되었을 때 발생합니다.
        /// </summary>
        public static event Action<DungeonLog.Character.Character, DungeonLog.Data.SkillData> OnSkillAttempt;

        // ========================================================================
        // 정적 생성자 - 씬 전환 이벤트 구독
        // ========================================================================

        static BattleEvents()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        // ========================================================================
        // 헬퍼 메서드 (이벤트 발생)
        // ========================================================================

        /// <summary>
        /// 전투 상태 변경 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyBattleStateChanged(BattleState state)
        {
            OnBattleStateChanged?.Invoke(state);
        }

        /// <summary>
        /// 턴 변경 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyTurnChanged(int turn)
        {
            OnTurnChanged?.Invoke(turn);
        }

        /// <summary>
        /// AP 변경 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyAPChanged(int current, int max)
        {
            OnAPChanged?.Invoke(current, max);
        }

        /// <summary>
        /// 전투 종료 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifyBattleEnded(bool victory)
        {
            OnBattleEnded?.Invoke(victory);
        }

        /// <summary>
        /// 스킬 추첨 완료 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifySkillDrawn(DungeonLog.Character.Character character)
        {
            OnSkillDrawn?.Invoke(character);
        }

        /// <summary>
        /// 스킬 리롤 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifySkillRerolled(DungeonLog.Character.Character character)
        {
            OnSkillRerolled?.Invoke(character);
        }

        /// <summary>
        /// 스킬 사용 시도 이벤트를 발생시킵니다.
        /// </summary>
        public static void NotifySkillAttempt(DungeonLog.Character.Character character, DungeonLog.Data.SkillData skill)
        {
            OnSkillAttempt?.Invoke(character, skill);
        }

        // ========================================================================
        // 이벤트 정리 (메모리 누수 방지)
        // ========================================================================

        /// <summary>
        /// 씬 전환 시 자동으로 호출됩니다. 모든 이벤트 구독자를 제거합니다.
        /// </summary>
        private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            ClearAllListeners();
            Debug.Log($"[BattleEvents] 씬 전환 ({scene.name}) - 모든 이벤트 구독자 제거 완료.");
        }

        /// <summary>
        /// 모든 이벤트 구독자를 수동으로 해제합니다.
        /// </summary>
        public static void ClearAllListeners()
        {
            OnBattleStateChanged = null;
            OnTurnChanged = null;
            OnAPChanged = null;
            OnBattleEnded = null;
            OnSkillDrawn = null;
            OnSkillRerolled = null;
            OnSkillAttempt = null;

            Debug.Log("[BattleEvents] 모든 이벤트 구독자가 해제되었습니다.");
        }
    }
}
