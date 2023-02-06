using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SMAPI.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Moq for unit testing
[assembly: InternalsVisibleTo("ContentPatcher")]
[assembly: InternalsVisibleTo("ErrorHandler")]
#if SMAPI_FOR_MOBILE
[assembly: InternalsVisibleTo("VirtualKeyboard")]
#endif

