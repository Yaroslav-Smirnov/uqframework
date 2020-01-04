using System;
using System.Linq;
using UQFramework.XTest.Dummies;
using Xunit;

namespace UQFramework.XTest
{
    public class CacheProviderTest
    {
        [Fact]
        public void TestSetupCorrectly()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);

            // Act
            var item99 = context.Dummies.FirstOrDefault(x => x.Id == "99");
            var item100 = context.Dummies.FirstOrDefault(x => x.Id == "100");

            // Assert
            Assert.NotNull(item99);
            Assert.Null(item100);
            Assert.Equal(2, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntities)));
        }

        [Fact]
        public void TestPendingChangesAny()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);
            var item = context.Dummies.FirstOrDefault(x => x.Id == "57");
            item.Name = "XName";
            context.Dummies.Update(item);
            methodCallsCounter.Reset();
            // Act            
            var xNameExists = context.Dummies.Any(d => d.Name == "XName");

            // Assert
            Assert.True(xNameExists);
            Assert.Equal(0, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntities)));
            Assert.Equal(1, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntitiesWithCachedPropertiesOnly)));
        }

        [Fact]
        public void TestPendingChangesProjection()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);
            var item = context.Dummies.FirstOrDefault(x => x.Id == "57");
            item.Name = "XName";
            context.Dummies.Update(item);
            methodCallsCounter.Reset();

            // Act            
            var Id = context.Dummies.Where(d => d.Name == "XName").Select(d => d.Id).ToList();

            // Assert
            Assert.Single(Id);
            Assert.Equal("57", Id[0]);
            Assert.Equal(0, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntities)));
            Assert.Equal(1, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntitiesWithCachedPropertiesOnly)));
        }

        [Fact]
        public void TestPendingChangesProjectionDict()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);
            var item = context.Dummies.FirstOrDefault(x => x.Id == "57");
            item.Name = "XName";
            context.Dummies.Update(item);
            methodCallsCounter.Reset();

            // Act            
            var Id = context.Dummies.Where(d => d.Name == "XName").ToDictionary(d => d.Id, d => d.Name);

            // Assert
            Assert.Single(Id);
            Assert.Equal("XName", Id["57"]);
            Assert.Equal(0, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntities)));
            Assert.Equal(1, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntitiesWithCachedPropertiesOnly)));

        }

        [Fact]
        public void TestPendingChangesFirstOrDefault()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);
            var item = context.Dummies.FirstOrDefault(x => x.Id == "57");
            item.Name = "XName";
            context.Dummies.Update(item);

            // Act            
            var xItem = context.Dummies.Where(d => d.Name == "XName").FirstOrDefault();
            var xItem2 = context.Dummies.FirstOrDefault(d => d.Name == "XName");
            var item37 = context.Dummies.FirstOrDefault(d => d.Id == "37");

            // Assert
            Assert.NotNull(xItem);
            Assert.NotNull(xItem2);
            Assert.NotNull(item37);
        }

        [Fact]
        public void TestPendingChangesCount()
        {
            // Arrange
            var methodCallsCounter = new MethodCallsCounter();
            var context = new DummyContext(100, methodCallsCounter);
            var item57 = context.Dummies.FirstOrDefault(x => x.Id == "57");
            var item19 = context.Dummies.FirstOrDefault(x => x.Id == "19");
            var name = item19.Name;
            item57.Name = name;

            context.Dummies.Update(item57);
            methodCallsCounter.Reset();

            // Act            
            var result = context.Dummies
                .Where(d => d.Name == name)
                .Select(x => new 
                        {
                            x.Id,
                            x.Name
                        }).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(0, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntities)));
            Assert.Equal(1, methodCallsCounter.GetCallsCount(nameof(DummyDao.GetEntitiesWithCachedPropertiesOnly)));

        }
    }
}
