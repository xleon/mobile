using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Tests.Data.Models
{
    [TestFixture]
    public class ProjectModelTest : ModelTest<ProjectModel>
    {
        [Test]
        public void TestColorEnforcing()
        {
            var project = new ProjectModel(new ProjectData()
            {
                Id = Guid.NewGuid(),
                Color = 12345,
            });

            // Make sure that we return the underlying color
            Assert.AreEqual(12345, project.Color);
            Assert.That(() => project.GetHexColor(), Throws.Nothing);

            // And that we enforce to the colors we know of
            project.Color = 54321;
            Assert.AreNotEqual(54321, project.Color);
            Assert.AreEqual(54321 % ProjectModel.HexColors.Length, project.Color);
            Assert.That(() => project.GetHexColor(), Throws.Nothing);
        }
    }
}
