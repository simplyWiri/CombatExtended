using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class WorkGiver_HunterHuntCE : WorkGiver_HunterHunt
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return base.ShouldSkip(pawn, forced) || CE_Utility.HasMeleeShieldAndTwoHandedWeapon(pawn);
        }
    }
}
