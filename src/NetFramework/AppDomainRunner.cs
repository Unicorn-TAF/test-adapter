#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter.NetFramework
{
    /// <summary>
    /// Provides ability to run unicorn tests in dedicated AppDomain.
    /// </summary>
    public class AppDomainRunner : MarshalByRefObject
    {
        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig)
        {
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            UnicornAppDomainIsolation<AppDomainRunner> runnerIsolation =
                new UnicornAppDomainIsolation<AppDomainRunner>(assemblyDirectory, "Unicorn.TestAdapter Runner AppDomain");

            return runnerIsolation.Instance.RunTests(assemblyPath, testsMasks, unicornConfig);
        }

        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks, string unicornConfig)
        {
            if (!string.IsNullOrEmpty(unicornConfig))
            {
                Config.FillFromFile(unicornConfig);
            }

            Config.SetTestsMasks(testsMasks);
            Assembly testAssembly = Assembly.LoadFrom(assemblyPath);
            var runner = new TestsRunner(testAssembly, false);
            runner.RunTests();
            return runner.Outcome;
        }
    }
}
#endif