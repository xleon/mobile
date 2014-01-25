using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelTest
    {
        private class PlainModel : Model
        {
        }

        [TestFixtureSetUp]
        public void Init ()
        {
            ServiceContainer.Register<MessageBus> ();
        }

        [TestFixtureTearDown]
        public void Cleanup ()
        {
            ServiceContainer.Clear ();
        }

        [SetUp]
        public void SetUp ()
        {
            ServiceContainer.AddScope ();
        }

        [TearDown]
        public void TearDown ()
        {
            ServiceContainer.RemoveScope ();
        }

        [Test]
        public void TestDefaults ()
        {
            var model = new PlainModel ();

            Assert.IsNull (model.Id, "Id must be null");
            Assert.IsNull (model.RemoteId, "RemoteId must be null");
            Assert.IsFalse (model.IsShared, "IsShared must be false");
            Assert.IsFalse (model.IsPersisted, "IsPersisted must be false");
            Assert.That (model.ModifiedAt, Is.EqualTo (DateTime.UtcNow).Within (1).Seconds, "ModifiedAt must be the time of model creation");
            Assert.IsFalse (model.IsDirty, "IsDirty must be false");
            Assert.IsFalse (model.IsMerging, "IsMerging must be false");
            Assert.IsNull (model.DeletedAt, "DeletedAt must be null");
            Assert.IsNull (model.RemoteDeletedAt, "RemoteDeletedAt must be null");
            Assert.IsEmpty (model.Errors, "Errors must be empty");
            Assert.IsTrue (model.IsValid, "IsValid must be true");
        }

        private MessageBus MessageBus {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }

        [Test]
        public void TestMakeShared ()
        {
            // Verify the ModelChangedMessage send count
            var messageCount = 0;
            MessageBus.Subscribe<ModelChangedMessage> ((msg) => {
                messageCount++;
            });

            var model = new PlainModel ();
            model.PropertyChanged += (sender, e) => {
                if (e.PropertyName == PlainModel.PropertyIsShared) {
                    // Check that model is present in cache
                    Assert.That (Model.GetCached<PlainModel> (), Has.Exactly (1).SameAs (model), "The newly shared object should be present in cache already.");
                } else if (e.PropertyName == PlainModel.PropertyId) {
                    // Expect ID assignment
                } else {
                    Assert.Fail (String.Format ("Property '{0}' changed unexpectedly.", e.PropertyName));
                }
            };

            var shared = Model.Update (model);

            Assert.AreSame (model, shared, "Promoting to shared should return the initial model.");
            Assert.NotNull (model.Id, "Should have received a new unique Id.");
            Assert.AreEqual (messageCount, 1, "Received invalid number of OnModelChanged messages");
        }
    }
}
