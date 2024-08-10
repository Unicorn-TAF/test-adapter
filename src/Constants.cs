using System;

namespace Unicorn.TestAdapter
{
    internal class Constants
    {
        internal const string ExecutorUriString = "executor://UnicornTestExecutor/v3";

        internal const string RunStart = "Test run starting";
        internal const string RunComplete = "Test run complete";
        internal const string RunInitFailed = "Run initialization failed";
        internal const string RunnerError = "Runner error";
        internal const string RunFromContainer = "Running tests from container ";
        internal const string DiscoveryStarted = "Test discovery starting";
        internal const string DiscoveryComplete = "Test discovery complete";

        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
    }
}
