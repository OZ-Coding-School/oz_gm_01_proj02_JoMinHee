using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonLog.Data;

namespace DungeonLog.Character
{
    /// <summary>
    /// 캐릭터의 스킬 풀과 스킬 추첨, 리롤을 관리하는 클래스입니다.
    /// 턴마다 4개 스킬 중 1개를 랜덤 추첨하고, 리롤 1회를 제공합니다.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public class SkillManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("리롤 최대 횟수")]
        private int maxRerollCount = 1;

        [SerializeField, Tooltip("턴당 추첨 스킬 수")]
        private int skillsPerTurn = 4;

        // 스킬 ID 저장 (SkillData는 지연 로딩)
        private List<string> _skillPoolIds;
        private List<string> _currentAvailableSkillIds;
        private string _currentSelectedSkillId;

        // 지연 로딩 캐시 (성능 최적화)
        private Dictionary<string, SkillData> _skillDataCache;

        // 리롤 상태
        private int _currentRerollCount = 0;

        // 초기화 완료 플래그
        private bool _isInitialized = false;

        // ========================================================================
        // 초기화
        // ========================================================================

        private void Awake()
        {
            _skillPoolIds = new List<string>();
            _currentAvailableSkillIds = new List<string>();
            _skillDataCache = new Dictionary<string, SkillData>();
        }

        /// <summary>
        /// 스킬 풀로 초기화합니다.
        /// </summary>
        public void Initialize(List<string> skillIds)
        {
            if (skillIds == null || skillIds.Count == 0)
            {
                Debug.LogError($"[SkillManager] 스킬 ID 리스트가 비어있습니다.", this);
                return;
            }

            // 방어적 초기화 (Awake()가 아직 호출되지 않은 경우 대비)
            _skillPoolIds ??= new List<string>();
            _currentAvailableSkillIds ??= new List<string>();
            _skillDataCache ??= new Dictionary<string, SkillData>();

            _skillPoolIds = new List<string>(skillIds);
            _skillDataCache.Clear();

            _isInitialized = true;
            Debug.Log($"[SkillManager] 초기화 완료: {_skillPoolIds.Count}개 스킬 풀");
        }

        // ========================================================================
        // 스킬 추첨
        // ========================================================================

        /// <summary>
        /// 턴 시작 시 호출하여 새로운 스킬을 추첨합니다.
        /// </summary>
        public void DrawNewSkills()
        {
            if (!_isInitialized)
            {
                Debug.LogError($"[SkillManager] 초기화되지 않았습니다.");
                return;
            }

            // 스킬 풀이 부족하면 전체 사용
            int drawCount = Mathf.Min(skillsPerTurn, _skillPoolIds.Count);

            // 현재 사용 가능한 스킬 ID 선택
            _currentAvailableSkillIds = new List<string>();

            // 풀에서 랜덤 선택 (중복 없이)
            List<string> tempPool = new List<string>(_skillPoolIds);

            for (int i = 0; i < drawCount && tempPool.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
                string skillId = tempPool[randomIndex];
                _currentAvailableSkillIds.Add(skillId);
                tempPool.RemoveAt(randomIndex);
            }

            // 리롤 카운트 초기화 (턴 시작 시에만)
            _currentRerollCount = 0;

            // 첫 번째 스킬 자동 선택
            _currentSelectedSkillId = _currentAvailableSkillIds.Count > 0
                ? _currentAvailableSkillIds[0]
                : null;

            Debug.Log($"[SkillManager] {drawCount}개 스킬 추첨 완료");

            // 이벤트 발생
            SkillData currentSkill = GetCurrentSkillData();
            if (currentSkill != null)
            {
                CharacterEvents.NotifySkillDrawn(gameObject, currentSkill);
            }
        }

        /// <summary>
        /// 새로운 스킬을 추첨합니다 (리롤용 내부 메서드).
        /// 리롤 카운트를 초기화하지 않습니다.
        /// </summary>
        private void DrawNewSkillsInternal()
        {
            int drawCount = Mathf.Min(4, _skillPoolIds.Count);
            _currentAvailableSkillIds.Clear();

            // 풀에서 랜덤 선택 (중복 없이)
            List<string> tempPool = new List<string>(_skillPoolIds);

            for (int i = 0; i < drawCount && tempPool.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
                string skillId = tempPool[randomIndex];
                _currentAvailableSkillIds.Add(skillId);
                tempPool.RemoveAt(randomIndex);
            }

            // 첫 번째 스킬 자동 선택
            _currentSelectedSkillId = _currentAvailableSkillIds.Count > 0
                ? _currentAvailableSkillIds[0]
                : null;

            Debug.Log($"[SkillManager] {drawCount}개 스킬 재추첨 완료");

            // 이벤트 발생
            SkillData currentSkill = GetCurrentSkillData();
            if (currentSkill != null)
            {
                CharacterEvents.NotifySkillDrawn(gameObject, currentSkill);
            }
        }

