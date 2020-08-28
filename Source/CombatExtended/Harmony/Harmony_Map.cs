using System;
using HarmonyLib;
using RimWorld.Planet;
using Steamworks;
using Verse;

namespace CombatExtended.Storage.Harmony
{
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class H_RangeStorage_FinalizeInit
    {
        public static void Postfix(Map __instance)
        {
            __instance.rangeStore = new RangeStorage(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
    public static class H_RangeStorage_FinalizeLoading
    {
        public static void Postfix(Map __instance)
        {
            __instance.rangeStore = new RangeStorage(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.MapPostTick))]
    public static class H_RangeStorage_PostTick
    {
        public static void Postfix(Map __instance)
        {
            RangeStorage.ticksGame = GenTicks.TicksGame;
        }
    }
}
