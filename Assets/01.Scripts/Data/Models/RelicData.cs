using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 유물 타입을 정의합니다.
    /// </summary>
    public enum RelicType
    {
        Passive,        // 패시브 (지속 효과)
        Active,         // 액티브 (사용 효과)
        Trigger         // 트리거 (특정 조건 시 발동)
    }

    /// <summary>
    /// 유물 데이터를 정의하는 ScriptableObject입니다.
    /// 유물은 플레이어에게 강력한 패시브 보너스를 제공합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRelic", menuName = "Dungeon Log/Relic Data")]
    public class RelicData : BaseData, IDataLoadable
    {
        [Header("유물 정보")]
        [SerializeField, Tooltip("유물 타입")]
        private RelicType relicType;

        [SerializeField, Tooltip("유물 등급")]
        private ItemRarity rarity;

        [Header("패시브 효과")]
        [SerializeField, Tooltip("공격력 증가량 (퍼센트 또는 고정값)")]
        private float attackIncrease = 0f;

        [SerializeField, Tooltip("방어력 증가량")]
        private float defenseIncrease = 0f;

        [SerializeField, Tooltip("최대 HP 증가량")]
        private int hpIncrease = 0;

        [SerializeField, Tooltip("치명타 확률 증가량")]
        private float criticalChanceIncrease = 0f;

        [Header("스킬 보너스")]
        [SerializeField, Tooltip("스킬 리롤 비용 감소")]
        private int rerollCostReduction = 0;

        [SerializeField, Tooltip("초기 AP 증가")]
        private int startingAPIncrease = 0;

        [Header("시각 정보")]
        [SerializeField, Tooltip("유물 아이콘")]
        private Sprite icon;

        [Header("특수 효과")]
        [SerializeField, TextArea(2, 3), Tooltip("특수 효과 설명 (코드로 구현 필요)")]
        private string specialEffect;

        [SerializeField, Tooltip("특수 효과 ID (코드에서 참조용)")]
        private string specialEffectID = "";

        // 속성 접근자
        public RelicType RelicType => relicType;
        public ItemRarity Rarity => rarity;
        public float AttackIncrease => attackIncrease;
        public float DefenseIncrease => defenseIncrease;
        public int HPIncrease => hpIncrease;
        public float CriticalChanceIncrease => criticalChanceIncrease;
        public int RerollCostReduction => rerollCostReduction;
        public int StartingAPIncrease => startingAPIncrease;
        public Sprite Icon => icon;
        public string SpecialEffect => specialEffect;
        public string SpecialEffectID => specialEffectID;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            // 유물은 최소한 하나의 보너스가 있어야 함
            if (attackIncrease == 0f &&
                defenseIncrease == 0f &&
                hpIncrease == 0 &&
                criticalChanceIncrease == 0f &&
                rerollCostReduction == 0 &&
                startingAPIncrease == 0 &&
                string.IsNullOrEmpty(specialEffectID))
            {
                Debug.LogWarning($"[RelicData] {ID}: 유물은 최소 하나의 보너스 효과가 있어야 합니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// CSV 데이터를 로드합니다.
        /// </summary>
        public void LoadFromCSV(System.Collections.Generic.Dictionary<string, string> csvData)
        {
            // BaseData의 SetID() 사용 (리플렉션 제거)
            if (csvData.ContainsKey("ID"))
                SetID(csvData["ID"]);

            if (csvData.ContainsKey("Name"))
                SetDisplayName(csvData["Name"]);

            if (csvData.ContainsKey("Description"))
                SetDescription(csvData["Description"]);

            if (csvData.ContainsKey("RelicType") && System.Enum.TryParse<RelicType>(csvData["RelicType"], out RelicType type))
                relicType = type;

            if (csvData.ContainsKey("Rarity") && System.Enum.TryParse<ItemRarity>(csvData["Rarity"], out ItemRarity r))
                rarity = r;

            if (csvData.ContainsKey("AttackIncrease") && float.TryParse(csvData["AttackIncrease"], out float atk))
                attackIncrease = atk;

            if (csvData.ContainsKey("DefenseIncrease") && float.TryParse(csvData["DefenseIncrease"], out float def))
                defenseIncrease = def;

            if (csvData.ContainsKey("HPIncrease") && int.TryParse(csvData["HPIncrease"], out int hp))
                hpIncrease = hp;

            if (csvData.ContainsKey("CriticalChanceIncrease") && float.TryParse(csvData["CriticalChanceIncrease"], out float crit))
                criticalChanceIncrease = crit;

            if (csvData.ContainsKey("RerollCostReduction") && int.TryParse(csvData["RerollCostReduction"], out int reroll))
                rerollCostReduction = reroll;

            if (csvData.ContainsKey("StartingAPIncrease") && int.TryParse(csvData["StartingAPIncrease"], out int ap))
                startingAPIncrease = ap;

            if (csvData.ContainsKey("SpecialEffect"))
                specialEffect = csvData["SpecialEffect"];

            if (csvData.ContainsKey("SpecialEffectID"))
                specialEffectID = csvData["SpecialEffectID"];
        }
    }
}
