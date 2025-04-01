using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ReleaseNotesUpdater
{
    public class ReleasesUpdater : FileUpdater
    {
        private readonly string sourceDirectory = @"C:\relNotesNET\src\Templates\1MainReleasesMd\MainReleasesMd";
        private readonly string outputDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop";
        private readonly string templateFile = "core-releases-template.md";
        private readonly string newFileName = "releases.md";
        private readonly string jsonDirectory = @"C:\Core\core\release-notes";
        private readonly List<string> channelVersions = new List<string> { "1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1", "5.0", "6.0", "7.0", "8.0", "9.0" };
        private const string placeholderSupported = "SECTION-SUPPORTED";
        private const string placeholderUnsupported = "SECTION-UNSUPPORTED";

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

        private readonly Dictionary<string, string> endOfSupportLinks = new Dictionary<string, string>
        {
            { "1.0", "https://devblogs.microsoft.com/dotnet/net-core-1-0-and-1-1-will-reach-end-of-life-on-june-27-2019/" },
            { "1.1", "https://devblogs.microsoft.com/dotnet/net-core-1-0-and-1-1-will-reach-end-of-life-on-june-27-2019/" },
            { "2.0", "https://devblogs.microsoft.com/dotnet/net-core-2-0-will-reach-end-of-life-on-september-1-2018/" },
            { "2.1", "https://devblogs.microsoft.com/dotnet/net-core-2-1-will-reach-end-of-support-on-august-21-2021/" },
            { "2.2", "https://devblogs.microsoft.com/dotnet/net-core-2-2-will-reach-end-of-life-on-december-23-2019/" },
            { "3.0", "https://devblogs.microsoft.com/dotnet/net-core-3-0-end-of-life/" },
            { "3.1", "https://devblogs.microsoft.com/dotnet/net-core-3-1-will-reach-end-of-support-on-december-13-2022/" },
            { "5.0", "https://devblogs.microsoft.com/dotnet/dotnet-5-end-of-support-update/" },
            { "6.0", "https://devblogs.microsoft.com/dotnet/dotnet-6-end-of-support/" },
            { "7.0", "https://devblogs.microsoft.com/dotnet/dotnet-7-end-of-support/" }
        };

        public ReleasesUpdater(string templateDirectory, string logFileLocation)
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
                    // Check for placeholders sections in the template and call respective methods
                    if (line.StartsWith(placeholderSupported))
                    {
                        CreateSupportedTable(writer, releaseData, launchDates, announcementLinks);
                    }
                    else if (line.StartsWith(placeholderUnsupported))
                    {
                        CreateUnsupportedTable(writer, releaseData, launchDates, announcementLinks, endOfSupportLinks);
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
        }

        private void CreateUnsupportedTable(StreamWriter writer, List<ReleaseInfo> releaseData, Dictionary<string, DateTime> launchDates, Dictionary<string, string> announcementLinks, Dictionary<string, string> endOfSupportLinks)
        {
            writer.WriteLine("|  Version  | Release Date | Support | Final Patch Version | End of Support |");
            writer.WriteLine("| :-- | :-- | :-- | :-- | :-- |");
            foreach (var release in releaseData)
            {
                if (release.EolDate <= DateTime.Now)
                {
                    var idVersion = release.ChannelVersion.EndsWith(".0") ? release.ChannelVersion.Replace(".0", "") : release.ChannelVersion;
                    var launchDate = launchDates[release.ChannelVersion];
                    var announcementLink = announcementLinks[release.ChannelVersion];
                    var endOfSupportLink = endOfSupportLinks[release.ChannelVersion];
                    var displayVersion = release.ChannelVersion.StartsWith("1.") || release.ChannelVersion.StartsWith("2.") || release.ChannelVersion.StartsWith("3.") ? $".NET Core {release.ChannelVersion}" : $".NET {idVersion}";
                    writer.WriteLine($"| [{displayVersion}](release-notes/{release.ChannelVersion}/README.md) | [{launchDate:MMMM dd, yyyy}]({announcementLink})  | [{release.ReleaseType}][policies] | [{release.LatestRelease}][ {release.LatestRelease}] | [{release.EolDate:MMMM dd, yyyy}]({endOfSupportLink}) |");
                }
            }

            writer.WriteLine("");

            foreach (var release in releaseData)
            {
                if (release.EolDate <= DateTime.Now)
                {
                    writer.WriteLine($" [{release.LatestRelease}]: release-note/{release.ChannelVersion}/{release.LatestRelease}/{release.LatestRelease}.md");
                }
            }
        }
    }
}