using System;
using System.Collections.Generic;
using NUnit.Framework;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;

namespace Toggl.Phoebe.Tests.Data
{
    public class DataTest : Test
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [Test]
        public void TestPublicInstancePropertiesEqual()
        {
            var id = Guid.NewGuid ();
            var at = DateTime.UtcNow;
            var tagId1 = Guid.NewGuid ();
            var tagId2 = Guid.NewGuid ();

            var te1 = TimeEntryData.Create (x => {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.TagIds = new List<Guid> { tagId1, tagId2 };
            });

            var te2 = TimeEntryData.Create (x => {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.TagIds = new List<Guid> { tagId1, tagId2 };
            });

            var te3 = TimeEntryData.Create (x => {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "TEST";
                x.DeletedAt = DateTime.Today;
                x.TagIds = new List<Guid> { tagId1, tagId2 };
            });

            var te4 = TimeEntryData.Create (x => {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today.AddDays (1);
                x.TagIds = new List<Guid> { tagId1, tagId2 };
            });

            var te5 = TimeEntryData.Create (x => {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.TagIds = new List<Guid> { tagId1 };
            });

            Assert.That (te1.PublicInstancePropertiesEqual (te2), Is.True);
            Assert.That (te1.PublicInstancePropertiesEqual (te3), Is.False);
            Assert.That (te1.PublicInstancePropertiesEqual (te3, "Description"), Is.True);
            Assert.That (te1.PublicInstancePropertiesEqual (te4), Is.False);
            Assert.That (te1.PublicInstancePropertiesEqual (te4, "DeletedAt"), Is.True);
            Assert.That (te1.PublicInstancePropertiesEqual (te5), Is.False);
            Assert.That (te1.PublicInstancePropertiesEqual (te5, "TagIds"), Is.True);
        }
    }
}
