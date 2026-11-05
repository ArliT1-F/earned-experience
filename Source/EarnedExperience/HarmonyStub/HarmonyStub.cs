#if HARMONY_MISSING
// Minimal HarmonyLib stub to allow compilation when 0Harmony.dll is not installed locally.
// At runtime the real Harmony (if present) will be loaded from the player's Mods/workshop.
// This stub is only included when $(HarmonyPath) is not found (see .csproj).
using System;
using System.Reflection;

namespace HarmonyLib
{
    public class Harmony
    {
        public Harmony(string id) {}
        public void PatchAll(Assembly assembly) {}
        public void UnpatchAll(string id) {}
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HarmonyPatchAttribute : Attribute
    {
        public HarmonyPatchAttribute() {}
        public HarmonyPatchAttribute(Type type) {}
        public HarmonyPatchAttribute(Type type, string methodName) {}
        public HarmonyPatchAttribute(string methodName) {}
    }

    // Compatibility alias: some code uses [HarmonyPatch] shorthand
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HarmonyPatch : HarmonyPatchAttribute
    {
        public HarmonyPatch() {}
        public HarmonyPatch(Type type) : base(type) {}
        public HarmonyPatch(Type type, string methodName) : base(type, methodName) {}
        public HarmonyPatch(string methodName) : base(methodName) {}
    }
}
#endif
