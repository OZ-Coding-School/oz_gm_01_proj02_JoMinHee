using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonLog.UI.Core;
using DungeonLog.Combat;

namespace DungeonLog.UI.Battle
{
    /// <summary>
    /// 전투 UI 메인 클래스.
    /// 전투 화면의 전체 UI를 관리하고 이벤트를 처리합니다.
    /// Phase 8: 전투 UI 시스템 - Battle UI Main
    ///
    /// 와이어프레임 기반 설계:
    /// - 세로 일자 배열 (Vertical Single-Row Layout)
    /// - 플레이어가 자유롭게 행동 순서를 조종
    /// - AP 기반 시스템 (팀 공유 4 AP)
    /// - 행동 큐 관리
    /// </summary>
    public class BattleUI : UIScreen
    {
        // ========================================================================
        // Inspector 설정 - 아군 패널
        // ========================================================================

        [Header("아군 패널")]
        [SerializeField] private Transform _playerPartyContainer = null;
        [SerializeField] private GameObject _playerSlotPrefab = null;

        [Header("적군 패널")]
        [SerializeField] private Transform _enemyPartyContainer = null;
        [SerializeField] private GameObject _enemySlotPrefab = null;

        [Header("적 슬롯 동적 크기 조절")]
        [SerializeField] private int _maxEnemiesForFullSize = 4;
        [SerializeField] private float _minSlotScale = 0.5f;
        [SerializeField] private bool _enableDynamicSlotScaling = true;
        [Tooltip("적 슬롯의 기본 높이 (4명 이하일 때 적용)")]
        [SerializeField] private float _baseSlotHeight = 100f;

        [Header("상단 정보")]
        [SerializeField] private TMP_Text _floorText = null;
        [SerializeField] private TMP_Text _battleStateText = null;
        [SerializeField] private Button _pauseButton = null;

        [Header("AP 표시")]
        [SerializeField] private TMP_Text _apText = null;
        [SerializeField] private Slider _apSlider = null;

        [Header("행동 큐")]
        [SerializeField] private Transform _actionQueueContainer = null;
        [SerializeField] private GameObject _actionQueueItemPrefab = null;
        [SerializeField] private Button _executeActionsButton = null;
        [SerializeField] private Button _cancelActionsButton = null;

        [Header("턴 컨트롤")]
        [SerializeField] private Button _rerollButton = null;
        [SerializeField] private TMP_Text _rerollCountText = null;
        [SerializeField] private Button _endTurnButton = null;

        [Header("전투 로그")]
        [SerializeField] private BattleLogUI _battleLogUI = null;

        [Header("팝업")]
        [SerializeField] private GameObject _skillSelectionPopupPrefab = null;

        [Header("스킬 뽑기 UI")]
        [SerializeField] private SkillRerollPanel _skillRerollPanel = null;

        // ========================================================================
        // 상태
        // ========================================================================

        private List<CharacterSlot> _playerSlots = new List<CharacterSlot>();
        private List<CharacterSlot> _enemySlots = new List<CharacterSlot>();
        private List<ActionQueueItem> _actionQueue = new List<ActionQueueItem>();

        // Character → CharacterSlot 매핑 (성능 최적화)
        private Dictionary<DungeonLog.Character.Character, CharacterSlot> _characterToSlotMap = new Dictionary<DungeonLog.Character.Character, CharacterSlot>();

        private BattleManager _battleManager = null;
        private APSystem _apSystem = null;

        private int _currentRerollCount = 0;
        private const int MAX_REROLL_PER_TURN = 1;

        // 스킬 선택 팝업 관리
        private SkillSelectionPopup _skillSelectionPopup = null;
        private bool _isTargetSelectionMode = false;

        // 이전 전투 상태 추적 (스킬 뽑기 UI 중복 표시 방지)
        private BattleState _previousBattleState = BattleState.NotStarted;

        // ========================================================================
        // 초기화
        // ========================================================================

        protected override void Awake()
        {
            base.Awake();

            // 버튼 이벤트 연결
            SetupButtonEvents();
        }

