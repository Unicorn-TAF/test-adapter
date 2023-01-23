#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Provides ability to run unicorn tests in dedicated AppDomain.
    /// </summary>
    public class AppDomainRunner : MarshalByRefObject
    {
        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig)
        {
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            string appConfigFile = Path.Combine(assemblyDirectory, "app.config");
            AppDomainSetup domainSetup = new AppDomainSetup();

            if (File.Exists(appConfigFile))
            {
                domainSetup.SetConfigurationBytes(File.ReadAllBytes(appConfigFile));
            };

            AppDomain unicornDomain = AppDomain.CreateDomain(
                "Unicorn.TestAdapter Runner AppDomain",
                AppDomain.CurrentDomain.Evidence,
                domainSetup);

            try
            {
                string pathToDll = Assembly.GetExecutingAssembly().Location;

                AppDomainRunner runner = (AppDomainRunner)unicornDomain
                    .CreateInstanceFromAndUnwrap(pathToDll, typeof(AppDomainRunner).FullName);

                Environment.CurrentDirectory = Path.GetDirectoryName(assemblyPath);
                return runner.RunTests(assemblyPath, testsMasks, unicornConfig);
            }
            finally
            {
                AppDomain.Unload(unicornDomain);
            }
        }

        private LaunchOutcome RunTests(string assemblyPath, string[] testsMasks, string unicornConfig)
        {
            if (!string.IsNullOrEmpty(unicornConfig))
            {
                //string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                //string configPath = Path.Combine(assemblyDirectory, unicornConfig);
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