using System;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelLookupTest : Test
    {
        private class PlainModel : Model
        {
        }

        [Test]
        public void TestByIdFromStore ()
        {
            var id = Guid.NewGuid ();
            var storedModel = new PlainModel () {
                Id = id,
            };

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            Assert.AreSame (storedModel, Model.Get<PlainModel> (id));
        }

        [Test]
        public void TestByIdFromCache ()
        {
            var id = Guid.NewGuid ();
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            // TODO: Should instead just mock the cache
            var model = Model.Update (new PlainModel () {
                Id = id,
            });

            Assert.AreSame (model, Model.Get<PlainModel> (id));
        }

        [Test]
        public void TestByIdNotFound ()
        {
            var id = Guid.NewGuid ();
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.Get<PlainModel> (id) == storedModel &&
                store.Get (typeof(PlainModel), id) == storedModel
            ));

            Assert.IsNull (Model.Get<PlainModel> (id));
        }

        [Test]
        public void TestByRemoteIdFromStore ()
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

            Assert.AreSame (storedModel, Model.GetByRemoteId<PlainModel> (remoteId));
        }

        [Test]
        public void TestByRemoteIdFromCache ()
        {
            var remoteId = 2345;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            // TODO: Should instead just mock the cache
            var model = Model.Update (new PlainModel () {
                RemoteId = remoteId,
            });

            Assert.AreSame (model, Model.GetByRemoteId<PlainModel> (remoteId));
        }

        [Test]
        public void TestByRemoteIdNotFound ()
        {
            var remoteId = 3456;
            PlainModel storedModel = null;

            ServiceContainer.RegisterScoped (Mock.Of<IModelStore> (
                store => store.GetByRemoteId<PlainModel> (remoteId) == storedModel &&
                store.GetByRemoteId (typeof(PlainModel), remoteId) == storedModel
            ));

            Assert.IsNull (Model.GetByRemoteId<PlainModel> (remoteId));
        }
    }
}
