using System;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class AttributeLookupCacheTest : Test
    {
        [Test]
        public void TestDontDirtyOnModelType ()
        {
            var cache = new AttributeLookupCache<DontDirtyAttribute> ();
            Assert.IsTrue (cache.HasAttribute (typeof(Model), Model.PropertyId));
            Assert.IsTrue (cache.HasAttribute (typeof(Model), Model.PropertyRemoteId));
            Assert.IsFalse (cache.HasAttribute (typeof(Model), Model.PropertyRemoteDeletedAt));
            Assert.IsFalse (cache.HasAttribute (typeof(Model), Model.PropertyDeletedAt));
            Assert.IsFalse (cache.HasAttribute (typeof(Model), Model.PropertyModifiedAt));
            Assert.IsTrue (cache.HasAttribute (typeof(Model), Model.PropertyIsMerging));
            Assert.IsTrue (cache.HasAttribute (typeof(Model), Model.PropertyIsPersisted));
            Assert.IsTrue (cache.HasAttribute (typeof(Model), Model.PropertyIsShared));
        }

        [Test]
        public void TestSqliteIgnoreOnTimeEntry ()
        {
            var cache = new AttributeLookupCache<SQLite.IgnoreAttribute> ();
            var entry = new TimeEntryModel ();
            Assert.IsTrue (cache.HasAttribute (entry, TimeEntryModel.PropertyWorkspace));
            Assert.IsFalse (cache.HasAttribute (entry, TimeEntryModel.PropertyStartTime));
        }
    }
}
