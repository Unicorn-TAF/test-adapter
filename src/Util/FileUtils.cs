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
            string resultsDirectory = GetResultsDirectory(runContext.RunSettings.SettingsXml);

            if (resultsDirectory != null)
            {
                string outDir = $"{Environment.MachineName}_{DateTime.Now:MM-dd-yyyy_hh-mm}";
                string runDir = Path.Combine(runContext.SolutionDirectory, resultsDirectory, outDir);

                Directory.CreateDirectory(runDir);
                logger.Info("Run directory: " + runDir);

                CopyDeploymentItems(runContext, runDir, logger);
                return runDir;
            }
            else
            {
                logger.Info("Run directory: current build directory");
                return null;
            }
            
        }

        internal static string GetResultsDirectory(string settingsXml) =>
            XDocument.Parse(settingsXml).Element("RunSettings").Element("UnicornAdapter")?.Element("ResultsDirectory")?.Value;

        internal static void CopyBuildToRunDir(string sourceDir, string targetDir)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDir);
            DirectoryInfo diDest = new DirectoryInfo(targetDir);

            if (diSource.Name != diDest.Name)
            {
                CopyAll(diSource, diDest, true);
            }
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

            foreach (string deploymentItem in deploymentItems)
            {
                CopyItem(deploymentItem, runDir, runContext.SolutionDirectory, logger);
            }
        }

        private static void CopyItem(string deploymentItem, string runDir, string solutionDir, Logger logger)
        {
            try
            {
                var item = Path.IsPathRooted(deploymentItem) ?
                    deploymentItem :
                    Path.Combine(solutionDir, deploymentItem);

                var itemAttributes = File.GetAttributes(item);

                if (itemAttributes.HasFlag(FileAttributes.Directory))
                {
                    var itemDirectory = item.EndsWith("\\") ? Path.GetDirectoryName(item) : item;
                    CopyBuildToRunDir(itemDirectory, runDir);
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

        private static void CopyAll(DirectoryInfo source, DirectoryInfo dest, bool copySubDirs)
        {
            DirectoryInfo[] dirs = source.GetDirectories();
            FileInfo[] files = source.GetFiles();

            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(dest.FullName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(dest.FullName, subdir.Name);
                    Directory.CreateDirectory(temppath);
                    CopyAll(subdir, new DirectoryInfo(temppath), copySubDirs);
                }
            }
        }
    }
}
