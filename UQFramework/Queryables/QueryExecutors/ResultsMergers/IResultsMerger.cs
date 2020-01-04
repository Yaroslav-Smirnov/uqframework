namespace UQFramework.Queryables.QueryExecutors.ResultsMergers
{
    interface IResultsMerger
    {
        object Merge(object result1, object result2);
    }
}