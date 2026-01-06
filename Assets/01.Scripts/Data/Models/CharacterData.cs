using System.Collections.Generic;
using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 캐릭터 클래스 타입을 정의합니다.
    /// </summary>
    public enum CharacterClass
    {
        Warrior,    // 전사
        Mage,       // 법사
        Archer,     // 궁수
        Healer,     // 힐러
        Tank,       // 탱커
        Assassin    // 암살자
    }

    /// <summary>
    /// 캐릭터 데이터를 정의하는 ScriptableObject입니다.
    /// 캐릭터의 기본 스탯, 스킬, 해금 정보 등을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Dungeon Log/Character Data")]
    public class CharacterData : BaseData, IDataLoadable
    {
        [Header("스탯")]
        [SerializeField, Tooltip("최대 HP")]
        private int baseHP;

        [SerializeField, Tooltip("기본 공격력")]
        private int baseAttack;

        [SerializeField, Tooltip("기본 방어력 (컨셉: 기본 0, 스킬/유물로만 부여 가능)")]
        private int baseDefense = 0;

        [SerializeField, Range(0f, 1f), Tooltip("치명타 확률 (0.0 ~ 1.0)")]
        private float baseCriticalChance;

        [Header("시각 정보")]
        [SerializeField, Tooltip("캐릭터 초상화 이미지")]
        private Sprite portrait;

        [SerializeField, Tooltip("캐릭터 스프라이트")]
        private Sprite characterSprite;

        [Header("클래스")]
        [SerializeField, Tooltip("캐릭터의 기본 클래스")]
        private CharacterClass baseClass;

        [SerializeField, Tooltip("승급 가능한 클래스 목록")]
        private List<CharacterClass> promotionClasses;

        [Header("스킬")]
        [SerializeField, Tooltip("기본 제공 스킬 ID 목록 (Database에서 지연 로딩)")]
        private List<string> defaultSkillIds;

        [Header("해금 정보")]
        [SerializeField, Tooltip("잠김 여부 (true면 해금 필요)")]
        private bool isLocked = false;

        [SerializeField, Tooltip("해금에 필요한 골드")]
        private int unlockCost = 100;

        // 캐싱을 위한 필드
        [System.NonSerialized]
        private List<SkillData> _cachedDefaultSkills;
        [System.NonSerialized]
        private bool _isCacheInitialized = false;

        // 속성 접근자
        public int BaseHP => baseHP;
        public int BaseAttack => baseAttack;
        public int BaseDefense => baseDefense;
        public float BaseCriticalChance => baseCriticalChance;
        public Sprite Portrait => portrait;
        public Sprite CharacterSprite => characterSprite;
        public CharacterClass BaseClass => baseClass;
        public List<CharacterClass> PromotionClasses => promotionClasses;

        /// <summary>
        /// 스킬 ID 목록 (데이터 설정용)
        /// </summary>
        public List<string> DefaultSkillIds => defaultSkillIds;

        /// <summary>
        /// 기본 스킬 목록 (Database에서 지연 로딩하여 캐싱)
        /// </summary>
        public List<SkillData> DefaultSkills
        {
            get
            {
                if (!_isCacheInitialized)
                {
                    InitializeCache();
                }
                return _cachedDefaultSkills ?? new List<SkillData>();
            }
        }

        public bool IsLocked => isLocked;
        public int UnlockCost => unlockCost;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            if (baseHP <= 0)
            {
                Debug.LogWarning($"[CharacterData] {ID}: HP는 0보다 커야 합니다.");
                return false;
            }

            if (baseAttack < 0)
            {
                Debug.LogWarning($"[CharacterData] {ID}: 공격력은 0보다 작을 수 없습니다.");
                return false;
            }

            if (baseDefense < 0)
            {
                Debug.LogWarning($"[CharacterData] {ID}: 방어력은 0보다 작을 수 없습니다.");
                return false;
            }

            if (defaultSkillIds == null || defaultSkillIds.Count == 0)
            {
                Debug.LogWarning($"[CharacterData] {ID}: 최소 1개의 스킬 ID가 필요합니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 캐시를 초기화합니다. Database를 통해 스킬 데이터를 지연 로딩합니다.
        /// </summary>
        private void InitializeCache()
        {
            if (_isCacheInitialized) return;

            _cachedDefaultSkills = new List<SkillData>();

            if (defaultSkillIds != null && Database.Instance != null)
            {
                foreach (string skillId in defaultSkillIds)
                {
                    SkillData skill = Database.Instance.GetSkill(skillId);
                    if (skill != null)
                    {
                        _cachedDefaultSkills.Add(skill);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterData] {ID}: 스킬 ID '{skillId}'를 찾을 수 없습니다.");
                    }
                }
            }

            _isCacheInitialized = true;
        }

        /// <summary>
        /// 캐시를 초기화하여 다음 접근 시 다시 로드하도록 합니다.
        /// 데이터가 변경되었을 때 호출합니다.
        /// </summary>
        public void InvalidateCache()
        {
            _isCacheInitialized = false;
            _cachedDefaultSkills = null;
        }

        /// <summary>
        /// CSV 데이터를 로드합니다.
        /// </summary>
        public void LoadFromCSV(Dictionary<string, string> csvData)
        {
            if (csvData.ContainsKey("ID"))
                SetID(csvData["ID"]);

            if (csvData.ContainsKey("Name"))
                displayName = csvData["Name"];

            if (csvData.ContainsKey("Description"))
                description = csvData["Description"];

            if (csvData.ContainsKey("BaseHP") && int.TryParse(csvData["BaseHP"], out int hp))
                baseHP = hp;

            if (csvData.ContainsKey("BaseAttack") && int.TryParse(csvData["BaseAttack"], out int atk))
                baseAttack = atk;

            if (csvData.ContainsKey("BaseDefense") && int.TryParse(csvData["BaseDefense"], out int def))
                baseDefense = def;

            if (csvData.ContainsKey("CriticalChance") && float.TryParse(csvData["CriticalChance"], out float crit))
                baseCriticalChance = crit;

            if (csvData.ContainsKey("UnlockCost") && int.TryParse(csvData["UnlockCost"], out int cost))
                unlockCost = cost;

            // 스킬 ID 리스트 파싱 (세미콜론으로 구분)
            if (csvData.ContainsKey("DefaultSkillIDs"))
            {
                defaultSkillIds = new List<string>();
                string[] skillIdArray = csvData["DefaultSkillIDs"].Split(';');
                foreach (string skillId in skillIdArray)
                {
                    string trimmedId = skillId.Trim();
                    if (!string.IsNullOrEmpty(trimmedId))
                    {
                        defaultSkillIds.Add(trimmedId);
                    }
                }
            }
        }
    }
}
