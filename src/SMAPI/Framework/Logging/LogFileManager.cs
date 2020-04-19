using System;
using System.IO;
using System.Threading;

namespace StardewModdingAPI.Framework.Logging
{
    /// <summary>Manages reading and writing to log file.</summary>
    internal class LogFileManager : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying stream writer.</summary>
        private StreamWriter Stream;

        private readonly int MaxLogSize;

        private int logSize;

        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the log file being written.</summary>
        public string Path { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="path">The log file to write.</param>
        public LogFileManager(string path, int maxLogSize = int.MaxValue)
        {
            this.Path = path;

            // create log directory if needed
            string logDir = System.IO.Path.GetDirectoryName(path);
            if (logDir == null)
                throw new ArgumentException($"The log path '{path}' is not valid.");
            Directory.CreateDirectory(logDir);

            // open log file stream
            this.Stream = new StreamWriter(path, append: false) { AutoFlush = true };
            this.MaxLogSize = maxLogSize;
        }

        /// <summary>Write a message to the log.</summary>
        /// <param name="message">The message to log.</param>
        public void WriteLine(string message)
        {
            // always use Windows-style line endings for convenience
            // (Linux/Mac editors are fine with them, Windows editors often require them)
            this.Stream.Write(message + "\r\n");
            Interlocked.Add(ref this.logSize, message.Length + 2);
            if(this.logSize > this.MaxLogSize / 2)
            {
                lock (this)
                {
                    this.Stream.Dispose();
                    string oldPath = this.Path + ".old";
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                    File.Move(this.Path, this.Path + ".old");
                    this.Stream = new StreamWriter(this.Path, append: false) { AutoFlush = true };
                    this.logSize = 0;
                }
            }
        }

        /// <summary>Release all resources.</summary>
        public void Dispose()
        {
            this.Stream.Dispose();
        }
    }
}
