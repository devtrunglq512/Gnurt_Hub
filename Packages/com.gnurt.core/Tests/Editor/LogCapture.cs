using System;
using System.Collections.Generic;

namespace Gnurt.Tests
{
    /// <summary>Ghi lại log thay vì in ra Console, để test kiểm tra được. Dispose() để trả lại như cũ.</summary>
    sealed class LogCapture : IDisposable
    {
        public readonly List<(LogLevel level, string text)> Lines = new List<(LogLevel, string)>();

        readonly Action<LogLevel, string, UnityEngine.Object> _previousOutput;
        readonly LogLevel _previousLevel;

        public LogCapture(LogLevel level = LogLevel.Verbose)
        {
            _previousOutput = GLog.Output;
            _previousLevel = GLog.Level;
            GLog.Output = (lineLevel, text, _) => Lines.Add((lineLevel, text));
            GLog.Level = level;
        }

        public int Count(LogLevel level) => Lines.FindAll(line => line.level == level).Count;

        public void Dispose()
        {
            GLog.Output = _previousOutput;
            GLog.Level = _previousLevel;
        }
    }
}
