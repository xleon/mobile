using NUnit.Framework;
using UITests.UIHelpers;
using Xamarin.UITest;

namespace UITests
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public abstract class TestFixtureBase
    {
        protected IApp app { get; private set; }
        protected Platform platform { get; }
        protected UI ui { get; } = new UI();

        public TestFixtureBase(Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            app = AppInitializer.StartApp(platform);
        }

    }
}