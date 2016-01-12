using System;
using NUnit.Framework;
using Toggl.Phoebe;

namespace Toggl.Phoebe.Tests.Misc
{
    public class ExceptionTest : Test
    {
        [Test]
        public void TestExceptionExtensions ()
        {
            Exception nullException = null;
            var normalException = new Exception ();
            var networkException = new System.Net.WebException ();
            var nestedNetworkException = new Exception ("", new System.Net.Sockets.SocketException ());
            var nestedNonNetworkException = new Exception ("", new Exception ());

            Assert.IsFalse (nullException.IsNetworkFailure ());
            Assert.IsFalse (normalException.IsNetworkFailure ());
            Assert.IsTrue (networkException.IsNetworkFailure ());
            Assert.IsTrue (nestedNetworkException.IsNetworkFailure ());
            Assert.IsFalse (nestedNonNetworkException.IsNetworkFailure ());
        }
    }
}

