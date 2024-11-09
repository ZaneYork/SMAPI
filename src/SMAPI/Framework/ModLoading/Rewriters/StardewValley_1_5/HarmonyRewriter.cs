using System;
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_5;

/// <summary>Detects Harmony references.</summary>
internal class HarmonyDetector : BaseInstructionHandler
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public HarmonyDetector()
        : base(defaultPhrase: "Harmony 1.x") { }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
    {
        // detect Harmony
        if (type.Scope is not AssemblyNameReference scope || scope.Name != "0Harmony")
            return false;

        this.MarkFlag(InstructionHandleResult.DetectedGamePatch);
        return false;
    }
}