        protected override void OnShow()
        {
            base.OnShow();

            // BattleManager 참조 (OnShow에서 가져오기 - 초기화 순서 문제 해결)
            _battleManager = BattleManager.Instance;
            if (_battleManager == null)
            {
                Debug.LogError("[BattleUI] BattleManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            // APSystem 참조
            _apSystem = _battleManager.APSystem;
            if (_apSystem == null)
            {
                Debug.LogError("[BattleUI] APSystem을 찾을 수 없습니다.");
                return;
            }

            // 이벤트 구독
            SubscribeToEvents();

            // UI 초기화
            InitializeBattleUI();

            // 이미 상태가 변경되었을 수 있으므로 현재 상태 확인
            // (이벤트 구독 이전에 상태가 변경된 경우 처리)
            CheckCurrentStateOnShow();
        }

        /// <summary>
        /// OnShow에서 현재 BattleManager의 상태를 확인하고 처리합니다.
        /// 이벤트 구독 이전에 상태가 변경된 경우를 대응합니다.
        /// </summary>
        private void CheckCurrentStateOnShow()
        {
            if (_battleManager == null) return;

            var currentState = _battleManager.CurrentState;
            Debug.Log($"[BattleUI] OnShow에서 현재 상태 확인: {currentState}");

            switch (currentState)
            {
                case BattleState.PlayerTurn:
                    OnPlayerTurnStart();
                    break;
                case BattleState.EnemyTurn:
                    OnEnemyTurnStart();
                    break;
                case BattleState.BattleEnd:
                    // 전투 종료 - 별도 처리 필요 시
                    break;
            }
        }

        protected override void OnHide()
        {
            base.OnHide();

            // 이벤트 구독 해제
            UnsubscribeFromEvents();
        }

        private void SetupButtonEvents()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(HandlePause);
            }

            if (_executeActionsButton != null)
            {
                _executeActionsButton.onClick.AddListener(HandleExecuteActions);
            }

            if (_cancelActionsButton != null)
            {
                _cancelActionsButton.onClick.AddListener(HandleCancelActions);
            }

            if (_rerollButton != null)
            {
                _rerollButton.onClick.AddListener(HandleReroll);
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(HandleEndTurn);
            }
        }

        // ========================================================================
        // UI 초기화
        // ========================================================================

        private void InitializeBattleUI()
        {
            if (_battleManager == null) return;

            // 플레이어 슬롯 생성
            CreatePlayerSlots();

            // 적 슬롯 생성
            CreateEnemySlots();

            // AP 표시 초기화
            UpdateAPDisplay();

            // 리롤 표시 초기화
            UpdateRerollDisplay();

            // 전투 상태 표시
            UpdateBattleStateDisplay();

            Debug.Log("[BattleUI] 전투 UI 초기화 완료");
        }

        private void CreatePlayerSlots()
        {
            // 기존 슬롯 제거
            ClearContainer(_playerPartyContainer);
            _playerSlots.Clear();
            _characterToSlotMap.Clear();

            // 플레이어 파티 순회하며 슬롯 생성
            var playerParty = _battleManager.Players;
            if (playerParty == null) return;

            foreach (var character in playerParty)
            {
                if (character == null) continue;

                GameObject slotObj = Instantiate(_playerSlotPrefab, _playerPartyContainer);
                CharacterSlot slot = slotObj.GetComponent<CharacterSlot>();

                if (slot != null)
                {
                    slot.Initialize(character, true); // isPlayer = true
                    slot.OnCharacterClicked += HandleCharacterClicked;
                    _playerSlots.Add(slot);
                    _characterToSlotMap[character] = slot;
                }
            }

            Debug.Log($"[BattleUI] 아군 슬롯 {_playerSlots.Count}개 생성 완료");
        }

