using System;
using System.Collections.Generic;
using DungeonLog.AI.Core;
using DungeonLog.Character;

namespace DungeonLog.AI.Core
{
    /// <summary>
    /// AIAction Object Pool
    /// AIAction 객체 생성/파괴 비용을 절감하기 위한 Object Pool
    /// Thread-safe하게 구현되어 있습니다.
    /// </summary>
    public class AIActionPool
    {
        // 풀링된 AIAction 객체들
        private readonly Stack<AIAction> _pool;

        // 현재 사용 중인 객체 수 (thread-safe)
        private int _activeCount;

        // Lock object for thread safety
        private readonly object _lock = new object();

        // 풀 사이즈
        private readonly int _initialSize;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="initialSize">초기 풀 사이즈</param>
        public AIActionPool(int initialSize = 10)
        {
            _initialSize = initialSize;
            _pool = new Stack<AIAction>(initialSize);
            _activeCount = 0;

            // 초기 풀 생성
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(new AIAction());
            }
        }

        /// <summary>
        /// AIAction을 풀에서 가져옵니다.
        /// Thread-safe합니다.
        /// </summary>
        public AIAction Get()
        {
            lock (_lock)
            {
                AIAction action;

                if (_pool.Count > 0)
                {
                    action = _pool.Pop();
                }
                else
                {
                    // 풀이 비어있으면 새로 생성
                    action = new AIAction();
                }

                _activeCount++;
                return action;
            }
        }

        /// <summary>
        /// AIAction을 풀로 반환합니다.
        /// Thread-safe합니다.
        /// </summary>
        /// <param name="action">반환할 AIAction</param>
        public void Return(AIAction action)
        {
            if (action == null)
                return;

            lock (_lock)
            {
                try
                {
                    // 데이터 초기화
                    action.Clear();

                    _pool.Push(action);
                    _activeCount--;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[AIActionPool] Return failed: {ex.Message}");
                    // 복구: activeCount만 감소 (pool에 못 넣어도 카운트는 일치하게)
                    _activeCount = UnityEngine.Mathf.Max(0, _activeCount - 1);
                }
            }
        }

        /// <summary>
        /// 여러 AIAction을 한꺼번에 반환합니다.
        /// Thread-safe합니다.
        /// </summary>
        public void ReturnRange(List<AIAction> actions)
        {
            if (actions == null)
                return;

            // 각각 Return() 호출 (각각 lock으로 보호됨)
            foreach (var action in actions)
            {
                Return(action);
            }
        }

        /// <summary>
        /// 풀 비우기 (메모리 정리)
        /// Thread-safe합니다.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _pool.Clear();
                _activeCount = 0;
            }
        }

        /// <summary>
        /// 현재 활성화된 객체 수
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// 풀에 대기 중인 객체 수
        /// </summary>
        public int InactiveCount => _pool.Count;

        /// <summary>
        /// 총 객체 수 (활성 + 비활성)
        /// </summary>
        public int TotalCount => _activeCount + _pool.Count;
    }
}
