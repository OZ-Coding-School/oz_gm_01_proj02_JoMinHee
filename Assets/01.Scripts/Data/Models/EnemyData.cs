using System.Collections.Generic;
using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 적의 AI 행동 패턴을 정의합니다.
    /// </summary>
    public enum AIBehaviorType
    {
        Random,         // 랜덤 공격
        Aggressive,     // 공격적 (가장 약한 아군 우선)
        Defensive,      // 방어적 (가장 강한 아군 공격)
        Tactical        // 전략적 (상황에 따라 다름)
    }

    /// <summary>
    /// 적 데이터를 정의하는 ScriptableObject입니다.
    /// 적의 스탯, 행동 패턴, 리워드 등을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Dungeon Log/Enemy Data")]
    public class EnemyData : BaseData, IDataLoadable
    {
        [Header("스탯")]
        [SerializeField, Tooltip("최대 HP")]
        private int maxHP;

        [SerializeField, Tooltip("공격력")]
        private int attack;

        [SerializeField, Tooltip("기본 방어력 (컨셉: 기본 0, 스킬/유물로만 부여 가능)")]
        private int defense = 0;

        [SerializeField, Range(0f, 1f), Tooltip("치명타 확률")]
        private float criticalChance = 0.05f;

        [Header("시각 정보")]
        [SerializeField, Tooltip("적 스프라이트")]
        private Sprite enemySprite;

        [Header("AI")]
        [SerializeField, Tooltip("AI 행동 패턴")]
        private AIBehaviorType aiBehavior;

        [SerializeField, Tooltip("사용 가능한 스킬 ID 목록 (Database에서 지연 로딩)")]
        private List<string> skillIds;

        [Header("전투 정보")]
        [SerializeField, Tooltip("출현하는 층 (1 ~ 4)")]
        private int minFloor = 1;

        [SerializeField, Tooltip("출현하는 최대 층")]
        private int maxFloor = 4;

        [SerializeField, Tooltip("보스 여부")]
        private bool isBoss = false;

        [Header("리워드")]
        [SerializeField, Tooltip("드랍 골드")]
        private int goldReward = 10;

        [SerializeField, Tooltip("드랍 경험치")]
        private int expReward = 5;

        [SerializeField, Tooltip("드랍 유물 ID (비어있으면 드랍하지 않음)")]
        private string dropRelicID = "";

        [SerializeField, Range(0f, 1f), Tooltip("유물 드랍 확률")]
        private float relicDropChance = 0.1f;

        // 캐싱을 위한 필드
        [System.NonSerialized]
        private List<SkillData> _cachedSkills;
        [System.NonSerialized]
        private bool _isCacheInitialized = false;

        // 속성 접근자
        public int MaxHP => maxHP;
        public int Attack => attack;
        public int Defense => defense;
        public float CriticalChance => criticalChance;
        public Sprite EnemySprite => enemySprite;
        public AIBehaviorType AIBehavior => aiBehavior;

        /// <summary>
        /// 스킬 ID 목록 (데이터 설정용)
        /// </summary>
        public List<string> SkillIds => skillIds;

        /// <summary>
        /// 스킬 목록 (Database에서 지연 로딩하여 캐싱)
        /// </summary>
        public List<SkillData> Skills
        {
            get
            {
                if (!_isCacheInitialized)
                {
                    InitializeCache();
                }
                return _cachedSkills ?? new List<SkillData>();
            }
        }

        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;
        public bool IsBoss => isBoss;
        public int GoldReward => goldReward;
        public int ExpReward => expReward;
        public string DropRelicID => dropRelicID;
        public float RelicDropChance => relicDropChance;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            if (maxHP <= 0)
            {
                Debug.LogWarning($"[EnemyData] {ID}: HP는 0보다 커야 합니다.");
                return false;
            }

            if (attack < 0)
            {
                Debug.LogWarning($"[EnemyData] {ID}: 공격력은 0보다 작을 수 없습니다.");
                return false;
            }

            if (skillIds == null || skillIds.Count == 0)
            {
                Debug.LogWarning($"[EnemyData] {ID}: 최소 1개의 스킬 ID가 필요합니다.");
                return false;
            }

            if (minFloor < 1 || maxFloor > 4 || minFloor > maxFloor)
            {
                Debug.LogWarning($"[EnemyData] {ID}: 층 설정이 유효하지 않습니다 (1 ~ 4).");
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

            _cachedSkills = new List<SkillData>();

            if (skillIds != null && Database.Instance != null)
            {
                foreach (string skillId in skillIds)
                {
                    SkillData skill = Database.Instance.GetSkill(skillId);
                    if (skill != null)
                    {
                        _cachedSkills.Add(skill);
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyData] {ID}: 스킬 ID '{skillId}'를 찾을 수 없습니다.");
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
            _cachedSkills = null;
        }

        /// <summary>
        /// CSV 데이터를 로드합니다.
        /// </summary>
        public void LoadFromCSV(Dictionary<string, string> csvData)
        {
            // BaseData의 SetID() 사용 (리플렉션 제거)
            if (csvData.ContainsKey("ID"))
                SetID(csvData["ID"]);

            if (csvData.ContainsKey("Name"))
                SetDisplayName(csvData["Name"]);

            if (csvData.ContainsKey("Description"))
                SetDescription(csvData["Description"]);

            if (csvData.ContainsKey("MaxHP") && int.TryParse(csvData["MaxHP"], out int hp))
                maxHP = hp;

            if (csvData.ContainsKey("Attack") && int.TryParse(csvData["Attack"], out int atk))
                attack = atk;

            if (csvData.ContainsKey("Defense") && int.TryParse(csvData["Defense"], out int def))
                defense = def;

            if (csvData.ContainsKey("CriticalChance") && float.TryParse(csvData["CriticalChance"], out float crit))
                criticalChance = crit;

            if (csvData.ContainsKey("AIBehavior") && System.Enum.TryParse<AIBehaviorType>(csvData["AIBehavior"], out AIBehaviorType ai))
                aiBehavior = ai;

            if (csvData.ContainsKey("MinFloor") && int.TryParse(csvData["MinFloor"], out int min))
                minFloor = min;

            if (csvData.ContainsKey("MaxFloor") && int.TryParse(csvData["MaxFloor"], out int max))
                maxFloor = max;

            if (csvData.ContainsKey("IsBoss") && bool.TryParse(csvData["IsBoss"], out bool boss))
                isBoss = boss;

            if (csvData.ContainsKey("GoldReward") && int.TryParse(csvData["GoldReward"], out int gold))
                goldReward = gold;

            if (csvData.ContainsKey("ExpReward") && int.TryParse(csvData["ExpReward"], out int exp))
                expReward = exp;

            if (csvData.ContainsKey("DropRelicID"))
                dropRelicID = csvData["DropRelicID"];

            if (csvData.ContainsKey("RelicDropChance") && float.TryParse(csvData["RelicDropChance"], out float dropChance))
                relicDropChance = dropChance;
        }
    }
}
