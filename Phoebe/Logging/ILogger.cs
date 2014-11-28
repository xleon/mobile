using System;

namespace Toggl.Phoebe.Logging
{
    public interface ILogger
    {
        void Debug (string tag, string message);
        void Debug (string tag, string message, object arg0);
        void Debug (string tag, string message, object arg0, object arg1);
        void Debug (string tag, string message, object arg0, object arg1, object arg2);
        void Debug (string tag, string message, params object[] args);
        void Debug (string tag, Exception exc, string message);
        void Debug (string tag, Exception exc, string message, object arg0);
        void Debug (string tag, Exception exc, string message, object arg0, object arg1);
        void Debug (string tag, Exception exc, string message, object arg0, object arg1, object arg2);
        void Debug (string tag, Exception exc, string message, params object[] args);

        void Info (string tag, string message);
        void Info (string tag, string message, object arg0);
        void Info (string tag, string message, object arg0, object arg1);
        void Info (string tag, string message, object arg0, object arg1, object arg2);
        void Info (string tag, string message, params object[] args);
        void Info (string tag, Exception exc, string message);
        void Info (string tag, Exception exc, string message, object arg0);
        void Info (string tag, Exception exc, string message, object arg0, object arg1);
        void Info (string tag, Exception exc, string message, object arg0, object arg1, object arg2);
        void Info (string tag, Exception exc, string message, params object[] args);

        void Warning (string tag, string message);
        void Warning (string tag, string message, object arg0);
        void Warning (string tag, string message, object arg0, object arg1);
        void Warning (string tag, string message, object arg0, object arg1, object arg2);
        void Warning (string tag, string message, params object[] args);
        void Warning (string tag, Exception exc, string message);
        void Warning (string tag, Exception exc, string message, object arg0);
        void Warning (string tag, Exception exc, string message, object arg0, object arg1);
        void Warning (string tag, Exception exc, string message, object arg0, object arg1, object arg2);
        void Warning (string tag, Exception exc, string message, params object[] args);

        void Error (string tag, string message);
        void Error (string tag, string message, object arg0);
        void Error (string tag, string message, object arg0, object arg1);
        void Error (string tag, string message, object arg0, object arg1, object arg2);
        void Error (string tag, string message, params object[] args);
        void Error (string tag, Exception exc, string message);
        void Error (string tag, Exception exc, string message, object arg0);
        void Error (string tag, Exception exc, string message, object arg0, object arg1);
        void Error (string tag, Exception exc, string message, object arg0, object arg1, object arg2);
        void Error (string tag, Exception exc, string message, params object[] args);
    }
}
