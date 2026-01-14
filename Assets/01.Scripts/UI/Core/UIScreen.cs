using System;
using System.Collections;
using UnityEngine;

namespace DungeonLog.UI.Core
{
    /// <summary>
    /// UI 화면 기반 클래스.
    /// 모든 UI 화면(Popup, Panel 등)의 기초가 되는 클래스입니다.
    /// 페이드 인/아웃 애니메이션, Show/Hide 상태 관리를 제공합니다.
    /// Phase 8: 전투 UI 시스템 - UI Core
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        // ========================================================================
        // Inspector 설정
        // ========================================================================

        [Header("애니메이션 설정")]
        [SerializeField] protected float _fadeDuration = 0.3f;
        [SerializeField] protected AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] protected bool _useAnimation = true;

        [Header("사운드 설정")]
        [SerializeField] protected string _showSound = "";
        [SerializeField] protected string _hideSound = "";

        // ========================================================================
        // 상태
        // ========================================================================

        protected CanvasGroup _canvasGroup;
        protected bool _isVisible = false;
        protected Coroutine _animationCoroutine = null;

        // ========================================================================
        // 프로퍼티
        // ========================================================================

        /// <summary>현재 화면이 표시되어 있는지 여부</summary>
        public bool IsVisible => _isVisible;

        /// <summary>현재 애니메이션 중인지 여부</summary>
        public bool IsAnimating => _animationCoroutine != null;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        protected virtual void Awake()
        {
            InitializeCanvasGroup();
        }

        protected virtual void OnDestroy()
        {
            // 코루틴 정리
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        // ========================================================================
        // 초기화
        // ========================================================================

        protected virtual void InitializeCanvasGroup()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 초기 상태: 비활성화
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // ========================================================================
        // Show/Hide
        // ========================================================================

        /// <summary>
        /// 화면을 표시합니다.
        /// </summary>
        public virtual void Show(bool animate = true)
        {
            if (_isVisible)
            {
                Debug.LogWarning($"[{GetType().Name}] 화면이 이미 표시되어 있습니다.");
                return;
            }

            gameObject.SetActive(true);

            if (animate && _useAnimation)
            {
                // 기존 애니메이션 중지
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }

                _animationCoroutine = StartCoroutine(AnimateShow());
            }
            else
            {
                SetVisibleImmediate(true);
            }

            _isVisible = true;
            OnShow();

            // 사운드 재생
            PlaySound(_showSound);
        }

        /// <summary>
        /// 화면을 숨깁니다.
        /// </summary>
        public virtual void Hide(bool animate = true)
        {
            if (!_isVisible)
            {
                return; // 이미 숨겨져 있으면 무시
            }

            if (animate && _useAnimation)
            {
                // 기존 애니메이션 중지
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }

                _animationCoroutine = StartCoroutine(AnimateHide());
            }
            else
            {
                SetVisibleImmediate(false);
                gameObject.SetActive(false);
            }

            _isVisible = false;
            OnHide();

            // 사운드 재생
            PlaySound(_hideSound);
        }

        /// <summary>
        /// 즉시 표시 상태로 설정합니다 (애니메이션 없음).
        /// </summary>
        protected virtual void SetVisibleImmediate(bool visible)
        {
            if (_canvasGroup == null) return;

            if (visible)
            {
                _canvasGroup.alpha = 1;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            else
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        // ========================================================================
        // 애니메이션
        // ========================================================================

        protected virtual IEnumerator AnimateShow()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            float elapsed = 0;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _fadeDuration;
                _canvasGroup.alpha = _fadeCurve.Evaluate(t);
                yield return null;
            }

            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _animationCoroutine = null;

            OnShowAnimationComplete();
        }

        protected virtual IEnumerator AnimateHide()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            float elapsed = 0;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _fadeDuration;
                _canvasGroup.alpha = 1 - _fadeCurve.Evaluate(t);
                yield return null;
            }

            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
            _animationCoroutine = null;

            OnHideAnimationComplete();
        }

        // ========================================================================
        // 가상 메서드 (자식 클래스에서 오버라이드)
        // ========================================================================

        /// <summary>
        /// 화면이 표시될 때 호출됩니다.
        /// </summary>
        protected virtual void OnShow()
        {
            // 자식 클래스에서 오버라이드
        }

        /// <summary>
        /// 화면이 숨겨질 때 호출됩니다.
        /// </summary>
        protected virtual void OnHide()
        {
            // 자식 클래스에서 오버라이드
        }

        /// <summary>
        /// 표시 애니메이션이 완료되었을 때 호출됩니다.
        /// </summary>
        protected virtual void OnShowAnimationComplete()
        {
            // 자식 클래스에서 오버라이드
        }

        /// <summary>
        /// 숨김 애니메이션이 완료되었을 때 호출됩니다.
        /// </summary>
        protected virtual void OnHideAnimationComplete()
        {
            // 자식 클래스에서 오버라이드
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        protected void PlaySound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            // TODO: 사운드 매니저 연동
            // AudioManager.Instance.PlaySound(soundName);
        }

        /// <summary>
        /// 현재 애니메이션을 강제로 완료합니다.
        /// </summary>
        public void ForceCompleteAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;

                if (_isVisible)
                {
                    SetVisibleImmediate(true);
                    OnShowAnimationComplete();
                }
                else
                {
                    SetVisibleImmediate(false);
                    gameObject.SetActive(false);
                    OnHideAnimationComplete();
                }
            }
        }
    }
}
