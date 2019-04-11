namespace UQFramework
{
    public interface ITransactionService
    {
        void BeginTransaction();

        void CommitChanges(string commitMessage = null);

        void Rollback();
    }
}