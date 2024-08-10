#if NET || NETCOREAPP
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unicorn.Taf.Api;
using Unicorn.Taf.Core;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    /// <summary>
    /// Provides ability to run unicorn tests in dedicated AppDomain.
    /// </summary>
    public class LoadContextRunner : ITestRunner
    {
        private readonly Assembly _testsAssembly;
        private readonly string[] _testsMasks;
        private readonly string _unicornConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadContextRunner"/> class based on assembly and tests masks.
        /// </summary>
        /// <param name="assembly">assembly to run on</param>
        /// <param name="testsMasks">masks of tests to be run</param>
        public LoadContextRunner(Assembly assembly, string[] testsMasks, string unicornConfig)
        {
            _testsAssembly = assembly;
            _testsMasks = testsMasks;
            _unicornConfig = unicornConfig;
        }

        /// <summary>
        /// Runs tests from specified assembly and specified configuration.
        /// </summary>
        /// <returns>outcome of tests run</returns>
        public IOutcome RunTests()
        {
            if (!string.IsNullOrEmpty(_unicornConfig))
            {
                Config.FillFromFile(_unicornConfig);
            }

            Config.SetTestsMasks(_testsMasks);

            var runner = new TestsRunner(_testsAssembly, false);
            runner.RunTests();
            return runner.Outcome;
        }

        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig)
        {
            string contextDirectory = Path.GetDirectoryName(assemblyPath);
            UnicornAssemblyLoadContext runnerContext = new UnicornAssemblyLoadContext(contextDirectory);
            runnerContext.Initialize(typeof(ITestRunner));

            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                Assembly testAssembly = runnerContext.GetAssembly(assemblyName);

                Type runnerType = runnerContext.GetAssemblyContainingType(typeof(LoadContextRunner))
                    .GetTypes()
                    .First(t => t.Name.Equals(typeof(LoadContextRunner).Name));

                ITestRunner runner = Activator.CreateInstance(runnerType, testAssembly, testsMasks, unicornConfig) as ITestRunner;
                
                Environment.CurrentDirectory = Path.GetDirectoryName(assemblyPath);
                IOutcome ioutcome = runner.RunTests();

                // Outcome transition between load contexts.
                byte[] bytes = LoadContextSerialization.Serialize(ioutcome);
                return LoadContextSerialization.Deserialize<LaunchOutcome>(bytes);
            }
            finally
            {
                runnerContext.Unload();
            }
        }
    }
}
#endif