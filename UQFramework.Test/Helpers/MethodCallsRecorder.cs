using System;
using System.Collections;
using System.Collections.Generic;

namespace UQFramework.Test.Helpers
{
	public class MethodCallsRecorder
	{
		private List<UpdateDataSourceCallInfo> _updateDataSourceCalls = new List<UpdateDataSourceCallInfo>();
		public void AddUpdateDataSourceCall(Type type, IEnumerable entitiesToAdd, IEnumerable entitiesToUpdate, IEnumerable entitiesToDelete)
		{
			_updateDataSourceCalls.Add(new UpdateDataSourceCallInfo
			{
				Type = type,
				EtitiesToAdd = entitiesToAdd,
				EtitiesToUpdate = entitiesToUpdate,
				EtitiesToDelete = entitiesToDelete
			});
		}

		public List<UpdateDataSourceCallInfo> UpdateDataSourceCalls => _updateDataSourceCalls;

		public class UpdateDataSourceCallInfo
		{
			public Type Type { get; set; }
			public IEnumerable EtitiesToAdd { get; set; }
			public IEnumerable EtitiesToUpdate { get; set; }
			public IEnumerable EtitiesToDelete { get; set; }
		}
	}
}
