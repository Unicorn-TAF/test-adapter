using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Unicorn.Taf.Core.Engine;
using UnicornOutcome = Unicorn.Taf.Core.Testing.TestOutcome;
using UnicornStatus = Unicorn.Taf.Core.Testing.Status;
using UnicornAttachment = Unicorn.Taf.Core.Testing.Attachment;

namespace Unicorn.TestAdapter.Util
{
    internal class AdapterUtils
    {
#if NET || NETCOREAPP
        internal static List<TestInfo> GetTestInfos(string source) =>
            NetCore.LoadContextObserver.GetTestsInfoInIsolation(source);

        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig) =>
            NetCore.LoadContextRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
#else
        internal static List<TestInfo> GetTestInfos(string source) =>
            NetFramework.AppDomainObserver.GetTestsInfoInIsolation(source);

        internal static LaunchOutcome RunTestsInIsolation(string assemblyPath, string[] testsMasks, string unicornConfig) =>
            NetFramework.AppDomainRunner.RunTestsInIsolation(assemblyPath, testsMasks, unicornConfig);
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

        internal static TestResult GetTestResultFromOutcome(UnicornOutcome outcome, TestCase testCase, Logger logger)
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

            if (outcome.Defect != null)
            {
                string info = "Related defect ID: " + outcome.Defect.Id + Environment.NewLine + Environment.NewLine;
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, info));
            }

            if (!string.IsNullOrEmpty(outcome.Output))
            {
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, outcome.Output));
            }

            AttachmentSet attachmentSet = CollectAttachments(outcome, logger);

            if (attachmentSet.Attachments.Count > 0)
            {
                testResult.Attachments.Add(attachmentSet);
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
                testcase.Traits.Add(new Trait(Constants.DisabledTrait, "true"));
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

        private static AttachmentSet CollectAttachments(UnicornOutcome outcome, Logger logger)
        {
            AttachmentSet attachmentSet = new AttachmentSet(new Uri(Constants.ExecutorUriString), "Attachments");

            foreach (UnicornAttachment attachment in outcome.Attachments)
            {
                try
                {
                    Uri fileUri = new Uri(attachment.FilePath, UriKind.Absolute);
                    attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, attachment.Name));
                }
                catch (UriFormatException ex)
                {
                    logger.Warn($"Ignoring attachment with path '{attachment.FilePath}' due to problem with path: {ex.Message}");
                }
                catch (Exception ex)
                {
                    logger.Warn($"Ignoring attachment with path '{attachment.FilePath}': {ex.Message}.");
                }
            }

            return attachmentSet;
        }
    }
}
