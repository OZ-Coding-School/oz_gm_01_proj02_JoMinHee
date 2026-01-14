using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DungeonLog.Combat;

namespace DungeonLog.UI
{
    /// <summary>
    /// 데미지 텍스트 오브젝트 풀입니다.
    /// 전투 중 데미지 텍스트를 효율적으로 관리합니다.
    /// Phase 5: 데미지 계산 및 스킬 시스템 UI
    /// </summary>
    public class DamageTextPool : MonoBehaviour
    {
        // ========================================================================
        // 필드
        // ========================================================================

        /// <summary>프리팹 참조</summary>
        [SerializeField] private GameObject damageTextPrefab = null;

        /// <summary>풀 크기</summary>
        [SerializeField] private int poolSize = 20;

        /// <summary>캔버스 설정</summary>
        [SerializeField] private Canvas canvas = null;

        /// <summary>텍스트 프리팹 경로</summary>
        private const string PREFAB_PATH = "UI/Prefabs/DamageText";

        // ========================================================================
        // 프라이빗 필드
        // ========================================================================

        private Queue<GameObject> pool;
        private Transform poolContainer;

        // ========================================================================
        // 싱글톤
        // ========================================================================

        private static DamageTextPool _instance;
        public static DamageTextPool Instance => _instance;

        // ========================================================================
        // Unity 생명주기
        // ========================================================================

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializePool();
                SubscribeToEvents();
            }
            else
            {
                // 중복 인스턴스: 이벤트 구독하지 않았으므로 해제 불필요, 바로 파괴
                Debug.LogWarning("[DamageTextPool] 이미 인스턴스가 존재합니다.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // 항상 이벤트 해제 (조건 제거)
            UnsubscribeFromEvents();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        // ========================================================================
        // 이벤트 구독
        // ========================================================================

        private void SubscribeToEvents()
        {
            DungeonLog.Character.CharacterEvents.OnDamagedWithInfo += OnDamagedWithInfo;
            DungeonLog.Character.CharacterEvents.OnHealedWithPosition += OnHealedWithPosition;
        }

        private void UnsubscribeFromEvents()
        {
            DungeonLog.Character.CharacterEvents.OnDamagedWithInfo -= OnDamagedWithInfo;
            DungeonLog.Character.CharacterEvents.OnHealedWithPosition -= OnHealedWithPosition;
        }

        private void OnDamagedWithInfo(Vector3 position, DungeonLog.Combat.DamageInfo damageInfo)
        {
            ShowDamageInfo(position, damageInfo);
        }

        private void OnHealedWithPosition(Vector3 position, int healAmount)
        {
            ShowHeal(position, healAmount);
        }

        // ========================================================================
        // 풀 초기화
        // ========================================================================

        private void InitializePool()
        {
            pool = new Queue<GameObject>();
            poolContainer = new GameObject("DamageTextPool").transform;
            poolContainer.SetParent(transform, false);

            // 캔버스 자동 찾기
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning("[DamageTextPool] Canvas를 찾을 수 없습니다.");
                    return;
                }
            }

            // 풀 미리 생성
            for (int i = 0; i < poolSize; i++)
            {
                CreateNewDamageText();
            }

            Debug.Log($"[DamageTextPool] 풀 초기화 완료 ({poolSize}개)");
        }

        private GameObject CreateNewDamageText()
        {
            GameObject textObj;

            // 에디터에서 할당된 프리팹 사용
            if (damageTextPrefab != null)
            {
                textObj = Instantiate(damageTextPrefab, poolContainer);
            }
            else
            {
                // 런타임 생성 (에디터에서 프리팹 할당되지 않은 경우)
                textObj = new GameObject("DamageText");
                textObj.transform.SetParent(poolContainer);

                var text = textObj.AddComponent<Text>();
                // 기본 폰트는 Unity에서 자동으로 할당됨
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 24;

                // 그림자 효과 (가돔성)
                var outline = textObj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2, -2);

                // 애니메이터 설정
                var animator = textObj.AddComponent<Animator>();
                // TODO: 애니메이션 컨트롤러 설정 (에디터에서)
            }

            textObj.SetActive(false);
            pool.Enqueue(textObj);

