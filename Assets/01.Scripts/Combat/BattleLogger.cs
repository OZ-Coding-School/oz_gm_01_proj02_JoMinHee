using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 전투 로그 엔트리 타입입니다.
    /// </summary>
    public enum LogType
    {
        /// <summary>일반 정보</summary>
        Info,

        /// <summary>데미지</summary>
        Damage,

        /// <summary>힐</summary>
        Heal,

        /// <summary>버프</summary>
        Buff,

        /// <summary>디버프</summary>
        Debuff,

        /// <summary>치명타</summary>
        Critical,

        /// <summary>사망</summary>
        Death,

        /// <summary>시스템</summary>
        System
    }

    /// <summary>
    /// 전투 로그 엔트리 클래스입니다.
    /// </summary>
    public class BattleLogEntry
    {
        /// <summary>로그 타입</summary>
        public LogType Type { get; set; }

        /// <summary>메시지</summary>
        public string Message { get; set; }

        /// <summary>타임스탬프</summary>
        public float Timestamp { get; set; }

        /// <summary>관련 캐릭터 (선택적)</summary>
        public DungeonLog.Character.Character RelatedCharacter { get; set; }

        /// <summary>관련 캐릭터 리스트 (선택적)</summary>
        public List<DungeonLog.Character.Character> RelatedCharacters { get; set; }

        /// <summary>수치 데이터 (데미지, 힐량 등)</summary>
        public int Value { get; set; }

        public BattleLogEntry()
        {
            RelatedCharacters = new List<DungeonLog.Character.Character>();
            Timestamp = Time.time;
        }

        public override string ToString()
        {
            string prefix = $"[{Timestamp:F2}] [{Type}]";
            string charInfo = RelatedCharacter != null ? $" ({RelatedCharacter.CharacterName})" : "";
            string valueInfo = Value != 0 ? $" {Value}" : "";
            return $"{prefix}{charInfo}: {Message}{valueInfo}";
        }
    }

    /// <summary>
    /// 전투 로그를 관리하는 클래스입니다.
    /// 전투 중 발생하는 모든 이벤트를 기록하고 디버깅 및 UI 표시를 지원합니다.
    /// </summary>
    public class BattleLogger
    {
        // ========================================================================
        // 필드
        // ========================================================================

        /// <summary>로그 엔트리 리스트</summary>
        private List<BattleLogEntry> logEntries;

        /// <summary>최대 로그 개수</summary>
        private const int MAX_LOG_ENTRIES = 100;

        /// <summary>로그 활성화 여부</summary>
        private bool isEnabled;

        // ========================================================================
        // 이벤트
        // ========================================================================

        /// <summary>
        /// 새로운 로그 엔트리가 추가되었을 때 발생합니다.
        /// </summary>
        public event Action<BattleLogEntry> OnLogEntryAdded;

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        /// <summary>로그 엔트리 리스트 (읽기 전용)</summary>
        public IReadOnlyList<BattleLogEntry> LogEntries => logEntries.AsReadOnly();

        /// <summary>로그 활성화 여부</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        // ========================================================================
        // 생성자
        // ========================================================================

        public BattleLogger()
        {
            logEntries = new List<BattleLogEntry>();
            isEnabled = true;

            // 이벤트 구독
            SubscribeToEvents();
        }

        // ========================================================================
        // 이벤트 구독
        // ========================================================================

        /// <summary>
        /// 전투 관련 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeToEvents()
        {
            // 캐릭터 이벤트 구독
            DungeonLog.Character.CharacterEvents.OnDamaged += OnCharacterDamaged;
            DungeonLog.Character.CharacterEvents.OnHealed += OnCharacterHealed;
            DungeonLog.Character.CharacterEvents.OnCharacterDeath += OnCharacterDied;

            // 전투 이벤트 구독
            BattleEvents.OnBattleStateChanged += OnBattleStateChanged;
            BattleEvents.OnTurnChanged += OnTurnChanged;
            BattleEvents.OnAPChanged += OnAPChanged;
            BattleEvents.OnSkillDrawn += OnSkillDrawn;
            BattleEvents.OnSkillRerolled += OnSkillRerolled;
            BattleEvents.OnSkillAttempt += OnSkillAttempt;
            BattleEvents.OnBattleEnded += OnBattleEnded;
        }

        /// <summary>
        /// 이벤트 구독을 해제합니다.
        /// </summary>
        public void UnsubscribeFromEvents()
        {
            // 캐릭터 이벤트 해제
            DungeonLog.Character.CharacterEvents.OnDamaged -= OnCharacterDamaged;
            DungeonLog.Character.CharacterEvents.OnHealed -= OnCharacterHealed;
            DungeonLog.Character.CharacterEvents.OnCharacterDeath -= OnCharacterDied;

            // 전투 이벤트 해제
            BattleEvents.OnBattleStateChanged -= OnBattleStateChanged;
            BattleEvents.OnTurnChanged -= OnTurnChanged;
            BattleEvents.OnAPChanged -= OnAPChanged;
            BattleEvents.OnSkillDrawn -= OnSkillDrawn;
            BattleEvents.OnSkillRerolled -= OnSkillRerolled;
            BattleEvents.OnSkillAttempt -= OnSkillAttempt;
            BattleEvents.OnBattleEnded -= OnBattleEnded;
        }

        // ========================================================================
        // 로그 기록 메서드
        // ========================================================================

        /// <summary>
        /// 일반 로그를 기록합니다.
        /// </summary>
        public void LogInfo(string message, DungeonLog.Character.Character character = null)
        {
            AddLogEntry(LogType.Info, message, character);
        }

        /// <summary>
        /// 데미지 로그를 기록합니다.
        /// </summary>
        public void LogDamage(DungeonLog.Character.Character target, int damage, DungeonLog.Character.Character attacker = null)
        {
            string message = attacker != null
                ? $"{attacker.CharacterName}이(가) {target.CharacterName}에게 {damage} 데미지!"
                : $"{target.CharacterName}이(가) {damage} 데미지를 받았습니다!";

            var entry = new BattleLogEntry
            {
                Type = LogType.Damage,
                Message = message,
                RelatedCharacter = target,
                Value = damage
            };

            AddLogEntry(entry);
        }

        /// <summary>
        /// 힐 로그를 기록합니다.
        /// </summary>
        public void LogHeal(DungeonLog.Character.Character target, int healAmount)
        {
            string message = $"{target.CharacterName}이(가) {healAmount}만큼 회복!";

            var entry = new BattleLogEntry
            {
                Type = LogType.Heal,
                Message = message,
                RelatedCharacter = target,
                Value = healAmount
            };

            AddLogEntry(entry);
        }

        /// <summary>
        /// 시스템 로그를 기록합니다.
        /// </summary>
        public void LogSystem(string message)
        {
            AddLogEntry(LogType.System, message);
        }

        /// <summary>
        /// 커스텀 로그 엔트리를 추가합니다.
        /// </summary>
        private void AddLogEntry(LogType type, string message, DungeonLog.Character.Character character = null)
        {
            var entry = new BattleLogEntry
            {
                Type = type,
                Message = message,
                RelatedCharacter = character
            };

            AddLogEntry(entry);
        }

        /// <summary>
        /// 로그 엔트리를 추가합니다.
        /// </summary>
        private void AddLogEntry(BattleLogEntry entry)
        {
            if (!isEnabled) return;

            logEntries.Add(entry);

            // 최대 로그 개수 제한
            if (logEntries.Count > MAX_LOG_ENTRIES)
            {
                logEntries.RemoveAt(0);
            }

            // Unity Console에도 출력
            Debug.Log($"[BattleLogger] {entry}");

            // 이벤트 발생
            OnLogEntryAdded?.Invoke(entry);
        }

        // ========================================================================
        // 로그 조회 메서드
        // ========================================================================

        /// <summary>
        /// 모든 로그를 반환합니다.
        /// </summary>
        public List<BattleLogEntry> GetAllLogs()
        {
            return new List<BattleLogEntry>(logEntries);
        }

        /// <summary>
        /// 특정 타입의 로그만 필터링하여 반환합니다.
        /// </summary>
        public List<BattleLogEntry> GetLogsByType(LogType type)
        {
            return logEntries.FindAll(log => log.Type == type);
        }

        /// <summary>
        /// 최근 N개의 로그를 반환합니다.
        /// </summary>
        public List<BattleLogEntry> GetRecentLogs(int count)
        {
            int startIndex = Mathf.Max(0, logEntries.Count - count);
            return logEntries.GetRange(startIndex, logEntries.Count - startIndex);
        }

        /// <summary>
        /// 특정 캐릭터와 관련된 로그를 반환합니다.
        /// </summary>
        public List<BattleLogEntry> GetLogsForCharacter(DungeonLog.Character.Character character)
        {
            return logEntries.FindAll(log =>
                log.RelatedCharacter == character ||
                (log.RelatedCharacters != null && log.RelatedCharacters.Contains(character))
            );
        }

        // ========================================================================
        // 로그 초기화
        // ========================================================================

        /// <summary>
        /// 모든 로그를 초기화합니다.
        /// </summary>
        public void ClearLogs()
        {
            logEntries.Clear();
            LogSystem("로그가 초기화되었습니다.");
        }

        // ========================================================================
        // 로그 내보내기
        // ========================================================================

        /// <summary>
        /// 모든 로그를 문자열로 반환합니다.
        /// </summary>
        public string ExportLogsToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 전투 로그 ===");
            sb.AppendLine($"총 {logEntries.Count}개의 엔트리");
            sb.AppendLine();

            foreach (var entry in logEntries)
            {
                sb.AppendLine(entry.ToString());
            }

            return sb.ToString();
        }

        // ========================================================================
        // 이벤트 핸들러
        // ========================================================================

        private void OnCharacterDamaged(GameObject character, int damage, bool isCritical)
        {
            // GameObject에서 Character 컴포넌트 찾기
            var charComponent = character.GetComponent<DungeonLog.Character.Character>();
            if (charComponent != null)
            {
                LogDamage(charComponent, damage, null);
            }
        }

        private void OnCharacterHealed(GameObject character, int healAmount)
        {
            // GameObject에서 Character 컴포넌트 찾기
            var charComponent = character.GetComponent<DungeonLog.Character.Character>();
            if (charComponent != null)
            {
                LogHeal(charComponent, healAmount);
            }
        }

        private void OnCharacterDied(GameObject character)
        {
            // GameObject에서 Character 컴포넌트 찾기
            var charComponent = character.GetComponent<DungeonLog.Character.Character>();
            if (charComponent != null)
            {
                var entry = new BattleLogEntry
                {
                    Type = LogType.Death,
                    Message = $"{charComponent.CharacterName}이(가) 사망했습니다!",
                    RelatedCharacter = charComponent
                };
                AddLogEntry(entry);
            }
        }

        private void OnBattleStateChanged(BattleState newState)
        {
            LogSystem($"전투 상태 변경: {newState}");
        }

        private void OnTurnChanged(int turn)
        {
            LogSystem($"턴 {turn} 시작");
        }

        private void OnAPChanged(int current, int max)
        {
            // AP 변경은 너무 빈번하므로 기본적으로 로그하지 않음
            // 필요시 주석 해제
            // LogSystem($"AP 변경: {current}/{max}");
        }

        private void OnSkillDrawn(DungeonLog.Character.Character character)
        {
            LogInfo($"{character.CharacterName}이(가) 스킬을 추첨했습니다.", character);
        }

        private void OnSkillRerolled(DungeonLog.Character.Character character)
        {
            LogInfo($"{character.CharacterName}이(가) 스킬을 리롤했습니다.", character);
        }

        private void OnSkillAttempt(DungeonLog.Character.Character character, DungeonLog.Data.SkillData skill)
        {
            LogInfo($"{character.CharacterName}이(가) {skill.DisplayName} 사용 시도", character);
        }

        private void OnBattleEnded(bool victory)
        {
            string result = victory ? "승리!" : "패배!";
            var entry = new BattleLogEntry
            {
                Type = LogType.System,
                Message = $"전투 종료 - {result}"
            };
            AddLogEntry(entry);
        }
    }
}
