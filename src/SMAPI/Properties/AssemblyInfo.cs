using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SMAPI.Tests")]
[assembly: InternalsVisibleTo("ConsoleCommands")]
[assembly: InternalsVisibleTo("ContentPatcher")]
[assembly: InternalsVisibleTo("ErrorHandler")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Moq for unit testing
#if SMAPI_FOR_MOBILE
[assembly: InternalsVisibleTo("VirtualKeyboard")]
#endif
