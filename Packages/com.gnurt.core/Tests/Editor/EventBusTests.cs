using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Gnurt.Tests
{
    public class EventBusTests
    {
        readonly struct Ping
        {
            public readonly int Value;
            public Ping(int value) => Value = value;
        }

        readonly struct Other { }

        [SetUp]
        public void SetUp() => EventBus.ClearAll();

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        [Test]
        public void Handlers_are_called_in_subscribe_order()
        {
            var calls = new List<string>();
            EventBus.Subscribe<Ping>(e => calls.Add("a" + e.Value));
            EventBus.Subscribe<Ping>(e => calls.Add("b" + e.Value));

            EventBus.Publish(new Ping(1));

            CollectionAssert.AreEqual(new[] { "a1", "b1" }, calls);
        }

        [Test]
        public void Only_handlers_of_that_event_type_are_called()
        {
            int calls = 0;
            EventBus.Subscribe<Other>(_ => calls++);

            EventBus.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Dispose_stops_receiving()
        {
            int calls = 0;
            var sub = EventBus.Subscribe<Ping>(_ => calls++);

            sub.Dispose();
            EventBus.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, EventBus.CountSubscribers<Ping>());
        }

        [Test]
        public void Throwing_handler_does_not_stop_others()
        {
            using var log = new LogCapture();
            bool secondCalled = false;
            EventBus.Subscribe<Ping>(_ => throw new Exception("x"));
            EventBus.Subscribe<Ping>(_ => secondCalled = true);

            EventBus.Publish(new Ping(1));

            Assert.IsTrue(secondCalled);
            Assert.AreEqual(1, log.Count(LogLevel.Error));
        }

        [Test]
        public void Unsubscribe_during_publish_skips_that_handler()
        {
            IDisposable second = null;
            int secondCalls = 0;
            EventBus.Subscribe<Ping>(_ => second.Dispose());
            second = EventBus.Subscribe<Ping>(_ => secondCalls++);

            EventBus.Publish(new Ping(1));

            Assert.AreEqual(0, secondCalls);
        }

        [Test]
        public void Subscribe_during_publish_waits_for_next_event()
        {
            int lateCalls = 0;
            EventBus.Subscribe<Ping>(_ => EventBus.Subscribe<Ping>(__ => lateCalls++));

            EventBus.Publish(new Ping(1));
            Assert.AreEqual(0, lateCalls);

            EventBus.Publish(new Ping(2));
            Assert.AreEqual(1, lateCalls);
        }
    }
}
