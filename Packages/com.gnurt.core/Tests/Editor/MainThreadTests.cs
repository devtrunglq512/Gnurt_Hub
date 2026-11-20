using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Gnurt.Tests
{
    public class MainThreadTests
    {
        [Test]
        public void Run_on_main_thread_executes_immediately()
        {
            bool ran = false;
            MainThread.Run(() => ran = true);
            Assert.IsTrue(ran);
        }

        [Test]
        public void Run_from_background_waits_for_pump()
        {
            bool ran = false;
            Task.Run(() => MainThread.Run(() => ran = true)).Wait();

            Assert.IsFalse(ran); // test đang giữ main thread nên việc vẫn còn chờ
            MainThread.Pump();
            Assert.IsTrue(ran);
        }

        [Test]
        public void Post_always_waits_for_pump()
        {
            bool ran = false;
            MainThread.Post(() => ran = true);

            Assert.IsFalse(ran);
            MainThread.Pump();
            Assert.IsTrue(ran);
        }

        [Test]
        public void Exception_does_not_stop_other_actions()
        {
            using var log = new LogCapture();
            bool ran = false;

            MainThread.Post(() => throw new InvalidOperationException("x"));
            MainThread.Post(() => ran = true);
            MainThread.Pump();

            Assert.IsTrue(ran);
            Assert.AreEqual(1, log.Count(LogLevel.Error));
        }
    }
}
