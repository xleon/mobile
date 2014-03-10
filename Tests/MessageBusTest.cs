using System;
using NUnit.Framework;

namespace Toggl.Phoebe.Tests
{
    public class MessageBusTest : Test
    {
        [Test]
        public void TestSendDuringDispatch ()
        {
            var i = 0;
            var locked = false;
            var s = MessageBus.Subscribe<TestMessage> ((msg) => {
                Assert.IsFalse (locked, "MessageBus dispatched message while old one was still being processed.");

                locked = true;
                if (i < 10) {
                    MessageBus.Send (new TestMessage (this));
                    i++;
                }
                locked = false;
            });

            MessageBus.Send (new TestMessage (this));
            MessageBus.Unsubscribe (s);
        }

        private class TestMessage : Message
        {
            public TestMessage (object sender) : base (sender)
            {
            }
        }
    }
}
