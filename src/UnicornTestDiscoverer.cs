using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.TestAdapter.Util;

namespace Unicorn.TestAdapter
{
    [Category("managed")]
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    public class UnicornTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, 
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            Logger loggerInstance = new Logger(logger);
            loggerInstance.Info(Constants.DiscoveryStarted);

            foreach (string source in sources)
            {
                try
                {
                    TestCaseFilter filter = new TestCaseFilter(discoveryContext, loggerInstance);
                    List<TestInfo> testsInfos = AdapterUtils.GetTestInfos(source);

                    // Collecting only test cases matching filters
                    IEnumerable<TestCase> testcases = testsInfos
                        .Select(testInfo => AdapterUtils.GetTestCaseFrom(testInfo, source))
                        .Where(testcase => filter.MatchTestCase(testcase));

                    loggerInstance.Info("Source {0}: found total {1} tests, {2} tests match filter",
                        Path.GetFileName(source), testsInfos.Count, testcases.Count());

                    foreach (TestCase testcase in testcases)
                    {
                        discoverySink.SendTestCase(testcase);
                    }

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Could not load file or assembly 'Unicorn.TestAdapter"))
                    {
                        loggerInstance.Info($"{source} has no dependency on test adapter, discovery skipped");
                    }
                    else
                    {
                        loggerInstance.Error($"Error discovering {source} source: {ex.Message}");
                    }
                }
            }

            loggerInstance.Info(Constants.DiscoveryComplete);
        }
    }
}