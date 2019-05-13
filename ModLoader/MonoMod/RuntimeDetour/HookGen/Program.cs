﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MonoMod.Utils;
using System.Reflection;

namespace MonoMod.RuntimeDetour.HookGen {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("MonoMod.RuntimeDetour.HookGen " + typeof(Program).GetTypeInfo().Assembly.GetName().Version);
            Console.WriteLine("using MonoMod " + typeof(MonoModder).GetTypeInfo().Assembly.GetName().Version);
            Console.WriteLine("using MonoMod.RuntimeDetour " + typeof(Detour).GetTypeInfo().Assembly.GetName().Version);

            if (args.Length == 0) {
                Console.WriteLine("No valid arguments (assembly path) passed.");
                if (System.Diagnostics.Debugger.IsAttached) // Keep window open when running in IDE
                    Console.ReadKey();
                return;
            }

            string pathIn;
            string pathOut;

            int pathInI = 0;

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--namespace" && i + 2 < args.Length) {
                    i++;
                    Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_NAMESPACE", args[i]);

                } else if (args[i] == "--namespace-il" && i + 2 < args.Length) {
                    i++;
                    Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_NAMESPACE_IL", args[i]);

                } else if (args[i] == "--orig") {
                    Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_ORIG", "1");

                } else if (args[i] == "--private") {
                    Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_PRIVATE", "1");

                } else {
                    pathInI = i;
                    break;
                }
            }

            string missingDependencyThrow = Environment.GetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW");
            if (string.IsNullOrEmpty(missingDependencyThrow))
                Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "0");


            if (pathInI >= args.Length) {
                Console.WriteLine("No assembly path passed.");
                if (System.Diagnostics.Debugger.IsAttached) // Keep window open when running in IDE
                    Console.ReadKey();
                return;
            }

            pathIn = args[pathInI];
            pathOut = args.Length != 1 && pathInI != args.Length - 1 ? args[args.Length - 1] : null;

            pathOut = pathOut ?? Path.Combine(Path.GetDirectoryName(pathIn), "MMHOOK_" + Path.ChangeExtension(Path.GetFileName(pathIn), "dll"));

            using (MonoModder mm = new MonoModder() {
                InputPath = pathIn,
                OutputPath = pathOut,
                ReadingMode = ReadingMode.Deferred
            }) {
                mm.Read();

                mm.MapDependencies();

                if (File.Exists(pathOut)) {
                    mm.Log($"[HookGen] Clearing {pathOut}");
                    File.Delete(pathOut);
                }

                mm.Log("[HookGen] Starting HookGenerator");
                HookGenerator gen = new HookGenerator(mm, Path.GetFileName(pathOut));
#if !CECIL0_9
                using (ModuleDefinition mOut = gen.OutputModule) {
#else
                ModuleDefinition mOut = gen.OutputModule;
                {
#endif

                    gen.Generate();
                    mOut.Write(pathOut);
                }

                mm.Log("[HookGen] Done.");
            }

            if (System.Diagnostics.Debugger.IsAttached) // Keep window open when running in IDE
                Console.ReadKey();
        }
    }
}
