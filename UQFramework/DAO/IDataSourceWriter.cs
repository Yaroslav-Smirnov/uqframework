namespace UQFramework.DAO
{
    public interface IDataSourceWriter<T>
    {
        void AddEntity(T entity);

        void UpdateEntity(T entity);

        void DeleteEntity(T entity);
    }
}
