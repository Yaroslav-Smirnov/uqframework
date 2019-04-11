namespace UQFramework
{
    // primary used to set stuff in UQCollection when reflection is used
    internal interface IUQCollectionInitializer
    {
        void Initialize(object dataAccessObject, object cacheProvider);
    }
}
