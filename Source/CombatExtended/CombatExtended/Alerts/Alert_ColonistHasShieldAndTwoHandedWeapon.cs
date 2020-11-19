using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Alert_ColonistHasShieldAndTwoHandedWeapon : Alert
    {
        public Alert_ColonistHasShieldAndTwoHandedWeapon()
        {
            defaultLabel = "CE_ColonistHasShieldAndTwoHandedWeapon".Translate();
            defaultExplanation = "CE_ColonistHasShieldAndTwoHandedWeaponDesc".Translate();
        }

        public override AlertReport GetReport()
        {
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (pawn.CEHasApparelShield && CE_Utility.HasMeleeShieldAndTwoHandedWeapon(pawn))
                {
                    return pawn;
                }
            }
            return false;
        }
    }
}
