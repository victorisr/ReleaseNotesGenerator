using System.IO;

namespace ReleaseNotesUpdater
{
    public class InstallMacosUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };

        public InstallMacosUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string installMacosTemplate = Path.Combine(TemplateDirectory, "install-macos-template.md");
                string newInstallMacosFile = Path.Combine($"core/release-notes/{version}.0", "install-macos.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newInstallMacosFile));
                File.Copy(installMacosTemplate, newInstallMacosFile, true);

                LogChanges($"Created install MacOS file: {newInstallMacosFile} for version: {version}");
            }
        }
    }
}