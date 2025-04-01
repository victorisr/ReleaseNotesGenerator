using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ReleaseNotesUpdater
{
    public class ReadMeUpdater : FileUpdater
    {
        private readonly string sourceDirectory = @"C:\relNotesNET\src\Templates\1MainReleasesReadMeMd\MainReleasesReadMeMd";
        private readonly string outputDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop";
        private readonly string templateFile = "core-README-template.md";
        private readonly string newFileName = "README.md";
        private readonly string jsonDirectory = @"C:\Core\core\release-notes";
        private readonly List<string> channelVersions = new List<string> { "1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1", "5.0", "6.0", "7.0", "8.0", "9.0" };
        private const string placeholderSupported = "SECTION-RELEASE";

        private readonly Dictionary<string, DateTime> launchDates = new Dictionary<string, DateTime>
        {
            { "1.0", new DateTime(2016, 6, 27) },
            { "1.1", new DateTime(2016, 11, 16) },
            { "2.0", new DateTime(2017, 8, 14) },
            { "2.1", new DateTime(2018, 5, 30) },
            { "2.2", new DateTime(2018, 12, 4) },
            { "3.0", new DateTime(2019, 9, 23) },
            { "3.1", new DateTime(2019, 12, 3) },
            { "5.0", new DateTime(2020, 11, 10) },
            { "6.0", new DateTime(2021, 11, 8) },
            { "7.0", new DateTime(2022, 11, 8) },
            { "8.0", new DateTime(2023, 11, 14) },
            { "9.0", new DateTime(2024, 11, 12) }
        };

        private readonly Dictionary<string, string> announcementLinks = new Dictionary<string, string>
        {
            { "1.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-1-0/" },
            { "1.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-1-1/" },
            { "2.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-0/" },
            { "2.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-1/" },
            { "2.2", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-2" },
            { "3.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-3-0/" },
            { "3.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-3-1/" },
            { "5.0", "https://devblogs.microsoft.com/dotnet/announcing-net-5-0/" },
            { "6.0", "https://devblogs.microsoft.com/dotnet/announcing-net-6/" },
            { "7.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-7/" },
            { "8.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-8/" },
            { "9.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/" }
        };

        public ReadMeUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            // Locate the template
            string sourceFilePath = Path.Combine(sourceDirectory, templateFile);
            if (!File.Exists(sourceFilePath))
            {
                LogChanges($"File not found: {sourceFilePath}");
                return;
            }

            // Read the content of the template file
            string[] lines = File.ReadAllLines(sourceFilePath);

            // Retrieve values from JSON files
            var releaseData = new List<ReleaseInfo>();
            foreach (var channelVersion in channelVersions)
            {
                string jsonFilePath = Path.Combine(jsonDirectory, channelVersion, "releases.json");
                if (!File.Exists(jsonFilePath))
                {
                    LogChanges($"File not found: {jsonFilePath}");
                    continue;
                }

                // Read and parse the JSON file
                string jsonContent = File.ReadAllText(jsonFilePath);
                JObject jsonObject = JObject.Parse(jsonContent);

                // Retrieve values from the JSON object
                var releaseInfo = new ReleaseInfo
                {
                    ChannelVersion = channelVersion,
                    LatestRelease = jsonObject["latest-release"]?.ToString() ?? string.Empty,
                    LatestReleaseDate = DateTime.TryParse(jsonObject["latest-release-date"]?.ToString(), out var latestReleaseDate) ? latestReleaseDate : DateTime.MinValue,
                    LatestRuntime = jsonObject["latest-runtime"]?.ToString() ?? string.Empty,
                    LatestSdk = jsonObject["latest-sdk"]?.ToString() ?? string.Empty,
                    SupportPhase = jsonObject["support-phase"]?.ToString() ?? string.Empty,
                    ReleaseType = jsonObject["release-type"]?.ToString()?.ToUpper() ?? string.Empty,
                    EolDate = DateTime.TryParse(jsonObject["eol-date"]?.ToString(), out var eolDate) ? eolDate : DateTime.MinValue,
                    LifecyclePolicy = jsonObject["lifecycle-policy"]?.ToString() ?? string.Empty
                };

                releaseData.Add(releaseInfo);
            }

            // Sort the release data by idVersion in descending order
            releaseData = releaseData.OrderByDescending(r => Version.Parse(r.ChannelVersion)).ToList();

            // Modify the content of the new file
            using (StreamWriter writer = new StreamWriter(Path.Combine(outputDirectory, newFileName)))
            {
                foreach (var line in lines)
                {
                    // Check for placeholder Sections and call respective methods
                    if (line.StartsWith(placeholderSupported))
                    {
                        CreateSupportedTable(writer, releaseData, launchDates, announcementLinks);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            LogChanges($"Modified file saved to: {Path.Combine(outputDirectory, newFileName)}");
        }

        private void CreateSupportedTable(StreamWriter writer, List<ReleaseInfo> releaseData, Dictionary<string, DateTime> launchDates, Dictionary<string, string> announcementLinks)
        {
            writer.WriteLine("|  Version  | Release Date | Support | Latest Patch Version | End of Support |");
            writer.WriteLine("| :-- | :-- | :-- | :-- | :-- |");
            foreach (var release in releaseData)
            {
                if (release.EolDate > DateTime.Now)
                {
                    var idVersion = release.ChannelVersion.EndsWith(".0") ? release.ChannelVersion.Replace(".0", "") : release.ChannelVersion;
                    var launchDate = launchDates[release.ChannelVersion];
                    var announcementLink = announcementLinks[release.ChannelVersion];
                    writer.WriteLine($"| [.NET {idVersion}](release-notes/{release.ChannelVersion}/README.md) | [{launchDate:MMMM dd, yyyy}]({announcementLink}) | [{release.ReleaseType}][policies] | [{release.LatestRelease}][ {release.LatestRelease}] | {release.EolDate:MMMM dd, yyyy} |");
                }
            }

            writer.WriteLine("");

            foreach (var release in releaseData)
            {
                if (release.EolDate > DateTime.Now)
                {
                    writer.WriteLine($" [{release.LatestRelease}]: release-note/{release.ChannelVersion}/{release.LatestRelease}/{release.LatestRelease}.md");
                }
            }

            writer.WriteLine("[policies]: release-policies.md");
        }
    }

    public class ReleaseInfo
    {
        public string ChannelVersion { get; set; } = string.Empty;
        public string LatestRelease { get; set; } = string.Empty;
        public DateTime LatestReleaseDate { get; set; }
        public string LatestRuntime { get; set; } = string.Empty;
        public string LatestSdk { get; set; } = string.Empty;
        public string SupportPhase { get; set; } = string.Empty;
        public string ReleaseType { get; set; } = string.Empty;
        public DateTime EolDate { get; set; }
        public string LifecyclePolicy { get; set; } = string.Empty;
    }
}