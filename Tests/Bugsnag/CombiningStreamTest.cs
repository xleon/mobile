using System;
using NUnit.Framework;
using System.IO;
using System.Text;
using Toggl.Phoebe.Bugsnag.IO;

namespace Toggl.Phoebe.Tests.Bugsnag
{
    [TestFixture]
    public class CombiningStreamTest : Test
    {
        private Stream GetStream (string data)
        {
            return new MemoryStream (Encoding.UTF8.GetBytes (data));
        }

        [Test]
        public void TestNoCombining ()
        {
            var stream = new CombiningStream (" & ");
            stream.Add (GetStream ("test"));
            Assert.AreEqual (4, stream.Length);

            var buf = new byte[100];
            var read = stream.Read (buf, 0, buf.Length);
            Assert.AreEqual (stream.Length, read);

            var data = Encoding.UTF8.GetString (buf, 0, read);
            Assert.AreEqual ("test", data);
        }

        [Test]
        public void TestSingleCombining ()
        {
            var stream = new CombiningStream (" bar ");
            stream.Add (GetStream ("foo"));
            stream.Add (GetStream ("baz"));
            Assert.AreEqual (11, stream.Length);

            var buf = new byte[100];
            var read = stream.Read (buf, 0, buf.Length);
            Assert.AreEqual (stream.Length, read);

            var data = Encoding.UTF8.GetString (buf, 0, read);
            Assert.AreEqual ("foo bar baz", data);
        }

        [Test]
        public void TestEmpty ()
        {
            var stream = new CombiningStream ();
            Assert.AreEqual (0, stream.Length);

            var buf = new byte[100];
            var read = stream.Read (buf, 0, buf.Length);
            Assert.AreEqual (0, read);
        }

        [Test]
        public void TestMultipleCombining ()
        {
            var subStream = new CombiningStream (", ");
            subStream.Add (GetStream ("{\"id\": 1}"));
            subStream.Add (GetStream ("{\"id\": 2}"));
            subStream.Add (GetStream ("{\"id\": 3}"));
            subStream.Add (GetStream ("{\"id\": 4}"));

            var stream = new CombiningStream ();
            stream.Add (GetStream ("{\"events\":["));
            stream.Add (subStream);
            stream.Add (GetStream ("]}"));

            Assert.AreEqual (13 + 4 * 9 + 3 * 2, stream.Length);

            var buf = new byte[stream.Length + 100];
            var read = stream.Read (buf, 0, buf.Length);
            Assert.AreEqual (stream.Length, read);

            var data = Encoding.UTF8.GetString (buf, 0, read);
            Assert.AreEqual ("{\"events\":[{\"id\": 1}, {\"id\": 2}, {\"id\": 3}, {\"id\": 4}]}", data);
        }
    }
}
