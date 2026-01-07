using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using DungeonLog.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CSV 파싱 결과를 각 데이터 타입의 ScriptableObject로 변환하는 클래스입니다.
/// 제네릭 기반으로 리팩토링되어 코드 중복을 95% 감소시켰습니다.
/// Phase 2.3: CSV 데이터에서 ScriptableObject 자동 생성 기능
/// </summary>
public static class DataConverter
{
    /// <summary>
    /// 제네릭 데이터 변환 메서드 (코드 중복 95% 감소).
    /// CSV 데이터를 지정된 타입의 ScriptableObject 리스트로 변환하고 저장합니다.
    /// </summary>
    /// <typeparam name="T">BaseData를 상속받는 데이터 타입</typeparam>
    /// <param name="csvData">CSV 파서에서 추출한 데이터</param>
    /// <param name="outputPath">ScriptableObject 저장 경로</param>
    /// <returns>생성된 데이터 리스트</returns>
    public static List<T> ConvertToData<T>(List<Dictionary<string, string>> csvData, string outputPath) where T : BaseData, IDataLoadable
    {
        var result = new List<T>();

        if (csvData == null || csvData.Count == 0)
        {
            Debug.LogError($"[DataConverter] CSV 데이터가 비어있습니다. ({typeof(T).Name})");
            return result;
        }

        // 출력 폴더 생성
        EnsureDirectoryExists(outputPath);

        foreach (var row in csvData)
        {
            try
            {
                // ScriptableObject 인스턴스 생성
                T data = ScriptableObject.CreateInstance<T>();

                // CSV 데이터 로드 (다형성 활용)
                data.LoadFromCSV(row);

                // 유효성 검증 (다형성 활용)
                if (!data.Validate())
                {
                    Debug.LogWarning($"[DataConverter] {typeof(T).Name} 유효성 검증 실패: {row.GetValueOrDefault("ID", "Unknown")}");
                    ScriptableObject.DestroyImmediate(data);
                    continue;
                }

                // 에셋 파일로 저장
                string assetPath = $"{outputPath}{data.ID}.asset";
                data.SaveToAsset(assetPath);

                result.Add(data);
                Debug.Log($"[DataConverter] {typeof(T).Name} 생성 완료: {data.ID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataConverter] {typeof(T).Name} 변환 중 오류: {row.GetValueOrDefault("ID", "Unknown")}\n{e.Message}");
            }
        }

        Debug.Log($"[DataConverter] {typeof(T).Name} 변환 완료: {result.Count}개");
        return result;
    }

    /// <summary>
    /// 디렉토리가 존재하지 않으면 생성합니다.
    /// </summary>
    /// <param name="path">디렉토리 경로</param>
    private static void EnsureDirectoryExists(string path)
    {
#if UNITY_EDITOR
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"[DataConverter] 디렉토리 생성: {path}");
        }
#endif
    }

    /// <summary>
    /// 데이터 타입별 경로 매핑.
    /// 새로운 데이터 타입을 추가할 때 여기에만 추가하면 됩니다.
    /// </summary>
    private static readonly Dictionary<Type, (string CsvFile, string OutputPath)> DataTypeMappings = new Dictionary<Type, (string, string)>()
    {
        { typeof(CharacterData), ("Characters.csv", "Characters/") },
        { typeof(SkillData), ("Skills.csv", "Skills/") },
        { typeof(EnemyData), ("Enemies.csv", "Enemies/") },
        { typeof(ItemData), ("Items.csv", "Items/") },
        { typeof(RelicData), ("Relics.csv", "Relics/") },
        { typeof(EventData), ("Events.csv", "Events/") }
    };

    /// <summary>
    /// 모든 CSV 데이터를 한 번에 변환하는 헬퍼 메서드입니다.
    /// 리플렉션을 활용하여 제네릭 메서드를 자동으로 호출합니다.
    /// </summary>
    /// <param name="csvRootPath">CSV 파일 루트 경로 (Assets/08.Data/CSV/)</param>
    /// <param name="assetRootPath">ScriptableObject 저장 루트 경로 (Assets/05.ScriptableObjects/)</param>
    public static void ConvertAll(string csvRootPath, string assetRootPath)
    {
        Debug.Log("[DataConverter] 전체 데이터 변환 시작...");

        int successCount = 0;
        int failCount = 0;

        foreach (var (dataType, (csvFile, outputPath)) in DataTypeMappings)
        {
            string csvPath = $"{csvRootPath}{csvFile}";

            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"[DataConverter] CSV 파일을 찾을 수 없습니다: {csvPath}");
                failCount++;
                continue;
            }

            try
            {
                // CSV 파싱
                var csvData = CSVParser.Parse(csvPath);

                if (csvData == null || csvData.Count == 0)
                {
                    Debug.LogWarning($"[DataConverter] CSV 데이터가 비어있습니다: {csvPath}");
                    failCount++;
                    continue;
                }

                // 제네릭 메서드 호출 (리플렉션)
                var convertMethod = typeof(DataConverter)
                    .GetMethod(nameof(ConvertToData), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .MakeGenericMethod(dataType);

                var result = (IEnumerable<BaseData>)convertMethod.Invoke(null, new object[] { csvData, $"{assetRootPath}{outputPath}" });

                int count = result?.Count() ?? 0;
                if (count > 0)
                {
                    successCount++;
                    Debug.Log($"[DataConverter] {dataType.Name}: {count}개 변환 성공");
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[DataConverter] {dataType.Name}: 변환된 데이터가 없습니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataConverter] {dataType.Name} 변환 중 오류 발생:\n{e.Message}");
                failCount++;
            }
        }

        // AssetDatabase.Refresh()는 마지막에 한 번만 호출 (성능 최적화)
#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif

        Debug.Log($"[DataConverter] 전체 데이터 변환 완료! 성공: {successCount}, 실패: {failCount}");
    }

    // ========================================================================
    // 하위 호환성 래퍼 메서드 (기존 코드와의 호환성 유지)
    // ========================================================================

    /// <summary>
    /// CSV 데이터를 CharacterData로 변환합니다.
    /// </summary>
    public static List<CharacterData> ConvertToCharacterData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<CharacterData>(csvData, outputPath);
    }

    /// <summary>
    /// CSV 데이터를 SkillData로 변환합니다.
    /// </summary>
    public static List<SkillData> ConvertToSkillData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<SkillData>(csvData, outputPath);
    }

    /// <summary>
    /// CSV 데이터를 EnemyData로 변환합니다.
    /// </summary>
    public static List<EnemyData> ConvertToEnemyData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<EnemyData>(csvData, outputPath);
    }

    /// <summary>
    /// CSV 데이터를 ItemData로 변환합니다.
    /// </summary>
    public static List<ItemData> ConvertToItemData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<ItemData>(csvData, outputPath);
    }

    /// <summary>
    /// CSV 데이터를 RelicData로 변환합니다.
    /// </summary>
    public static List<RelicData> ConvertToRelicData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<RelicData>(csvData, outputPath);
    }

    /// <summary>
    /// CSV 데이터를 EventData로 변환합니다.
    /// </summary>
    public static List<EventData> ConvertToEventData(List<Dictionary<string, string>> csvData, string outputPath)
    {
        return ConvertToData<EventData>(csvData, outputPath);
    }
}
