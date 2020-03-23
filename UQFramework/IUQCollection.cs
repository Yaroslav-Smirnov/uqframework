using System.Linq;

namespace UQFramework
{
    public interface IUQCollection<T> : IOrderedQueryable<T>
    {
        void Add(T item);

        void Remove(T item);

        void Update(T item);

        TResult Query<TResult>(string queryName, params object[] parameters);
    }
}
