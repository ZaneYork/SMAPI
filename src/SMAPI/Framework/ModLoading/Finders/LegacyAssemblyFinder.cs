#if SMAPI_DEPRECATED
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Detects assembly references which will break in SMAPI 4.0.0.</summary>
    internal class LegacyAssemblyFinder : BaseInstructionHandler
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public LegacyAssemblyFinder()
            : base(defaultPhrase: "legacy assembly references") { }


        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module)
        {
            foreach (AssemblyNameReference assembly in module.AssemblyReferences)
            {
                InstructionHandleResult flag = this.GetFlag(assembly);
                if (flag is InstructionHandleResult.None)
                    continue;

                this.MarkFlag(flag);
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the instruction handle flag for the given assembly reference, if any.</summary>
        /// <param name="assemblyRef">The assembly reference.</param>
        private InstructionHandleResult GetFlag(AssemblyNameReference assemblyRef)
        {
            return assemblyRef.Name switch
            {
                "System.Configuration.ConfigurationManager" => InstructionHandleResult.DetectedLegacyConfigurationDll,
                "System.Runtime.Caching" => InstructionHandleResult.DetectedLegacyCachingDll,
                "System.Security.Permission" => InstructionHandleResult.DetectedLegacyPermissionsDll,
                _ => InstructionHandleResult.None
            };
        }
    }
}
#endif
