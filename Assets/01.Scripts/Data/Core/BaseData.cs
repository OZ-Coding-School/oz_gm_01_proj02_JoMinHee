using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 모든 ScriptableObject 데이터의 기본 클래스입니다.
    /// 공통 필드와 데이터 유효성 검증 기능을 제공합니다.
    /// </summary>
    public abstract class BaseData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField]
        private string id;

        [SerializeField]
        protected string displayName;

        [SerializeField, TextArea(3, 5)]
        protected string description;

        [Header("버전 관리")]
        [SerializeField, Tooltip("데이터 버전 (데이터 변경 시 수동 증가)")]
        private int dataVersion = 1;

        /// <summary>
        /// 데이터의 고유 ID입니다. (예: CH_001, SK_001)
        /// 읽기 전용 프로퍼티로, 외부에서 수정할 수 없습니다.
        /// </summary>
        public string ID => id;

        /// <summary>
        /// 데이터의 표시 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 데이터에 대한 상세 설명입니다.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// 데이터 버전입니다. 데이터 변경 추적 및 마이그레이션에 사용됩니다.
        /// </summary>
        public int DataVersion => dataVersion;

        /// <summary>
        /// 데이터의 유효성을 검증합니다.
        /// 파생 클래스에서 필수 필드의 존재 여부와 값의 유효성을 검증하도록 오버라이드하세요.
        /// </summary>
        /// <returns>데이터가 윚효하면 true, 그렇지 않으면 false</returns>
        public virtual bool Validate()
        {
            return !string.IsNullOrEmpty(id);
        }

        /// <summary>
        /// Unity 에디터에서 데이터가 수정될 때 호출됩니다.
        /// 데이터의 유효성을 검증하고 로그를 출력합니다.
        /// </summary>
        protected virtual void OnValidate()
        {
            // ID가 아직 설정되지 않은 경우 (CreateInstance 직후 등)는 검증 스킵
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!Validate())
            {
                Debug.LogWarning($"[{GetType().Name}] 데이터 유효성 검증 실패: {id}");
            }
        }

        #region Protected Setters (리플렉션 최소화를 위한 헬퍼 메서드)

        /// <summary>
        /// ID를 설정합니다 (CSV 로드용).
        /// </summary>
        protected void SetID(string value)
        {
            id = value;
        }

        /// <summary>
        /// 표시 이름을 설정합니다 (CSV 로드용).
        /// </summary>
        protected void SetDisplayName(string value)
        {
            displayName = value;
        }

        /// <summary>
        /// 설명을 설정합니다 (CSV 로드용).
        /// </summary>
        protected void SetDescription(string value)
        {
            description = value;
        }

        /// <summary>
        /// ScriptableObject 에셋으로 저장합니다.
        /// 기존 에셋이 있으면 덮어쓰고, 없으면 새로 생성합니다.
        /// </summary>
        /// <param name="assetPath">에셋 저장 경로</param>
        public void SaveToAsset(string assetPath)
        {
#if UNITY_EDITOR
            // 기존 에셋 확인 후 덮어쓰기 처리
            var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, GetType());
            if (existingAsset != null)
            {
                UnityEditor.EditorUtility.CopySerialized(this, existingAsset);
                Debug.Log($"[BaseData] 기존 에셋 덮어쓰기: {assetPath}");
            }
            else
            {
                UnityEditor.AssetDatabase.CreateAsset(this, assetPath);
                Debug.Log($"[BaseData] 새 에셋 생성: {assetPath}");
            }
            // AssetDatabase.Refresh() 제거 - 호출자에서 한 번만 호출
#endif
        }

        #endregion
    }
}
