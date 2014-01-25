using System;
using NUnit.Framework;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelTest
    {
        private class PlainModel : Model
        {
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
    }
}
