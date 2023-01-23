using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Core.Engine;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class UnicornTestExecutor : ITestExecutor
    {
        internal const string ExecutorUriString = "executor://UnicornTestExecutor/v2";

        private const string Prefix = "Unicorn Adapter: ";

        private const string RunStart = Prefix + "test run starting";
        private const string RunComplete = Prefix + "test run complete";
        private const string RunInitFailed = Prefix + "test run initialization failed";
        private const string RunnerError = Prefix + "runner error";
        private const string NonVsRunDisabled = Prefix + "only run from Visual Studio is supported, exiting...";

        internal static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (string.IsNullOrEmpty(runContext.SolutionDirectory))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, NonVsRunDisabled);
                return;
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunStart);

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                try
                {
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    RunTests(newSource, new string[0], runContext, frameworkHandle);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (string.IsNullOrEmpty(runContext.SolutionDirectory))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Warning, NonVsRunDisabled);
                return;
            }

            var sources = tests.Select(t => t.Source).Distinct();

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunStart);

            var runDir = ExecutorUtilities.PrepareRunDirectory(runContext.SolutionDirectory);
            ExecutorUtilities.CopyDeploymentItems(runContext, runDir, frameworkHandle);

            foreach (var source in sources)
            {
                ExecutorUtilities.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);
                var masks = tests.Select(t => t.FullyQualifiedName).ToArray();

                try
                {
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    LaunchOutcome outcome = RunTests(newSource, masks, runContext, frameworkHandle);
                    ProcessLaunchOutcome(outcome, tests, frameworkHandle);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, 
                        RunnerError + Environment.NewLine + ex);

                    foreach (TestCase test in tests)
                    {
                        ExecutorUtilities.SkipTest(test, ex.ToString(), frameworkHandle);
                    }
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, RunComplete);
        }

        private void ProcessLaunchOutcome(LaunchOutcome outcome, IEnumerable<TestCase> tests, IFrameworkHandle fwHandle)
        {
            if (!outcome.RunInitialized)
            {
                fwHandle.SendMessage(
                    TestMessageLevel.Error,
                    RunInitFailed + Environment.NewLine + outcome.RunnerException);

                foreach (TestCase test in tests)
                {
                    ExecutorUtilities.SkipTest(test, "Assembly initialization failed.\n" + outcome.RunnerException.ToString(), fwHandle);
                }
            }
            else
            {
                foreach (TestCase test in tests)
                {
                    var outcomes = outcome.SuitesOutcomes
                        .SelectMany(so => so.TestsOutcomes)
                        .Where(to => to.FullMethodName.Equals(test.FullyQualifiedName));

                    if (outcomes.Any())
                    {
                        foreach (var outcomeToRecord in outcomes)
                        {
                            var testResult = ExecutorUtilities.GetTestResultFromOutcome(outcomeToRecord, test);
                            fwHandle.RecordResult(testResult);
                        }
                    }
                    else
                    {
                        ExecutorUtilities.SkipTest(test, "Test was not executed, possibly it's disabled", fwHandle);
                    }
                }
            }
        }

        public void Cancel() 
        { 
        }

        private LaunchOutcome RunTests(
            string assemblyPath, string[] testsMasks, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            string unicornConfig = XDocument.Parse(runContext.RunSettings.SettingsXml)
                .Element("RunSettings")
                .Element("TestRunParameters")?
                .Elements("Parameter")
                .FirstOrDefault(e => e.Attribute("name").Value.Equals("unicornConfig"))?
                .Attribute("value").Value;

            if (!string.IsNullOrEmpty(unicornConfig))
            {
                frameworkHandle.SendMessage(
                    TestMessageLevel.Informational,
                    "Loading unicorn configuration file: " + unicornConfig);
            }

#if NET || NETCOREAPP
            return LoadContextRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#else
            return AppDomainRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#endif
        }
    }
}
