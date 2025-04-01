using System.IO;
using System.Collections.Generic;

namespace ReleaseNotesUpdater
{
    public class RuntimeFileUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };
        private readonly Dictionary<string, string> runtimeVersions = new Dictionary<string, string>
        {
            { "NET6", "6.0.0" },
            { "NET8", "8.0.0" },
            { "NET9", "9.0.0" }
        };

        public RuntimeFileUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string runtimeVersion = runtimeVersions[version];
                string runtimeTemplate = Path.Combine(TemplateDirectory, "runtime-template.md");
                string newRuntimeFile = Path.Combine($"core/release-notes/{version}.0", runtimeVersion, $"{runtimeVersion}.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newRuntimeFile));
                File.Copy(runtimeTemplate, newRuntimeFile, true);

                LogChanges($"Created runtime file: {newRuntimeFile} with version: {runtimeVersion}");
            }
        }
    }
}