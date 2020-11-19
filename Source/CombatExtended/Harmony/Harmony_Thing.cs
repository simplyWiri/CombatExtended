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
                            AccessTools.Field(typeof(Thing), "CEIsPawn"));
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

            thing?.Map?.CERangeStore?.Notify_ThingPositionChanged(thing);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public static class Harmony_Thing_SpawnSetup
    {
        public static void Postfix(Thing __instance)
        {
            if (__instance is Pawn pawn)
            {
                __instance.CEIsPawn = true;
                __instance.CEInnerPawn = pawn;
                if (__instance.Map.components.Count == 0
                    || !__instance.Spawned
                    || __instance.Destroyed)
                    return;
                __instance?.Map?.CERangeStore?.Notify_ThingPositionChanged(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
    public static class Harmony_Thing_DeSpawn
    {
        public static void Prefix(Thing __instance)
        {
            if (__instance.CEIsPawn
                && __instance.CEIndexValid
                && __instance.Map != null)
            {
                // TODO: Only need to remove one entry and update the cache.
                __instance.CEIndexValid = false;
                __instance?.Map?.CERangeStore?.locationCacheX.Clear();
                __instance?.Map?.CERangeStore?.locationCacheZ.Clear();
                foreach (Pawn p in __instance?.Map.mapPawns.AllPawns)
                {
                    p.CEIndexValid = false;
                    if (p.Spawned && !p.Destroyed && !p.Dead)
                    {
                        p?.Map?.CERangeStore?.Notify_ThingPositionChanged(p);
                    }
                }
            }
        }
    }
}
