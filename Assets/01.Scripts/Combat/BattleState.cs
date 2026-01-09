using UnityEngine;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 전투의 전체 상태를 정의합니다.
    /// </summary>
    public enum BattleState
    {
        /// <summary>전투 시작 전</summary>
        NotStarted,

        /// <summary>플레이어 턴 (AP 소모)</summary>
        PlayerTurn,

        /// <summary>플레이어 스킬 리롤 중</summary>
        PlayerSkillReroll,

        /// <summary>적 턴 (AI 자동 행동)</summary>
        EnemyTurn,

        /// <summary>스킬 해석 중 (이펙트 재생 등)</summary>
        Resolving,

        /// <summary>전투 종료</summary>
        BattleEnd
    }

    /// <summary>
    /// 플레이어 턴 내의 세부 단계를 정의합니다.
    /// </summary>
    public enum PlayerActionPhase
    {
        /// <summary>스킬 추첨 단계</summary>
        SkillDraw,

        /// <summary>행동 선택 단계 (리롤/스킬 사용)</summary>
        ActionSelect,

        /// <summary>스킬 실행 중</summary>
        ActionExecuting,

        /// <summary>턴 종료 대기</summary>
        TurnEnd
    }

    /// <summary>
    /// 전투 결과를 정의합니다.
    /// </summary>
    public enum BattleResult
    {
        /// <summary>승리</summary>
        Victory,

        /// <summary>패배</summary>
        Defeat,

        /// <summary>도망 (미구현)</summary>
        Retreat,

        /// <summary>무승부 (동시 사망)</summary>
        Draw
    }
}
