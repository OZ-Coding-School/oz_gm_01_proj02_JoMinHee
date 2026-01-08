using UnityEngine;

namespace DungeonLog.Combat
{
    /// <summary>
    /// AP (Action Point) 시스템을 관리하는 클래스입니다.
    /// 파티 인원수에 따라 동적으로 AP를 할당합니다.
    /// 기본 AP: 1 + 파티 캐릭터당 +1 (4인 파티 = 5AP)
    /// </summary>
    public class APSystem
    {
        // ========================================================================
        // 상수 정의
        // ========================================================================

        /// <summary>기본 AP (모든 파티에 공통)</summary>
        private const int BASE_AP = 1;

        /// <summary>최소 AP (턴 시작 시 최소 보장)</summary>
        private const int MINIMUM_AP = 2;

        // ========================================================================
        // 필드
        // ========================================================================

        /// <summary>현재 사용 가능한 AP</summary>
        private int currentAP;

        /// <summary>현재 턴의 최대 AP</summary>
        private int maxAP;

        /// <summary>생존한 파티 멤버 수</summary>
        private int alivePartyCount;

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        /// <summary>현재 사용 가능한 AP</summary>
        public int CurrentAP => currentAP;

        /// <summary>현재 턴의 최대 AP</summary>
        public int MaxAP => maxAP;

        /// <summary>AP 사용 가능 여부</summary>
        public bool HasAP => currentAP > 0;

        // ========================================================================
        // 턴 초기화
        // ========================================================================

        /// <summary>
        /// 턴 시작 시 AP를 초기화합니다.
        /// 기본 1AP + 생존한 파티 멤버당 +1AP
        /// </summary>
        /// <param name="partyMemberCount">생존한 파티 멤버 수</param>
        public void ResetAPForTurn(int partyMemberCount)
        {
            alivePartyCount = Mathf.Max(0, partyMemberCount);

            // 기본 AP = 1 + 파티 멤버당 +1
            // 1인: 2AP, 2인: 3AP, 3인: 4AP, 4인: 5AP
            maxAP = Mathf.Max(MINIMUM_AP, BASE_AP + alivePartyCount);
            currentAP = maxAP;

            Debug.Log($"[APSystem] 턴 시작 AP 초기화: {currentAP}/{maxAP} (파티 인원: {alivePartyCount})");

            // AP 변경 이벤트 발생
            BattleEvents.NotifyAPChanged(currentAP, maxAP);
        }

        // ========================================================================
        // AP 조작
        // ========================================================================

        /// <summary>
        /// AP를 소비합니다.
        /// </summary>
        /// <param name="amount">소비할 AP 양</param>
        /// <returns>AP 소비 성공 여부</returns>
        public bool ConsumeAP(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[APSystem] 잘못된 AP 소비 양: {amount}");
                return false;
            }

            if (currentAP < amount)
            {
                Debug.LogWarning($"[APSystem] AP 부족: 필요 {amount}, 현재 {currentAP}");
                return false;
            }

            currentAP -= amount;
            Debug.Log($"[APSystem] AP 소비: -{amount} (잔여: {currentAP}/{maxAP})");

            // AP 변경 이벤트 발생
            BattleEvents.NotifyAPChanged(currentAP, maxAP);

            return true;
        }

        /// <summary>
        /// AP를 추가로 획득합니다.
        /// </summary>
        /// <param name="amount">추가할 AP 양</param>
        public void GrantAP(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[APSystem] 잘못된 AP 추가 양: {amount}");
                return;
            }

            currentAP += amount;
            Debug.Log($"[APSystem] AP 획득: +{amount} (현재: {currentAP}/{maxAP})");

            // AP 변경 이벤트 발생
            BattleEvents.NotifyAPChanged(currentAP, maxAP);
        }

        /// <summary>
        /// 특정 양의 AP 사용이 가능한지 확인합니다.
        /// </summary>
        /// <param name="amount">필요한 AP 양</param>
        /// <returns>사용 가능 여부</returns>
        public bool HasEnoughAP(int amount)
        {
            return currentAP >= amount;
        }

        /// <summary>
        /// 현재 AP를 소비하지 않고 잔량만 확인합니다.
        /// </summary>
        /// <returns>현재 잔여 AP</returns>
        public int GetCurrentAP()
        {
            return currentAP;
        }

        /// <summary>
        /// 턴 종료 시 남은 AP 잔량을 반환합니다.
        /// </summary>
        /// <returns>남은 AP 양</returns>
        public int GetRemainingAP()
        {
            return currentAP;
        }

        // ========================================================================
        // 디버그 및 정보
        // ========================================================================

        /// <summary>
        /// 현재 AP 시스템 상태를 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            return $"AP: {currentAP}/{maxAP} (파티: {alivePartyCount}인)";
        }

        /// <summary>
        /// AP 사용 내역을 디버깅용으로 반환합니다.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"[APSystem] 현재: {currentAP}/{maxAP}, 파티 인원: {alivePartyCount}, 남은 AP: {currentAP}";
        }
    }
}
