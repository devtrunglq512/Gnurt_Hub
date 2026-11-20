using System;
using System.Threading;
using NUnit.Framework;

namespace Gnurt.Tests
{
    public class GnurtServiceTests
    {
        // Service giả: test tự quyết khi nào xong. Mặc định không có timeout.
        sealed class FakeService : GnurtService
        {
            public float Timeout;
            public Action<FakeService> OnInit;

            public override float TimeoutSeconds => Timeout;
            protected override void OnInitialize() => OnInit?.Invoke(this);

            public void Finish() => Complete();
            public void Break(string error) => Fail(error);
        }

        [Test]
        public void Complete_makes_service_ready_and_calls_waiters()
        {
            var service = new FakeService();
            bool ready = false;
            service.WhenReady(() => ready = true);

            service.Initialize();
            Assert.AreEqual(ServiceState.Initializing, service.State);

            service.Finish();
            Assert.AreEqual(ServiceState.Ready, service.State);
            Assert.IsTrue(ready);
        }

        [Test]
        public void Late_waiter_is_called_immediately()
        {
            var service = new FakeService { OnInit = s => s.Finish() };
            service.Initialize();

            bool ready = false;
            service.WhenReady(() => ready = true);

            Assert.IsTrue(ready);
        }

        [Test]
        public void Fail_calls_onFailed_with_reason()
        {
            using var log = new LogCapture();
            var service = new FakeService();
            string reason = null;
            service.WhenReady(() => { }, r => reason = r);

            service.Initialize();
            service.Break("no network");

            Assert.AreEqual(ServiceState.Failed, service.State);
            Assert.AreEqual("no network", reason);
        }

        [Test]
        public void Exception_in_OnInitialize_fails_the_service()
        {
            using var log = new LogCapture();
            var service = new FakeService { OnInit = _ => throw new InvalidOperationException("boom") };

            service.Initialize();

            Assert.AreEqual(ServiceState.Failed, service.State);
            Assert.AreEqual("boom", service.Error);
        }

        [Test]
        public void Timeout_fails_then_late_complete_turns_ready_without_second_callback()
        {
            using var log = new LogCapture();
            var service = new FakeService { Timeout = 0.01f };
            int readyCalls = 0, failedCalls = 0;
            service.WhenReady(() => readyCalls++, _ => failedCalls++);

            service.Initialize();
            Thread.Sleep(200);  // chờ timeout chạy ở thread nền
            MainThread.Pump();  // đưa kết quả timeout về main thread
            Assert.AreEqual(ServiceState.Failed, service.State);

            service.Finish();
            Assert.AreEqual(ServiceState.Ready, service.State);
            Assert.AreEqual(0, readyCalls);
            Assert.AreEqual(1, failedCalls);
        }

        [Test]
        public void Initialize_runs_only_once()
        {
            int inits = 0;
            var service = new FakeService { OnInit = _ => inits++ };

            service.Initialize();
            service.Initialize();

            Assert.AreEqual(1, inits);
        }
    }
}
