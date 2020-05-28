using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using StardewModdingAPI.ModBuildConfig.Framework;

namespace StardewModdingAPI.ModBuildConfig
{
    /// <summary>A build task which deploys the mod files and prepares a release zip.</summary>
    public class DeployModTask : Task
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The name of the mod folder.</summary>
        [Required]
        public string ModFolderName { get; set; }

        /// <summary>The absolute or relative path to the folder which should contain the generated zip file.</summary>
        [Required]
        public string ModZipPath { get; set; }

        /// <summary>The folder containing the project files.</summary>
        [Required]
        public string ProjectDir { get; set; }

        /// <summary>The folder containing the build output.</summary>
        [Required]
        public string TargetDir { get; set; }

        /// <summary>The folder containing the game files.</summary>
        [Required]
        public string GameDir { get; set; }

        /// <summary>Whether to enable copying the mod files into the game's Mods folder.</summary>
        [Required]
        public bool EnableModDeploy { get; set; }

        /// <summary>Whether to enable the release zip.</summary>
        [Required]
        public bool EnableModZip { get; set; }

        /// <summary>Custom comma-separated regex patterns matching files to ignore when deploying or zipping the mod.</summary>
        public string IgnoreModFilePatterns { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute()
        {
            if (!this.EnableModDeploy && !this.EnableModZip)
                return true; // nothing to do

            try
            {
                // parse ignore patterns
                Regex[] ignoreFilePatterns = this.GetCustomIgnorePatterns().ToArray();

                // get mod info
                ModFileManager package = new ModFileManager(this.ProjectDir, this.TargetDir, ignoreFilePatterns, validateRequiredModFiles: this.EnableModDeploy || this.EnableModZip);

                // deploy mod files
                if (this.EnableModDeploy)
                {
                    string outputPath = Path.Combine(this.GameDir, "Mods", this.EscapeInvalidFilenameCharacters(this.ModFolderName));
                    this.Log.LogMessage(MessageImportance.High, $"The mod build package is copying the mod files to {outputPath}...");
                    this.CreateModFolder(package.GetFiles(), outputPath);
                }

                // create release zip
                if (this.EnableModZip)
                {
                    this.Log.LogMessage(MessageImportance.High, $"The mod build package is generating a release zip at {this.ModZipPath} for {this.ModFolderName}...");
                    this.CreateReleaseZip(package.GetFiles(), this.ModFolderName, package.GetManifestVersion(), this.ModZipPath);
                }

                return true;
            }
            catch (UserErrorException ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
            catch (Exception ex)
            {
                this.Log.LogError($"The mod build package failed trying to deploy the mod.\n{ex}");
                return false;
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the custom ignore patterns provided by the user.</summary>
        private IEnumerable<Regex> GetCustomIgnorePatterns()
        {
            if (string.IsNullOrWhiteSpace(this.IgnoreModFilePatterns))
                yield break;

            foreach (string raw in this.IgnoreModFilePatterns.Split(','))
            {
                Regex regex;
                try
                {
                    regex = new Regex(raw.Trim(), RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    this.Log.LogWarning($"Ignored invalid <{nameof(this.IgnoreModFilePatterns)}> pattern {raw}:\n{ex}");
                    continue;
                }

                yield return regex;
            }
        }

        /// <summary>Copy the mod files into the game's mod folder.</summary>
        /// <param name="files">The files to include.</param>
        /// <param name="modFolderPath">The folder path to create with the mod files.</param>
        private void CreateModFolder(IDictionary<string, FileInfo> files, string modFolderPath)
        {
            foreach (var entry in files)
            {
                string fromPath = entry.Value.FullName;
                string toPath = Path.Combine(modFolderPath, entry.Key);

                // ReSharper disable once AssignNullToNotNullAttribute -- not applicable in this context
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));

                File.Copy(fromPath, toPath, overwrite: true);
            }
        }

        /// <summary>Create a release zip in the recommended format for uploading to mod sites.</summary>
        /// <param name="files">The files to include.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <param name="modVersion">The mod version string.</param>
        /// <param name="outputFolderPath">The absolute or relative path to the folder which should contain the generated zip file.</param>
        private void CreateReleaseZip(IDictionary<string, FileInfo> files, string modName, string modVersion, string outputFolderPath)
        {
            // get names
            string zipName = this.EscapeInvalidFilenameCharacters($"{modName} {modVersion}.zip");
            string folderName = this.EscapeInvalidFilenameCharacters(modName);
            string zipPath = Path.Combine(outputFolderPath, zipName);

            // create zip file
            Directory.CreateDirectory(outputFolderPath);
            using Stream zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            foreach (var fileEntry in files)
            {
                string relativePath = fileEntry.Key;
                FileInfo file = fileEntry.Value;

                // get file info
                string filePath = file.FullName;
                string entryName = folderName + '/' + relativePath.Replace(Path.DirectorySeparatorChar, '/');

                // add to zip
                using Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using Stream fileStreamInZip = archive.CreateEntry(entryName).Open();
                fileStream.CopyTo(fileStreamInZip);
            }
        }

        /// <summary>Get a copy of a filename with all invalid filename characters substituted.</summary>
        /// <param name="name">The filename.</param>
        private string EscapeInvalidFilenameCharacters(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '.');
            return name;
        }
    }
}
