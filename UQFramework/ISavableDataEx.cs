namespace UQFramework
{
    interface ISavableDataEx
    {
        void SaveChanges();

        void UpdateCacheWithPendingChanges();
    }
}
