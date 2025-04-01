using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ReleaseNotesUpdater
{
    public class CveFileUpdater : FileUpdater
    {
        private readonly List<int> idVersions = new List<int> { 8, 9 }; // Add your desired versions here
        private readonly string idLastUpdated = DateTime.Now.ToString("yyyy-MM-dd"); // Current date as last updated date
        private readonly string sourceDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop\Test Script\src\Templates\MajorCveMd"; // Directory where the Template file is located
        private readonly string outputDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop"; // Directory where the modified file will be saved
        private readonly string templateFile = "major-cve-template.md";
        private readonly string jsonDirectoryBase = @"C:\Febcore\core\release-notes\"; // Base directory where the JSON files are located
        private const string placeholderCveTable = "SECTION-CVETABLE"; // Placeholder Sections in template
        private readonly string githubToken = "your_github_token"; // Add your GitHub token here

        public CveFileUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            UpdateFilesAsync().Wait();
        }

        private async Task UpdateFilesAsync()
        {
            foreach (var idVersion in idVersions)
            {
                string jsonDirectory = Path.Combine(jsonDirectoryBase, $"{idVersion}.0"); // Directory where the JSON file is located
                string jsonFileName = "releases.json"; // Name of the JSON file
                string newFileName = $"cve{idVersion}.md"; // New file name for the modified file

                try
                {
                    // Locate the template
                    string sourceFilePath = Path.Combine(sourceDirectory, templateFile);
                    ValidateFilePath(sourceFilePath);

                    // Read the content of the template file
                    string[] lines = File.ReadAllLines(sourceFilePath);

                    // Locate the JSON file
                    string jsonFilePath = Path.Combine(jsonDirectory, jsonFileName);
                    ValidateFilePath(jsonFilePath);

                    // Read and parse the JSON file
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    JObject jsonObject = JObject.Parse(jsonContent);

                    // Modify the content
                    string outputFilePath = Path.Combine(outputDirectory, newFileName);
                    using (StreamWriter writer = new StreamWriter(outputFilePath))
                    {
                        foreach (var line in lines)
                        {
                            string modifiedLine = line
                                // Replace placeholders values in the template.
                                .Replace("{ID-VERSION}", $"{idVersion}");

                            if (modifiedLine.StartsWith(placeholderCveTable))
                            {
                                // Call to create CVE List.
                                await CreateCveTable(writer, jsonObject, githubToken);
                            }
                            else
                            {
                                writer.WriteLine(modifiedLine);
                            }
                        }
                    }

                    LogChanges($"Modified file saved to: {outputFilePath}");
                }
                catch (Exception ex)
                {
                    LogChanges($"An error occurred: {ex.Message}");
                }
            }
        }

        private void ValidateFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
        }

        private async Task CreateCveTable(StreamWriter writer, JObject jsonObject, string githubToken)
        {
            var releases = jsonObject["releases"];
            if (releases == null)
            {
                throw new Exception("Releases section not found in JSON.");
            }

            foreach (var release in releases)
            {
                string releaseVersion = release["release-version"]?.ToString();
                string releaseDate = release["release-date"]?.ToString();
                var cveList = release["cve-list"];

                if (releaseVersion == null || releaseDate == null || cveList == null)
                {
                    LogChanges("Missing release details in JSON.");
                    continue;
                }

                if (!releaseVersion.Contains("rc") && !releaseVersion.Contains("preview"))
                {
                    writer.WriteLine($"- {releaseVersion} ({DateTime.Parse(releaseDate):MMMM yyyy})");
                    foreach (var cve in cveList)
                    {
                        string cveId = cve["cve-id"]?.ToString();
                        if (cveId == null)
                        {
                            LogChanges("Missing CVE ID in JSON.");
                            continue;
                        }

                        var (cveUrl, cveIssueHeader) = await GetCveIssueUrlAsync(cveId, githubToken);

                        writer.WriteLine($"  - [{cveId} | {cveIssueHeader}]({cveUrl})");
                    }
                }
            }
        }

        private async Task<(string, string)> GetCveIssueUrlAsync(string cveId, string githubToken)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; dotnet-cve-search/1.0)");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

                string url = $"https://api.github.com/search/issues?q={cveId}+repo:dotnet/announcements";

                int maxRetries = 5;
                int delay = 2000; // Start with a 2 second delay

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    var response = await client.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var rateLimitReset = response.Headers.Contains("X-RateLimit-Reset")
                            ? response.Headers.GetValues("X-RateLimit-Reset").FirstOrDefault()
                            : null;

                        if (rateLimitReset != null)
                        {
                            var resetTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(rateLimitReset));
                            var waitTime = resetTime - DateTimeOffset.Now;
                            waitTime = waitTime < TimeSpan.Zero ? TimeSpan.Zero : waitTime;

                            LogChanges($"Rate limit exceeded. Waiting for {waitTime.TotalSeconds} seconds before retrying...");
                            await Task.Delay(waitTime);
                        }
                        else
                        {
                            LogChanges("Rate limit exceeded. Waiting before retrying...");
                            await Task.Delay(delay);
                            delay *= 2; // Exponential backoff
                        }

                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject searchResults = JObject.Parse(responseBody);
                    var items = searchResults["items"];

                    if (items != null && items.HasValues)
                    {
                        string issueUrl = items[0]["html_url"]?.ToString();
                        string issueTitle = items[0]["title"]?.ToString();
                        if (issueTitle == null)
                        {
                            LogChanges("Missing issue title in JSON.");
                            return ("An external link was removed to protect your privacy.", "No title available");
                        }

                        string[] titleParts = issueTitle.Split('|');
                        string cveIssueHeader = titleParts.Length > 1 ? titleParts[1].Trim() : "No title available";
                        return (issueUrl, cveIssueHeader);
                    }

                    return ("An external link was removed to protect your privacy.", "An external link was removed to protect your privacy.");
                }

                throw new Exception("Rate limit exceeded. Please try again later.");
            }
        }
    }
}
