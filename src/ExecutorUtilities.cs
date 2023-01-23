using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using UnicornTest = Unicorn.Taf.Core.Testing;

namespace Unicorn.TestAdapter
{
    internal class ExecutorUtilities
    {
        internal static string PrepareRunDirectory(string baseDir)
        {
            string outDir = $"{Environment.MachineName}_{DateTime.Now:MM-dd-yyyy_hh-mm}";
            string runDir = Path.Combine(baseDir, "TestResults", outDir);
            Directory.CreateDirectory(runDir);
            return runDir;
        }

        internal static void CopySourceFilesToRunDir(string sourceDir, string targetDir)
        {
            Array.ForEach(Directory.GetFiles(sourceDir), s =>
                File.Copy(s, s.Replace(sourceDir, targetDir), true));
        }

        internal static void CopyDeploymentItems(IRunContext runContext, string runDir, IFrameworkHandle frameworkHandle)
        {
            var runSettingsXml = XDocument.Parse(runContext.RunSettings.SettingsXml);

            var msTestElement = runSettingsXml
                .Element("RunSettings")
                .Element("MSTest");

            if (msTestElement == null)
            {
                return;
            }

            var testSettingsPath = msTestElement
                .Element("SettingsFile")
                .Value;

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Test Settings: " + Path.GetFileName(testSettingsPath));

            var testSettingsXml = XDocument.Load(testSettingsPath);

            XNamespace nsa = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            var deploymentItems = testSettingsXml
                .Element(nsa + "TestSettings")
                .Element(nsa + "Deployment")
                .Elements(nsa + "DeploymentItem")
                .Select(d => d.Attribute("filename").Value);

            foreach (var deploymentItem in deploymentItems)
            {
                try
                {
                    var item = Path.IsPathRooted(deploymentItem) ?
                        deploymentItem :
                        Path.Combine(runContext.SolutionDirectory, deploymentItem);

                    var itemAttributes = File.GetAttributes(item);

                    if (itemAttributes.HasFlag(FileAttributes.Directory))
                    {
                        var itemDirectory = item.EndsWith("\\") ? Path.GetDirectoryName(item) : item;
                        CopySourceFilesToRunDir(itemDirectory, runDir);
                    }
                    else
                    {
                        var itemDirectory = Path.GetDirectoryName(item);
                        File.Copy(item, item.Replace(itemDirectory, runDir), true);
                    }
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, $"Unable to copy deployment item '{deploymentItem}': " + ex);
                    throw;
                }

            }
        }

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
                ComputerName = Environment.MachineName
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
    }
}
