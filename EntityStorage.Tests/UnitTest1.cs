using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EntityStorage.Core;
using EntityStorage.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EntityStorage.Tests
{
    public class SimpleTests : BaseTest
    {
        private readonly IEntityStorage _entityStorage;

        public SimpleTests():base()
        {
            _entityStorage = ServiceProvider.GetRequiredService<IEntityStorage>();
        }
        
        [SetUp]
        public void Setup()
        {
            if (File.Exists(DevDb))
            {
                File.Delete(DevDb);
            }
            var context = ServiceProvider.GetRequiredService<EfDbContext>();
            context.Database.EnsureCreated();
        }

        [Test]
        public async Task UpdateSingleUpdateServiceField()
        {
            var newEntity = await _entityStorage.CreateEntity(new TestModel
            {
                Count = 1
            });

            Assert.That(newEntity.Count, Is.EqualTo(1));
            Assert.That(newEntity.Version, Is.EqualTo(0));
            Assert.That(newEntity.ModificationTime.ToString("dd.MM.yyyy hh:mm:ss"), Is.EqualTo("01.01.0001 12:00:00"));

            await _entityStorage.UpdateSingle(newEntity, x => new TestModel
            {
                Count = x.Count + 1,
            });
            
            var datetimeNow1 = DateTime.Now;
            Assert.That(newEntity.Count, Is.EqualTo(2));
            Assert.That(newEntity.Version, Is.EqualTo(1));
            Assert.That(newEntity.ModificationTime.ToString("dd.MM.yyyy hh:mm:ss"), Is.EqualTo(datetimeNow1.ToString("dd.MM.yyyy hh:mm:ss")));

            
            var reload = _entityStorage.Reload(newEntity);
            var datetimeNow2 = DateTime.Now;
            Assert.That(reload.Count, Is.EqualTo(2));
            Assert.That(reload.Version, Is.EqualTo(1));
            Assert.That(reload.ModificationTime.ToString("dd.MM.yyyy hh:mm:ss"), Is.EqualTo(datetimeNow2.ToString("dd.MM.yyyy hh:mm:ss")));
        }
        
        [Test]
        public async Task UpdateServiceField()
        {
            var newEntity = await _entityStorage.CreateEntity(new TestModel
            {
                Count = 1
            });

            Assert.That(newEntity.Id, Is.Not.EqualTo(0));
            Assert.That(newEntity.Count, Is.EqualTo(1));
            Assert.That(newEntity.Version, Is.EqualTo(0));
            Assert.That(newEntity.ModificationTime.ToString("dd.MM.yyyy hh:mm:ss"), Is.EqualTo("01.01.0001 12:00:00"));

            await _entityStorage.Update<TestModel>(x=>x.Id == newEntity.Id, x => new TestModel
            {
                Count = x.Count + 1,
            });
            
            var reload = _entityStorage.Reload(newEntity);
            var datetimeNow2 = DateTime.Now;
            Assert.That(reload.Count, Is.EqualTo(2));
            Assert.That(reload.Version, Is.EqualTo(1));
            Assert.That(reload.ModificationTime.ToString("dd.MM.yyyy hh:mm:ss"), Is.EqualTo(datetimeNow2.ToString("dd.MM.yyyy hh:mm:ss")));
        }

        [Test]
        public async Task ChangeTrackingNotRaiseExceptionReSelect()
        {
            var newEntity = await _entityStorage.CreateEntity(new TestModel
            {
                Count = 1
            });

            var selectedFirst = _entityStorage.Select<TestModel>().FirstOrDefault(x => x.Id == newEntity.Id);

            Assert.That(selectedFirst, Is.Not.Null);
            Assert.That(selectedFirst.Count, Is.EqualTo(1));
            await _entityStorage.UpdateSingle(selectedFirst, x => new TestModel
            {
                Count = x.Count + 1
            });
            
            var selectedSecond = _entityStorage.Select<TestModel>().FirstOrDefault(x => x.Id == newEntity.Id);
            
            Assert.That(selectedSecond, Is.Not.Null);
            Assert.That(selectedSecond.Count, Is.EqualTo(2));
            await _entityStorage.UpdateSingle(selectedSecond, x => new TestModel
            {
                Count = x.Count + 1
            });

            var selectedFinish = _entityStorage.Select<TestModel>().FirstOrDefault(x => x.Id == newEntity.Id);

            Assert.That(selectedFinish, Is.Not.Null);
            Assert.That(selectedFinish.Count, Is.EqualTo(3));
        }
    }
}