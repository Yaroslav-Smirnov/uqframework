namespace UQFramework
{
    interface ISavableDataEx
    {
		void Delete();

		void CreateAndUpdate();

        void UpdateCacheWithPendingChanges();
    }
}
