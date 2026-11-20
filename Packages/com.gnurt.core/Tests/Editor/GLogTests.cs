using NUnit.Framework;

namespace Gnurt.Tests
{
    public class GLogTests
    {
        [Test]
        public void Skips_lines_below_level()
        {
            using var log = new LogCapture(LogLevel.Warning);
            var logger = GLog.Tag("Test");

            logger.Info("info");
            logger.Warning("warn");
            logger.Error("error");

            Assert.AreEqual(2, log.Lines.Count);
            StringAssert.Contains("[Gnurt][Test] warn", log.Lines[0].text);
        }

        [Test]
        public void Muted_tag_prints_nothing_but_still_reports_errors()
        {
            using var log = new LogCapture();
            string reported = null;
            void OnError(string tag, string message) => reported = message;

            GLog.ErrorLogged += OnError;
            GLog.SetMuted("Quiet", true);
            try
            {
                GLog.Tag("Quiet").Error("boom");
            }
            finally
            {
                GLog.SetMuted("Quiet", false);
                GLog.ErrorLogged -= OnError;
            }

            Assert.AreEqual(0, log.Lines.Count);
            Assert.AreEqual("boom", reported);
        }

        [Test]
        public void None_level_prints_nothing()
        {
            using var log = new LogCapture(LogLevel.None);
            GLog.Tag("Test").Error("x");
            Assert.AreEqual(0, log.Lines.Count);
        }
    }
}
