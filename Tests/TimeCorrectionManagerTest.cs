using System;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    [TestFixture]
    public class TimeCorrectionManagerTest : Test
    {
        public TimeCorrectionManager TimeManager
        {
            get { return ServiceContainer.Resolve<TimeCorrectionManager> (); }
        }

        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual(TimeSpan.Zero, TimeManager.Correction);
        }

        [Test]
        public void TestSingleCorrection()
        {
            var correction = TimeSpan.FromSeconds(1);
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = correction.Ticks,
            });
            Assert.AreEqual(correction, TimeManager.Correction);
        }

        [Test]
        public void TestMultipleCorrection()
        {
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = TimeSpan.FromSeconds(0.1).Ticks,
            });
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = TimeSpan.FromSeconds(0.2).Ticks,
            });
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = TimeSpan.FromSeconds(0.7).Ticks,
            });
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = TimeSpan.FromSeconds(0.2).Ticks,
            });
            TimeManager.AddMeasurement(new TimeCorrectionData()
            {
                MeasuredAt = DateTime.UtcNow,
                Correction = TimeSpan.FromSeconds(0.2).Ticks,
            });
            Assert.AreEqual(TimeSpan.FromSeconds(0.2), TimeManager.Correction);
        }

        [Test]
        public void TestManyCorrection()
        {
            for (var i = 0; i < 100; i++)
            {
                TimeManager.AddMeasurement(new TimeCorrectionData()
                {
                    MeasuredAt = DateTime.UtcNow,
                    Correction = TimeSpan.FromSeconds(1).Ticks,
                });
            }
            Assert.AreEqual(TimeSpan.FromSeconds(1), TimeManager.Correction);

            for (var i = 0; i < 20; i++)
            {
                TimeManager.AddMeasurement(new TimeCorrectionData()
                {
                    MeasuredAt = DateTime.UtcNow,
                    Correction = 0,
                });
            }
            Assert.AreEqual(TimeSpan.Zero, TimeManager.Correction);
        }
    }
}
