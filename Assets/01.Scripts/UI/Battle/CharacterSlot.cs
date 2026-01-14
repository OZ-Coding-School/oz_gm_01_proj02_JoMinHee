using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace DungeonLog.UI.Battle
{
    /// <summary>
    /// 캐릭터 슬롯 UI.
    /// 파티 구성 화면과 전투 화면에서 모두 사용됩니다.
    /// Phase 9: 파티 구성 + 전투 UI 재사용
    ///
    /// 와이어프레임 기반:
    /// - 파티 구성: 캐릭터 선택/배치, 레벨/전투력 표시
    /// - 전투: HP 바, 추첨된 스킬, 상태 이상, 타겟 선택
    /// - 스킬 시전: 스킬 이미지 클릭 → 대상 슬롯 클릭
    /// </summary>
    public class CharacterSlot : MonoBehaviour, IPointerClickHandler
    {
        // ========================================================================
        // Inspector 설정
        // ========================================================================

        [Header("모드 설정")]
        [SerializeField] private SlotMode _mode = SlotMode.Battle;
        [Tooltip("파티 구성 모드인지 전투 모드인지 설정합니다 (Inspector에서 변경 가능)")]
        [SerializeField] private bool _isBattleMode = true;

        [Header("공통 UI 요소")]
        [SerializeField] private Image _portraitImage = null;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _classText = null;
        [SerializeField] private Image _selectionFrame = null;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _deadColor = Color.gray;
        [SerializeField] private Color _emptySlotColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("파티 구성 모드 UI")]
        [SerializeField] private GameObject _partyCompositionPanel = null;
        [SerializeField] private TMP_Text _levelText = null;
        [SerializeField] private TMP_Text _powerStatsText = null;
        [SerializeField] private Button _removeButton = null;
        [SerializeField] private GameObject _emptySlotIndicator = null;

        [Header("전투 모드 UI")]
        [SerializeField] private GameObject _battlePanel = null;
        [SerializeField] private Slider _hpSlider = null;
        [SerializeField] private TMP_Text _hpText = null;
        [SerializeField] private Image _hpFillImage = null;

        [Header("전체 슬롯 클릭 (전투 모드 타겟 선택용)")]
        [SerializeField] private Button _slotButton = null;

        [Header("스킬 시스템")]
        [SerializeField] private Image _skillIconImage = null;
        [SerializeField] private TMP_Text _skillNameText = null;
        [SerializeField] private TMP_Text _skillCostText = null;
        [SerializeField] private Button _skillButton = null;
        [SerializeField] private Image _skillSelectedFrame = null;
        [SerializeField] private Color _skillNormalColor = Color.white;
        [SerializeField] private Color _skillSelectedColor = new Color(1f, 0.84f, 0f); // 금색

        [Header("상태 이상")]
        [SerializeField] private Transform _statusEffectContainer = null;
        [SerializeField] private GameObject _statusEffectIconPrefab = null;

        [Header("데미지 인디케이터")]
        private Component _damageTextPool = null;  // 런타임에 FindObjectOfType로 찾음

        // ========================================================================
        // 열거형
        // ========================================================================

        public enum SlotMode
        {
            PartyComposition, // 파티 구성 모드
            Battle            // 전투 모드
        }

        // ========================================================================
        // 이벤트
        // ========================================================================

        public event Action<DungeonLog.Character.Character> OnCharacterClicked;
        public event Action<DungeonLog.Character.Character> OnRemoveClicked;
        public event Action<DungeonLog.Character.Character> OnSkillClicked;

        // ========================================================================
        // 상태
        // ========================================================================

        private DungeonLog.Character.Character _character = null;
        private GameObject _characterGameObject = null;
        private bool _isPlayer = false;
        private bool _isSelected = false;
        private bool _isDead = false;
        private bool _isEmpty = false;
        private bool _isSkillSelected = false;
        private bool _isSkillUsedThisTurn = false;  // 스킬 사용 상태 추적

        // 상태 이상 아이콘 매핑
        private System.Collections.Generic.Dictionary<int, Component> _statusEffectIcons =
            new System.Collections.Generic.Dictionary<int, Component>();

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        public DungeonLog.Character.Character Character => _character;
        public bool IsPlayer => _isPlayer;
        public bool IsEmpty => _isEmpty;
        public bool IsBattleMode => _isBattleMode;
        public bool IsSkillSelected => _isSkillSelected;

        // ========================================================================
        // 초기화
        // ========================================================================

        /// <summary>
        /// 캐릭터 슬롯을 초기화합니다 (전투 모드).
        /// </summary>
        public void Initialize(DungeonLog.Character.Character character, bool isPlayer)
        {
            SetCharacter(character, isPlayer);
            SetMode(SlotMode.Battle);
        }

        /// <summary>
        /// 캐릭터 슬롯을 초기화합니다 (모드 지정).
        /// </summary>
        public void Initialize(DungeonLog.Character.Character character, bool isPlayer, SlotMode mode)
        {
            SetCharacter(character, isPlayer);
            SetMode(mode);
        }

        /// <summary>
        /// 빈 슬롯으로 초기화합니다 (파티 구성 모드).
        /// </summary>
        public void InitializeAsEmpty()
        {
            _character = null;
            _characterGameObject = null;
            _isPlayer = false;
            _isDead = false;
            _isSelected = false;
            _isEmpty = true;
            _isBattleMode = false;

            UpdateEmptySlotDisplay();
        }

        /// <summary>
        /// 캐릭터를 설정합니다.
        /// </summary>
        private void SetCharacter(DungeonLog.Character.Character character, bool isPlayer)
        {
            _character = character;
            _characterGameObject = character?.gameObject;
            _isPlayer = isPlayer;
            _isDead = false;
            _isSelected = false;
            _isEmpty = (character == null);

            if (!_isEmpty)
            {
                // 캐릭터 정보 표시
                UpdateCharacterInfo();

                // HP 표시 (전투 모드)
                if (_isBattleMode)
                {
                    UpdateHPDisplay(_character.Health.CurrentHP, _character.Health.MaxHP);
                }

                // 스킬 표시
                UpdateSkill(_character.SkillManager?.GetCurrentSkillData());

                // 상태 이상 이벤트 구독
                SubscribeToStatusEffectEvents();

                // 스킬 사용 이벤트 구독
                DungeonLog.Combat.BattleEvents.OnSkillUsedInTurn += HandleSkillUsedInTurn;

                // HP 변경 이벤트 구독 (회복/데미지 UI 업데이트용)
                DungeonLog.Character.CharacterEvents.OnHealthChanged += HandleHealthChanged;

                // 스킬 사용 상태 초기화
                _isSkillUsedThisTurn = false;
                UpdateSkillButtonState();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[CharacterSlot] {_character.CharacterName} 슬롯 초기화 완료 (모드: {_mode})");
#endif
            }
        }

        /// <summary>
        /// 슬롯 모드를 설정합니다.
        /// </summary>
        public void SetMode(SlotMode mode)
        {
            _mode = mode;
            _isBattleMode = (mode == SlotMode.Battle);

            // 모드별 패널 활성화
            if (_partyCompositionPanel != null)
            {
                _partyCompositionPanel.SetActive(!_isBattleMode);
            }

            if (_battlePanel != null)
            {
                _battlePanel.SetActive(_isBattleMode);
            }

            // 버튼 이벤트 연결
            SetupButtonEvents();

            // UI 갱신
            RefreshDisplay();
        }

        // ========================================================================
        // 버튼 이벤트 설정
        // ========================================================================

        private void SetupButtonEvents()
        {
            // 전체 슬롯 버튼 (전투 모드 타겟 선택용)
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveListener(HandleSlotButtonClick);
                _slotButton.onClick.AddListener(HandleSlotButtonClick);
            }

            // 스킬 버튼
            if (_skillButton != null)
            {
                _skillButton.onClick.RemoveListener(HandleSkillButtonClick);
                _skillButton.onClick.AddListener(HandleSkillButtonClick);
            }

            // 제거 버튼 (파티 구성 모드)
            if (_removeButton != null)
            {
                _removeButton.onClick.RemoveListener(HandleRemoveButtonClick);
                _removeButton.onClick.AddListener(HandleRemoveButtonClick);
            }
        }

        // ========================================================================
        // UI 갱신
        // ========================================================================

        /// <summary>
        /// 현재 모드에 따라 UI를 갱신합니다.
        /// </summary>
        private void RefreshDisplay()
        {
            if (_isEmpty)
            {
                UpdateEmptySlotDisplay();
                return;
            }

            if (_isBattleMode)
            {
                UpdateBattleModeDisplay();
            }
            else
            {
                UpdatePartyCompositionModeDisplay();
            }

            UpdateSelectionFrame();
        }

        /// <summary>
        /// 전투 모드 UI를 갱신합니다.
        /// </summary>
        private void UpdateBattleModeDisplay()
        {
            if (_character == null) return;

            // HP 표시
            UpdateHPDisplay(_character.Health.CurrentHP, _character.Health.MaxHP);

            // 스킬 표시
            UpdateSkill(_character.SkillManager?.GetCurrentSkillData());
        }

        /// <summary>
        /// 파티 구성 모드 UI를 갱신합니다.
        /// </summary>
        private void UpdatePartyCompositionModeDisplay()
        {
            if (_character == null) return;

            // 레벨 표시
            if (_levelText != null)
            {
                _levelText.text = $"Lv.{_character.Data.Level}";
            }

            // 전투력 표시
            if (_powerStatsText != null)
            {
                // TODO: 전투력 계산 로직
                int power = _character.Stats.Attack + _character.Stats.Defense;
                _powerStatsText.text = $"전투력: {power}";
            }

            // 제거 버튼 활성화
            if (_removeButton != null)
            {
                _removeButton.gameObject.SetActive(true);
            }

            // 빈 슬롯 표시기 비활성화
            if (_emptySlotIndicator != null)
            {
                _emptySlotIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// 빈 슬롯을 표시합니다.
        /// </summary>
        private void UpdateEmptySlotDisplay()
        {
            // 캐릭터 정보 숨기기
            if (_portraitImage != null)
            {
                _portraitImage.gameObject.SetActive(false);
            }

            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(false);
            }

            if (_classText != null)
            {
                _classText.gameObject.SetActive(false);
            }

            // 전투 모드 요소 숨기기
            if (_battlePanel != null)
            {
                _battlePanel.SetActive(false);
            }

            // 빈 슬롯 표시기 활성화
            if (_emptySlotIndicator != null)
            {
                _emptySlotIndicator.SetActive(true);
            }

            // 제거 버튼 숨기기
            if (_removeButton != null)
            {
                _removeButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 캐릭터 기본 정보를 갱신합니다.
        /// </summary>
        public void UpdateCharacterInfo()
        {
            if (_character == null) return;

            // Portrait 처리
            if (_portraitImage != null)
            {
                _portraitImage.gameObject.SetActive(true);
                Sprite portrait = null;
                if (_character.Data != null)
                {
                    portrait = _character.Data.Portrait;
                }
                else if (_character.EnemyData != null)
                {
                    portrait = _character.EnemyData.EnemySprite;
                }
                _portraitImage.sprite = portrait;
                _portraitImage.color = Color.white; // 사망 상태가 아니면 흰색
            }

            // Name 처리
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(true);
                _nameText.text = _character.CharacterName;
            }

            // Class 처리 (EnemyData에는 BaseClass가 없음)
            if (_classText != null)
            {
                _classText.gameObject.SetActive(true);
                if (_character.Data != null)
                {
                    _classText.text = _character.Data.BaseClass.ToString();
                }
                else if (_character.EnemyData != null)
                {
                    // 적의 경우 "Enemy" 또는 비워둠
                    _classText.text = "Enemy";
                }
            }

            // 스킬 업데이트 (전투 모드인 경우)
            if (_isBattleMode)
            {
                UpdateSkill(_character.SkillManager?.GetCurrentSkillData());
            }
        }

        // ========================================================================
        // HP 표시
        // ========================================================================

        /// <summary>
        /// HP 표시를 갱신합니다.
        /// </summary>
        public void UpdateHPDisplay(int currentHP, int maxHP)
        {
            if (_hpText != null)
            {
                _hpText.text = $"{currentHP}/{maxHP}";
            }

            if (_hpSlider != null)
            {
                _hpSlider.maxValue = maxHP;
                _hpSlider.value = currentHP;
            }

            if (_hpFillImage != null)
            {
                // HP 비율에 따른 색상 변경
                float hpPercent = (float)currentHP / maxHP;
                _hpFillImage.color = GetHPColor(hpPercent);
            }
        }

        // ========================================================================
        // 스킬 시스템
        // ========================================================================

        /// <summary>
        /// 현재 스킬을 갱신합니다.
        /// </summary>
        public void UpdateSkill(DungeonLog.Data.SkillData skill)
        {
            if (skill == null)
            {
                // 스킬이 없으면 비활성화
                if (_skillIconImage != null)
                {
                    _skillIconImage.gameObject.SetActive(false);
                }
                if (_skillNameText != null)
                {
                    _skillNameText.text = "스킬 없음";
                }
                if (_skillCostText != null)
                {
                    _skillCostText.text = "";
                }
                if (_skillButton != null)
                {
                    _skillButton.interactable = false;
                }
                return;
            }

            // 스킬이 있으면 활성화
            if (_skillIconImage != null)
            {
                _skillIconImage.gameObject.SetActive(true);
                _skillIconImage.sprite = skill.Icon;
            }

            if (_skillNameText != null)
            {
                _skillNameText.text = skill.DisplayName;
            }

            if (_skillCostText != null)
            {
                _skillCostText.text = $"AP {skill.APCost}";
            }

            if (_skillButton != null)
            {
                _skillButton.interactable = !_isDead;
            }
        }

        /// <summary>
        /// 스킬 선택 상태를 설정합니다.
        /// </summary>
        public void SetSkillSelected(bool selected)
        {
            _isSkillSelected = selected;

            if (_skillSelectedFrame != null)
            {
                _skillSelectedFrame.color = selected ? _skillSelectedColor : _skillNormalColor;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character?.CharacterName} 스킬 선택: {selected}");
#endif
        }

        // ========================================================================
        // 버튼 핸들러
        // ========================================================================

        /// <summary>
        /// 스킬 버튼 클릭 핸들러.
        /// BattleManager의 스킬 시전 시스템을 호출합니다.
        /// </summary>
        private void HandleSkillButtonClick()
        {
            if (_isDead || _isEmpty) return;

            // 현재 캐릭터의 스킬 가져오기
            var currentSkill = _character?.SkillManager?.GetCurrentSkillData();
            if (currentSkill == null)
            {
                Debug.LogWarning($"[CharacterSlot] {_character.CharacterName}의 현재 스킬이 없습니다.");
                return;
            }

            // BattleManager에 스킬 버튼 클릭 알림
            DungeonLog.Combat.BattleManager.Instance?.OnSkillButtonClicked(_character, currentSkill);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 스킬 버튼 클릭: {currentSkill.DisplayName}");
#endif
        }

        /// <summary>
        /// 슬롯 버튼 클릭 핸들러 (전투 모드 타겟 선택용).
        /// 타겟 선택 모드인 경우 타겟으로 지정합니다.
        /// </summary>
        private void HandleSlotButtonClick()
        {
            if (_isDead || _isEmpty)
            {
                return;
            }

            // 타겟 선택 모드인 경우
            var battleManager = DungeonLog.Combat.BattleManager.Instance;

            if (battleManager != null && battleManager.IsTargetingMode)
            {
                // 타겟 선택
                battleManager.SelectTarget(_character);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[CharacterSlot] 타겟 선택: {_character.CharacterName}");
#endif
                return;
            }

            // 일반 클릭 동작 (캐릭터 선택 등)
            OnCharacterClicked?.Invoke(_character);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 슬롯 클릭");
#endif
        }

        /// <summary>
        /// 제거 버튼 클릭 핸들러 (파티 구성 모드).
        /// </summary>
        private void HandleRemoveButtonClick()
        {
            if (_isBattleMode || _isEmpty) return;

            // 제거 이벤트 발생
            OnRemoveClicked?.Invoke(_character);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 제거 클릭");
#endif
        }

        // ========================================================================
        // 캐릭터 상태
        // ========================================================================

        /// <summary>
        /// 캐릭터가 사망했음을 표시합니다.
        /// </summary>
        public void SetDead()
        {
            _isDead = true;

            // 회색으로 변경
            if (_portraitImage != null)
            {
                _portraitImage.color = _deadColor;
            }

            // 버튼 비활성화
            if (_skillButton != null)
            {
                _skillButton.interactable = false;
            }

            // HP 슬라이더 비활성화
            if (_hpSlider != null)
            {
                _hpSlider.interactable = false;
            }

            // 스킬 선택 해제
            if (_isSkillSelected)
            {
                SetSkillSelected(false);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 사망 처리");
#endif
        }

        /// <summary>
        /// 선택 상태를 설정합니다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateSelectionFrame();
        }

        private void UpdateSelectionFrame()
        {
            if (_selectionFrame == null) return;

            if (_isDead)
            {
                _selectionFrame.color = _deadColor;
            }
            else if (_isSelected)
            {
                _selectionFrame.color = _selectedColor;
            }
            else
            {
                _selectionFrame.color = _normalColor;
            }
        }

        // ========================================================================
        // 상태 이상 아이콘
        // ========================================================================

        /// <summary>
        /// 상태 이상 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeToStatusEffectEvents()
        {
            var eventManager = DungeonLog.StatusEffects.StatusEffectEventManager.Instance;
            if (eventManager != null)
            {
                eventManager.OnEffectApplied += HandleStatusEffectApplied;
                eventManager.OnEffectRemoved += HandleStatusEffectRemoved;
                eventManager.OnEffectExpired += HandleStatusEffectExpired;
            }
        }

        /// <summary>
        /// 상태 이상 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeFromStatusEffectEvents()
        {
            var eventManager = DungeonLog.StatusEffects.StatusEffectEventManager.Instance;
            if (eventManager != null)
            {
                eventManager.OnEffectApplied -= HandleStatusEffectApplied;
                eventManager.OnEffectRemoved -= HandleStatusEffectRemoved;
                eventManager.OnEffectExpired -= HandleStatusEffectExpired;
            }
        }

        /// <summary>
        /// 상태 이상 적용 핸들러.
        /// </summary>
        private void HandleStatusEffectApplied(GameObject target, DungeonLog.StatusEffects.StatusEffectInstance effect)
        {
            if (_characterGameObject == null || target == null) return;

            // 캐싱된 GameObject 참조로 비교 (GetComponent 호출 최적화)
            if (target != _characterGameObject) return;

            AddStatusEffectIcon(effect);
        }

        /// <summary>
        /// 상태 이상 제거 핸들러.
        /// </summary>
        private void HandleStatusEffectRemoved(GameObject target, DungeonLog.StatusEffects.StatusEffectInstance effect)
        {
            if (_characterGameObject == null || target == null) return;

            if (target != _characterGameObject) return;

            RemoveStatusEffectIcon(effect.InstanceId);
        }

        /// <summary>
        /// 상태 이상 만료 핸들러.
        /// </summary>
        private void HandleStatusEffectExpired(GameObject target, DungeonLog.StatusEffects.StatusEffectInstance effect)
        {
            if (_characterGameObject == null || target == null) return;

            if (target != _characterGameObject) return;

            RemoveStatusEffectIcon(effect.InstanceId);
        }

        /// <summary>
        /// 상태 이상 아이콘을 추가합니다.
        /// </summary>
        public void AddStatusEffectIcon(DungeonLog.StatusEffects.StatusEffectInstance effect)
        {
            if (_statusEffectContainer == null || _statusEffectIconPrefab == null) return;
            if (effect == null || effect.Data == null) return;

            // 이미 존재하는지 확인 (중첩 처리)
            if (_statusEffectIcons.ContainsKey(effect.InstanceId))
            {
                // 기존 아이콘 업데이트
                var existingIcon = _statusEffectIcons[effect.InstanceId];
                if (existingIcon != null)
                {
                    existingIcon.SendMessage("UpdateDuration", effect.RemainingTurns);
                }
                return;
            }

            // 새 아이콘 생성
            GameObject iconObj = Instantiate(_statusEffectIconPrefab, _statusEffectContainer);
            Component icon = iconObj.GetComponent("StatusEffectIcon");

            if (icon != null)
            {
                // 리플렉션을 사용하여 Initialize 메서드 호출 (2개의 파라미터)
                var method = icon.GetType().GetMethod("Initialize",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(icon, new object[] { effect.Data, effect.RemainingTurns });
                }
                _statusEffectIcons[effect.InstanceId] = icon;
            }
        }

        /// <summary>
        /// 상태 이상 아이콘을 제거합니다.
        /// </summary>
        public void RemoveStatusEffectIcon(int instanceId)
        {
            if (!_statusEffectIcons.TryGetValue(instanceId, out var icon)) return;
            if (icon == null) return;

            _statusEffectIcons.Remove(instanceId);

            if (icon.gameObject != null)
            {
                Destroy(icon.gameObject);
            }
        }

        /// <summary>
        /// 모든 상태 이상 아이콘을 제거합니다.
        /// </summary>
        public void ClearStatusEffectIcons()
        {
            if (_statusEffectContainer == null) return;

            foreach (Transform child in _statusEffectContainer)
            {
                Destroy(child.gameObject);
            }

            _statusEffectIcons.Clear();
        }

        // ========================================================================
        // 데미지 인디케이터
        // ========================================================================

        /// <summary>
        /// 데미지 텍스트를 표시합니다.
        /// </summary>
        public void ShowDamage(int damage, bool isCritical)
        {
            if (_damageTextPool == null)
            {
                // 리플렉션을 사용하여 DamageTextPool 타입 찾기
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var damagePoolType = assembly.GetType("DungeonLog.UI.DamageTextPool");
                    if (damagePoolType != null)
                    {
                        var obj = FindObjectOfType(damagePoolType);
                        if (obj != null)
                        {
                            _damageTextPool = obj as Component;
                            break;
                        }
                    }
                }
            }

            if (_damageTextPool != null)
            {
                Vector3 position = transform.position;
                // 리플렉션을 사용하여 ShowDamage 메서드 호출 (3개의 파라미터)
                var method = _damageTextPool.GetType().GetMethod("ShowDamage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(_damageTextPool, new object[] { position, damage, isCritical });
                }
            }
        }

        /// <summary>
        /// 힐 텍스트를 표시합니다.
        /// </summary>
        public void ShowHeal(int healAmount)
        {
            if (_damageTextPool == null)
            {
                // 리플렉션을 사용하여 DamageTextPool 타입 찾기
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var damagePoolType = assembly.GetType("DungeonLog.UI.DamageTextPool");
                    if (damagePoolType != null)
                    {
                        var obj = FindObjectOfType(damagePoolType);
                        if (obj != null)
                        {
                            _damageTextPool = obj as Component;
                            break;
                        }
                    }
                }
            }

            if (_damageTextPool != null)
            {
                Vector3 position = transform.position;
                // 리플렉션을 사용하여 ShowHeal 메서드 호출 (2개의 파라미터)
                var method = _damageTextPool.GetType().GetMethod("ShowHeal",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(_damageTextPool, new object[] { position, healAmount });
                }
            }
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        private Color GetHPColor(float percent)
        {
            if (percent > 0.6f)
            {
                return Color.green;
            }
            else if (percent > 0.3f)
            {
                return Color.yellow;
            }
            else
            {
                return Color.red;
            }
        }

        // ========================================================================
        // IPointerClickHandler 구현 (버튼 대체)
        // ========================================================================

        /// <summary>
        /// CharacterSlot GameObject 자체를 클릭했을 때 호출됩니다.
        /// 버튼 컴포넌트 대신 직접 클릭을 처리합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDead || _isEmpty)
            {
                return;
            }

            // 타겟 선택 모드인 경우
            var battleManager = DungeonLog.Combat.BattleManager.Instance;

            if (battleManager != null && battleManager.IsTargetingMode)
            {
                // 타겟 선택
                battleManager.SelectTarget(_character);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[CharacterSlot] 타겟 선택: {_character.CharacterName}");
#endif
                return;
            }

            // 전투 모드에서는 일반 클릭 동작을 하지 않음
            if (_isBattleMode)
            {
                return;
            }

            // 일반 클릭 동작 (파티 구성 모드에서만)
            OnCharacterClicked?.Invoke(_character);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 슬롯 클릭");
#endif
        }

        // ========================================================================
        // 정리
        // ========================================================================

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (_slotButton != null)
            {
                _slotButton.onClick.RemoveListener(HandleSlotButtonClick);
            }

            if (_skillButton != null)
            {
                _skillButton.onClick.RemoveListener(HandleSkillButtonClick);
            }

            if (_removeButton != null)
            {
                _removeButton.onClick.RemoveListener(HandleRemoveButtonClick);
            }

            // 스킬 사용 이벤트 해제
            DungeonLog.Combat.BattleEvents.OnSkillUsedInTurn -= HandleSkillUsedInTurn;

            // HP 변경 이벤트 해제
            DungeonLog.Character.CharacterEvents.OnHealthChanged -= HandleHealthChanged;

            // 상태 이상 이벤트 해제
            UnsubscribeFromStatusEffectEvents();

            // 상태 이상 아이콘 정리
            ClearStatusEffectIcons();
        }

        // ========================================================================
        // 스킬 사용 제한 이벤트 핸들러
        // ========================================================================

        /// <summary>
        /// 스킬 사용 상태가 변경되었을 때 호출됩니다.
        /// </summary>
        private void HandleSkillUsedInTurn(DungeonLog.Character.Character character)
        {
            // 현재 슬롯의 캐릭터인지 확인
            if (_character != character)
                return;

            _isSkillUsedThisTurn = true;
            UpdateSkillButtonState();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[CharacterSlot] {_character.CharacterName} 스킬 사용으로 버튼 비활성화");
#endif
        }

        /// <summary>
        /// 스킬 버튼 상태를 업데이트합니다.
        /// </summary>
        private void UpdateSkillButtonState()
        {
            if (_skillButton != null)
            {
                // 스킬 사용했거나, 캐릭터가 사망했거나, 비어있으면 비활성화
                _skillButton.interactable = !_isSkillUsedThisTurn && !_isDead && !_isEmpty;
            }
        }

        // ========================================================================
        // HP 변경 이벤트 핸들러
        // ========================================================================

        /// <summary>
        /// HP가 변경되었을 때 호출됩니다 (회복/데미지 UI 업데이트용).
        /// </summary>
        private void HandleHealthChanged(GameObject characterGameObject, int oldHP, int newHP)
        {
            // 현재 슬롯의 캐릭터인지 확인
            if (_characterGameObject == null || characterGameObject != _characterGameObject)
                return;

            // 전투 모드에서만 HP UI 업데이트
            if (_isBattleMode && _character != null && _character.Health != null)
            {
                UpdateHPDisplay(_character.Health.CurrentHP, _character.Health.MaxHP);
            }
        }
    }
}
