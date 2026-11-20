using System.Collections.Generic;
using NUnit.Framework;

namespace Gnurt.Tests
{
    public class ObservableTests
    {
        [Test]
        public void Changed_fires_only_when_value_changes()
        {
            var value = new Observable<int>(1);
            int calls = 0;
            value.Changed += _ => calls++;

            value.Value = 1;
            value.Value = 2;
            value.Value = 2;

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Subscribe_gets_current_value_then_changes()
        {
            var value = new Observable<string>("a");
            var seen = new List<string>();

            var sub = value.Subscribe(seen.Add);
            value.Value = "b";
            sub.Dispose();
            value.Value = "c";

            CollectionAssert.AreEqual(new[] { "a", "b" }, seen);
        }
    }
}
