using System;
using System.Collections.Generic;
using DungeonLog.AI.Core;
using DungeonLog.AI.Data;
using DungeonLog.AI.Evaluators;
using DungeonLog.AI.Strategies.Interfaces;
using DungeonLog.Data;
using UnityEngine;

namespace DungeonLog.AI
{
    /// <summary>
    /// AI 컨트롤러 MonoBehaviour
    /// 적 캐릭터에 부착되어 턴마다 AI 행동을 결정하고 실행
    /// 난이도 시스템과 실수 확률을 지원
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AIController : MonoBehaviour
    {
        [Header("AI 설정")]
        [SerializeField, Tooltip("사용할 타겟팅 전략")]
        private ITargetingStrategy _targetingStrategy;

        [SerializeField, Tooltip("사용할 스킬 선택 전략")]
        private ISkillSelectionStrategy _skillSelectionStrategy;

        [Header("난이도 설정")]
        [SerializeField, Tooltip("AI 난이도 데이터 (비어있으면 Normal 사용)")]
        private AIDifficultyData difficultyData;

        [Header("디버깅")]
        [SerializeField, Tooltip("AI 결정 로그 표시")]
        private bool _showDebugLog = false;

        // AI 의사결정 평가자
        private AIDecisionEvaluator _decisionEvaluator;

        // 캐싱
        private DungeonLog.Character.Character _character;
        private List<DungeonLog.Character.Character> _alliesCache = new List<DungeonLog.Character.Character>();
        private List<DungeonLog.Character.Character> _enemiesCache = new List<DungeonLog.Character.Character>();
        private System.Random _random;

        // 현재 타겟 (Focus Fire용)
        private DungeonLog.Character.Character _currentTarget;

        /// <summary>
        /// 초기화
        /// </summary>
        private void Awake()
        {
            _character = GetComponent<DungeonLog.Character.Character>();

            if (_character == null)
            {
                Debug.LogError($"[AIController] Character component not found on {gameObject.name}", this);
                return;
            }

            // 난이도 데이터가 없으면 Normal 난이도 생성
            if (difficultyData == null)
            {
                difficultyData = AIDifficultyData.CreateDefault(AIDifficultyTier.Normal);
            }

            // Random 인스턴스 초기화 (실수 확률 적용용)
            _random = new System.Random();

            // 전략이 null이면 기본 전략 생성
            if (_skillSelectionStrategy == null)
            {
                var skillScorers = new List<Strategies.Interfaces.ISkillScorer>
                {
                    new Strategies.SkillSelection.DamageSkillScorer { Weight = 1.0f },
                    new Strategies.SkillSelection.HealSkillScorer { Weight = 1.2f },
                    new Strategies.SkillSelection.StatusSkillScorer { Weight = 0.8f }
                };
                _skillSelectionStrategy = new Strategies.SkillSelection.UtilitySkillSelectionStrategy(skillScorers);
            }

            if (_targetingStrategy == null)
            {
                var targetScorers = new List<Strategies.Interfaces.ITargetScorer>
                {
                    new Strategies.Targeting.LowHpTargetScorer { Weight = 1.0f },
                    new Strategies.Targeting.MarkTargetScorer { Weight = 1.5f },
                    new Strategies.Targeting.NoShieldTargetScorer { Weight = 1.0f }
                };
                _targetingStrategy = new Strategies.Targeting.UtilityTargetingStrategy(targetScorers);
            }

            // 의사결정 평가자 초기화
            _decisionEvaluator = new AIDecisionEvaluator(_skillSelectionStrategy, _targetingStrategy);

            if (_showDebugLog)
                Debug.Log($"[AIController] Initialized for {_character.CharacterName} (Difficulty: {difficultyData.DifficultyTier})");
        }

        /// <summary>
        /// AI 행동을 결정하고 실행
        /// BattleManager에서 턴 시작 시 호출
        /// </summary>
        /// <param name="allies">아군 리스트</param>
        /// <param name="enemies">적군 리스트</param>
        /// <param name="currentTurn">현재 턴 번호</param>
        /// <param name="difficultyModifier">난이도 수정자 (1.0 = 기본, 사용되지 않음)</param>
        /// <returns>결정된 AI 행동 (null인 경우 행동 불가)</returns>
        public AIAction DecideAction(
            List<DungeonLog.Character.Character> allies,
            List<DungeonLog.Character.Character> enemies,
            int currentTurn,
            float difficultyModifier = 1.0f)
        {
            // 난이도 수정자 사용 (difficultyData에서 가져옴)
            float actualDifficultyModifier = difficultyData != null ? difficultyData.DifficultyModifier : 1.0f;

            // 실수 확률 적용
            if (ShouldMakeMistake())
            {
                if (_showDebugLog)
                    Debug.Log($"[AIController] {_character.CharacterName} made a mistake!");

                // 실수: 랜덤 행동 반환
                return GetRandomMistakeAction(allies, enemies, currentTurn);
            }

            // 상태 이상 확인
            if (IsDisabledByStatusEffect())
            {
                if (_showDebugLog)
                    Debug.Log($"[AIController] {_character.CharacterName} is disabled by status effect");
                return null;
            }

            // 캐싱 업데이트
            _alliesCache.Clear();
            _enemiesCache.Clear();

            if (allies != null)
                _alliesCache.AddRange(allies);

            if (enemies != null)
                _enemiesCache.AddRange(enemies);

            // AI 컨텍스트 생성
            AIContext context = AIContext.Create(_character, _alliesCache, _enemiesCache, currentTurn, actualDifficultyModifier);

            // 사용 가능한 스킬 리스트
            List<SkillData> availableSkills = _character.SkillManager.GetCurrentAvailableSkills();

            if (availableSkills == null || availableSkills.Count == 0)
            {
                if (_showDebugLog)
                    Debug.LogWarning($"[AIController] {_character.CharacterName} has no available skills");
                return null;
            }

            // 의사결정 수행
            AIAction action = _decisionEvaluator.DecideAction(context, availableSkills);

            if (action != null)
            {
                // 현재 타겟 저장 (Focus Fire용)
                _currentTarget = action.PrimaryTarget;

                if (_showDebugLog)
                {
                    Debug.Log($"[AIController] {_character.CharacterName} decided: {action}");
                }
            }

            return action;
        }

        /// <summary>
        /// 실수를 할 것인지 확인
        /// 난이도에 따른 확률로 결정
        /// </summary>
        private bool ShouldMakeMistake()
        {
            if (difficultyData == null)
                return false;

            float mistakeChance = difficultyData.MistakeChance;
            double roll = _random.NextDouble();

            return roll < mistakeChance;
        }

        /// <summary>
        /// 실수 시 랜덤 행동 생성
        /// 유효하지 않거나 비효율적인 행동 반환
        /// </summary>
        private AIAction GetRandomMistakeAction(
            List<DungeonLog.Character.Character> allies,
            List<DungeonLog.Character.Character> enemies,
            int turn)
        {
            // 랜덤한 스킬과 타겟 선택
            List<SkillData> availableSkills = _character.SkillManager.GetCurrentAvailableSkills();

            if (availableSkills == null || availableSkills.Count == 0)
                return null;

            // 첫 번째 스킬 선택 (최적이 아님)
            SkillData randomSkill = availableSkills[0];

            // 랜덤 타겟 선택 (아군/적군 구분 없이)
            List<DungeonLog.Character.Character> allTargets = new List<DungeonLog.Character.Character>();
            if (enemies != null)
                allTargets.AddRange(enemies.FindAll(e => e != null && e.IsAlive));

            if (allTargets.Count == 0 && allies != null)
                allTargets.AddRange(allies.FindAll(a => a != null && a.IsAlive && a != _character));

            if (allTargets.Count == 0)
                return null;

            DungeonLog.Character.Character randomTarget = allTargets[_random.Next(allTargets.Count)];

            // 현재 타겟 저장 (Focus Fire용 - 실수 행동도 포함)
            _currentTarget = randomTarget;

            // 실수 행동 생성 (Object Pool 사용)
            AIAction mistakeAction = Evaluators.AIDecisionEvaluator.GetAction();
            mistakeAction.Actor = _character;
            mistakeAction.Skill = randomSkill;
            mistakeAction.PrimaryTarget = randomTarget;
            mistakeAction.Targets.Add(randomTarget);
            mistakeAction.UtilityScore = 0f;
            mistakeAction.Reason = "Mistake action (random choice)";

            return mistakeAction;
        }

        /// <summary>
        /// 결정된 행동 실행
        /// </summary>
        /// <param name="action">실행할 AI 행동</param>
        public void ExecuteAction(AIAction action)
        {
            if (action == null || !action.IsValid)
            {
                Debug.LogWarning($"[AIController] Invalid action for {_character.CharacterName}");
                // 잘못된 액션도 Pool로 반환
                Evaluators.AIDecisionEvaluator.ReturnAction(action);
                return;
            }

            if (_showDebugLog)
                Debug.Log($"[AIController] {_character.CharacterName} executing: {action.Reason}");

            // 스킬 실행
            // TODO: BattleManager 또는 SkillSystem에 실행 위임
            // 현재는 Character에서 직접 실행
            if (action.Targets != null && action.Targets.Count > 0)
            {
                // 스킬 사용
                // _character.SkillManager.UseSkill(action.Skill, action.Targets);
                // 실제 구현은 SkillSystem 연동 후 완성
            }

            // 실행 후 Pool로 반환
            Evaluators.AIDecisionEvaluator.ReturnAction(action);
        }

        /// <summary>
        /// 상태 이상으로 행동 불가능한지 확인
        /// </summary>
        private bool IsDisabledByStatusEffect()
        {
            if (_character == null || _character.StatusEffects == null)
                return false;

            // 기절 상태 확인
            if (_character.StatusEffects.IsStunned)
            {
                if (_showDebugLog)
                    Debug.Log($"[AIController] {_character.CharacterName} is stunned");
                return true;
            }

            // 침묵 상태 확인 (스킬 사용 불가)
            // 공격만 가능한지 확인 필요
            if (_character.StatusEffects.IsSilenced)
            {
                // TODO: 침묵 상태에서는 기본 공격만 가능하도록 처리
                if (_showDebugLog)
                    Debug.Log($"[AIController] {_character.CharacterName} is silenced");
                // 일단 true로 처리 (기본 공격 시스템 구현 후 수정)
                return true;
            }

            return false;
        }

        /// <summary>
        /// 타겟팅 전략 설정 (에디터 또는 코드에서 설정)
        /// </summary>
        public void SetTargetingStrategy(ITargetingStrategy strategy)
        {
            _targetingStrategy = strategy;

            if (_decisionEvaluator != null)
            {
                _decisionEvaluator = new AIDecisionEvaluator(_skillSelectionStrategy, _targetingStrategy);
            }
        }

        /// <summary>
        /// 스킬 선택 전략 설정 (에디터 또는 코드에서 설정)
        /// </summary>
        public void SetSkillSelectionStrategy(ISkillSelectionStrategy strategy)
        {
            _skillSelectionStrategy = strategy;

            if (_decisionEvaluator != null)
            {
                _decisionEvaluator = new AIDecisionEvaluator(_skillSelectionStrategy, _targetingStrategy);
            }
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugLog(bool enabled)
        {
            _showDebugLog = enabled;
        }

        /// <summary>
        /// 현재 타겟을 반환 (Focus Fire 전략용)
        /// </summary>
        public DungeonLog.Character.Character GetCurrentTarget()
        {
            return _currentTarget;
        }

        /// <summary>
        /// 현재 타겟을 설정 (외부에서 설정 시 사용)
        /// </summary>
        public void SetCurrentTarget(DungeonLog.Character.Character target)
        {
            _currentTarget = target;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 AI 설정 시각화 (Gizmo)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_character == null) return;

            // AI가 있는 캐릭터 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
#endif
    }
}
