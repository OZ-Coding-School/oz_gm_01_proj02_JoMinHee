using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 아이템 타입을 정의합니다.
    /// </summary>
    public enum ItemType
    {
        Weapon,         // 무기
        Armor,          // 방어구
        Accessory,      // 액세서리
        Consumable      // 소비품
    }

    /// <summary>
    /// 아이템 등급을 정의합니다.
    /// </summary>
    public enum ItemRarity
    {
        Common,         // 일반 (★)
        Rare,           // 희귀 (★★)
        Epic,           // 에픽 (★★★)
        Legendary       // 전설 (★★★★)
    }

    /// <summary>
    /// 아이템 데이터를 정의하는 ScriptableObject입니다.
    /// 장비 아이템의 스탯 보정, 가격, 등급 등을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Dungeon Log/Item Data")]
    public class ItemData : BaseData, IDataLoadable
    {
        [Header("아이템 정보")]
        [SerializeField, Tooltip("아이템 타입")]
        private ItemType itemType;

        [SerializeField, Tooltip("아이템 등급")]
        private ItemRarity rarity;

        [Header("스탯 보정")]
        [SerializeField, Tooltip("공격력 보정 (+값: 증가, -값: 감소)")]
        private int attackBonus = 0;

        [SerializeField, Tooltip("방어력 보정 (+값: 증가, -값: 감소)")]
        private int defenseBonus = 0;

        [SerializeField, Tooltip("HP 보정")]
        private int hpBonus = 0;

        [SerializeField, Tooltip("치명타 확률 보정 (0.0 ~ 1.0)")]
        private float criticalBonus = 0f;

        [Header("시각 정보")]
        [SerializeField, Tooltip("아이템 아이콘")]
        private Sprite icon;

        [SerializeField, Tooltip("장비 시 표시할 스프라이트")]
        private Sprite equippedSprite;

        [Header("가격")]
        [SerializeField, Tooltip("구매 가격")]
        private int buyPrice = 50;

        [SerializeField, Tooltip("판매 가격")]
        private int sellPrice = 25;

        [Header("해금")]
        [SerializeField, Tooltip("잠김 여부")]
        private bool isLocked = false;

        [SerializeField, Tooltip("해금에 필요한 골드")]
        private int unlockCost = 100;

        // 속성 접근자
        public ItemType ItemType => itemType;
        public ItemRarity Rarity => rarity;
        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;
        public int HPBonus => hpBonus;
        public float CriticalBonus => criticalBonus;
        public Sprite Icon => icon;
        public Sprite EquippedSprite => equippedSprite;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public bool IsLocked => isLocked;
        public int UnlockCost => unlockCost;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            if (buyPrice < 0 || sellPrice < 0)
            {
                Debug.LogWarning($"[ItemData] {ID}: 가격은 0보다 작을 수 없습니다.");
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

            if (csvData.ContainsKey("ItemType") && System.Enum.TryParse<ItemType>(csvData["ItemType"], out ItemType type))
                itemType = type;

            if (csvData.ContainsKey("Rarity") && System.Enum.TryParse<ItemRarity>(csvData["Rarity"], out ItemRarity r))
                rarity = r;

            if (csvData.ContainsKey("AttackBonus") && int.TryParse(csvData["AttackBonus"], out int atk))
                attackBonus = atk;

            if (csvData.ContainsKey("DefenseBonus") && int.TryParse(csvData["DefenseBonus"], out int def))
                defenseBonus = def;

            if (csvData.ContainsKey("HPBonus") && int.TryParse(csvData["HPBonus"], out int hp))
                hpBonus = hp;

            if (csvData.ContainsKey("CriticalBonus") && float.TryParse(csvData["CriticalBonus"], out float crit))
                criticalBonus = crit;

            if (csvData.ContainsKey("BuyPrice") && int.TryParse(csvData["BuyPrice"], out int buy))
                buyPrice = buy;

            if (csvData.ContainsKey("SellPrice") && int.TryParse(csvData["SellPrice"], out int sell))
                sellPrice = sell;

            if (csvData.ContainsKey("IsLocked") && bool.TryParse(csvData["IsLocked"], out bool locked))
                isLocked = locked;

            if (csvData.ContainsKey("UnlockCost") && int.TryParse(csvData["UnlockCost"], out int cost))
                unlockCost = cost;
        }
    }
}
