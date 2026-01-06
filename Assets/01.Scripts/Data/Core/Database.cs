using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonLog.Data
{
    /// <summary>
    /// 중앙 데이터 접근 계층입니다.
    /// 모든 ScriptableObject 데이터를 로드, 관리, 캐싱합니다.
    /// 에디터와 빌드 환경에서 모두 작동하도록 설계되었습니다.
    /// 제네릭 기반으로 리팩토링되어 코드 중복을 최소화했습니다.
    /// </summary>
    public class Database : MonoBehaviour
    {
        // 싱글톤 인스턴스
        private static Database _instance;
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 찾기 시도 (FindObjectOfType는 obsolete되었으므로 FindAnyObjectByType 사용)
                    _instance = FindAnyObjectByType<Database>();

                    if (_instance == null)
                    {
                        // 없으면 새로 생성
                        GameObject go = new GameObject("Database");
                        _instance = go.AddComponent<Database>();
                        DontDestroyOnLoad(go); // 항상 DontDestroyOnLoad 사용 (메모리 누수 수정)
                    }
                }
                return _instance;
            }
        }

        // 데이터 로더 (에디터/빌드 분리)
        private IDataLoader _dataLoader;

        // 제네릭 기반 통합 캐시 (Type → Dictionary<string, BaseData>)
        private Dictionary<Type, Dictionary<string, BaseData>> _cache;

        // 지원하는 데이터 타입 목록
        private static readonly Type[] SupportedTypes = new Type[]
        {
            typeof(CharacterData),
            typeof(SkillData),
            typeof(EnemyData),
            typeof(ItemData),
            typeof(RelicData),
            typeof(EventData)
        };

        // 초기화 플래그
        private bool _isInitialized = false;

        /// <summary>
        /// 데이터가 로드되었는지 확인합니다.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            // 싱글톤 패턴
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // Awake에서도 항상 적용

            Initialize();
        }

        /// <summary>
        /// Database를 초기화하고 데이터를 로드합니다.
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;

            Debug.Log("[Database] 초기화 시작...");

            // 통합 캐시 초기화
            _cache = new Dictionary<Type, Dictionary<string, BaseData>>();

            // 데이터 로더 생성 (에디터/빌드 분리)
            _dataLoader = CreateDataLoader();

            // 모든 데이터 로드
            LoadAll();

            _isInitialized = true;

            Debug.Log("[Database] 초기화 완료.");
            LogStatistics();
        }

        /// <summary>
        /// 환경에 맞는 데이터 로더를 생성합니다.
        /// 에디터에서는 AssetDatabase, 빌드에서는 Resources를 사용합니다.
        /// </summary>
        private IDataLoader CreateDataLoader()
        {
#if UNITY_EDITOR
            return new EditorDataLoader();
#else
            return new RuntimeDataLoader();
#endif
        }

        /// <summary>
        /// 모든 데이터를 로드하고 캐싱합니다.
        /// 제네릭을 활용하여 코드 중복을 제거했습니다.
        /// </summary>
        public void LoadAll()
        {
            Debug.Log("[Database] 데이터 로드 시작...");

            // 캐시 초기화
            _cache.Clear();

            // 각 타입별 데이터 로드 (제네릭 기반 자동화)
            foreach (var type in SupportedTypes)
            {
                LoadDataWithType(type);
            }

            // 중복 ID 검증
            ValidateAllData();

            Debug.Log("[Database] 데이터 로드 완료.");
        }

        /// <summary>
        /// 특정 타입의 데이터를 로드하고 캐싱합니다 (제네릭 기반).
        /// </summary>
        private void LoadDataWithType(Type type)
        {
            // 리플렉션을 사용하여 제네릭 메서드 호출
            var loadMethod = typeof(Database).GetMethod(nameof(LoadDataGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = loadMethod.MakeGenericMethod(type);

            genericMethod.Invoke(this, null);
        }

        /// <summary>
        /// 제네릭 데이터 로드 메서드 (리플렉션으로 호출됨).
        /// </summary>
        private void LoadDataGeneric<T>() where T : BaseData
        {
            T[] dataArray = _dataLoader.LoadAll<T>();

            if (dataArray == null || dataArray.Length == 0)
            {
                Debug.LogWarning($"[Database] {typeof(T).Name} 데이터가 없습니다.");
                return;
            }

            var typeCache = new Dictionary<string, BaseData>();

            foreach (T data in dataArray)
            {
                if (data == null) continue;

                string id = data.ID;

                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[Database] {typeof(T).Name}: ID가 비어있는 데이터가 있습니다.");
                    continue;
                }

                // 중복 ID 체크
                if (typeCache.ContainsKey(id))
                {
                    Debug.LogError($"[Database] {typeof(T).Name}: 중복 ID 발견! '{id}'가 이미 캐시에 있습니다. 기존 데이터를 유지합니다.");
                    continue;
                }

                typeCache[id] = data;
            }

            _cache[typeof(T)] = typeCache;

            Debug.Log($"[Database] {typeof(T).Name}: {typeCache.Count}개 로드됨.");
        }

        /// <summary>
        /// 모든 데이터의 유효성을 검증합니다.
        /// </summary>
        private void ValidateAllData()
        {
            Debug.Log("[Database] 데이터 유효성 검증 시작...");

            int totalErrors = 0;

            foreach (var type in SupportedTypes)
            {
                if (_cache.TryGetValue(type, out var typeCache))
                {
                    int errors = ValidateDataInCache(typeCache);
                    totalErrors += errors;
                }
            }

            if (totalErrors == 0)
            {
                Debug.Log("[Database] 모든 데이터 유효성 검증 통과!");
            }
            else
            {
                Debug.LogWarning($"[Database] {totalErrors}개의 데이터 유효성 검증 실패.");
            }
        }

        /// <summary>
        /// 캐시 내의 데이터 유효성을 검증합니다.
        /// </summary>
        private int ValidateDataInCache(Dictionary<string, BaseData> cache)
        {
            int errorCount = 0;

            foreach (var kvp in cache)
            {
                if (!kvp.Value.Validate())
                {
                    errorCount++;
                }
            }

            return errorCount;
        }

        /// <summary>
        /// 캐시를 모두 비웁니다.
        /// </summary>
        public void ClearCache()
        {
            _cache?.Clear();
            Debug.Log("[Database] 캐시가 초기화되었습니다.");
        }

        /// <summary>
        /// 특정 데이터의 캐시를 무효화합니다.
        /// </summary>
        public void InvalidateDataCache(string id)
        {
            // 모든 캐시에서 해당 ID를 제거
            foreach (var typeCache in _cache.Values)
            {
                typeCache.Remove(id);
            }

            Debug.Log($"[Database] 데이터 '{id}'의 캐시가 무효화되었습니다.");
        }

        // ========================================================================
        // 제네릭 접근자 (코드 중복 90% 감소)
        // ========================================================================

        /// <summary>
        /// 제네릭 데이터 가져오기 메서드.
        /// </summary>
        /// <typeparam name="T">데이터 타입 (BaseData 상속)</typeparam>
        /// <param name="id">데이터 ID</param>
        /// <returns>데이터 객체. 없으면 null 반환.</returns>
        public T Get<T>(string id) where T : BaseData
        {
            Type type = typeof(T);

            if (!_cache.TryGetValue(type, out var typeCache))
            {
                Debug.LogWarning($"[Database] {type.Name} 타입의 캐시가 없습니다.");
                return null;
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[Database] ID가 비어있습니다.");
                return null;
            }

            if (typeCache.TryGetValue(id, out BaseData data))
            {
                return data as T;
            }

            Debug.LogWarning($"[Database] ID '{id}'를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 제네릭 전체 데이터 가져오기 메서드.
        /// </summary>
        /// <typeparam name="T">데이터 타입 (BaseData 상속)</typeparam>
        /// <returns>해당 타입의 모든 데이터 열거.</returns>
        public IEnumerable<T> GetAll<T>() where T : BaseData
        {
            Type type = typeof(T);

            if (!_cache.TryGetValue(type, out var typeCache))
            {
                return Enumerable.Empty<T>();
            }

            return typeCache.Values.Cast<T>();
        }

        // ========================================================================
        // 하위 호환성 래퍼 메서드 (기존 코드와의 호환성 유지)
        // ========================================================================

        /// <summary>
        /// 캐릭터 데이터를 가져옵니다.
        /// </summary>
        public CharacterData GetCharacter(string id)
        {
            return Get<CharacterData>(id);
        }

        /// <summary>
        /// 모든 캐릭터 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<CharacterData> GetAllCharacters()
        {
            return GetAll<CharacterData>();
        }

        /// <summary>
        /// 스킬 데이터를 가져옵니다.
        /// </summary>
        public SkillData GetSkill(string id)
        {
            return Get<SkillData>(id);
        }

        /// <summary>
        /// 모든 스킬 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<SkillData> GetAllSkills()
        {
            return GetAll<SkillData>();
        }

        /// <summary>
        /// 적 데이터를 가져옵니다.
        /// </summary>
        public EnemyData GetEnemy(string id)
        {
            return Get<EnemyData>(id);
        }

        /// <summary>
        /// 모든 적 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<EnemyData> GetAllEnemies()
        {
            return GetAll<EnemyData>();
        }

        /// <summary>
        /// 아이템 데이터를 가져옵니다.
        /// </summary>
        public ItemData GetItem(string id)
        {
            return Get<ItemData>(id);
        }

        /// <summary>
        /// 모든 아이템 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<ItemData> GetAllItems()
        {
            return GetAll<ItemData>();
        }

        /// <summary>
        /// 유물 데이터를 가져옵니다.
        /// </summary>
        public RelicData GetRelic(string id)
        {
            return Get<RelicData>(id);
        }

        /// <summary>
        /// 모든 유물 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<RelicData> GetAllRelics()
        {
            return GetAll<RelicData>();
        }

        /// <summary>
        /// 이벤트 데이터를 가져옵니다.
        /// </summary>
        public EventData GetEvent(string id)
        {
            return Get<EventData>(id);
        }

        /// <summary>
        /// 모든 이벤트 데이터를 가져옵니다.
        /// </summary>
        public IEnumerable<EventData> GetAllEvents()
        {
            return GetAll<EventData>();
        }

        // ========================================================================
        // 유틸리티 메서드
        // ========================================================================

        /// <summary>
        /// 데이터베이스 통계를 출력합니다.
        /// </summary>
        public void LogStatistics()
        {
            int totalCount = 0;
            var stats = new System.Text.StringBuilder();

            stats.AppendLine("[Database] 통계:");

            foreach (var type in SupportedTypes)
            {
                if (_cache.TryGetValue(type, out var typeCache))
                {
                    stats.AppendLine($"  {type.Name}: {typeCache.Count}");
                    totalCount += typeCache.Count;
                }
            }

            stats.AppendLine($"  총합: {totalCount}");
            Debug.Log(stats.ToString());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    // ========================================================================
    // 데이터 로더 구현 (에디터/빌드 분리)
    // ========================================================================

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 환경에서 AssetDatabase를 사용하여 데이터를 로드합니다.
    /// </summary>
    internal class EditorDataLoader : IDataLoader
    {
        public T Load<T>(string path) where T : BaseData
        {
            // AssetDatabase 사용
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (data != null && data.ID == path)
                {
                    return data;
                }
            }

            return null;
        }

        public T[] LoadAll<T>() where T : BaseData
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> dataList = new List<T>();

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T data = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (data != null)
                {
                    dataList.Add(data);
                }
            }

            return dataList.ToArray();
        }
    }
#endif

    /// <summary>
    /// 런타임/빌드 환경에서 Resources를 사용하여 데이터를 로드합니다.
    /// (향후 Addressables로 교체 가능)
    /// </summary>
    internal class RuntimeDataLoader : IDataLoader
    {
        public T Load<T>(string path) where T : BaseData
        {
            // Resources 폴더에서 로드
            // 예: "Characters/CH_001"
            T[] allData = LoadAll<T>();

            if (allData != null)
            {
                foreach (T data in allData)
                {
                    if (data != null && data.ID == path)
                    {
                        return data;
                    }
                }
            }

            return null;
        }

        public T[] LoadAll<T>() where T : BaseData
        {
            // Resources 폴더에서 모든 데이터 로드
            // 예: "Data/Characters"
            string folderPath = GetFolderPathForType<T>();
            T[] loadedData = Resources.LoadAll<T>(folderPath);

            if (loadedData == null || loadedData.Length == 0)
            {
                Debug.LogWarning($"[RuntimeDataLoader] {folderPath} 폴더에서 {typeof(T).Name} 데이터를 찾을 수 없습니다.");
            }

            return loadedData ?? new T[0];
        }

        /// <summary>
        /// 데이터 타입에 맞는 Resources 폴더 경로를 반환합니다.
        /// </summary>
        private string GetFolderPathForType<T>() where T : BaseData
        {
            string typeName = typeof(T).Name;

            return typeName switch
            {
                nameof(CharacterData) => "Data/Characters",
                nameof(SkillData) => "Data/Skills",
                nameof(EnemyData) => "Data/Enemies",
                nameof(ItemData) => "Data/Items",
                nameof(RelicData) => "Data/Relics",
                nameof(EventData) => "Data/Events",
                _ => "Data"
            };
        }
    }
}
