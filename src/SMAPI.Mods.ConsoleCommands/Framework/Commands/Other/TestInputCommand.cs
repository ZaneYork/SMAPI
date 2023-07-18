using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other
{
    /// <summary>A command which logs the keys being pressed for 30 seconds once enabled.</summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
    internal class TestInputCommand : ConsoleCommand
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether the command should print input.</summary>
        private bool Enabled;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public TestInputCommand()
            : base("test_input", "Prints all input to the console for 30 seconds.", mayNeedUpdate: true, mayNeedInput: true) { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            this.Enabled = !this.Enabled;

            monitor.Log(
                this.Enabled ? "OK, logging all player input until you run this command again." : "OK, no longer logging player input.",
                LogLevel.Info
            );
        }

        /// <summary>Perform any logic when input is received.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="button">The button that was pressed.</param>
        public override void OnButtonPressed(IMonitor monitor, SButton button)
        {
            if (this.Enabled)
                monitor.Log($"Pressed {button}", LogLevel.Info);
        }
    }
}
