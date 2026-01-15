using System.Collections.Generic;

namespace DungeonLog.AI.Core
{
    /// <summary>
    /// AI 의사결정에 필요한 컨텍스트 정보
    /// GC 할당을 최소화하기 위해 struct로 정의
    /// </summary>
    public struct AIContext
    {
        /// <summary>
        /// 행동을 결정할 액터 (적)
        /// </summary>
        public DungeonLog.Character.Character Actor;

        /// <summary>
        /// 아군 캐릭터 리스트 (읽기 전용)
        /// </summary>
        public IReadOnlyList<DungeonLog.Character.Character> Allies;

        /// <summary>
        /// 적 캐릭터 리스트 (읽기 전용)
        /// </summary>
        public IReadOnlyList<DungeonLog.Character.Character> Enemies;

        /// <summary>
        /// 현재 턴 번호
        /// </summary>
        public int CurrentTurn;

        /// <summary>
        /// 결정 시간 예산 (밀리초)
        /// </summary>
        public float DecisionTimeBudget;

        /// <summary>
        /// AI 난이도 수정자
        /// </summary>
        public float DifficultyModifier;

        /// <summary>
        /// 유효한 컨텍스트인지 확인
        /// </summary>
        public bool IsValid => Actor != null && Allies != null && Enemies != null;

        /// <summary>
        /// 컨텍스트 생성 헬퍼 메서드
        /// </summary>
        public static AIContext Create(DungeonLog.Character.Character actor, IReadOnlyList<DungeonLog.Character.Character> allies, IReadOnlyList<DungeonLog.Character.Character> enemies, int turn, float difficulty = 1f)
        {
            return new AIContext
            {
                Actor = actor,
                Allies = allies,
                Enemies = enemies,
                CurrentTurn = turn,
                DecisionTimeBudget = 50f, // 50ms 기본 예산
                DifficultyModifier = difficulty
            };
        }
    }
}
