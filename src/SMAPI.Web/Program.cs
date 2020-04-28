using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace StardewModdingAPI.Web
{
    /// <summary>The main app entry point.</summary>
    public class Program
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The main app entry point.</summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            // configure web server
            WebHost
                .CreateDefaultBuilder(args)
                .CaptureStartupErrors(true)
                .UseSetting("detailedErrors", "true")
                .UseKestrel().UseIISIntegration() // must be used together; fixes intermittent errors on Azure: https://stackoverflow.com/a/38312175/262123
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
