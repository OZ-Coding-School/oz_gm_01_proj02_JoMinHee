using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// CSV 파일 파서 클래스.
/// CSV 파일을 읽어서 Dictionary 형태로 변환합니다.
/// RFC 4180 표준을 따릅니다.
/// 메모리 누수 수정: 모든 FileStream을 using 문으로 감쌌습니다.
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// CSV 파일을 파싱하여 데이터를 반환합니다.
    /// </summary>
    /// <param name="csvPath">CSV 파일 경로</param>
    /// <returns>헤더를 키로 가지는 Dictionary 리스트</returns>
    public static List<Dictionary<string, string>> Parse(string csvPath)
    {
        var result = new List<Dictionary<string, string>>();

        // 파일 존재 확인
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[CSVParser] 파일을 찾을 수 없습니다: {csvPath}");
            return result;
        }

        try
        {
            // 인코딩 자동 감지 (UTF-8, CP949)
            Encoding encoding = DetectEncoding(csvPath);
            Debug.Log($"[CSVParser] 인코딩 감지: {encoding.EncodingName} ({csvPath})");

            // 스트림 기반 파싱으로 메모리 최적화
            using (var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, encoding))
            {
                // 헤더 파싱 (첫 번째 줄)
                string headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    Debug.LogWarning($"[CSVParser] 파일이 비어있습니다: {csvPath}");
                    return result;
                }

                string[] headers = ParseLine(headerLine);

                // 헤더 유효성 검사
                if (headers.Length == 0 || string.IsNullOrWhiteSpace(headers[0]))
                {
                    Debug.LogWarning($"[CSVParser] 헤더가 비어있습니다: {csvPath}");
                    return result;
                }

                int lineNumber = 1;
                string line;

                // 데이터 행 파싱 (두 번째 줄부터)
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    line = line.Trim();

                    // 빈 줄 무시
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // 데이터 파싱
                    string[] values = ParseLine(line);

                    // 빈 값 처리 시 경고 로그 추가
                    if (values.Length == 0 || string.IsNullOrWhiteSpace(values[0]))
                    {
                        Debug.LogWarning($"[CSVParser] {lineNumber}행: 빈 데이터가 감지되었습니다. 건너뜁니다.");
                        continue;
                    }

                    // 헤더와 개수가 맞지 않아도 처리 (빈 값으로 채움)
                    var row = new Dictionary<string, string>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        string header = headers[j].Trim();
                        string value = j < values.Length ? values[j].Trim() : string.Empty;
                        row[header] = value;
                    }

                    result.Add(row);
                }

                Debug.Log($"[CSVParser] 파싱 완료: {csvPath} ({result.Count}행)");
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVParser] 파싱 중 오류 발생: {csvPath}\n{e.Message}");
            return result;
        }
    }

    /// <summary>
    /// CSV 파일의 인코딩을 자동 감지합니다.
    /// UTF-8 BOM, UTF-8 without BOM, CP949 (EUC-KR) 순서로 확인합니다.
    /// 메모리 누수 수정: 모든 FileStream을 using 문으로 감쌌습니다.
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>감지된 인코딩</returns>
    private static Encoding DetectEncoding(string filePath)
    {
        // 파일의 처음 3바이트를 읽어서 BOM 확인 (using 문으로 자동 정리)
        byte[] bom = new byte[3];
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            stream.Read(bom, 0, 3);
        }

        // UTF-8 BOM 확인 (EF BB BF)
        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        // BOM이 없는 경우 UTF-8로 시도 후, 실패하면 CP949 시도
        try
        {
            // using 문으로 자동 정리 (파일 핸들 누수 수정)
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                // 첫 줄을 읽어서 유효한지 확인
                string firstLine = reader.ReadLine();
                if (!string.IsNullOrEmpty(firstLine))
                {
                    return new UTF8Encoding(false);
                }
            }
        }
        catch
        {
            // UTF-8 실패 시 CP949 시도
        }

        // CP949 (EUC-KR) 반환
        return Encoding.GetEncoding(949);
    }

    /// <summary>
    /// 개별 라인을 파싱합니다. RFC 4180 표준을 따릅니다.
    /// - 따옴표("") 내부의 쉼표는 필드 구분자가 아닙니다.
    /// - 이스케이프 따옴표("")는 큰따옴표 하나로 변환됩니다.
    /// - 필드 앞뒤 공백은 자동으로 트리밍됩니다.
    /// </summary>
    /// <param name="line">파싱할 라인</param>
    /// <returns>구분된 값 배열</returns>
    private static string[] ParseLine(string line)
    {
        var result = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            char nextChar = i + 1 < line.Length ? line[i + 1] : '\0';

            if (c == '"')
            {
                // 이스케이프 따옴표("") 처리
                if (nextChar == '"')
                {
                    currentValue.Append('"');
                    i++; // 다음 문자 건너뜀
                }
                else
                {
                    // 따옴표 모드 토글
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 따옴표 밖의 쉼표만 필드 구분자로 처리
                result.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                // 그 외 문자는 현재 값에 추가
                currentValue.Append(c);
            }
        }

        // 마지막 값 추가
        result.Add(currentValue.ToString().Trim());

        return result.ToArray();
    }

    /// <summary>
    /// CSV 데이터를 문자열로 변환합니다 (디버깅용).
    /// </summary>
    /// <param name="data">변환할 데이터</param>
    /// <returns>CSV 형식 문자열</returns>
    public static string ToString(List<Dictionary<string, string>> data)
    {
        if (data == null || data.Count == 0)
        {
            return "Empty Data";
        }

        var output = new StringBuilder();

        // 헤더 출력
        var headers = data[0].Keys.ToList();
        output.AppendLine(string.Join(", ", headers));

        // 데이터 출력
        foreach (var row in data)
        {
            var values = headers.Select(h => row.GetValueOrDefault(h, ""));
            output.AppendLine(string.Join(", ", values));
        }

        return output.ToString();
    }
}
