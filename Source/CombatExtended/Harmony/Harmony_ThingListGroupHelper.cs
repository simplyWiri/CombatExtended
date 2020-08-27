using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{

    /* We are aiming to add the field 'Ammo' to the ThingRequestGroup enum, as such, we need to get rid of the error that is thrown
     * when a switch statement using ThingRequestGroup falls through and (by vanilla) errors.
     * (we just get rid of the code which throws the error)
     */

    [HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
    internal static class Harmony_ThingListGroupHelper
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var insts = instructions.MethodReplacer(typeof(ThingDef).GetMethod("get_IsShell"),
                typeof(AmmoUtility).GetMethod(nameof(AmmoUtility.IsShell), BindingFlags.Public | BindingFlags.Static));

            foreach (var inst in insts)
            {
                if (inst.opcode == OpCodes.Ldstr && (string)inst.operand == "group")
                {
                    yield return new CodeInstruction(OpCodes.Ret).MoveLabelsFrom(inst);
                    break;
                }
                else 
                { 
                    yield return inst;
                }
            }
        }

        internal static void Postfix(ThingRequestGroup group, ThingDef def, ref bool __result)
        {
            if(group == (ThingRequestGroup)Controller.Ammo_ThingRequestGroupInteger)
            {
                __result = def is AmmoDef;
            }
        }
    }
}