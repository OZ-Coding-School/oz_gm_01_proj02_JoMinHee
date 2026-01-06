namespace DungeonLog.Data
{
    /// <summary>
    /// CSV 파일에서 데이터를 로드할 수 있는 기능을 정의하는 인터페이스입니다.
    /// ScriptableObject 데이터 클래스가 CSV로부터 초기화될 수 있도록 지원합니다.
    /// </summary>
    public interface IDataLoadable
    {
        /// <summary>
        /// CSV 파일의 데이터를 로드하여 현재 객체에 적용합니다.
        /// </summary>
        /// <param name="csvData">CSV 행 데이터를 나타내는 문자열 배열 (키-값 쌍)</param>
        void LoadFromCSV(System.Collections.Generic.Dictionary<string, string> csvData);

        /// <summary>
        /// ScriptableObject를 에셋 파일로 저장합니다.
        /// </summary>
        /// <param name="assetPath">저장할 에셋 경로</param>
        void SaveToAsset(string assetPath);
    }
}
