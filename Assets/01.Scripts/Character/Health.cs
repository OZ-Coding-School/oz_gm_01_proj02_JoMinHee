using System;
using UnityEngine;

namespace DungeonLog.Character
{
    /// <summary>
    /// 캐릭터의 HP와 생존/사망 상태를 관리하는 클래스입니다.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class Health : MonoBehaviour
    {
        private CharacterStats _stats;

        private int _currentHP;
        private int _maxHP;

        // 사망 상태 캐싱
        private bool _isDead = false;

        // 코루틴 캐싱 (성능 최적화)
        private Coroutine _deathCoroutine;

        // ========================================================================
        // 초기화
        // ========================================================================

        private void Awake()
        {
            // CharacterStats 컴포넌트 캐싱
            _stats = GetComponent<CharacterStats>();
            if (_stats == null)
            {
                Debug.LogError($"[Health] CharacterStats 컴포넌트가 없습니다.", this);
            }
        }

        private void Start()
        {
            // HP 초기화 (CharacterStats가 이미 초기화되었다고 가정)
            ResetForBattle();
        }

        // ========================================================================
        // HP 프로퍼티
        // ========================================================================

        /// <summary>현재 HP</summary>
        public int CurrentHP => _currentHP;

        /// <summary>최대 HP</summary>
        public int MaxHP => _maxHP;

        /// <summary>HP 퍼센트 (0.0 ~ 1.0)</summary>
        public float HPPercent => _maxHP > 0 ? (float)_currentHP / _maxHP : 0f;

        /// <summary>생존 여부</summary>
        public bool IsAlive => !_isDead && _currentHP > 0;

        /// <summary>사망 여부</summary>
        public bool IsDead => _isDead;

        // ========================================================================
        // 데미지/회복
        // ========================================================================

        /// <summary>
        /// 데미지를 입습니다.
        /// </summary>
        public void TakeDamage(int damage, bool isCritical = false)
        {
            if (_isDead)
            {
                Debug.LogWarning($"[Health] 이미 사망한 상태입니다.");
                return;
            }

            if (damage <= 0)
            {
                Debug.LogWarning($"[Health] 데미지가 0 이하입니다: {damage}");
                return;
            }

            int oldHP = _currentHP;
            _currentHP = Mathf.Max(0, _currentHP - damage);

            // 이벤트 발생
            CharacterEvents.NotifyHealthChanged(gameObject, oldHP, _currentHP);
            CharacterEvents.NotifyDamaged(gameObject, damage, isCritical);

            Debug.Log($"[Health] 데미지 {damage}받음 (치명타: {isCritical}), HP: {oldHP} → {_currentHP}");

            // 사망 체크
            if (_currentHP == 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 회복합니다.
        /// </summary>
        public void Heal(int amount)
        {
            if (_isDead)
            {
                Debug.LogWarning($"[Health] 사망 상태에서는 회복할 수 없습니다.");
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning($"[Health] 회복량이 0 이하입니다: {amount}");
                return;
            }

            int oldHP = _currentHP;
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);

            // 이벤트 발생
            CharacterEvents.NotifyHealthChanged(gameObject, oldHP, _currentHP);
            CharacterEvents.NotifyHealed(gameObject, amount);

            Debug.Log($"[Health] {amount} 회복, HP: {oldHP} → {_currentHP}");
        }

        /// <summary>
        /// HP를 직접 설정합니다 (디버그용).
        /// </summary>
        public void SetHP(int hp)
        {
            int oldHP = _currentHP;
            _currentHP = Mathf.Clamp(hp, 0, _maxHP);

            CharacterEvents.NotifyHealthChanged(gameObject, oldHP, _currentHP);

            if (_currentHP == 0 && oldHP > 0)
            {
                Die();
            }
        }

        // ========================================================================
        // 사망 처리
        // ========================================================================

        /// <summary>
        /// 사망 처리를 수행합니다.
        /// </summary>
        private void Die()
        {
            if (_isDead) return;

            _isDead = true;
            Debug.Log($"[Health] 사망!");

            // 이벤트 발생
            CharacterEvents.NotifyDeath(gameObject);
        }

        /// <summary>
        /// 부활합니다 (디버그/특수 상황용).
        /// </summary>
        public void Revive(int reviveHP = 0)
        {
            if (!_isDead)
            {
                Debug.LogWarning($"[Health] 이미 살아있습니다.");
                return;
            }

            _isDead = false;
            _currentHP = reviveHP > 0 ? Mathf.Min(reviveHP, _maxHP) : Mathf.RoundToInt(_maxHP * 0.5f);

            Debug.Log($"[Health] 부활! HP: {_currentHP}/{_maxHP}");
        }

        // ========================================================================
        // 전투 시작/종료
        // ========================================================================

        /// <summary>
        /// 전투 시작 시 HP를 초기화합니다.
        /// </summary>
        public void ResetForBattle()
        {
            if (_stats == null)
            {
                Debug.LogError($"[Health] ResetForBattle 실패: CharacterStats가 없습니다.");
                return;
            }

            _maxHP = _stats.MaxHP;
            _currentHP = _maxHP;
            _isDead = false;

            Debug.Log($"[Health] 전투 시작: HP {_currentHP}/{_maxHP}");
        }

        // ========================================================================
        // 생명주기 정리
        // ========================================================================

        private void OnDestroy()
        {
            // 실행 중인 코루틴 정리
            if (_deathCoroutine != null)
            {
                StopCoroutine(_deathCoroutine);
                _deathCoroutine = null;
            }
        }
    }
}
