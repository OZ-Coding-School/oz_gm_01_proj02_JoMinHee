using UnityEngine;

namespace DungeonLog.AI.Data
{
    /// <summary>
    /// AI 난이도 티어를 정의합니다.
    /// 난이도에 따라 의사결정 정확도, 실수율, 반응 속도가 달라집니다.
    /// </summary>
    public enum AIDifficultyTier
    {
        /// <summary>매우 쉬움: 30% 실수율, 예측 가능한 패턴</summary>
        VeryEasy,

        /// <summary>쉬움: 20% 실수율, 기본 전략</summary>
        Easy,

        /// <summary>보통: 10% 실수율, 균형 잡힌 전략</summary>
        Normal,

        /// <summary>어려움: 5% 실수율, 최적의 전략</summary>
        Hard,

        /// <summary>매우 어려움: 0% 실수율, 완벽한 플레이</summary>
        VeryHard
    }

    /// <summary>
    /// AI 난이도 설정 데이터
    /// 난이도 티어에 따른 파라미터를 정의
    /// </summary>
    [CreateAssetMenu(fileName = "NewAIDifficulty", menuName = "Dungeon Log/AI Difficulty")]
    public class AIDifficultyData : ScriptableObject
    {
        [Header("난이도 설정")]
        [SerializeField, Tooltip("난이도 티어")]
        private AIDifficultyTier difficultyTier;

        [SerializeField, Range(0f, 1f), Tooltip("실수 확률 (0 = 완벽, 0.3 = 30% 실수)")]
        private float mistakeChance = 0.1f;

        [SerializeField, Range(0.5f, 2f), Tooltip("의사결정 난이도 수정자 (1.0 = 기본)")]
        private float difficultyModifier = 1.0f;

        [SerializeField, Range(0.5f, 2f), Tooltip("반응 속도 수정자 (1.0 = 기본, 낮을수록 빠름)")]
        private float reactionSpeedModifier = 1.0f;

        [Header("AI 성격 보정")]
        [SerializeField, Range(0f, 2f), Tooltip("공격성 보정 (1.0 = 기본)")]
        private float aggressivenessModifier = 1.0f;

        [SerializeField, Range(0f, 2f), Tooltip("방어성 보정 (1.0 = 기본)")]
        private float defensivenessModifier = 1.0f;

        [SerializeField, Range(0f, 2f), Tooltip("전략적 보정 (1.0 = 기본)")]
        private float tacticalModifier = 1.0f;

        // 속성 접근자
        public AIDifficultyTier DifficultyTier => difficultyTier;
        public float MistakeChance => mistakeChance;
        public float DifficultyModifier => difficultyModifier;
        public float ReactionSpeedModifier => reactionSpeedModifier;
        public float AggressivenessModifier => aggressivenessModifier;
        public float DefensivenessModifier => defensivenessModifier;
        public float TacticalModifier => tacticalModifier;

        /// <summary>
        /// 난이도 티어에 따른 기본 설정을 생성합니다.
        /// </summary>
        public static AIDifficultyData CreateDefault(AIDifficultyTier tier)
        {
            AIDifficultyData data = ScriptableObject.CreateInstance<AIDifficultyData>();

            switch (tier)
            {
                case AIDifficultyTier.VeryEasy:
                    data.difficultyTier = AIDifficultyTier.VeryEasy;
                    data.mistakeChance = 0.30f;
                    data.difficultyModifier = 0.7f;
                    data.reactionSpeedModifier = 1.5f;
                    break;

                case AIDifficultyTier.Easy:
                    data.difficultyTier = AIDifficultyTier.Easy;
                    data.mistakeChance = 0.20f;
                    data.difficultyModifier = 0.85f;
                    data.reactionSpeedModifier = 1.2f;
                    break;

                case AIDifficultyTier.Normal:
                    data.difficultyTier = AIDifficultyTier.Normal;
                    data.mistakeChance = 0.10f;
                    data.difficultyModifier = 1.0f;
                    data.reactionSpeedModifier = 1.0f;
                    break;

                case AIDifficultyTier.Hard:
                    data.difficultyTier = AIDifficultyTier.Hard;
                    data.mistakeChance = 0.05f;
                    data.difficultyModifier = 1.2f;
                    data.reactionSpeedModifier = 0.9f;
                    break;

                case AIDifficultyTier.VeryHard:
                    data.difficultyTier = AIDifficultyTier.VeryHard;
                    data.mistakeChance = 0.0f;
                    data.difficultyModifier = 1.5f;
                    data.reactionSpeedModifier = 0.7f;
                    break;
            }

            return data;
        }
    }
}
