using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended
{
    public enum BulletClass
    {
        Controlled,
        Fragmenting,
        LargeCaliber
    }

    public class AmmoCategoryDef : Def
    {
        public bool advanced = false;
        public BulletClass bulletClass = BulletClass.Controlled;
        public string labelShort;

        public string LabelCapShort => labelShort.CapitalizeFirst();
    }
}
