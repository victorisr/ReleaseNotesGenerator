using System;

namespace ReleaseNotesUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Define the template directory and log file location
                string templateDirectory = "templates/";
                string logFileLocation = "logfile.log";

                // Create instances of the updater classes
                var readMeUpdater = new ReadMeUpdater(templateDirectory, logFileLocation);
                var releasesUpdater = new ReleasesUpdater(templateDirectory, logFileLocation);
                var rnReadMeUpdater = new RNReadMeUpdater(templateDirectory, logFileLocation);
                var cveFileUpdater = new CveFileUpdater(templateDirectory, logFileLocation);
                /* var versionReadMeUpdater = new VersionReadMeUpdater(templateDirectory, logFileLocation);
                var installLinuxUpdater = new InstallLinuxUpdater(templateDirectory, logFileLocation);
                var installMacosUpdater = new InstallMacosUpdater(templateDirectory, logFileLocation);
                var installWindowsUpdater = new InstallWindowsUpdater(templateDirectory, logFileLocation);
                var runtimeFileUpdater = new RuntimeFileUpdater(templateDirectory, logFileLocation);
                var sdkFileUpdater = new SdkFileUpdater(templateDirectory, logFileLocation); */

                // Update the files
                readMeUpdater.UpdateFiles();
                releasesUpdater.UpdateFiles();
                rnReadMeUpdater.UpdateFiles();
                cveFileUpdater.UpdateFiles();
                /* versionReadMeUpdater.UpdateFiles();
                installLinuxUpdater.UpdateFiles();
                installMacosUpdater.UpdateFiles();
                installWindowsUpdater.UpdateFiles();
                runtimeFileUpdater.UpdateFiles();
                sdkFileUpdater.UpdateFiles(); */

                Console.WriteLine("Successfully updated all files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}