using System;
using System.Collections.Generic;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    /// <summary>The base class for a mod.</summary>
    public abstract class Mod : IMod, IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Provides simplified APIs for writing mods.</summary>
        public IModHelper Helper { get; internal set; }

        /// <summary>Writes messages to the console and log file.</summary>
        public IMonitor Monitor { get; internal set; }

        /// <summary>The mod's manifest.</summary>
        public IManifest ModManifest { get; internal set; }


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public abstract void Entry(IModHelper helper);

        public virtual List<OptionsElement> GetConfigMenuItems()
        {
            return new List<OptionsElement>();
        }
        public virtual bool ApplyForHooks()
        {
            return false;
        }

        public virtual bool OnCommonHook_Prefix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref object __result)
        {
            return true;
        }

        public virtual void OnCommonHook_Postfix(string hookName, object __instance, ref object param1, ref object param2, ref object param3, ref object param4, ref bool __state, ref object __result)
        {
        }
        public virtual bool OnCommonStaticHook_Prefix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref object __result)
        {
            return true;
        }
        public virtual void OnCommonStaticHook_Postfix(string hookName, ref object param1, ref object param2, ref object param3, ref object param4, ref object param5, ref bool __state, ref object __result)
        {
        }


        /// <summary>Get an API that other mods can access. This is always called after <see cref="Entry"/>.</summary>
        public virtual object GetApi() => null;

        /// <summary>Release or reset unmanaged resources.</summary>
        public void Dispose()
        {
            (this.Helper as IDisposable)?.Dispose(); // deliberate do this outside overridable dispose method so mods don't accidentally suppress it
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Release or reset unmanaged resources when the game exits. There's no guarantee this will be called on every exit.</summary>
        /// <param name="disposing">Whether the instance is being disposed explicitly rather than finalised. If this is false, the instance shouldn't dispose other objects since they may already be finalised.</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>Destruct the instance.</summary>
        ~Mod()
        {
            this.Dispose(false);
        }
    }
}
