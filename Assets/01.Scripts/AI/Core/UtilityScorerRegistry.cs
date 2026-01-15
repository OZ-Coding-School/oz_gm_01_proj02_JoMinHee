using System;
using System.Collections.Generic;
using DungeonLog.AI.Strategies.Interfaces;
using UnityEngine;

namespace DungeonLog.AI.Core
{
    /// <summary>
    /// 모든 Utility Scorer를 관리하는 레지스트리
    /// StatusEffectBehaviorRegistry 패턴을 따름
    /// </summary>
    public class UtilityScorerRegistry
    {
        private static UtilityScorerRegistry _instance;
        public static UtilityScorerRegistry Instance => _instance ??= new UtilityScorerRegistry();

        private readonly List<ITargetScorer> _targetScorers;
        private readonly List<ISkillScorer> _skillScorers;

        public IReadOnlyList<ITargetScorer> TargetScorers => _targetScorers;
        public IReadOnlyList<ISkillScorer> SkillScorers => _skillScorers;

        private UtilityScorerRegistry()
        {
            _targetScorers = new List<ITargetScorer>();
            _skillScorers = new List<ISkillScorer>();
        }

        /// <summary>
        /// 타겟 Scorer 등록
        /// </summary>
        public void RegisterTargetScorer(ITargetScorer scorer)
        {
            if (scorer == null)
            {
                Debug.LogError("[UtilityScorerRegistry] Cannot register null target scorer");
                return;
            }

            if (_targetScorers.Contains(scorer))
            {
                Debug.LogWarning($"[UtilityScorerRegistry] Target scorer '{scorer.ScorerName}' already registered");
                return;
            }

            _targetScorers.Add(scorer);
            Debug.Log($"[UtilityScorerRegistry] Registered target scorer: {scorer.ScorerName}");
        }

        /// <summary>
        /// 스킬 Scorer 등록
        /// </summary>
        public void RegisterSkillScorer(ISkillScorer scorer)
        {
            if (scorer == null)
            {
                Debug.LogError("[UtilityScorerRegistry] Cannot register null skill scorer");
                return;
            }

            if (_skillScorers.Contains(scorer))
            {
                Debug.LogWarning($"[UtilityScorerRegistry] Skill scorer '{scorer.ScorerName}' already registered");
                return;
            }

            _skillScorers.Add(scorer);
            Debug.Log($"[UtilityScorerRegistry] Registered skill scorer: {scorer.ScorerName}");
        }

        /// <summary>
        /// 타겟 Scorer 제거
        /// </summary>
        public bool UnregisterTargetScorer(ITargetScorer scorer)
        {
            return _targetScorers.Remove(scorer);
        }

        /// <summary>
        /// 스킬 Scorer 제거
        /// </summary>
        public bool UnregisterSkillScorer(ISkillScorer scorer)
        {
            return _skillScorers.Remove(scorer);
        }

        /// <summary>
        /// 모든 Scorer 초기화 (Phase 7 구현용)
        /// </summary>
        public void InitializeScorers()
        {
            _targetScorers.Clear();
            _skillScorers.Clear();
            Debug.Log("[UtilityScorerRegistry] All scorers cleared");
        }

        /// <summary>
        /// 이름으로 타겟 Scorer 찾기
        /// </summary>
        public ITargetScorer GetTargetScorer(string name)
        {
            foreach (var scorer in _targetScorers)
            {
                if (scorer.ScorerName == name)
                    return scorer;
            }
            return null;
        }

        /// <summary>
        /// 이름으로 스킬 Scorer 찾기
        /// </summary>
        public ISkillScorer GetSkillScorer(string name)
        {
            foreach (var scorer in _skillScorers)
            {
                if (scorer.ScorerName == name)
                    return scorer;
            }
            return null;
        }
    }
}
