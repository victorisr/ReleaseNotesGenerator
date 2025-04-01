using System.IO;

namespace ReleaseNotesUpdater
{
    public class InstallLinuxUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };

        public InstallLinuxUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string installLinuxTemplate = Path.Combine(TemplateDirectory, "install-linux-template.md");
                string newInstallLinuxFile = Path.Combine($"core/release-notes/{version}.0", "install-linux.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newInstallLinuxFile));
                File.Copy(installLinuxTemplate, newInstallLinuxFile, true);

                LogChanges($"Created install Linux file: {newInstallLinuxFile} for version: {version}");
            }
        }
    }
}