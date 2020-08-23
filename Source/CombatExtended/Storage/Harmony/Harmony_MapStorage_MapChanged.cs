using System;
using HarmonyLib;
using RimWorld.Planet;
using Steamworks;
using Verse;

namespace CombatExtended.Storage.Harmony
{
    [HarmonyPatch(typeof(Room), nameof(Room.Notify_ContainedThingSpawnedOrDespawned))]
    public static class H_MapStorage_MapChanged_Notify_ContainedThingSpawnedOrDespawned
    {
        public static void Postfix(Map __instance, Thing th)
        {

        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.Notify_RoofChanged))]
    public static class H_MapStorage_Notify_RoofChanged
    {
        public static void Postfix(Map __instance)
        {

        }
    }
}
