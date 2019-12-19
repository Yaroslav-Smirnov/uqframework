using System.Data.Common;

namespace UQFramework
{
	public interface ITransactionServiceDb : ITransactionService
	{
		DbTransaction GetTransaction();
	}
}
