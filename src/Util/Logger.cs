using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Unicorn.TestAdapter.Util
{
    public class Logger
    {
        private const string Prefix = "[Unicorn TestAdapter] ";

        private readonly IMessageLogger _messageLogger;

        internal Logger(IMessageLogger messageLogger)
        {
            _messageLogger = messageLogger;
        }

        internal void Info(string message) =>
            _messageLogger?.SendMessage(TestMessageLevel.Informational, Prefix + message);

        internal void Info(string message, params object[] parameters) =>
            Info(string.Format(message, parameters));

        internal void Warn(string message) =>
            _messageLogger?.SendMessage(TestMessageLevel.Warning, Prefix + message);

        internal void Warn(string message, params object[] parameters) =>
            Warn(string.Format(message, parameters));

        internal void Error(string message) =>
            _messageLogger?.SendMessage(TestMessageLevel.Error, Prefix + message);
    }
}
