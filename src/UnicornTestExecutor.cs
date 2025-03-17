using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Unicorn.Taf.Core.Engine;
using Unicorn.TestAdapter.Util;

namespace Unicorn.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class UnicornTestExecutor : ITestExecutor
    {
        private Logger logger;

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger = new Logger(frameworkHandle);
            logger.Info(Constants.RunStart);

            string runDir = FileUtils.PrepareRunDirectory(runContext, logger);

            foreach (string source in sources)
            {
                try
                {
                    TestCaseFilter filter = new TestCaseFilter(runContext, logger, source);
                    List<TestInfo> testsInfos = AdapterUtils.GetTestInfos(source);

                    // Collecting only test cases matching filters
                    IEnumerable<TestCase> testcases = testsInfos
                        .Select(testInfo => AdapterUtils.GetTestCaseFrom(testInfo, source))
                        .Where(testcase => filter.MatchTestCase(testcase));

                    logger.Info("Source {0}: found total {1} tests, {2} tests match run filter",
                        Path.GetFileName(source), testsInfos.Count, testcases.Count());

                    RunTestsForSource(testcases, runContext, frameworkHandle, source, runDir);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error executing tests from {source} source: {ex.Message}");
                }
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            logger = new Logger(frameworkHandle);
            logger.Info(Constants.RunStart);

            string runDir = FileUtils.PrepareRunDirectory(runContext, logger);
            var sources = tests.Select(t => t.Source).Distinct();

            foreach (var source in sources)
            {
                RunTestsForSource(tests, runContext, frameworkHandle, source, runDir);
            }
        }

        public void Cancel() 
        { 
        }

        public void RunTestsForSource(
            IEnumerable<TestCase> tests, 
            IRunContext runContext, 
            IFrameworkHandle frameworkHandle, 
            string source, 
            string runDir)
        {
            logger.Info(Constants.RunFromContainer + source);
            string sourceDirectory = Path.GetDirectoryName(source);

            if (string.IsNullOrEmpty(runDir))
            {
                runDir = sourceDirectory;
            }
            else
            {
                FileUtils.CopyBuildToRunDir(sourceDirectory, runDir);
            }

            string[] testsMasks = tests.Select(t => t.FullyQualifiedName).ToArray();

            try
            {
                string assemblyPath = source.Replace(sourceDirectory, runDir);
                string unicornConfig = AdapterUtils.GetUnicornConfigPath(runContext.RunSettings.SettingsXml);

                if (!string.IsNullOrEmpty(unicornConfig))
                {
                    logger.Info("Using configuration file: " + unicornConfig);
                }

                LaunchOutcome outcome = AdapterUtils.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);

                logger.Info(Constants.RunComplete);
                ProcessLaunchOutcome(outcome, tests, frameworkHandle);
            }
            catch (Exception ex)
            {
                logger.Error(Constants.RunnerError + Environment.NewLine + ex);

                foreach (TestCase test in tests)
                {
                    AdapterUtils.SkipTest(test, ex.ToString(), frameworkHandle);
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
                    AdapterUtils.SkipTest(test, Constants.RunInitFailed + Environment.NewLine + outcome.RunnerException.ToString(), fwHandle);
                }

                return;
            }

            foreach (TestCase test in tests)
            {
                var outcomes = outcome.SuitesOutcomes
                    .SelectMany(so => so.TestsOutcomes)
                    .Where(to => to.FullMethodName.Equals(test.FullyQualifiedName));

                if (outcomes.Any())
                {
                    foreach (var outcomeToRecord in outcomes)
                    {
                        var testResult = AdapterUtils.GetTestResultFromOutcome(outcomeToRecord, test, logger);
                        fwHandle.RecordResult(testResult);
                    }
                }
                else
                {
                    AdapterUtils.SkipTest(test, "Test was not executed, possibly it's disabled", fwHandle);
                }
            }
        }
    }
}
