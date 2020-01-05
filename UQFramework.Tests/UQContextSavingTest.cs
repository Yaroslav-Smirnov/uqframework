using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UQFramework.Tests.Helpers;

namespace UQFramework.Tests
{
	[TestClass]
	public class UQContextSavingTest
	{
		[TestMethod]
		public void TestSaveConsideringOrder()
		{
			// Arrange
			var methodCalledRecorder = new MethodCallsRecorder();
			var context = new ContextWithRelations(methodCalledRecorder);

			var entities1 = new DummyEntity1
			{
				Key = "1"
			};
			var entities2 = new DummyEntity2
			{
				Key = "2"
			};
			var entities3 = new DummyEntity3
			{
				Key = "3"
			};
			var entities4 = new DummyEntity4
			{
				Key = "4"
			};

			context.DummyEntities1.Add(entities1);
			context.DummyEntities2.Add(entities2);
			context.DummyEntities3.Add(entities3);
			context.DummyEntities4.Add(entities4);

			// Act
			context.SaveChanges("test");

			// Assert
			Assert.IsNotNull(methodCalledRecorder.UpdateDataSourceCalls);
			Assert.AreEqual(4, methodCalledRecorder.UpdateDataSourceCalls.Count);
			Assert.AreEqual(typeof(DummyEntity3), methodCalledRecorder.UpdateDataSourceCalls[0].Type);
			Assert.AreEqual(typeof(DummyEntity2), methodCalledRecorder.UpdateDataSourceCalls[1].Type);
			Assert.AreEqual(typeof(DummyEntity1), methodCalledRecorder.UpdateDataSourceCalls[2].Type);
			Assert.AreEqual(typeof(DummyEntity4), methodCalledRecorder.UpdateDataSourceCalls[3].Type);
		}
	}
}
