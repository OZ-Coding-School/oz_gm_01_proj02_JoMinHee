using System.Collections.Generic;
using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 던전 방 타입을 정의합니다.
    /// </summary>
    public enum RoomType
    {
        Battle,         // 전투
        Event,          // 이벤트
        Shop,           // 상점
        Rest,           // 휴식
        Boss            // 보스
    }

    /// <summary>
    /// 이벤트 선택지 타입을 정의합니다.
    /// </summary>
    public enum ChoiceType
    {
        None,           // 없음
        Heal,           // 회복
        Gold,           // 골드 획득
        Item,           // 아이템 획득
        Relic,          // 유물 획득
        Buff,           // 버프
        Debuff,         // 디버프
        Battle,         // 강제 전투
        Risk            // 도박 (50/50)
    }

    /// <summary>
    /// 이벤트 선택지 데이터를 정의하는 클래스입니다.
    /// </summary>
    [System.Serializable]
    public class EventChoice
    {
        [Header("선택지 정보")]
        [SerializeField, Tooltip("선택지 텍스트")]
        private string choiceText;

        [SerializeField, Tooltip("선택지 타입")]
        private ChoiceType choiceType;

        [Header("효과")]
        [SerializeField, Tooltip("회복량 (HP)")]
        private int healAmount = 0;

        [SerializeField, Tooltip("획득 골드")]
        private int goldAmount = 0;

        [SerializeField, Tooltip("획득 아이템 ID")]
        private string itemID = "";

        [SerializeField, Tooltip("획득 유물 ID")]
        private string relicID = "";

        [SerializeField, Tooltip("버프/디버프 설명")]
        private string buffDescription = "";

        [Header("확률")]
        [SerializeField, Range(0f, 1f), Tooltip("성공 확률 (1.0이면 항상 성공)")]
        private float successChance = 1.0f;

        [SerializeField, Tooltip("실패 시 효과 설명")]
        private string failureEffect = "";

        // 속성 접근자
        public string ChoiceText => choiceText;
        public ChoiceType ChoiceType => choiceType;
        public int HealAmount => healAmount;
        public int GoldAmount => goldAmount;
        public string ItemID => itemID;
        public string RelicID => relicID;
        public string BuffDescription => buffDescription;
        public float SuccessChance => successChance;
        public string FailureEffect => failureEffect;
    }

    /// <summary>
    /// 던전 이벤트 데이터를 정의하는 ScriptableObject입니다.
    /// 랜덤 이벤트, 선택지, 보상 등을 포함합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEvent", menuName = "Dungeon Log/Event Data")]
    public class EventData : BaseData, IDataLoadable
    {
        [Header("이벤트 정보")]
        [SerializeField, Tooltip("방 타입")]
        private RoomType roomType;

        [SerializeField, Tooltip("출현 가능한 층")]
        private int minFloor = 1;

        [SerializeField, Tooltip("출현 가능한 최대 층")]
        private int maxFloor = 4;

        [SerializeField, Tooltip("출현 확률 가중치")]
        private int spawnWeight = 10;

        [Header("시각 정보")]
        [SerializeField, Tooltip("이벤트 이미지")]
        private Sprite eventImage;

        [Header("선택지")]
        [SerializeField, Tooltip("이벤트 선택지 목록")]
        private List<EventChoice> choices;

        [Header("추가 정보")]
        [SerializeField, TextArea(2, 3), Tooltip("이벤트 플레이버 텍스트")]
        private string flavorText;

        // 속성 접근자
        public RoomType RoomType => roomType;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;
        public int SpawnWeight => spawnWeight;
        public Sprite EventImage => eventImage;
        public List<EventChoice> Choices => choices;
        public string FlavorText => flavorText;

        /// <summary>
        /// 데이터 유효성을 검증합니다.
        /// </summary>
        public override bool Validate()
        {
            if (!base.Validate())
                return false;

            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning($"[EventData] {ID}: 최소 1개의 선택지가 필요합니다.");
                return false;
            }

            if (minFloor < 1 || maxFloor > 4 || minFloor > maxFloor)
            {
                Debug.LogWarning($"[EventData] {ID}: 층 설정이 유효하지 않습니다 (1 ~ 4).");
                return false;
            }

            if (spawnWeight <= 0)
            {
                Debug.LogWarning($"[EventData] {ID}: 출현 가중치는 0보다 커야 합니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// CSV 데이터를 로드합니다.
        /// </summary>
        public void LoadFromCSV(System.Collections.Generic.Dictionary<string, string> csvData)
        {
            // Choices 필드 초기화 (NullReferenceException 방지)
            if (choices == null)
            {
                choices = new List<EventChoice>();
            }

            // BaseData의 SetID() 사용 (리플렉션 제거)
            if (csvData.ContainsKey("ID"))
                SetID(csvData["ID"]);

            if (csvData.ContainsKey("Name"))
                SetDisplayName(csvData["Name"]);

            if (csvData.ContainsKey("Description"))
                SetDescription(csvData["Description"]);

            if (csvData.ContainsKey("RoomType") && System.Enum.TryParse<RoomType>(csvData["RoomType"], out RoomType type))
                roomType = type;

            if (csvData.ContainsKey("MinFloor") && int.TryParse(csvData["MinFloor"], out int min))
                minFloor = min;

            if (csvData.ContainsKey("MaxFloor") && int.TryParse(csvData["MaxFloor"], out int max))
                maxFloor = max;

            if (csvData.ContainsKey("SpawnWeight") && int.TryParse(csvData["SpawnWeight"], out int weight))
                spawnWeight = weight;

            if (csvData.ContainsKey("FlavorText"))
                flavorText = csvData["FlavorText"];

            // 선택지는 CSV에서 직접 파싱하기 어렵으므로 에디터에서 설정
            // 또는 별도의 선택지 CSV 파일 참조
        }
    }
}
