using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_AIO
{
    public class Logger
    {
        public event LoggerHandler OnLog;

        internal StringBuilder _log;

        public Logger()
        {
            _log = new StringBuilder();
        }


        #region Helpers
        public void Log(byte kind, DateTime dateTime, string text)
        {
            OnLog?.Invoke(this, kind, dateTime, text);
            lock (_log)
            {
                _log.AppendFormat("[{0}] - {1} - {2}\r\n", LogKind.LogKindToString(kind).ToUpperInvariant(), dateTime.ToLocalTime().ToString(), text);
            }
        }

        private void Log(byte kind, string text)
        {
            Log(kind, DateTime.Now, text);
        }

        public string ExportLogs(bool clear)
        {
            lock (_log)
            {
                string s = _log.ToString();

                if (clear) _log = new StringBuilder();

                return s;
            }
        }
        #endregion




        #region Error
        public void LogError(string text, params object[] args)
        {
            LogError(string.Format(text, args));
        }

        public void LogError(string text)
        {
            Log(LogKind.Error, text);
        }
        #endregion

        #region Success
        public void LogSuccess(string text, params object[] args)
        {
            LogSuccess(string.Format(text, args));
        }


        public void LogSuccess(string text)
        {
            Log(LogKind.Success, text);
        }
        #endregion

        #region Warning
        public void LogWarning(string text, params object[] args)
        {
            LogWarning(string.Format(text, args));
        }


        public void LogWarning(string text)
        {
            Log(LogKind.Warning, text);
        }
        #endregion

        #region Information
        public void LogInformation(string text, params object[] args)
        {
            LogInformation(string.Format(text, args));
        }

        public void LogInformation(string text)
        {
            Log(LogKind.Information, text);
        }
        #endregion


    }

    public static class LogKind
    {
        public static byte Success = 0;
        public static byte Information = 1;
        public static byte Warning = 2;
        public static byte Error = 3;

        public static string LogKindToString(byte kind)
        {
            switch (kind)
            {
                case 0:
                    return "success";
                case 1: return "information";
                case 2: return "warning";
                case 3: return "error";
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public delegate void LoggerHandler(Logger sender, byte kind, DateTime dateTime, string text);
}
