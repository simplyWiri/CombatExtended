using System;
using HarmonyLib;
using RimWorld.Planet;
using Steamworks;
using Verse;

namespace CombatExtended.Storage.Harmony
{
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class H_MapStorage_FinalizeInit
    {
        public static void Postfix(Map __instance)
        {
            __instance.CEDataStore = new MapStorage(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
    public static class H_MapStorage_FinalizeLoading
    {
        public static void Postfix(Map __instance)
        {
            __instance.CEDataStore = new MapStorage(__instance);
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.MapPostTick))]
    public static class H_MapStorage_PostTick
    {
        public static void Postfix(Map __instance)
        {
            MapStorage.ticksGame = GenTicks.TicksGame;
        }
    }

}
