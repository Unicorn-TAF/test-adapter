using System;

namespace Unicorn.TestAdapter
{
    internal static class Constants
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

        internal const string CategoryTrait = "Category";
        internal const string TagTrait = "Tag";
        internal const string DisabledTrait = "Disabled";

        internal const string DisplayNameString = "DisplayName";
        internal const string FullyQualifiedNameString = "FullyQualifiedName";
    }
}
