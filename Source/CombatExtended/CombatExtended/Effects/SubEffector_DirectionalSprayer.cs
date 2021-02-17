using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    class SubEffector_DirectionalSprayer : SubEffecter
    {
        public SubEffector_DirectionalSprayer(SubEffecterDef def, Effecter parent) : base(def, parent) { }

        public override void SubTrigger(TargetInfo target, TargetInfo source)
        {
            // I don't particularly know a case for this happening off the top of my head, but I vaguely recall
            // seeing safety measures in other parts of the code for similar cases (source is bullet.Launcher)
            if (source.HasThing == false) return;

            var spawnVector = target.CenterVector3;
            var map = target.Map ?? source.Map;

            if (spawnVector.ShouldSpawnMotesAt(map) == false) return;

            var moteCount = def.burstCount.RandomInRange;
            var scale = parent?.scale ?? 1.0f;
            var angle = (source.CenterVector3 - target.CenterVector3).AngleFlat();

            for (int i = 0; i < moteCount; i++)
            {
                var mote = ThingMaker.MakeThing(def.moteDef) as Mote;
                GenSpawn.Spawn(mote, spawnVector.ToIntVec3(), map);

                mote.Scale = def.scale.RandomInRange * scale;
                mote.exactPosition = spawnVector + def.positionOffset * scale;
                mote.exactRotation = angle + Rand.Range(-45.0f, 45.0f);
                mote.instanceColor = target.Thing?.Graphic.color ?? def.color;

                if (mote is MoteThrown moteThrown)
                {
                    moteThrown.airTimeLeft = def.airTime.RandomInRange;
                    moteThrown.SetVelocity(def.angle.RandomInRange + angle, def.speed.RandomInRange);
                }
            }
        }
    }
}
