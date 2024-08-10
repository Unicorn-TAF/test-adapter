using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Unicorn.TestAdapter.Util
{
    internal class FileUtils
    {
        internal static string PrepareRunDirectory(IRunContext runContext, Logger logger)
        {
            string outDir = $"{Environment.MachineName}_{DateTime.Now:MM-dd-yyyy_hh-mm}";
            string runDir = Path.Combine(runContext.TestRunDirectory, outDir);
            Directory.CreateDirectory(runDir);

            logger.Info("run directory: " + runDir);

            CopyDeploymentItems(runContext, runDir, logger);

            return runDir;
        }

        internal static void CopySourceFilesToRunDir(string sourceDir, string targetDir)
        {
            Array.ForEach(Directory.GetFiles(sourceDir), s =>
                File.Copy(s, s.Replace(sourceDir, targetDir), true));
        }

        internal static void CopyDeploymentItems(IRunContext runContext, string runDir, Logger logger)
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

            logger.Info("Test Settings: " + Path.GetFileName(testSettingsPath));

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
                    logger.Error($"Unable to copy deployment item '{deploymentItem}': " + ex);
                    throw;
                }

            }
        }
    }
}
