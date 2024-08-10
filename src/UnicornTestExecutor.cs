using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing;
using Unicorn.TestAdapter.Util;
using UOutcome = Unicorn.Taf.Core.Testing.TestOutcome;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class UnicornTestExecutor : ITestExecutor
    {
        private Logger logger;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger = new Logger(frameworkHandle);
            logger.Info(Constants.RunStart + " (sources)");

            string runDir = FileUtils.PrepareRunDirectory(runContext, logger);

            foreach (var source in sources)
            {
                logger.Info(Constants.RunFromContainer + source);
                FileUtils.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                try
                {
                    string newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    LaunchOutcome outcome = RunTests(newSource, new string[0], runContext, frameworkHandle);
                    ProcessLaunchOutcome(outcome, frameworkHandle, source);
                }
                catch (Exception ex)
                {
                    logger.Error(Constants.RunnerError + Environment.NewLine + ex);
                }
            }

            logger.Info(Constants.RunComplete);
        }


        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger = new Logger(frameworkHandle);
            logger.Info(Constants.RunStart + " (tests)");

            string runDir = FileUtils.PrepareRunDirectory(runContext, logger);

            var sources = tests.Select(t => t.Source).Distinct();

            foreach (var source in sources)
            {
                logger.Info(Constants.RunFromContainer + source);
                FileUtils.CopySourceFilesToRunDir(Path.GetDirectoryName(source), runDir);

                var masks = tests.Select(t => t.FullyQualifiedName).ToArray();

                try
                {
                    var newSource = source.Replace(Path.GetDirectoryName(source), runDir);
                    LaunchOutcome outcome = RunTests(newSource, masks, runContext, frameworkHandle);
                    ProcessLaunchOutcome(outcome, tests, frameworkHandle);
                }
                catch (Exception ex)
                {
                    logger.Error(Constants.RunnerError + Environment.NewLine + ex);

                    foreach (TestCase test in tests)
                    {
                        ExecutorUtils.SkipTest(test, ex.ToString(), frameworkHandle);
                    }
                }
            }

            logger.Info(Constants.RunComplete);
        }

        public void Cancel() 
        { 
        }

        private LaunchOutcome RunTests(
            string assemblyPath, string[] testsMasks, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            XElement runSettings = XDocument.Parse(runContext.RunSettings.SettingsXml).Element("RunSettings");

            string unicornConfig = runSettings.Element("UnicornAdapter").Element("ConfigFile")?.Value;

            if (string.IsNullOrEmpty(unicornConfig))
            {
                unicornConfig = runSettings.Element("TestRunParameters")?
                .Elements("Parameter")
                .FirstOrDefault(e => e.Attribute("name").Value.Equals("unicornConfig"))?
                .Attribute("value").Value;
            }

            if (!string.IsNullOrEmpty(unicornConfig))
            {
                logger.Info("Using configuration file: " + unicornConfig);
            }

#if NET || NETCOREAPP
            return LoadContextRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#else
            return AppDomainRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#endif
        }

        private void ProcessLaunchOutcome(LaunchOutcome outcome, IFrameworkHandle fwHandle, string source)
        {
            if (!outcome.RunInitialized)
            {
                logger.Error(Constants.RunInitFailed + Environment.NewLine + outcome.RunnerException);
            }
            else
            {
                List<TestCase> testCases = new List<TestCase>();

                foreach (SuiteOutcome suiteOutcome in outcome.SuitesOutcomes)
                {
                    foreach (UOutcome testOutcome in suiteOutcome.TestsOutcomes)
                    {
                        TestCase testCase = testCases.FirstOrDefault(tc => Equals(tc.FullyQualifiedName, testOutcome.FullMethodName));

                        if (testCase == null)
                        {
                            testCase = ExecutorUtils.GetTestCaseFrom(testOutcome, source);
                            testCases.Add(testCase);
                        }

                        TestResult testResult = ExecutorUtils.GetTestResultFromOutcome(testOutcome, testCase);
                        fwHandle.RecordResult(testResult);
                    }
                }
            }
        }

        private void ProcessLaunchOutcome(LaunchOutcome outcome, IEnumerable<TestCase> tests, IFrameworkHandle fwHandle)
        {
            if (!outcome.RunInitialized)
            {
                logger.Error(Constants.RunInitFailed + Environment.NewLine + outcome.RunnerException);

                foreach (TestCase test in tests)
                {
                    ExecutorUtils.SkipTest(test, Constants.RunInitFailed + Environment.NewLine + outcome.RunnerException.ToString(), fwHandle);
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
                            var testResult = ExecutorUtils.GetTestResultFromOutcome(outcomeToRecord, test);
                            fwHandle.RecordResult(testResult);
                        }
                    }
                    else
                    {
                        ExecutorUtils.SkipTest(test, "Test was not executed, possibly it's disabled", fwHandle);
                    }
                }
            }
        }
    }
}
