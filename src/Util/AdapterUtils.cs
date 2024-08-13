using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Security.Cryptography;
using System.Text;
using UnicornStatus = Unicorn.Taf.Core.Testing.Status;
using UnicornOutcome = Unicorn.Taf.Core.Testing.TestOutcome;
using System.Collections.Generic;
using Unicorn.Taf.Core.Engine;
using System.Xml.Linq;
using System.Linq;


namespace Unicorn.TestAdapter.Util
{
    internal class AdapterUtils
    {
#if NET || NETCOREAPP
        internal static List<TestInfo> GetTestInfos(string source) =>
            LoadContextObserver.GetTestsInfoInIsolation(source);

        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig) =>
            LoadContextRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#else
        internal static List<TestInfo> GetTestInfos(string source) =>
            AppDomainObserver.GetTestsInfoInIsolation(source);

        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig) =>
            AppDomainRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#endif

        internal static void SkipTest(TestCase test, string reason, IFrameworkHandle frameworkHandle)
        {
            var testResult = new TestResult(test)
            {
                ComputerName = Environment.MachineName,
                Outcome = TestOutcome.Skipped
            };

            if (!string.IsNullOrEmpty(reason))
            {
                testResult.ErrorMessage = reason;
            }

            frameworkHandle.RecordResult(testResult);
        }

        internal static TestResult GetTestResultFromOutcome(UnicornOutcome outcome, TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName
            };

            switch (outcome.Result)
            {
                case UnicornStatus.Passed:
                    testResult.Outcome = TestOutcome.Passed;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case UnicornStatus.Failed:
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = outcome.FailMessage;
                    testResult.ErrorStackTrace = outcome.FailStackTrace;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case UnicornStatus.Skipped:
                    testResult.Outcome = TestOutcome.Skipped;
                    testResult.ErrorMessage = "Check for fails in: BeforeSuite, BeforeTest or test specified in DependsOn attribute.";
                    break;
                default:
                    testResult.Outcome = TestOutcome.None;
                    break;
            }

            return testResult;
        }

        internal static TestCase GetTestCaseFrom(TestInfo testInfo, string source)
        {
            string fullName = testInfo.ClassPath + "." + testInfo.MethodName;

            TestCase testcase = new TestCase(fullName, Constants.ExecutorUri, source)
            {
                DisplayName = testInfo.MethodName,
                Id = GuidFromString(fullName)
            };

            if (testInfo.Disabled)
            {
                testcase.Traits.Add(new Trait("Disabled", string.Empty));
            }

            if (!string.IsNullOrEmpty(testInfo.Author))
            {
                testcase.Traits.Add(new Trait("Author", testInfo.Author));
            }

            foreach (string category in testInfo.Categories)
            {
                testcase.Traits.Add(new Trait(Constants.CategoryTrait, category));
            }

            foreach (string tag in testInfo.Tags)
            {
                testcase.Traits.Add(new Trait(Constants.TagTrait, tag));
            }

            if (testInfo.TestParametersCount > 0)
            {
                testcase.Traits.Add(new Trait("Parameters", testInfo.TestParametersCount.ToString()));
            }

            return testcase;
        }

        internal static string GetUnicornConfigPath(string settingsXml)
        {
            XElement runSettings = XDocument.Parse(settingsXml).Element("RunSettings");

            string unicornConfig = runSettings.Element("UnicornAdapter")?
                .Element("ConfigFile")?
                .Value;

            if (string.IsNullOrEmpty(unicornConfig))
            {
                unicornConfig = runSettings.Element("TestRunParameters")?
                .Elements("Parameter")
                .FirstOrDefault(e => e.Attribute("name").Value.Equals("unicornConfig"))?
                .Attribute("value")
                .Value;
            }

            return unicornConfig;
        }

        private static Guid GuidFromString(string data)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return new Guid(md5.ComputeHash(Encoding.Unicode.GetBytes(data)));
            }
        }
    }
}
