using System;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    public abstract class Test
    {
        [TestFixtureSetUp]
        public virtual void Init ()
        {
            ServiceContainer.Register<MessageBus> ();
        }

        [TestFixtureTearDown]
        public virtual void Cleanup ()
        {
            ServiceContainer.Clear ();
        }

        [SetUp]
        public virtual void SetUp ()
        {
            ServiceContainer.AddScope ();
            ServiceContainer.RegisterScoped (new ModelManager ());
        }

        [TearDown]
        public virtual void TearDown ()
        {
            ServiceContainer.RemoveScope ();
        }

        protected MessageBus MessageBus {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }
    }
}
