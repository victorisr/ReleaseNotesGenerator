using System.IO;
using System.Collections.Generic;

namespace ReleaseNotesUpdater
{
    public class SdkFileUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };
        private readonly Dictionary<string, string> sdkVersions = new Dictionary<string, string>
        {
            { "NET6", "6.0.100" },
            { "NET8", "8.0.100" },
            { "NET9", "9.0.100" }
        };

        public SdkFileUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string sdkVersion = sdkVersions[version];
                string sdkTemplate = Path.Combine(TemplateDirectory, "sdk-template.md");
                string newSdkFile = Path.Combine($"core/release-notes/{version}.0", sdkVersion, $"{sdkVersion}.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newSdkFile));
                File.Copy(sdkTemplate, newSdkFile, true);

                LogChanges($"Created SDK file: {newSdkFile} with version: {sdkVersion}");
            }
        }
    }
}