            return textObj;
        }

        // ========================================================================
        // 텍스트 표시
        // ========================================================================

        /// <summary>
        /// 데미지 텍스트를 표시합니다.
        /// </summary>
        public void ShowDamage(Vector3 position, int damage, bool isCritical)
        {
            GameObject textObj = GetFromPool();
            if (textObj == null) return;

            var text = textObj.GetComponent<Text>();
            if (text == null)
            {
                text = textObj.AddComponent<Text>();
            }

            // 텍스트 설정
            text.text = damage.ToString();
            text.color = isCritical ? Color.red : Color.white;
            text.fontSize = isCritical ? 36 : 24;

            // 위치 설정
            textObj.transform.position = position;
            textObj.SetActive(true);

            // 코루틴으로 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(textObj));
        }

        /// <summary>
        /// 힐 텍스트를 표시합니다.
        /// </summary>
        public void ShowHeal(Vector3 position, int healAmount)
        {
            GameObject textObj = GetFromPool();
            if (textObj == null) return;

            var text = textObj.GetComponent<Text>();
            if (text == null)
            {
                text = textObj.AddComponent<Text>();
            }

            // 텍스트 설정
            text.text = $"+{healAmount}";
            text.color = Color.green;
            text.fontSize = 24;

            // 위치 설정
            textObj.transform.position = position;
            textObj.SetActive(true);

            // 코루틴으로 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(textObj));
        }

        /// <summary>
        /// DamageInfo를 사용하여 데미지를 표시합니다.
        /// </summary>
        public void ShowDamageInfo(Vector3 position, DamageInfo damageInfo)
        {
            if (damageInfo == null) return;

            GameObject textObj = GetFromPool();
            if (textObj == null) return;

            var text = textObj.GetComponent<Text>();
            if (text == null)
            {
                text = textObj.AddComponent<Text>();
            }

            // 텍스트 설정
            string critText = damageInfo.IsCritical ? " [치명타!]" : "";
            text.text = $"{damageInfo.FinalDamage}{critText}";
            text.color = damageInfo.IsCritical ? Color.red : Color.white;
            text.fontSize = damageInfo.IsCritical ? 36 : 24;

            // 위치 설정
            textObj.transform.position = position;
            textObj.SetActive(true);

            // 코루틴으로 텍스트 애니메이션
            StartCoroutine(AnimateDamageText(textObj));
        }

        // ========================================================================
        // 풀 관리
        // ========================================================================

        private GameObject GetFromPool()
        {
            GameObject textObj;

            if (pool.Count > 0)
            {
                textObj = pool.Dequeue();
            }
            else
            {
                // 풀이 비어있으면 새로 생성
                textObj = CreateNewDamageText();
                Debug.LogWarning("[DamageTextPool] 풀이 비어있어 새로 생성합니다.");
            }

            return textObj;
        }

        private void ReturnToPool(GameObject textObj)
        {
            if (textObj == null) return;

            textObj.SetActive(false);
            pool.Enqueue(textObj);
        }

        // ========================================================================
        // 애니메이션
        // ========================================================================

        private System.Collections.IEnumerator AnimateDamageText(GameObject textObj)
        {
            Vector3 startPos = textObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f; // 위로 2유닛 이동

            float duration = 1f;
            float elapsed = 0f;

            var text = textObj.GetComponent<Text>();
            Color startColor = text.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f); // 투명하게

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 위치 이동
                textObj.transform.position = Vector3.Lerp(startPos, endPos, t);

                // 페이드 아웃
                if (text != null)
                {
                    text.color = Color.Lerp(startColor, endColor, t);
                }

                yield return null;
            }

            // 풀로 반환
            ReturnToPool(textObj);
        }

        // ========================================================================
        // 유틸리티
        // ========================================================================

        /// <summary>
        /// 풀을 모두 정리합니다.
        /// </summary>
        public void ClearPool()
        {
            foreach (var textObj in pool)
            {
                if (textObj != null)
                {
                    Destroy(textObj);
                }
            }

            pool.Clear();
            Debug.Log("[DamageTextPool] 풀 정리 완료");
        }
    }
}