        /// <summary>
        /// 스킬을 재추첨합니다 (리롤).
        /// </summary>
        public bool RerollSkills()
        {
            if (!CanReroll())
            {
                Debug.LogWarning($"[SkillManager] 리롤 불가: 최대 횟수 초과");
                return false;
            }

            _currentRerollCount++;

            // 새로 추첨 (리롤 카운트 초기화하지 않음)
            DrawNewSkillsInternal();

            Debug.Log($"[SkillManager] 리롤 수행 ({_currentRerollCount}/{maxRerollCount})");
            return true;
        }

        /// <summary>
        /// 리롤 가능 여부를 확인합니다.
        /// </summary>
        public bool CanReroll()
        {
            return _currentRerollCount < maxRerollCount;
        }

        // ========================================================================
        // 스킬 선택
        // ========================================================================

        /// <summary>
        /// 현재 추첨된 스킬 중 특정 인덱스의 스킬을 선택합니다.
        /// </summary>
        public void SelectSkill(int index)
        {
            if (index < 0 || index >= _currentAvailableSkillIds.Count)
            {
                Debug.LogWarning($"[SkillManager] 잘못된 인덱스: {index}");
                return;
            }

            _currentSelectedSkillId = _currentAvailableSkillIds[index];
            Debug.Log($"[SkillManager] 스킬 선택: {_currentSelectedSkillId}");
        }

        // ========================================================================
        // 스킬 데이터 접근 (지연 로딩)
        // ========================================================================

        /// <summary>
        /// 현재 선택된 스킬 데이터를 가져옵니다.
        /// </summary>
        public SkillData GetCurrentSkillData()
        {
            if (string.IsNullOrEmpty(_currentSelectedSkillId))
                return null;

            return GetSkillData(_currentSelectedSkillId);
        }

        /// <summary>
        /// 스킬 데이터를 가져옵니다 (캐싱됨).
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;

            // 캐시에 있으면 반환
            if (_skillDataCache.TryGetValue(skillId, out SkillData cached))
            {
                return cached;
            }

            // Database에서 로드
            SkillData skill = Database.Instance?.GetSkill(skillId);
            if (skill != null)
            {
                _skillDataCache[skillId] = skill;
                return skill;
            }

            Debug.LogWarning($"[SkillManager] 스킬 데이터를 찾을 수 없음: {skillId}");
            return null;
        }

        /// <summary>
        /// 현재 사용 가능한 모든 스킬 데이터를 가져옵니다.
        /// </summary>
        public List<SkillData> GetCurrentAvailableSkills()
        {
            List<SkillData> skills = new List<SkillData>();

            foreach (string skillId in _currentAvailableSkillIds)
            {
                SkillData skill = GetSkillData(skillId);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }

            return skills;
        }

        // ========================================================================
        // 스킬 풀 관리
        // ========================================================================

        /// <summary>
        /// 스킬 풀에 새로운 스킬을 추가합니다 (아이템/버프 등).
        /// </summary>
        public void AddSkillToPool(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                Debug.LogWarning($"[SkillManager] 스킬 ID가 비어있습니다.");
                return;
            }

            if (_skillPoolIds.Contains(skillId))
            {
                Debug.LogWarning($"[SkillManager] 스킬이 이미 풀에 있습니다: {skillId}");
                return;
            }

            _skillPoolIds.Add(skillId);
            Debug.Log($"[SkillManager] 스킬 풀에 추가됨: {skillId}");
        }

        /// <summary>
        /// 스킬 풀에서 스킬을 제거합니다.
        /// </summary>
        public void RemoveSkillFromPool(string skillId)
        {
            if (_skillPoolIds.Remove(skillId))
            {
                Debug.Log($"[SkillManager] 스킬 풀에서 제거됨: {skillId}");
            }
        }

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        /// <summary>현재 선택된 스킬 ID</summary>
        public string CurrentSkillId => _currentSelectedSkillId;

        /// <summary>현재 사용 가능한 스킬 ID 리스트</summary>
        public List<string> CurrentAvailableSkillIds => new List<string>(_currentAvailableSkillIds);

        /// <summary>스킬 풀의 전체 스킬 수</summary>
        public int TotalSkillPoolCount => _skillPoolIds.Count;

        /// <summary>현재 리롤 사용 횟수</summary>
        public int CurrentRerollCount => _currentRerollCount;

        /// <summary>최대 리롤 횟수</summary>
        public int MaxRerollCount => maxRerollCount;

        /// <summary>초기화 여부</summary>
        public bool IsInitialized => _isInitialized;

        // ========================================================================
        // 전투 시작/종료
        // ========================================================================

        /// <summary>
        /// 전투 시작 시 스킬을 추첨합니다.
        /// </summary>
        public void ResetForBattle()
        {
            _currentRerollCount = 0;
            DrawNewSkills();
            Debug.Log($"[SkillManager] 전투 시작: 스킬 추첨 완료");
        }

        // ========================================================================
        // 생명주기 정리
        // ========================================================================

        private void OnDestroy()
        {
            // 캐시 정리
            _skillDataCache?.Clear();
        }
    }
}
