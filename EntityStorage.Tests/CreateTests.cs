using System;
using System.Linq;
using System.Threading.Tasks;
using EntityStorage.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EntityStorage.Tests
{
    public class CreateTests : BaseTest
    {
        private readonly IEntityStorage _entityStorage;

        public CreateTests()
        {
            _entityStorage = ServiceProvider.GetRequiredService<IEntityStorage>();
        }
        
        [Test]
        public async Task Create()
        {
            var testModel = await _entityStorage.CreateEntity(new TestModel()
            {
                Count = 1
            });
            Assert.That(testModel.Id, Is.Not.EqualTo(0));

            var reloaded = _entityStorage.Reload(testModel);
            Assert.That(reloaded.Id, Is.EqualTo(testModel.Id));
            Assert.That(reloaded.Count, Is.EqualTo(testModel.Count));
        }
        
        [Test]
        public async Task CreateIfNotExist()
        {
            var person1 = await _entityStorage.CreateEntity(new TestModel
            {
                Count = 1
            });
            var person2 = await _entityStorage.CreateEntity(new TestModel()
            {
                Count = 2
            });

            var res = await _entityStorage.CreateIfNotExist(x => x.Count == 3, () => new TestModel
            {
                Count = 3
            });
            
            Assert.That(res, Is.True);
            var newPerson = _entityStorage.Select<TestModel>().Single(x => x.Count == 3);
            Assert.That(newPerson.Id, Is.Not.EqualTo(0));
            Assert.That(newPerson.Count, Is.EqualTo(3));
        }
    }
}