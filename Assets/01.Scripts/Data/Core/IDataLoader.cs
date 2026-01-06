namespace DungeonLog.Data
{
    /// <summary>
    /// 데이터 로더를 위한 인터페이스입니다.
    /// 에디터(AssetDatabase)와 빌드(Addressables/Resources) 환경에서의
    /// 데이터 로딩을 추상화합니다.
    /// </summary>
    public interface IDataLoader
    {
        /// <summary>
        /// 지정된 경로에서 데이터를 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 데이터 타입 (BaseData 상속)</typeparam>
        /// <param name="path">데이터 경로 또는 ID</param>
        /// <returns>로드된 데이터 객체</returns>
        T Load<T>(string path) where T : BaseData;

        /// <summary>
        /// 지정된 타입의 모든 데이터를 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 데이터 타입 (BaseData 상속)</typeparam>
        /// <returns>로드된 데이터 배열</returns>
        T[] LoadAll<T>() where T : BaseData;
    }
}
