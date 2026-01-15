using System;
using System.Collections.Generic;
using DungeonLog.AI.Strategies.Interfaces;

namespace DungeonLog.AI.Core
{
    /// <summary>
    /// Strategy Cache
    /// Strategy 인스턴스 생성 비용을 절감하기 위한 캐싱 시스템
    /// </summary>
    public class StrategyCache
    {
        // 캐싱된 Targeting Strategies
        private readonly Dictionary<string, ITargetingStrategy> _targetingStrategyCache;

        // 캐싱된 Skill Selection Strategies
        private readonly Dictionary<string, ISkillSelectionStrategy> _skillSelectionStrategyCache;

        // Target Scorers 캐시
        private readonly Dictionary<Type, object> _targetScorerCache;

        // Skill Scorers 캐시
        private readonly Dictionary<Type, object> _skillScorerCache;

        /// <summary>
        /// 생성자
        /// </summary>
        public StrategyCache()
        {
            _targetingStrategyCache = new Dictionary<string, ITargetingStrategy>();
            _skillSelectionStrategyCache = new Dictionary<string, ISkillSelectionStrategy>();
            _targetScorerCache = new Dictionary<Type, object>();
            _skillScorerCache = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Targeting Strategy를 가져오거나 생성하여 캐시합니다.
        /// </summary>
        /// <typeparam name="T">Targeting Strategy 타입</typeparam>
        /// <param name="key">캐시 키</param>
        /// <param name="factory">Strategy 생성 함수</param>
        public T GetOrCreateTargetingStrategy<T>(string key, Func<T> factory) where T : class, ITargetingStrategy
        {
            if (_targetingStrategyCache.TryGetValue(key, out var cached))
            {
                return cached as T;
            }

            T strategy = factory();
            _targetingStrategyCache[key] = strategy;
            return strategy;
        }

        /// <summary>
        /// Skill Selection Strategy를 가져오거나 생성하여 캐시합니다.
        /// </summary>
        /// <typeparam name="T">Skill Selection Strategy 타입</typeparam>
        /// <param name="key">캐시 키</param>
        /// <param name="factory">Strategy 생성 함수</param>
        public T GetOrCreateSkillSelectionStrategy<T>(string key, Func<T> factory) where T : class, ISkillSelectionStrategy
        {
            if (_skillSelectionStrategyCache.TryGetValue(key, out var cached))
            {
                return cached as T;
            }

            T strategy = factory();
            _skillSelectionStrategyCache[key] = strategy;
            return strategy;
        }

        /// <summary>
        /// Target Scorer를 가져오거나 생성하여 캐시합니다.
        /// </summary>
        /// <typeparam name="T">Target Scorer 타입</typeparam>
        /// <param name="factory">Scorer 생성 함수</param>
        public T GetOrCreateTargetScorer<T>(Func<T> factory) where T : class
        {
            Type type = typeof(T);

            if (_targetScorerCache.TryGetValue(type, out var cached))
            {
                return cached as T;
            }

            T scorer = factory();
            _targetScorerCache[type] = scorer;
            return scorer;
        }

        /// <summary>
        /// Skill Scorer를 가져오거나 생성하여 캐시합니다.
        /// </summary>
        /// <typeparam name="T">Skill Scorer 타입</typeparam>
        /// <param name="factory">Scorer 생성 함수</param>
        public T GetOrCreateSkillScorer<T>(Func<T> factory) where T : class
        {
            Type type = typeof(T);

            if (_skillScorerCache.TryGetValue(type, out var cached))
            {
                return cached as T;
            }

            T scorer = factory();
            _skillScorerCache[type] = scorer;
            return scorer;
        }

        /// <summary>
        /// 캐시를 비웁니다.
        /// </summary>
        public void Clear()
        {
            _targetingStrategyCache.Clear();
            _skillSelectionStrategyCache.Clear();
            _targetScorerCache.Clear();
            _skillScorerCache.Clear();
        }

        /// <summary>
        /// 캐시된 항목 수
        /// </summary>
        public int Count => _targetingStrategyCache.Count + 
                           _skillSelectionStrategyCache.Count + 
                           _targetScorerCache.Count + 
                           _skillScorerCache.Count;
    }
}
