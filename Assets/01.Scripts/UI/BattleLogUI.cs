using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonLog.UI
{
    /// <summary>
    /// 전투 로그 UI 패널입니다.
    /// BattleLogger의 로그를 시각적으로 표시합니다.
    /// Phase 5: 전투 로그 UI 연동
    /// </summary>
    public class BattleLogUI : MonoBehaviour
    {
        // ========================================================================
        // 필드
        // ========================================================================

        /// <summary>로그 텍스트 참조</summary>
        [SerializeField] private Text logText = null;

        /// <summary>스크롤 뷰 참조</summary>
        [SerializeField] private ScrollRect scrollView = null;

        /// <summary>최대 표시 라인 수</summary>
        [SerializeField] private int maxLines = 50;

        /// <summary>자동 스크롤 활성화 여부</summary>
        [SerializeField] private bool autoScroll = true;

        /// <summary>로그 색상 설정</summary>
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private Color buffColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color debuffColor = new Color(0.8f, 0.3f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color deathColor = new Color(0.5f, 0f, 0f);
        [SerializeField] private Color systemColor = Color.yellow;
        [SerializeField] private Color infoColor = Color.white;

        // ========================================================================
        // 프라이빗 필드
        // ========================================================================

        private List<string> logLines;
        private DungeonLog.Combat.BattleLogger battleLogger;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        private void Awake()
        {
            logLines = new List<string>();
        }

        private void Start()
        {
            // BattleLogger 초기화 (BattleManager에서 가져오기)
            InitializeBattleLogger();

            // 초기 로그 표시
            RefreshLogs();
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            UnsubscribeFromEvents();
        }

        // ========================================================================
        // 초기화
        // ========================================================================

        private void InitializeBattleLogger()
        {
            // BattleManager에서 BattleLogger 찾기
            var battleManager = FindObjectOfType<DungeonLog.Combat.BattleManager>();
            if (battleManager != null && battleManager.BattleLogger != null)
            {
                battleLogger = battleManager.BattleLogger;
                Debug.Log("[BattleLogUI] BattleManager의 BattleLogger 참조 완료");
            }
            else
            {
                Debug.LogWarning("[BattleLogUI] BattleManager나 BattleLogger를 찾을 수 없습니다.");
                // Fallback: 새로 생성 (테스트용)
                if (battleLogger == null)
                {
                    battleLogger = new DungeonLog.Combat.BattleLogger();
                    Debug.Log("[BattleLogUI] 새 BattleLogger 인스턴스 생성");
                }
            }

            // 이벤트 구독
            SubscribeToEvents();
        }

        // ========================================================================
        // 이벤트 구독
        // ========================================================================

        private void SubscribeToEvents()
        {
            if (battleLogger != null)
            {
                battleLogger.OnLogEntryAdded += HandleLogEntryAdded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (battleLogger != null)
            {
                battleLogger.OnLogEntryAdded -= HandleLogEntryAdded;
            }
        }

        /// <summary>
        /// 새 로그 엔트리 추가 핸들러.
        /// </summary>
        private void HandleLogEntryAdded(DungeonLog.Combat.BattleLogEntry entry)
        {
            string logLine = FormatLogEntry(entry);

            // 중복 방지
            if (logLines.Count == 0 || logLines[logLines.Count - 1] != logLine)
            {
                logLines.Add(logLine);

                // 최대 라인 수 제한
                while (logLines.Count > maxLines)
                {
                    logLines.RemoveAt(0);
                }

                // UI 갱신
                UpdateDisplay();
            }
        }

        // ========================================================================
        // 로그 갱신
        // ========================================================================

        /// <summary>
        /// 로그를 갱신합니다 (초기화 시 호출).
        /// </summary>
        public void RefreshLogs()
        {
            if (battleLogger == null || logText == null) return;

            var entries = battleLogger.GetAllLogs();

            // 기존 로그를 모두 제거하고 다시 표시
            logLines.Clear();

            foreach (var entry in entries)
            {
                string logLine = FormatLogEntry(entry);
                logLines.Add(logLine);
            }

            // 최대 라인 수 제한
            while (logLines.Count > maxLines)
            {
                logLines.RemoveAt(0);
            }

            // UI 갱신
            UpdateDisplay();
        }

        /// <summary>
        /// 로그 엔트리를 포맷팅합니다.
        /// </summary>
        private string FormatLogEntry(DungeonLog.Combat.BattleLogEntry entry)
        {
            string color = GetColorString(entry.Type);
            string message = entry.Message;

            if (entry.Value != 0)
            {
                message += $" ({entry.Value})";
            }

            return $"<color={color}>{message}</color>";
        }

        /// <summary>
        /// 로그 타입에 따른 색상 문자열을 반환합니다.
        /// </summary>
        private string GetColorString(DungeonLog.Combat.LogType logType)
        {
            switch (logType)
            {
                case DungeonLog.Combat.LogType.Damage:
                    return ColorToHex(damageColor);
                case DungeonLog.Combat.LogType.Heal:
                    return ColorToHex(healColor);
                case DungeonLog.Combat.LogType.Buff:
                    return ColorToHex(buffColor);
                case DungeonLog.Combat.LogType.Debuff:
                    return ColorToHex(debuffColor);
                case DungeonLog.Combat.LogType.Critical:
                    return ColorToHex(criticalColor);
                case DungeonLog.Combat.LogType.Death:
                    return ColorToHex(deathColor);
                case DungeonLog.Combat.LogType.System:
                    return ColorToHex(systemColor);
                case DungeonLog.Combat.LogType.Info:
                default:
                    return ColorToHex(infoColor);
            }
        }

        /// <summary>
        /// Color를 16진수 문자열로 변환합니다.
        /// </summary>
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        /// <summary>
        /// UI를 갱신합니다.
        /// </summary>
        private void UpdateDisplay()
        {
            if (logText == null) return;

            logText.text = string.Join("\n", logLines);

            // 자동 스크롤
            if (autoScroll && scrollView != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollView.verticalNormalizedPosition = 0f;
            }
        }

        // ========================================================================
        // 퍼블릭 메서드
        // ========================================================================

        /// <summary>
        /// 로그를 모두 지웁니다.
        /// </summary>
        public void ClearLogs()
        {
            logLines.Clear();
            battleLogger?.ClearLogs();
            UpdateDisplay();
        }

        /// <summary>
        /// 로거를 직접 설정합니다 (외부에서 주입시 사용).
        /// </summary>
        public void SetBattleLogger(DungeonLog.Combat.BattleLogger logger)
        {
            battleLogger = logger;
            RefreshLogs();
        }
    }
}
