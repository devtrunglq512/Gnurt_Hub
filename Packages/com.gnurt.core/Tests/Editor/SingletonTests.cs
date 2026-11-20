using NUnit.Framework;

namespace Gnurt.Tests
{
    public class SingletonTests
    {
        sealed class Counter : Singleton<Counter>
        {
            public int Value;
        }

        [Test]
        public void Instance_is_always_the_same_object()
        {
            Counter.Instance.Value = 5;

            Assert.AreSame(Counter.Instance, Counter.Instance);
            Assert.AreEqual(5, Counter.Instance.Value);
        }
    }
}
