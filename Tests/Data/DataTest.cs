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
            var id = Guid.NewGuid();
            var at = DateTime.UtcNow;
            var tag1 = "tag1";
            var tag2 = "tag2";

            var te1 = TimeEntryData.Create(x =>
            {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.Tags = new List<string> { tag1, tag2 };
            });

            var te2 = TimeEntryData.Create(x =>
            {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.Tags = new List<string> { tag1, tag2 };
            });

            var te3 = TimeEntryData.Create(x =>
            {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "TEST";
                x.DeletedAt = DateTime.Today;
                x.Tags = new List<string> { tag1, tag2 };
            });

            var te4 = TimeEntryData.Create(x =>
            {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today.AddDays(1);
                x.Tags = new List<string> { tag1, tag2 };
            });

            var te5 = TimeEntryData.Create(x =>
            {
                x.Id = id;
                x.ModifiedAt = at;
                x.Description = "Test";
                x.DeletedAt = DateTime.Today;
                x.Tags = new List<string> { tag1 };
            });

            Assert.That(te1.PublicInstancePropertiesEqual(te2), Is.True);
            Assert.That(te1.PublicInstancePropertiesEqual(te3), Is.False);
            Assert.That(te1.PublicInstancePropertiesEqual(te3, "Description"), Is.True);
            Assert.That(te1.PublicInstancePropertiesEqual(te4), Is.False);
            Assert.That(te1.PublicInstancePropertiesEqual(te4, "DeletedAt"), Is.True);
            Assert.That(te1.PublicInstancePropertiesEqual(te5), Is.False);
            Assert.That(te1.PublicInstancePropertiesEqual(te5, "Tags"), Is.True);
        }
    }
}
