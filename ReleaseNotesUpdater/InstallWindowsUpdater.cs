using System.IO;

namespace ReleaseNotesUpdater
{
    public class InstallWindowsUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };

        public InstallWindowsUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string installWindowsTemplate = Path.Combine(TemplateDirectory, "install-windows-template.md");
                string newInstallWindowsFile = Path.Combine($"core/release-notes/{version}.0", "install-windows.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newInstallWindowsFile));
                File.Copy(installWindowsTemplate, newInstallWindowsFile, true);

                LogChanges($"Created install Windows file: {newInstallWindowsFile} for version: {version}");
            }
        }
    }
}