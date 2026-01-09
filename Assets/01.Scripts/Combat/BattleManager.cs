using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DungeonLog.StatusEffects;
using DungeonLog.AI;
using DungeonLog.AI.Core;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 전투 매니저 싱글톤 클래스입니다.
    /// 턴제 전투의 전체 흐름을 제어하고 상태를 관리합니다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        // ========================================================================
        // 싱글톤 패턴
        // ========================================================================

        private static BattleManager _instance;
        public static BattleManager Instance => _instance;

        // ========================================================================
        // 필드
        // ========================================================================

        /// <summary>현재 전투 상태</summary>
        private BattleState currentState;

        /// <summary>플레이어 파티 캐릭터 리스트</summary>
        private List<DungeonLog.Character.Character> players;

        /// <summary>적 캐릭터 리스트</summary>
        private List<DungeonLog.Character.Character> enemies;

        /// <summary>현재 턴 수 (1부터 시작)</summary>
        private int currentTurn;

        /// <summary>AP 시스템</summary>
        private APSystem apSystem;

        /// <summary>전투 진행 중 여부</summary>
        private bool isBattleActive;

        /// <summary>스킬 실행기</summary>
        private SkillExecutor skillExecutor;

        /// <summary>현재 실행 중인 코루틴</summary>
        private Coroutine currentTurnCoroutine;

        /// <summary>전투 로거</summary>
        private BattleLogger battleLogger;

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        /// <summary>현재 전투 상태 (읽기 전용)</summary>
        public BattleState CurrentState => currentState;

        /// <summary>현재 턴 수 (읽기 전용)</summary>
        public int CurrentTurn => currentTurn;

        /// <summary>AP 시스템 (읽기 전용)</summary>
        public APSystem APSystem => apSystem;

        /// <summary>플레이어 파티 (읽기 전용)</summary>
        public IReadOnlyList<DungeonLog.Character.Character> Players => players.AsReadOnly();

        /// <summary>적 리스트 (읽기 전용)</summary>
        public IReadOnlyList<DungeonLog.Character.Character> Enemies => enemies.AsReadOnly();

        /// <summary>전투 진행 중 여부</summary>
        public bool IsBattleActive => isBattleActive;

        /// <summary>전투 로거 (읽기 전용)</summary>
        public BattleLogger BattleLogger => battleLogger;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        private void Awake()
        {
            // 싱글톤 초기화
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Debug.LogWarning("[BattleManager] BattleManager 인스턴스가 이미 존재합니다. 중복 생성을 방지합니다.");
                Destroy(gameObject);
                return;
            }

            // 시스템 초기화
            apSystem = new APSystem();
            skillExecutor = new SkillExecutor(this);
            battleLogger = new BattleLogger();
            currentState = BattleState.NotStarted;
            isBattleActive = false;

            Debug.Log("[BattleManager] BattleManager 초기화 완료");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;

                // 진행 중인 코루틴 정지
                if (currentTurnCoroutine != null)
                {
                    StopCoroutine(currentTurnCoroutine);
                    currentTurnCoroutine = null;
                }

                // 전투 로거 이벤트 구독 해제
                battleLogger?.UnsubscribeFromEvents();
            }
        }

        // ========================================================================
        // 전투 제어
        // ========================================================================

        /// <summary>
        /// 전투를 시작합니다.
        /// </summary>
        /// <param name="playerParty">플레이어 파티 캐릭터 리스트</param>
        /// <param name="enemyParty">적 캐릭터 리스트</param>
        public void StartBattle(List<DungeonLog.Character.Character> playerParty, List<DungeonLog.Character.Character> enemyParty)
        {
            if (isBattleActive)
            {
                Debug.LogWarning("[BattleManager] 이미 전투가 진행 중입니다.");
                return;
            }

            // 캐릭터 리스트 초기화
            players = new List<DungeonLog.Character.Character>(playerParty ?? Enumerable.Empty<DungeonLog.Character.Character>());
            enemies = new List<DungeonLog.Character.Character>(enemyParty ?? Enumerable.Empty<DungeonLog.Character.Character>());

            if (players.Count == 0)
            {
                Debug.LogError("[BattleManager] 플레이어 파티가 비어있습니다!");
                return;
            }

            if (enemies.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[BattleManager] 적 파티가 비어있습니다! 테스트 모드로 진행합니다.");
#endif
                // 테스트 모드: 적 없이 진행 (EnemyData ScriptableObject 생성 전까지)
            }

            // 전투 초기화
            currentTurn = 1;
            isBattleActive = true;

            // 초기 상태로 전환
            ChangeState(BattleState.PlayerTurn);

            Debug.Log($"[BattleManager] 전투 시작! 플레이어: {players.Count}인, 적: {enemies.Count}인");
        }

        /// <summary>
        /// 전투를 종료합니다.
        /// </summary>
        /// <param name="result">전투 결과</param>
        public void EndBattle(BattleResult result)
        {
            if (!isBattleActive)
            {
                Debug.LogWarning("[BattleManager] 전투가 진행 중이 아닙니다.");
                return;
            }

            isBattleActive = false;

            // 전투 종료 상태로 전환
            ChangeState(BattleState.BattleEnd);

            // 전투 종료 이벤트 발생
            bool isVictory = result == BattleResult.Victory;
            BattleEvents.NotifyBattleEnded(isVictory);

            Debug.Log($"[BattleManager] 전투 종료! 결과: {result}, 총 턴: {currentTurn}");
        }

        /// <summary>
        /// 전투 상태를 변경합니다.
        /// </summary>
        /// <param name="newState">새로운 상태</param>
        private void ChangeState(BattleState newState)
        {
            if (currentState == newState)
            {
                Debug.LogWarning($"[BattleManager] 이미 {newState} 상태입니다.");
                return;
            }

            BattleState previousState = currentState;
            currentState = newState;

            Debug.Log($"[BattleManager] 상태 변경: {previousState} → {newState}");

            // 상태 변경 이벤트 발생
            BattleEvents.NotifyBattleStateChanged(newState);

            // 상태별 진입 로직
            OnEnterState(newState);
        }

        // ========================================================================
        // 상태별 진입 로직
        // ========================================================================

        /// <summary>
        /// 상태 진입 시 실행되는 로직입니다.
        /// </summary>
        /// <param name="state">진입할 상태</param>
        private void OnEnterState(BattleState state)
        {
            switch (state)
            {
                case BattleState.NotStarted:
                    // 전투 시작 전 상태
                    break;

                case BattleState.PlayerTurn:
                    OnPlayerTurnStart();
                    break;

                case BattleState.PlayerSkillReroll:
                    // 플레이어 스킬 리롤 상태
                    break;

                case BattleState.EnemyTurn:
                    OnEnemyTurnStart();
                    break;

                case BattleState.Resolving:
                    // 스킬 해석 중 상태
                    break;

                case BattleState.BattleEnd:
                    // 전투 종료 상태
                    break;

                default:
                    Debug.LogWarning($"[BattleManager] 알 수 없는 상태: {state}");
                    break;
            }
        }

        // ========================================================================
        // 턴 진행 로직
        // ========================================================================

        /// <summary>
        /// 플레이어 턴 시작 시 호출됩니다.
        /// </summary>
        private void OnPlayerTurnStart()
        {
            // Phase 6: 상태 이상 턴 시작 처리
            ProcessStatusEffectsTurnStart(players);

            // AP 초기화
            int alivePlayerCount = players.Count(p => !p.IsDead);
            apSystem.ResetAPForTurn(alivePlayerCount);

            // 턴 변경 이벤트 발생
            BattleEvents.NotifyTurnChanged(currentTurn);

            Debug.Log($"[BattleManager] 플레이어 턴 시작! (턴 {currentTurn}, AP: {apSystem.CurrentAP}/{apSystem.MaxAP})");

            // 플레이어 턴 코루틴 시작
            if (currentTurnCoroutine != null)
            {
                StopCoroutine(currentTurnCoroutine);
                currentTurnCoroutine = null;
            }
            currentTurnCoroutine = StartCoroutine(PlayerTurnCoroutine());
        }

        /// <summary>
        /// 적 턴 시작 시 호출됩니다.
        /// </summary>
        private void OnEnemyTurnStart()
        {
            // Phase 6: 상태 이상 턴 시작 처리
            ProcessStatusEffectsTurnStart(enemies);

            Debug.Log("[BattleManager] 적 턴 시작!");

            // 적 턴 코루틴 시작
            if (currentTurnCoroutine != null)
            {
                StopCoroutine(currentTurnCoroutine);
                currentTurnCoroutine = null;
            }
            currentTurnCoroutine = StartCoroutine(EnemyTurnCoroutine());
        }

        // ========================================================================
        // 턴 진행 코루틴
        // ========================================================================

        /// <summary>
        /// 플레이어 턴 코루틴입니다.
        /// 플레이어 입력을 대기하며, AP가 모두 소모되면 턴을 종료합니다.
        /// </summary>
        private System.Collections.IEnumerator PlayerTurnCoroutine()
        {
            Debug.Log("[BattleManager] 플레이어 턴 코루틴 시작");

            // 플레이어 턴 동안 입력 대기
            while (currentState == BattleState.PlayerTurn)
            {
                // 전투 종료 조건 확인
                if (CheckBattleEnd())
                {
                    yield break;
                }

                // AP가 모두 소모되면 자동으로 턴 종료
                if (apSystem.CurrentAP <= 0)
                {
                    Debug.Log("[BattleManager] AP 소진으로 플레이어 턴 자동 종료");
                    EndTurn();
                    yield break;
                }

                // 매 프레임 대기
                yield return null;
            }

            Debug.Log("[BattleManager] 플레이어 턴 코루틴 종료");
        }

        /// <summary>
        /// 적 턴 코루틴입니다.
        /// 모든 적이 행동을 완료하면 턴을 종료합니다.
        /// </summary>
        private System.Collections.IEnumerator EnemyTurnCoroutine()
        {
            Debug.Log("[BattleManager] 적 턴 코루틴 시작");

            // 생존한 적 리스트
            var aliveEnemies = enemies.Where(e => !e.IsDead).ToList();

            if (aliveEnemies.Count == 0)
            {
                Debug.LogWarning("[BattleManager] 생존한 적이 없습니다.");
                EndTurn();
                yield break;
            }

            // 각 적의 턴 진행
            foreach (var enemy in aliveEnemies)
            {
                // 전투 종료 조건 확인
                if (CheckBattleEnd())
                {
                    yield break;
                }

                // 적 행동 실행
                yield return StartCoroutine(EnemyActionCoroutine(enemy));

                // 약간의 딜레이 (시각적 피드백용)
                yield return new WaitForSeconds(0.5f);
            }

            // 모든 적 행동 완료 후 턴 종료
            Debug.Log("[BattleManager] 모든 적 행동 완료, 턴 종료");
            EndTurn();

            Debug.Log("[BattleManager] 적 턴 코루틴 종료");
        }

        /// <summary>
        /// 개별 적의 행동 코루틴입니다.
        /// AI 시스템을 통해 스킬과 타겟을 결정하고 실행
        /// AIAction은 항상 Pool로 반환됩니다 (Memory Leak 방지).
        /// </summary>
        /// <param name="enemy">행동할 적 캐릭터</param>
        private System.Collections.IEnumerator EnemyActionCoroutine(DungeonLog.Character.Character enemy)
        {
            Debug.Log($"[BattleManager] {enemy.CharacterName}의 행동 시작");

            // AI 컨트롤러 가져오기
            AIController aiController = enemy.GetComponent<AIController>();

            if (aiController == null)
            {
                Debug.LogWarning($"[BattleManager] {enemy.CharacterName}에 AIController가 없습니다. 기본 동작을 사용합니다.");
                yield break;
            }

            // AI 의사결정 요청
            List<DungeonLog.Character.Character> aliveEnemies = enemies.Where(e => !e.IsDead).ToList();
            List<DungeonLog.Character.Character> alivePlayers = players.Where(p => !p.IsDead).ToList();

            if (alivePlayers.Count == 0)
            {
                Debug.LogWarning("[BattleManager] 공격할 플레이어가 없습니다.");
                yield break;
            }

            AIAction action = aiController.DecideAction(aliveEnemies, alivePlayers, currentTurn, 1.0f);

            try
            {
                if (action == null || !action.IsValid)
                {
                    Debug.LogWarning($"[BattleManager] {enemy.CharacterName}의 AI 결정이 유효하지 않습니다.");
                    yield break;
                }

                Debug.Log($"[BattleManager] {enemy.CharacterName}의 AI 결정: {action.Reason}");

                // 스킬 실행
                var result = skillExecutor.ExecuteSkill(action.Actor, action.Skill, action.Targets);

                if (result.IsSuccess)
                {
                    Debug.Log($"[BattleManager] {enemy.CharacterName}이(가) {action.Skill.DisplayName} 사용!");
                }
                else
                {
                    Debug.LogWarning($"[BattleManager] {enemy.CharacterName} 스킬 실행 실패: {result.ErrorMessage}");
                }

                // 행동 딜레이
                yield return new WaitForSeconds(1.0f);

                Debug.Log($"[BattleManager] {enemy.CharacterName}의 행동 종료");
            }
            finally
            {
                // AIAction을 Pool로 반환 (Memory Leak 방지)
                if (action != null)
                {
                    AI.Evaluators.AIDecisionEvaluator.ReturnAction(action);
                }
            }
        }

        /// <summary>
        /// 턴을 종료하고 다음 턴으로 넘어갑니다.
        /// </summary>
        public void EndTurn()
        {
            if (!isBattleActive)
            {
                Debug.LogWarning("[BattleManager] 전투가 진행 중이 아닙니다.");
                return;
            }

            Debug.Log($"[BattleManager] 턴 종료 (턴 {currentTurn})");

            // Phase 6: 상태 이상 턴 종료 처리
            ProcessStatusEffectsTurnEnd();

            // 전투 종료 조건 확인
            if (CheckBattleEnd())
            {
                return;
            }

            // 턴 교체
            if (currentState == BattleState.PlayerTurn)
            {
                ChangeState(BattleState.EnemyTurn);
            }
            else if (currentState == BattleState.EnemyTurn)
            {
                currentTurn++;
                ChangeState(BattleState.PlayerTurn);
            }
        }

        // ========================================================================
        // 전투 종료 판정
        // ========================================================================

        /// <summary>
        /// 전투 종료 조건을 확인합니다.
        /// </summary>
        /// <returns>전투 종료 여부</returns>
        private bool CheckBattleEnd()
        {
            bool allEnemiesDead = enemies.All(e => e.IsDead);
            bool allPlayersDead = players.All(p => p.IsDead);

            // 동시 사망 확인 (무승부) - 먼저 체크
            if (allEnemiesDead && allPlayersDead)
            {
                EndBattle(BattleResult.Draw);
                return true;
            }

            // 적 전원 처치 (승리)
            if (allEnemiesDead)
            {
                EndBattle(BattleResult.Victory);
                return true;
            }

            // 아군 전원 사망 (패배)
            if (allPlayersDead)
            {
                EndBattle(BattleResult.Defeat);
                return true;
            }

            return false;
        }

        // ========================================================================
        // 플레이어 행동 지원
        // ========================================================================

        /// <summary>
        /// 스킬 사용을 시도합니다.
        /// </summary>
        /// <param name="character">스킬을 사용하는 캐릭터</param>
        /// <param name="skill">사용할 스킬</param>
        /// <param name="targets">대상 캐릭터 리스트</param>
        /// <returns>사용 성공 여부</returns>
        public bool TryUseSkill(DungeonLog.Character.Character character, DungeonLog.Data.SkillData skill, List<DungeonLog.Character.Character> targets)
        {
            if (!isBattleActive)
            {
                Debug.LogWarning("[BattleManager] 전투가 진행 중이 아닙니다.");
                return false;
            }

            if (currentState != BattleState.PlayerTurn)
            {
                Debug.LogWarning($"[BattleManager] 현재 스킬 사용 불가 상태: {currentState}");
                return false;
            }

            // 타겟 유효성 검증
            if (targets == null || targets.Count == 0)
            {
                Debug.LogWarning("[BattleManager] 스킬 대상이 없습니다.");
                return false;
            }

            // AP 확인
            int apCost = skill != null ? skill.APCost : 0;
            if (!apSystem.HasEnoughAP(apCost))
            {
                Debug.LogWarning($"[BattleManager] AP 부족: 필요 {apCost}, 현재 {apSystem.CurrentAP}");
                return false;
            }

            // 스킬 사용 시도 이벤트 발생
            BattleEvents.NotifySkillAttempt(character, skill);

            Debug.Log($"[BattleManager] {character.CharacterName}가 {skill?.DisplayName} 사용 시도 (AP: {apCost})");

            // 실제 스킬 실행
            var result = skillExecutor.ExecuteSkill(character, skill, targets);

            if (!result.IsSuccess)
            {
                Debug.LogWarning($"[BattleManager] 스킬 실행 실패: {result.ErrorMessage}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 스킬 리롤을 요청합니다.
        /// Phase 5: SkillManager와 연동 완료
        /// </summary>
        /// <param name="character">리롤할 캐릭터</param>
        /// <returns>리롤 성공 여부</returns>
        public bool TryRerollSkill(DungeonLog.Character.Character character)
        {
            if (!isBattleActive)
            {
                Debug.LogWarning("[BattleManager] 전투가 진행 중이 아닙니다.");
                return false;
            }

            if (currentState != BattleState.PlayerTurn)
            {
                Debug.LogWarning($"[BattleManager] 현재 리롤 불가 상태: {currentState}");
                return false;
            }

            if (character == null)
            {
                Debug.LogWarning("[BattleManager] 캐릭터가 null입니다.");
                return false;
            }

            // 리롤 가능 여부 확인
            if (!character.SkillManager.CanReroll())
            {
                Debug.LogWarning($"[BattleManager] 리롤 횟수 초과: {character.CharacterName}");
                return false;
            }

            // 리롤 실행
            bool success = character.SkillManager.RerollSkills();

            if (success)
            {
                // 이벤트 발생
                BattleEvents.NotifySkillRerolled(character);
                Debug.Log($"[BattleManager] {character.CharacterName} 스킬 리롤 성공");
            }

            return success;
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        /// <summary>
        /// 생존한 플레이어 수를 반환합니다.
        /// </summary>
        public int GetAlivePlayerCount()
        {
            return players.Count(p => !p.IsDead);
        }

        /// <summary>
        /// 생존한 적 수를 반환합니다.
        /// </summary>
        public int GetAliveEnemyCount()
        {
            return enemies.Count(e => !e.IsDead);
        }

        /// <summary>
        /// 모든 생존 캐릭터를 반환합니다.
        /// </summary>
        public List<DungeonLog.Character.Character> GetAllAliveCharacters()
        {
            var all = new List<DungeonLog.Character.Character>();
            all.AddRange(players.Where(p => !p.IsDead));
            all.AddRange(enemies.Where(e => !e.IsDead));
            return all;
        }

        /// <summary>
        /// 현재 전투 상태 정보를 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            if (!isBattleActive)
            {
                return $"[BattleManager] 전투 진행 중 아님 (상태: {currentState})";
            }

            return $"[BattleManager] 턴 {currentTurn}, 상태: {currentState}, AP: {apSystem?.CurrentAP}/{apSystem?.MaxAP}, " +
                   $"플레이어: {GetAlivePlayerCount()}/{players.Count}, 적: {GetAliveEnemyCount()}/{enemies.Count}";
        }

        // ========================================================================
        // Phase 6: 상태 이상 처리
        // ========================================================================

        /// <summary>
        /// 상태 이상 턴 시작 처리를 수행합니다.
        /// </summary>
        private void ProcessStatusEffectsTurnStart(List<Character.Character> characters)
        {
            foreach (var character in characters)
            {
                if (character.IsDead) continue;

                var statusManager = character.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    statusManager.ProcessTurnStart();
                }
            }
        }

        /// <summary>
        /// 상태 이상 턴 종료 처리를 수행합니다.
        /// </summary>
        private void ProcessStatusEffectsTurnEnd()
        {
            // 모든 생존 캐릭터의 상태 이상 턴 종료 처리
            var allCharacters = GetAllAliveCharacters();
            foreach (var character in allCharacters)
            {
                var statusManager = character.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    statusManager.ProcessTurnEnd();
                }
            }
        }
    }
}
