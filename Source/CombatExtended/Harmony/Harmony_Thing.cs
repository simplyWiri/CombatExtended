using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Thing), "SmeltProducts")]
    public class Harmony_Thing_SmeltProducts
    {
        public static void Postfix(Thing __instance, ref IEnumerable<Thing> __result)
        {
            var ammoUser = (__instance as ThingWithComps)?.TryGetComp<CompAmmoUser>();

            if (ammoUser != null && (ammoUser.HasMagazine && ammoUser.CurMagCount > 0 && ammoUser.CurrentAmmo != null))
            {
                var ammoThing = ThingMaker.MakeThing(ammoUser.CurrentAmmo, null);
                ammoThing.stackCount = ammoUser.CurMagCount;
                __result = __result.AddItem(ammoThing);
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Position), MethodType.Setter)]
    public static class Harmony_Thing_Position
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var finished = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!finished)
                {
                    if (codes[i].opcode == OpCodes.Ret)
                    {
                        finished = true;
                        var l1 = generator.DefineLabel();
                        yield return codes[i];
                        yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = new List<Label>(new[] { codes[i + 1].labels[0] }) };
                        yield return new CodeInstruction(OpCodes.Ldfld,
                            AccessTools.Field(typeof(Thing), "isPawn"));
                        yield return new CodeInstruction(OpCodes.Brfalse_S, l1);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld,
                            AccessTools.Field(typeof(Thing), "positionInt"));
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call,
                            AccessTools.Method("Harmony_Thing_Position:OnPositionChanged"));
                        codes[i + 1].labels.Clear();
                        codes[i + 1].labels.Add(l1);
                        continue;
                    }
                }
                yield return codes[i];
            }
        }

        // TODO: Rewrite in pure IL        
        public static void OnPositionChanged(Thing thing,
            IntVec3 oldPos,
            IntVec3 newPos)
        {
            if (thing?.Map == null) return;
            if (thing?.positionInt == null) return;
            if (thing.Destroyed || !thing.Spawned) return;

            thing?.Map?.rangeStore?.Notify_ThingPositionChanged(thing);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
    public static class Harmony_Thing_Destroy
    {
        public static void Prefix(Thing __instance)
        {
            if (__instance.isPawn)
            {
                if (__instance?.Map == null)
                    return;

                // TODO: Only need to remove one entry and update the cache.

                __instance?.Map?.rangeStore?.locationCacheX.Clear();
                __instance?.Map?.rangeStore?.locationCacheZ.Clear();

                foreach (Pawn p in __instance?.Map.mapPawns.AllPawns)
                    p.indexValid = false;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public static class Harmony_Thing_SpawnSetup
    {
        public static void Postfix(Thing __instance)
        {
            if (__instance is Pawn pawn && !CaravanUtility.IsCaravanMember(pawn))
            {
                __instance.isPawn = true;
                __instance.innerPawn = pawn;
            }
        }
    }
}
