using NUnit.Framework;

namespace Gnurt.Tests
{
    public class ServicesTests
    {
        interface IGreeter
        {
            string Hello();
        }

        sealed class Greeter : IGreeter
        {
            public string Hello() => "hi";
        }

        sealed class SlowService : GnurtService
        {
            public override float TimeoutSeconds => 0;
            protected override void OnInitialize() { }
            public void Finish() => Complete();
        }

        [SetUp]
        public void SetUp() => Services.Clear();

        [TearDown]
        public void TearDown() => Services.Clear();

        [Test]
        public void Register_then_get_by_interface()
        {
            Services.Register<IGreeter>(new Greeter());

            Assert.AreEqual("hi", Services.Get<IGreeter>().Hello());
            Assert.IsNull(Services.Get<Greeter>()); // đăng ký theo IGreeter thì chỉ lấy được theo IGreeter
        }

        [Test]
        public void Duplicate_register_keeps_first_and_warns()
        {
            using var log = new LogCapture();
            var first = new Greeter();
            Services.Register<IGreeter>(first);

            var result = Services.Register<IGreeter>(new Greeter());

            Assert.AreSame(first, result);
            Assert.AreSame(first, Services.Get<IGreeter>());
            Assert.AreEqual(1, log.Count(LogLevel.Warning));
        }

        [Test]
        public void WhenReady_before_register_waits_for_register()
        {
            IGreeter received = null;
            Services.WhenReady<IGreeter>(g => received = g);
            Assert.IsNull(received);

            var greeter = new Greeter();
            Services.Register<IGreeter>(greeter);

            Assert.AreSame(greeter, received);
        }

        [Test]
        public void WhenReady_waits_for_GnurtService_to_be_ready()
        {
            var service = Services.Register(new SlowService());
            bool ready = false;
            Services.WhenReady<SlowService>(_ => ready = true);

            Services.InitializeAll();
            Assert.IsFalse(ready);

            service.Finish();
            Assert.IsTrue(ready);
        }
    }
}
