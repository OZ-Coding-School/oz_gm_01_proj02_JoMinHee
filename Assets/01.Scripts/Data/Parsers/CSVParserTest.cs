using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CSVParser 테스트용 스크립트
/// 에디터에서 실행하여 파싱 결과를 확인할 수 있습니다.
/// </summary>
public class CSVParserTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private string testCSVPath = "Assets/08.Data/CSV/Characters.csv";

    [ContextMenu("Run CSV Parser Test")]
    public void RunTest()
    {
        Debug.Log("=== CSV Parser 테스트 시작 ===");

        // CSV 파싱
        var data = CSVParser.Parse(testCSVPath);

        // 결과 출력
        Debug.Log($"파싱된 데이터 수: {data.Count}");

        if (data.Count > 0)
        {
            // 첫 번째 데이터 확인
            var firstRow = data[0];
            Debug.Log($"첫 번째 캐릭터:");
            foreach (var kvp in firstRow)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }

            // 검증
            Debug.Log("\n=== 검증 결과 ===");

            // 데이터 개수 검증
            bool countCheck = data.Count == 4;
            Debug.Log($"데이터 개수 (4개 예상): {data.Count} - {(countCheck ? "성공" : "실패")}");

            // ID 검증
            bool idCheck = data[0]["ID"] == "CH_001";
            Debug.Log($"첫 번째 데이터 ID (CH_001 예상): {data[0]["ID"]} - {(idCheck ? "성공" : "실패")}");

            // Name 검증
            bool nameCheck = data[0]["Name"] == "전사";
            Debug.Log($"첫 번째 데이터 Name (전사 예상): {data[0]["Name"]} - {(nameCheck ? "성공" : "실패")}");

            // BaseHP 검증
            bool hpCheck = data[0]["BaseHP"] == "120";
            Debug.Log($"첫 번째 데이터 BaseHP (120 예상): {data[0]["BaseHP"]} - {(hpCheck ? "성공" : "실패")}");

            // 전체 성공 여부
            bool allPassed = countCheck && idCheck && nameCheck && hpCheck;
            Debug.Log($"\n전체 테스트: {(allPassed ? "모두 성공 ✓" : "일부 실패 ✗")}");
        }
        else
        {
            Debug.LogError("파싱된 데이터가 없습니다!");
        }

        Debug.Log("=== CSV Parser 테스트 종료 ===");
    }

    [ContextMenu("Print All Data")]
    public void PrintAllData()
    {
        var data = CSVParser.Parse(testCSVPath);

        Debug.Log("=== 전체 데이터 출력 ===");
        Debug.Log(CSVParser.ToString(data));
    }
}
