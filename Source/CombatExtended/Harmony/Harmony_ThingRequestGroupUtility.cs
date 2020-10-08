using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /* We are aiming to add the field 'Ammo' to the ThingRequestGroup enum, as such, we need to get rid of the error that is thrown
     * when a switch statement using ThingRequestGroup falls through and (by vanilla) errors.
     * (we just get rid of the code which throws the error)
     */

    [HarmonyPatch(typeof(ThingRequestGroupUtility), "StoreInRegion")]
    internal static class Harmony_ThingRequestGroupUtility
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var inst in instructions)
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

        internal static void Postfix(ThingRequestGroup group, ref bool __result)
        {
            if(group == (ThingRequestGroup)Controller.Ammo_ThingRequestGroupInteger)
            {
                __result = true;
            }
        }
    }
}
