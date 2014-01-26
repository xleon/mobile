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
    }
}
