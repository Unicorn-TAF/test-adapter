using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
                    DiscoverAssembly(source, loggerInstance, discoverySink);
                }
                catch (Exception ex)
                {
                    loggerInstance.Error($"Error discovering {source} source: {ex.Message}");
                }
            }

            loggerInstance.Info(Constants.DiscoveryComplete);
        }

        private static void DiscoverAssembly(string source, Logger logger, ITestCaseDiscoverySink discoverySink)
        {
#if NET || NETCOREAPP
            List<TestInfo> testsInfos = LoadContextObserver.GetTestsInfoInIsolation(source);
#else
            List<TestInfo> testsInfos = AppDomainObserver.GetTestsInfoInIsolation(source);
#endif

            logger.Info($"Source: {Path.GetFileName(source)} (found {testsInfos.Count} tests)");

            foreach (TestInfo testInfo in testsInfos)
            {
                TestCase testcase = ExecutorUtils.GetTestCaseFrom(testInfo, source);
                discoverySink.SendTestCase(testcase);
            }
        }
    }
}