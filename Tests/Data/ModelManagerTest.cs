using System;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelManagerTest : Test
    {
        private class PlainModel : Model
        {
        }

        private ModelManager Manager {
            get { return ServiceContainer.Resolve<ModelManager> (); }
        }

        [Test]
        public void TestCaching ()
        {
            Assert.IsEmpty (Manager.Cached<PlainModel> (), "Nothing should be cached yet.");
            var model = Manager.Update (new PlainModel ());
            Assert.That (Manager.Cached<PlainModel> (), Has.Exactly (1).EqualTo (model), "Only single item should be cached.");
            Assert.IsEmpty (Manager.Cached<Model> (), "Other types cache shouldn't be affected.");
        }

        [Test]
        public void TestGetByIdFromStore ()
        {
            var id = Guid.NewGuid ();
            var storedModel = new PlainModel () {
                Id = id,
            };

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            Assert.AreSame (storedModel, Manager.Get<PlainModel> (id));
        }

        [Test]
        public void TestGetByIdFromCache ()
        {
            var id = Guid.NewGuid ();
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            // TODO: Should instead just mock the cache
            var model = Manager.Update (new PlainModel () {
                Id = id,
            });

            Assert.AreSame (model, Manager.Get<PlainModel> (id));
        }

        [Test]
        public void TestGetByIdNotFound ()
        {
            var id = Guid.NewGuid ();
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            Assert.IsNull (Manager.Get<PlainModel> (id));
        }

        [Test]
        public void TestGetByRemoteIdFromStore ()
        {
            var remoteId = 1234;
            var storedModel = new PlainModel () {
                Id = Guid.NewGuid (),
                RemoteId = remoteId,
            };

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            Assert.AreSame (storedModel, Manager.GetByRemoteId<PlainModel> (remoteId));
        }

        [Test]
        public void TestGetByRemoteIdFromCache ()
        {
            var remoteId = 1234;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            // TODO: Should instead just mock the cache
            var model = Manager.Update (new PlainModel () {
                RemoteId = remoteId,
            });

            Assert.AreSame (model, Manager.GetByRemoteId<PlainModel> (remoteId));
        }

        [Test]
        public void TestGetByRemoteIdNotFound ()
        {
            var remoteId = 1234;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            Assert.IsNull (Manager.GetByRemoteId<PlainModel> (remoteId));
        }

        [Test]
        public void TestMergeSettingRemoteId ()
        {
            var remoteId = 1234;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            var model = Manager.Update (new PlainModel () {
                ModifiedAt = new DateTime (),
            });

            model.Merge (new PlainModel () {
                RemoteId = remoteId,
                ModifiedAt = DateTime.UtcNow,
            });

            Assert.AreEqual (remoteId, model.RemoteId, "Model remote id was not updated by merge.");
            Assert.AreSame (model, Manager.GetByRemoteId<PlainModel> (remoteId), "Unable to find model by remote id after merge.");
        }

        [Test]
        public void TestMergeUpdatingRemoteId ()
        {
            var remoteId1 = 1234;
            var remoteId2 = 4321;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId1) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId1) == storedModel &&
                store.GetByRemoteId<PlainModel> (remoteId2) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId2) == storedModel
            ));

            var model = Manager.Update (new PlainModel () {
                RemoteId = remoteId1,
                ModifiedAt = new DateTime (),
            });

            Assert.AreSame (model, Manager.GetByRemoteId<PlainModel> (remoteId1));
            Assert.IsNull (Manager.GetByRemoteId<PlainModel> (remoteId2));

            model.Merge (new PlainModel () {
                RemoteId = remoteId2,
                ModifiedAt = DateTime.UtcNow,
            });

            Assert.AreEqual (remoteId2, model.RemoteId, "Model remote id was not updated by merge.");
            Assert.IsNull (Manager.GetByRemoteId<PlainModel> (remoteId1), "Manager still returns model by old remote id.");
            Assert.AreSame (model, Manager.GetByRemoteId<PlainModel> (remoteId2), "Unable to find model by remote id after merge.");
        }
    }
}
