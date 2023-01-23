using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Unicorn.Taf.Api;

namespace Unicorn.TestAdapter
{
    [DefaultExecutorUri(UnicornTestExecutor.ExecutorUriString)]
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [Category("managed")]
    public class UnicornTestDiscoverer : ITestDiscoverer
    {
        private const string Prefix = "Unicorn Adapter: ";

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger?.SendMessage(TestMessageLevel.Informational, Prefix + "test discovery starting");

            foreach (string source in sources)
            {
                try
                {
                    DiscoverAssembly(source, logger, discoverySink);
                }
                catch (Exception ex)
                {
                    logger?.SendMessage(TestMessageLevel.Error, 
                        Prefix + $"error discovering {source} source: {ex.Message}");
                }
            }

            logger?.SendMessage(TestMessageLevel.Informational, Prefix + "test discovery complete");
        }

        private static void DiscoverAssembly(string source, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<TestInfo> testsInfos = GetTestsInfo(source);

            logger?.SendMessage(TestMessageLevel.Informational, 
                $"Source: {Path.GetFileName(source)} (found {testsInfos.Count} tests)");

            foreach (TestInfo testInfo in testsInfos)
            {
                string fullName = testInfo.ClassPath + "." + testInfo.MethodName;

                TestCase testcase = new TestCase(fullName, UnicornTestExecutor.ExecutorUri, source)
                {
                    DisplayName = testInfo.MethodName,
                };

                if (testInfo.Disabled)
                {
                    testcase.Traits.Add(new Trait("Disabled", string.Empty));
                }

                if (!string.IsNullOrEmpty(testInfo.Author))
                {
                    testcase.Traits.Add(new Trait("Author", testInfo.Author));
                }

                if (testInfo.Categories.Any())
                {
                    testcase.Traits.Add(new Trait("Categories", string.Join(",", testInfo.Categories)));
                }

                if (testInfo.TestParametersCount > 0)
                {
                    testcase.Traits.Add(new Trait("Parameters", testInfo.TestParametersCount.ToString()));
                }

                discoverySink.SendTestCase(testcase);
            }
        }

        private static List<TestInfo> GetTestsInfo(string source)
        {
#if NET || NETCOREAPP
            return LoadContextObserver.GetTestsInfoInIsolation(source);
#else
            return AppDomainObserver.GetTestsInfoInIsolation(source);
#endif
        }
    }
}