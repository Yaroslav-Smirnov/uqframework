namespace UQFramework.DAO
{
    public interface IDataSourceReader<T>
    {
        T GetEntity(string identifier);
    }
}
