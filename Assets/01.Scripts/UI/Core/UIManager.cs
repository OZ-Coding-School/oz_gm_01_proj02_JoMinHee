using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonLog.UI.Core
{
    /// <summary>
    /// UI 매니저 싱글톤 클래스.
    /// 모든 UI 화면의 전환, 팝업 표시, 캔버스 관리를 담당합니다.
    /// Phase 8: 전투 UI 시스템 - UI Core
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ========================================================================
        // 싱글톤
        // ========================================================================

        private static UIManager _instance;
        public static UIManager Instance => _instance;

        // ========================================================================
        // Inspector 필드
        // ========================================================================

        [Header("캔버스 설정")]
        [SerializeField] private Canvas _mainCanvas = null;
        [SerializeField] private Canvas _popupCanvas = null;

        [Header("화면 등록")]
        [SerializeField] private List<ScreenEntry> _screens = new List<ScreenEntry>();

        [Header("팝업 프리팹")]
        [SerializeField] private Popup _popupPrefab = null;

        // ========================================================================
        // 프라이벗 필드
        // ========================================================================

        private Dictionary<string, UIScreen> _loadedScreens = new Dictionary<string, UIScreen>();
        private Stack<UIScreen> _screenStack = new Stack<UIScreen>();
        private List<Popup> _activePopups = new List<Popup>();

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        public Canvas MainCanvas => _mainCanvas;
        public Canvas PopupCanvas => _popupCanvas;
        public int ActivePopupCount => _activePopups.Count;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[UIManager] UIManager 인스턴스가 이미 존재합니다. 중복을 제거합니다.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // ========================================================================
        // 초기화
        // ========================================================================

        private void Initialize()
        {
            // 캔버스 자동 찾기
            if (_mainCanvas == null)
            {
                _mainCanvas = FindObjectOfType<Canvas>();
                if (_mainCanvas == null)
                {
                    Debug.LogError("[UIManager] 메인 캔버스를 찾을 수 없습니다!");
                }
            }

            if (_popupCanvas == null)
            {
                // 팝업용 별도 캔버스가 없으면 메인 캔버스 사용
                _popupCanvas = _mainCanvas;
            }

            // 등록된 화면 사전 등록
            RegisterScreens();

            Debug.Log("[UIManager] UI Manager 초기화 완료");
        }

        private void RegisterScreens()
        {
            _loadedScreens.Clear();

            foreach (var entry in _screens)
            {
                if (entry.ScreenPrefab == null)
                {
                    Debug.LogWarning($"[UIManager] 화면 프리팹이 null입니다: {entry.ScreenName}");
                    continue;
                }

                // 이미 인스턴스화된 경우 참조, 아니면 null로 저장
                if (_loadedScreens.ContainsKey(entry.ScreenName))
                {
                    Debug.LogWarning($"[UIManager] 중복된 화면 이름입니다: {entry.ScreenName}");
                    continue;
                }

                // 나중에 로드될 수 있도록 null로 저장 (lazy loading)
                _loadedScreens[entry.ScreenName] = null;
            }

            Debug.Log($"[UIManager] {_loadedScreens.Count}개의 화면이 등록되었습니다.");
        }

        // ========================================================================
        // 화면 전환 관리
        // ========================================================================

        /// <summary>
        /// 특정 화면을 표시합니다.
        /// </summary>
        public void ShowScreen(string screenName, bool animate = true)
        {
            if (string.IsNullOrEmpty(screenName))
            {
                Debug.LogError("[UIManager] 화면 이름이 비어있습니다.");
                return;
            }

            UIScreen screen = GetOrCreateScreen(screenName);
            if (screen == null)
            {
                Debug.LogError($"[UIManager] 화면을 찾을 수 없습니다: {screenName}");
                return;
            }

            // 현재 화면 숨기기
            if (_screenStack.Count > 0)
            {
                HideCurrentScreen(false);
            }

            screen.Show(animate);
            _screenStack.Push(screen);

            Debug.Log($"[UIManager] 화면 표시: {screenName}");
        }

        /// <summary>
        /// 제네릭 화면 표시 (타입 안전성)
        /// </summary>
        public T ShowScreen<T>(string screenName, bool animate = true) where T : UIScreen
        {
            ShowScreen(screenName, animate);
            return GetScreen<T>(screenName);
        }

        /// <summary>
        /// 현재 화면을 숨기고 이전 화면으로 돌아갑니다.
        /// </summary>
        public void HideCurrentScreen(bool animate = true)
        {
            if (_screenStack.Count > 0)
            {
                UIScreen screen = _screenStack.Pop();
                screen.Hide(animate);
            }
            else
            {
                Debug.LogWarning("[UIManager] 숨길 화면이 없습니다.");
            }
        }

        /// <summary>
        /// 특정 화면을 숨깁니다.
        /// </summary>
        public void HideScreen(string screenName, bool animate = true)
        {
            if (_loadedScreens.TryGetValue(screenName, out UIScreen screen))
            {
                if (screen != null && screen.gameObject.activeInHierarchy)
                {
                    screen.Hide(animate);
                }
            }
        }

        /// <summary>
        /// 모든 화면을 숨깁니다.
        /// </summary>
        public void HideAllScreens(bool animate = true)
        {
            foreach (var kvp in _loadedScreens)
            {
                if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
                {
                    kvp.Value.Hide(animate);
                }
            }
            _screenStack.Clear();
        }

        // ========================================================================
        // 화면 조회
        // ========================================================================

        /// <summary>
        /// 화면을 가져오거나 생성합니다 (Lazy Loading).
        /// </summary>
        private UIScreen GetOrCreateScreen(string screenName)
        {
            // 이미 로드된 경우
            if (_loadedScreens.TryGetValue(screenName, out UIScreen screen))
            {
                if (screen != null)
                {
                    return screen;
                }
            }

            // 등록된 프리팹에서 찾기
            UIScreen prefab = null;
            foreach (var entry in _screens)
            {
                if (entry.ScreenName == screenName)
                {
                    prefab = entry.ScreenPrefab;
                    break;
                }
            }

            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 등록되지 않은 화면입니다: {screenName}");
                return null;
            }

            // 인스턴스화
            screen = Instantiate(prefab, _mainCanvas.transform);
            screen.name = $"{screenName} (Instance)";
            _loadedScreens[screenName] = screen;

            return screen;
        }

        /// <summary>
        /// 특정 타입의 화면을 가져옵니다.
        /// </summary>
        public T GetScreen<T>(string screenName) where T : UIScreen
        {
            if (_loadedScreens.TryGetValue(screenName, out UIScreen screen))
            {
                return screen as T;
            }
            return null;
        }

        /// <summary>
        /// 현재 활성화된 화면을 가져옵니다.
        /// </summary>
        public UIScreen GetCurrentScreen()
        {
            return _screenStack.Count > 0 ? _screenStack.Peek() : null;
        }

        // ========================================================================
        // 팝업 관리
        // ========================================================================

        /// <summary>
        /// 팝업을 표시합니다.
        /// </summary>
        public Popup ShowPopup(string message, string title = "알림", Popup.PopupType type = Popup.PopupType.Info, Action onConfirm = null, Action onCancel = null)
        {
            if (_popupPrefab == null)
            {
                Debug.LogError("[UIManager] 팝업 프리팹이 할당되지 않았습니다.");
                return null;
            }

            Popup popup = Instantiate(_popupPrefab, _popupCanvas.transform);
            popup.Initialize(message, title, type, onConfirm, onCancel);
            popup.Show(true);

            _activePopups.Add(popup);

            // 팝업이 닫힐 때 리스트에서 제거
            popup.OnPopupClosed += () => _activePopups.Remove(popup);

            return popup;
        }

        /// <summary>
        /// 확인 다이얼로그를 표시합니다.
        /// </summary>
        public void ShowConfirmDialog(string message, string title = "확인", Action onConfirm = null, Action onCancel = null)
        {
            ShowPopup(message, title, Popup.PopupType.Confirm, onConfirm, onCancel);
        }

        /// <summary>
        /// 모든 활성 팝업을 닫습니다.
        /// </summary>
        public void CloseAllPopups()
        {
            foreach (var popup in new List<Popup>(_activePopups))
            {
                if (popup != null)
                {
                    popup.Hide(true);
                }
            }
            _activePopups.Clear();
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        /// <summary>
        /// 월드 좌표를 UI 좌표로 변환합니다.
        /// </summary>
        public Vector2 WorldToUIPoint(Vector3 worldPosition)
        {
            if (_mainCanvas == null) return Vector2.zero;

            Camera uiCamera = _mainCanvas.worldCamera;
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }

            if (uiCamera == null) return Vector2.zero;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mainCanvas.transform as RectTransform,
                screenPoint,
                uiCamera,
                out Vector2 localPoint
            );

            return localPoint;
        }

        // ========================================================================
        // Serializable 클래스
        // ========================================================================

        [System.Serializable]
        public class ScreenEntry
        {
            public string ScreenName;
            public UIScreen ScreenPrefab;
        }
    }
}