        private void CreateEnemySlots()
        {
            // 기존 슬롯 제거
            ClearContainer(_enemyPartyContainer);
            _enemySlots.Clear();

            // 적 파티 순회하며 슬롯 생성
            var enemyParty = _battleManager.Enemies;
            if (enemyParty == null) return;

            foreach (var character in enemyParty)
            {
                if (character == null) continue;

                GameObject slotObj = Instantiate(_enemySlotPrefab, _enemyPartyContainer);
                CharacterSlot slot = slotObj.GetComponent<CharacterSlot>();

                if (slot != null)
                {
                    slot.Initialize(character, false); // isPlayer = false
                    slot.OnCharacterClicked += HandleCharacterClicked;
                    _enemySlots.Add(slot);
                    _characterToSlotMap[character] = slot;
                }
            }

            Debug.Log($"[BattleUI] 적군 슬롯 {_enemySlots.Count}개 생성 완료");

            // 적 슬롯 크기 동적 조절
            if (_enableDynamicSlotScaling)
            {
                AdjustEnemySlotSizes();
            }
        }

        /// <summary>
        /// 적 슬롯 크기를 동적으로 조절합니다.
        /// 적이 4명보다 많으면 슬롯 크기를 축소하여 컨테이너 범위 내에 표시합니다.
        /// </summary>
        private void AdjustEnemySlotSizes()
        {
            if (_enemyPartyContainer == null || _enemySlots.Count == 0) return;

            RectTransform containerRect = _enemyPartyContainer as RectTransform;
            if (containerRect == null) return;

            int enemyCount = _enemySlots.Count;

            // 4명 이하이면 기본 크기 사용
            if (enemyCount <= _maxEnemiesForFullSize)
            {
                foreach (var slot in _enemySlots)
                {
                    if (slot != null)
                    {
                        RectTransform slotRect = slot.transform as RectTransform;
                        if (slotRect != null)
                        {
                            slotRect.localScale = Vector3.one;
                        }
                    }
                }
                return;
            }

            // 4명 초과 시 크기 조절
            // 컨테이너의 사용 가능한 높이 계산
            float containerHeight = containerRect.rect.height;
            float availableHeightPerSlot = containerHeight / enemyCount;

            // 기본 슬롯 높이 대비 비율 계산
            float scaleRatio = availableHeightPerSlot / _baseSlotHeight;

            // 최소 스케일 제한 적용
            scaleRatio = Mathf.Max(scaleRatio, _minSlotScale);

            // 각 슬롯에 스케일 적용
            foreach (var slot in _enemySlots)
            {
                if (slot != null)
                {
                    RectTransform slotRect = slot.transform as RectTransform;
                    if (slotRect != null)
                    {
                        slotRect.localScale = new Vector3(scaleRatio, scaleRatio, 1f);
                    }
                }
            }

            Debug.Log($"[BattleUI] 적 슬롯 크기 조절: {enemyCount}명, 스케일 {scaleRatio:F2}");
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        // ========================================================================
        // 이벤트 구독
        // ========================================================================

        private void SubscribeToEvents()
        {
            // BattleEvents 정적 이벤트
            DungeonLog.Combat.BattleEvents.OnBattleStateChanged += HandleBattleStateChanged;
            DungeonLog.Combat.BattleEvents.OnTurnChanged += HandleTurnChanged;
            DungeonLog.Combat.BattleEvents.OnAPChanged += HandleAPChanged;
            DungeonLog.Combat.BattleEvents.OnTargetingModeChanged += HandleTargetingModeChanged;

            // Character 이벤트
            DungeonLog.Character.CharacterEvents.OnHealthChanged += HandleHPChanged;
            DungeonLog.Character.CharacterEvents.OnCharacterDeath += HandleCharacterDied;
            DungeonLog.Character.CharacterEvents.OnSkillDrawn += HandleSkillDrawn;
        }

        private void UnsubscribeFromEvents()
        {
            DungeonLog.Combat.BattleEvents.OnBattleStateChanged -= HandleBattleStateChanged;
            DungeonLog.Combat.BattleEvents.OnTurnChanged -= HandleTurnChanged;
            DungeonLog.Combat.BattleEvents.OnAPChanged -= HandleAPChanged;
            DungeonLog.Combat.BattleEvents.OnTargetingModeChanged -= HandleTargetingModeChanged;

            DungeonLog.Character.CharacterEvents.OnHealthChanged -= HandleHPChanged;
            DungeonLog.Character.CharacterEvents.OnCharacterDeath -= HandleCharacterDied;
            DungeonLog.Character.CharacterEvents.OnSkillDrawn -= HandleSkillDrawn;
        }

        // ========================================================================
        // BattleManager 이벤트 핸들러
        // ========================================================================

        private void HandleBattleStateChanged(BattleState newState)
        {
            UpdateBattleStateDisplay();

            // 스킬 뽑기 UI는 실제로 플레이어 턴이 시작될 때만 표시
            // (Resolving 상태에서 돌아올 때는 제외)
            bool shouldShowSkillReroll = (newState == BattleState.PlayerTurn) &&
                                       (_previousBattleState != BattleState.PlayerTurn &&
                                        _previousBattleState != BattleState.Resolving);

            // 이전 상태 업데이트
            _previousBattleState = newState;

            switch (newState)
            {
                case BattleState.PlayerTurn:
                    OnPlayerTurnStart(shouldShowSkillReroll);
                    break;
                case BattleState.EnemyTurn:
                    OnEnemyTurnStart();
                    break;
                case BattleState.BattleEnd:
                    // 전투 종료 - OnBattleEnded 이벤트에서 결과 처리
                    break;
            }
        }

        private void HandleTargetingModeChanged(bool isTargeting, DungeonLog.Character.Character user, DungeonLog.Data.SkillData skill)
        {
            _isTargetSelectionMode = isTargeting;

            Debug.Log($"[BattleUI] 타겟 선택 모드 변경: {(isTargeting ? "시작" : "종료")}, 사용자: {user?.CharacterName}, 스킬: {skill?.DisplayName}");

            // TODO: 타겟 선택 모드 시각적 피드백 (적 슬롯 하이라이트 등)
        }

        private void HandleTurnChanged(int turn)
        {
            // 턴 변경 시 UI 갱신
            UpdateFloorText();

            // 리롤 카운트 리셋
            _currentRerollCount = 0;
            UpdateRerollDisplay();

            // 행동 큐 초기화
            ClearActionQueue();
        }

        // ========================================================================
        // AP 이벤트 핸들러
        // ========================================================================

        private void HandleAPChanged(int currentAP, int maxAP)
        {
            UpdateAPDisplay();
        }

        private void UpdateAPDisplay()
        {
            if (_apSystem == null) return;

            int currentAP = _apSystem.CurrentAP;
            int maxAP = _apSystem.MaxAP;

            if (_apText != null)
            {
                _apText.text = $"팀 AP: {currentAP}/{maxAP}";
            }

            if (_apSlider != null)
            {
                _apSlider.maxValue = maxAP;
                _apSlider.value = currentAP;
            }
        }

        // ========================================================================
        // Character 이벤트 핸들러
        // ========================================================================

        private void HandleHPChanged(GameObject characterGameObject, int oldHP, int newHP)
        {
            DungeonLog.Character.Character character = characterGameObject.GetComponent<DungeonLog.Character.Character>();
            CharacterSlot slot = FindCharacterSlot(character);
            slot?.UpdateHPDisplay(newHP, character.Health.MaxHP);
        }

        private void HandleCharacterDied(GameObject characterGameObject)
        {
            DungeonLog.Character.Character character = characterGameObject.GetComponent<DungeonLog.Character.Character>();
            CharacterSlot slot = FindCharacterSlot(character);
            slot?.SetDead();
        }

        private void HandleSkillDrawn(GameObject characterGameObject, DungeonLog.Data.SkillData skill)
        {
            DungeonLog.Character.Character character = characterGameObject.GetComponent<DungeonLog.Character.Character>();
            CharacterSlot slot = FindCharacterSlot(character);
            slot?.UpdateSkill(skill);
        }

        // ========================================================================
        // 캐릭터 상호작용
        // ========================================================================

        private void HandleCharacterClicked(DungeonLog.Character.Character character)
        {
            // 캐릭터 클릭 시 처리
            // 1. 아군 캐릭터: 행동 큐에 추가 모드
            // 2. 적 캐릭터: 타겟 선택 모드

            bool isPlayer = _battleManager.Players.Any(c => c == character);

            if (isPlayer)
            {
                OnPlayerCharacterClicked(character);
            }
            else
            {
                OnEnemyCharacterClicked(character);
            }
        }

        private void OnPlayerCharacterClicked(DungeonLog.Character.Character player)
        {
            // 아군 캐릭터 클릭 시:
            // 1. 현재 추첨된 스킬 표시
            // 2. 스킬 선택 UI 표시 (4개 중 1개 이미 선택됨)
            // 3. 선택 시 행동 큐에 추가

            Debug.Log($"[BattleUI] 아군 캐릭터 클릭: {player.CharacterName}");

            // TODO: 스킬 선택 팝업 표시
            ShowSkillSelectionPopup(player);
        }

        private void OnEnemyCharacterClicked(DungeonLog.Character.Character enemy)
        {
            // 타겟 선택 모드인 경우 스킬 선택 팝업에 타겟 전달
            if (_skillSelectionPopup != null && _skillSelectionPopup.IsTargetSelectionMode)
            {
                _skillSelectionPopup.SelectTarget(enemy);
                Debug.Log($"[BattleUI] 타겟 선택 완료: {enemy.CharacterName}");
                return;
            }

            // 일반적인 적 캐릭터 정보 표시 (TODO: 적 정보 팝업)
            Debug.Log($"[BattleUI] 적 캐릭터 클릭: {enemy.CharacterName}");
        }

        // ========================================================================
        // 스킬 선택
        // ========================================================================

        private void ShowSkillSelectionPopup(DungeonLog.Character.Character character)
        {
            // 팝업이 없으면 생성
            if (_skillSelectionPopup == null)
            {
                if (_skillSelectionPopupPrefab == null)
                {
                    Debug.LogError("[BattleUI] SkillSelectionPopup 프리팹이 할당되지 않았습니다.");
                    return;
                }

                GameObject popupObj = Instantiate(_skillSelectionPopupPrefab, transform);
                _skillSelectionPopup = popupObj.GetComponent<SkillSelectionPopup>();

                if (_skillSelectionPopup == null)
                {
                    Debug.LogError("[BattleUI] SkillSelectionPopup 컴포넌트를 찾을 수 없습니다.");
                    Destroy(popupObj);
                    return;
                }

                // 이벤트 구독
                _skillSelectionPopup.OnSkillSelected += HandleSkillSelected;
                _skillSelectionPopup.OnCancelled += HandleSkillCancelled;
            }

            // 팝업 열기
            _skillSelectionPopup.Open(character);
            Debug.Log($"[BattleUI] 스킬 선택 팝업 표시: {character.CharacterName}");
        }

        // ========================================================================
        // 행동 큐 관리
        // ========================================================================

        private void AddToActionQueue(DungeonLog.Character.Character character, DungeonLog.Data.SkillData skill, DungeonLog.Character.Character target = null)
        {
            // AP 체크
            if (!_apSystem.HasEnoughAP(skill.APCost))
            {
                UIManager.Instance?.ShowPopup("AP가 부족합니다.", "알림");
                return;
            }

            // 행동 큐 아이템 생성
            GameObject itemObj = Instantiate(_actionQueueItemPrefab, _actionQueueContainer);
            ActionQueueItem item = itemObj.GetComponent<ActionQueueItem>();

            if (item != null)
            {
                int order = _actionQueue.Count;
                item.Initialize(character, skill, target, order);

                // 이벤트 구독 (람다 대신 명시적 핸들러 사용하여 메모리 누수 방지)
                item.OnRemoveClicked += HandleActionItemRemove;
                item.OnMoveUp += HandleActionItemMoveUp;
                item.OnMoveDown += HandleActionItemMoveDown;

                _actionQueue.Add(item);

                // AP 미리 차감 (취소 시 반환)
                _apSystem.ConsumeAP(skill.APCost);

                UpdateExecuteButton();
            }
        }

        // ========================================================================
        // 행동 큐 아이템 핸들러
        // ========================================================================

        private void HandleActionItemRemove(ActionQueueItem item)
        {
            if (item != null)
            {
                RemoveFromActionQueue(item);
            }
        }

        private void HandleActionItemMoveUp(ActionQueueItem item)
        {
            MoveActionInQueue(item, -1);
        }

        private void HandleActionItemMoveDown(ActionQueueItem item)
        {
            MoveActionInQueue(item, 1);
        }

        private void MoveActionInQueue(ActionQueueItem item, int direction)
        {
            int currentIndex = _actionQueue.IndexOf(item);
            if (currentIndex < 0) return;

            int newIndex = currentIndex + direction;

            // 범위 체크
            if (newIndex < 0 || newIndex >= _actionQueue.Count) return;

            // 리스트에서 스왑
            ActionQueueItem temp = _actionQueue[currentIndex];
            _actionQueue[currentIndex] = _actionQueue[newIndex];
            _actionQueue[newIndex] = temp;

            // 순서 업데이트
            for (int i = 0; i < _actionQueue.Count; i++)
            {
                _actionQueue[i].SetOrder(i);
            }

            // 이동 버튼 상태 갱신
            UpdateMoveButtonStates();

            Debug.Log($"[BattleUI] 행동 재정렬: {item.Actor.CharacterName}의 행동 {(direction > 0 ? "아래로" : "위로")} 이동");
        }

        private void UpdateMoveButtonStates()
        {
            for (int i = 0; i < _actionQueue.Count; i++)
            {
                _actionQueue[i].SetMoveButtonsEnabled(
                    canMoveUp: i > 0,
                    canMoveDown: i < _actionQueue.Count - 1
                );
            }
        }

        private void RemoveFromActionQueue(ActionQueueItem item)
        {
            if (item == null) return;

            // 이벤트 해제
            item.OnRemoveClicked -= HandleActionItemRemove;
            item.OnMoveUp -= HandleActionItemMoveUp;
            item.OnMoveDown -= HandleActionItemMoveDown;

            // AP 반환
            _apSystem.GrantAP(item.SkillData.APCost);

            _actionQueue.Remove(item);

            // GameObject 파괴 (다음 프레임에서 실제 파괴됨)
            if (item.gameObject != null)
            {
                Destroy(item.gameObject);
            }

            UpdateExecuteButton();
            UpdateMoveButtonStates();
        }

        private void ClearActionQueue()
        {
            foreach (var item in _actionQueue)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _actionQueue.Clear();
        }

        private void UpdateExecuteButton()
        {
            if (_executeActionsButton != null)
            {
                _executeActionsButton.interactable = _actionQueue.Count > 0;
            }
        }

        // ========================================================================
        // 버튼 핸들러
        // ========================================================================

        private void HandlePause()
        {
            Debug.Log("[BattleUI] 일시정지 클릭");
            // TODO: 일시정지 팝업 표시
        }

        private void HandleExecuteActions()
        {
            Debug.Log($"[BattleUI] 행동 실행: {_actionQueue.Count}개");

            // 행동 큐의 모든 액션 실행
            foreach (var item in _actionQueue)
            {
                if (item != null && item.IsValid)
                {
                    List<DungeonLog.Character.Character> targets = item.Target != null ? new List<DungeonLog.Character.Character> { item.Target } : new List<DungeonLog.Character.Character>();
                    _battleManager.TryUseSkill(item.Actor, item.SkillData, targets);
                }
            }

            // 행동 큐 초기화
            ClearActionQueue();
        }

        private void HandleCancelActions()
        {
            Debug.Log("[BattleUI] 행동 취소");

            // 모든 행동 취소 및 AP 반환
            foreach (var item in _actionQueue)
            {
                if (item != null)
                {
                    _apSystem.GrantAP(item.SkillData.APCost);
                    Destroy(item.gameObject);
                }
            }
            _actionQueue.Clear();

            UpdateExecuteButton();
        }

        private void HandleReroll()
        {
            if (_currentRerollCount >= MAX_REROLL_PER_TURN)
            {
                UIManager.Instance?.ShowPopup("리롤 횟수를 모두 소진했습니다.", "알림");
                return;
            }

            Debug.Log("[BattleUI] 스킬 리롤");

            // TODO: 선택된 캐릭터 스킬 리롤
            _currentRerollCount++;
            UpdateRerollDisplay();
        }

        private void HandleEndTurn()
        {
            Debug.Log("[BattleUI] 턴 종료");

            // 행동 큐 초기화
            ClearActionQueue();

            // 턴 종료 요청
            _battleManager.EndTurn();
        }

        // ========================================================================
        // 턴 시작/종료
        // ========================================================================

        private void OnPlayerTurnStart(bool showSkillRerollUI = true)
        {
            Debug.Log("[BattleUI] 플레이어 턴 시작");

            // 스킬 뽑기 UI 표시 (실제로 새로운 턴이 시작된 경우에만)
            if (showSkillRerollUI)
            {
                ShowSkillRerollPanel();
            }
        }

        private void OnEnemyTurnStart()
        {
            Debug.Log("[BattleUI] 적 턴 시작");

            // TODO: 적 턴 시작 시 UI 비활성화
        }

        // ========================================================================
        // 스킬 뽑기 UI
        // ========================================================================

        /// <summary>
        /// 스킬 뽑기 패널을 표시하고 플레이어 캐릭터들의 스킬을 추첨합니다.
        /// </summary>
        private void ShowSkillRerollPanel()
        {
            if (_skillRerollPanel == null)
            {
                Debug.LogWarning("[BattleUI] 스킬 뽑기 패널이 설정되지 않았습니다.");
                return;
            }

            // 플레이어 파티의 생존 캐릭터만 필터링
            var alivePlayers = _battleManager.Players.Where(c => c != null && c.IsAlive).ToList();

            if (alivePlayers.Count == 0)
            {
                Debug.LogWarning("[BattleUI] 생존한 플레이어 캐릭터가 없습니다.");
                return;
            }

            // 확인 버튼 콜백 설정
            _skillRerollPanel.OnConfirm = OnSkillRerollConfirmed;

            // 스킬 추첨 및 UI 표시
            _skillRerollPanel.RerollAndDisplaySkills(alivePlayers);
            _skillRerollPanel.Show();

            Debug.Log($"[BattleUI] 스킬 뽑기 UI 표시 ({alivePlayers.Count}명)");
        }

        /// <summary>
        /// 스킬 리롤 확정 시 호출됩니다.
        /// </summary>
        private void OnSkillRerollConfirmed()
        {
            Debug.Log("[BattleUI] 스킬 리롤 확정 - 스킬 적용 및 UI 업데이트");

            // 추첨된 스킬들 가져오기
            var rerolledSkills = _skillRerollPanel.GetRerolledSkills();

            // 각 캐릭터에게 스킬 적용
            foreach (var kvp in rerolledSkills)
            {
                var character = kvp.Key;
                var skill = kvp.Value;

                if (character == null || skill == null) continue;

                // SkillManager에 현재 턴의 스킬 설정
                if (character.SkillManager != null)
                {
                    character.SkillManager.SetCurrentTurnSkill(skill);
                    Debug.Log($"[BattleUI] {character.CharacterName}의 현재 턴 스킬 설정: {skill.DisplayName}");
                }
            }

            // PartySlotUI 업데이트
            UpdateAllPartySlots();
        }

        /// <summary>
        /// 모든 파티 슬롯 UI를 업데이트합니다.
        /// </summary>
        private void UpdateAllPartySlots()
        {
            if (_battleManager == null) return;

            foreach (var player in _battleManager.Players)
            {
                if (player == null || !player.IsAlive) continue;

                var slot = FindCharacterSlot(player);
                if (slot != null)
                {
                    slot.UpdateCharacterInfo();
                }
            }

            Debug.Log("[BattleUI] 모든 파티 슬롯 UI 업데이트 완료");
        }

        // ========================================================================
        // UI 갱신
        // ========================================================================

        private void UpdateFloorText()
        {
            if (_floorText != null && _battleManager != null)
            {
                // TODO: 던전 층 정보 표시
                _floorText.text = $"1층 - 전투";
            }
        }

        private void UpdateBattleStateDisplay()
        {
            if (_battleStateText == null || _battleManager == null) return;

            BattleState state = _battleManager.CurrentState;

            string stateText = state switch
            {
                BattleState.NotStarted => "전투 준비 중",
                BattleState.PlayerTurn => "플레이어 턴",
                BattleState.PlayerSkillReroll => "스킬 리롤",
                BattleState.EnemyTurn => "적 턴",
                BattleState.Resolving => "전투 진행 중",
                BattleState.BattleEnd => "전투 종료",
                _ => "알 수 없는 상태"
            };

            _battleStateText.text = stateText;
        }

        private void UpdateRerollDisplay()
        {
            if (_rerollCountText != null)
            {
                int remaining = MAX_REROLL_PER_TURN - _currentRerollCount;
                _rerollCountText.text = $"리롤 {remaining}/{MAX_REROLL_PER_TURN}";
            }

            if (_rerollButton != null)
            {
                _rerollButton.interactable = _currentRerollCount < MAX_REROLL_PER_TURN;
            }
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        private CharacterSlot FindCharacterSlot(DungeonLog.Character.Character character)
        {
            // Dictionary를 사용한 O(1) 조회 (성능 최적화)
            if (_characterToSlotMap.TryGetValue(character, out var slot))
            {
                return slot;
            }

            return null;
        }

        private void ShowVictoryPopup()
        {
            UIManager.Instance?.ShowPopup("전투에서 승리했습니다!", "승리", Popup.PopupType.Info, () =>
            {
                // TODO: 승리 보상 화면으로 이동
                Debug.Log("[BattleUI] 승리 보상 화면으로 이동");
            });
        }

        private void ShowDefeatPopup()
        {
            UIManager.Instance?.ShowPopup("전투에서 패배했습니다...", "패배", Popup.PopupType.Info, () =>
            {
                // TODO: 메인 화면으로 이동
                Debug.Log("[BattleUI] 메인 화면으로 이동");
            });
        }

        // ========================================================================
        // 스킬 선택 핸들러
        // ========================================================================

        private void HandleSkillSelected(DungeonLog.Character.Character actor, DungeonLog.Data.SkillData skill, DungeonLog.Character.Character target)
        {
            AddToActionQueue(actor, skill, target);

            // 이벤트 해제
            if (_skillSelectionPopup != null)
            {
                _skillSelectionPopup.OnSkillSelected -= HandleSkillSelected;
                _skillSelectionPopup.OnCancelled -= HandleSkillCancelled;
            }

            Debug.Log($"[BattleUI] 스킬 선택 완료: {actor.CharacterName} -> {skill.DisplayName}");
        }

        private void HandleSkillCancelled()
        {
            // 이벤트 해제
            if (_skillSelectionPopup != null)
            {
                _skillSelectionPopup.OnSkillSelected -= HandleSkillSelected;
                _skillSelectionPopup.OnCancelled -= HandleSkillCancelled;
            }

            Debug.Log("[BattleUI] 스킬 선택 취소됨");
        }

        // ========================================================================
        // 정리
        // ========================================================================

        private void OnDestroy()
        {
            // 이벤트 구독 해제 (OnHide가 호출되지 않을 경우 대비)
            UnsubscribeFromEvents();

            // 팝업 정리
            if (_skillSelectionPopup != null)
            {
                _skillSelectionPopup.OnSkillSelected -= HandleSkillSelected;
                _skillSelectionPopup.OnCancelled -= HandleSkillCancelled;
                Destroy(_skillSelectionPopup.gameObject);
                _skillSelectionPopup = null;
            }

            // 슬롯 이벤트 해제
            foreach (var slot in _playerSlots)
            {
                if (slot != null)
                {
                    slot.OnCharacterClicked -= HandleCharacterClicked;
                }
            }

            foreach (var slot in _enemySlots)
            {
                if (slot != null)
                {
                    slot.OnCharacterClicked -= HandleCharacterClicked;
                }
            }
        }
    }
}
