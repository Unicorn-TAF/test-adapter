using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnicornTest = Unicorn.Taf.Core.Testing;
using UnicornOutcome = Unicorn.Taf.Core.Testing.TestOutcome;


namespace Unicorn.TestAdapter.Util
{
    internal class ExecutorUtils
    {
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

        internal static TestResult GetTestResultFromOutcome(UnicornTest.TestOutcome outcome, TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                ComputerName = Environment.MachineName,
                DisplayName = testCase.DisplayName
            };

            switch (outcome.Result)
            {
                case UnicornTest.Status.Passed:
                    testResult.Outcome = TestOutcome.Passed;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case UnicornTest.Status.Failed:
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = outcome.FailMessage;
                    testResult.ErrorStackTrace = outcome.FailStackTrace;
                    testResult.Duration = outcome.ExecutionTime;
                    break;
                case UnicornTest.Status.Skipped:
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

            if (testInfo.Categories.Any())
            {
                testcase.Traits.Add(new Trait("Categories", string.Join(",", testInfo.Categories)));
            }

            if (testInfo.TestParametersCount > 0)
            {
                testcase.Traits.Add(new Trait("Parameters", testInfo.TestParametersCount.ToString()));
            }

            return testcase;
        }

        internal static TestCase GetTestCaseFrom(UnicornOutcome outcome, string source)
        {
            string fullName = outcome.FullMethodName;

            TestCase testcase = new TestCase(fullName, Constants.ExecutorUri, source)
            {
                DisplayName = fullName.Substring(fullName.LastIndexOf(".") + 1),
                Id = GuidFromString(fullName)
            };

            return testcase;
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
