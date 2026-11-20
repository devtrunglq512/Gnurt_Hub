using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Gnurt
{
    /// <summary>Mức log. Số cố định để Remote Config chỉnh được bằng một con số.</summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        None = 5,
    }

    /// <summary>
    /// Log chung của mọi package Gnurt. Mỗi dòng có tag (tên module) và level.
    /// Dùng: <c>static readonly GLogger Log = GLog.Tag("Hub");</c> rồi <c>Log.Info("...")</c>.
    /// </summary>
    public static class GLog
    {
        static readonly HashSet<string> Muted = new HashSet<string>();
        static readonly object Lock = new object();

        /// <summary>Chỉ in dòng có level từ mức này trở lên.</summary>
        public static LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>
        /// Bắn mỗi khi có lỗi, kể cả khi tag bị tắt hay level là None.
        /// Dùng để chuyển lỗi sang Crashlytics. Tham số: tag, nội dung.
        /// </summary>
        public static event Action<string, string> ErrorLogged;

        /// <summary>Nơi in log thật. Test thay bằng hàm ghi lại để kiểm tra.</summary>
        internal static Action<LogLevel, string, Object> Output = WriteToConsole;

        /// <summary>Tạo logger gắn sẵn tag.</summary>
        public static GLogger Tag(string tag) => new GLogger(tag);

        /// <summary>Tắt hoặc bật lại log của một tag.</summary>
        public static void SetMuted(string tag, bool muted)
        {
            lock (Lock)
            {
                if (muted) Muted.Add(tag);
                else Muted.Remove(tag);
            }
        }

        public static bool IsMuted(string tag)
        {
            lock (Lock) return Muted.Contains(tag);
        }

        /// <summary>Dòng log ở level và tag này có được in không.</summary>
        public static bool IsEnabled(LogLevel level, string tag) =>
            level != LogLevel.None && level >= Level && !IsMuted(tag);

        internal static void Write(LogLevel level, string tag, string message, Object context)
        {
            if (level == LogLevel.Error)
                NotifyError(tag, message);

            if (IsEnabled(level, tag))
                Output(level, $"[Gnurt][{tag}] {message}", context);
        }

        static void NotifyError(string tag, string message)
        {
            try
            {
                ErrorLogged?.Invoke(tag, message);
            }
            catch (Exception e)
            {
                // Không dùng GLog ở đây để tránh vòng lặp lỗi.
                UnityEngine.Debug.LogException(e);
            }
        }

        static void WriteToConsole(LogLevel level, string text, Object context)
        {
            switch (level)
            {
                case LogLevel.Error: UnityEngine.Debug.LogError(text, context); break;
                case LogLevel.Warning: UnityEngine.Debug.LogWarning(text, context); break;
                default: UnityEngine.Debug.Log(text, context); break;
            }
        }
    }

    /// <summary>
    /// Logger gắn sẵn một tag. Tạo bằng <see cref="GLog.Tag"/>.
    /// Chuỗi log tốn công ghép thì kiểm tra <see cref="IsEnabled"/> trước.
    /// </summary>
    public readonly struct GLogger
    {
        public readonly string TagName;

        public GLogger(string tag) => TagName = tag;

        public bool IsEnabled(LogLevel level) => GLog.IsEnabled(level, TagName);

        public void Verbose(string message, Object context = null) => GLog.Write(LogLevel.Verbose, TagName, message, context);
        public void Debug(string message, Object context = null) => GLog.Write(LogLevel.Debug, TagName, message, context);
        public void Info(string message, Object context = null) => GLog.Write(LogLevel.Info, TagName, message, context);
        public void Warning(string message, Object context = null) => GLog.Write(LogLevel.Warning, TagName, message, context);
        public void Error(string message, Object context = null) => GLog.Write(LogLevel.Error, TagName, message, context);

        /// <summary>Lỗi kèm exception: in cả stack trace.</summary>
        public void Error(string message, Exception exception, Object context = null) =>
            GLog.Write(LogLevel.Error, TagName, $"{message}\n{exception}", context);
    }
}
