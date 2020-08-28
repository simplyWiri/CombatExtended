using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.Storage
{
    public static class StorageUtils
    {
        public static IEnumerable<Thing> ThingsInRange(this Thing thing, float distance)
        {
            if (thing?.Map == null) yield return null;
            else if (thing.Spawned && !thing.Destroyed)
            {
                Thing other;
                RangeStorage dataStore = thing.Map.rangeStore;
                if (thing.indexValid)
                {
                    var x = thing.positionIndex_x;
                    var z = thing.positionIndex_z;

                    int index = x;
                    while (index + 1 < dataStore.locationCacheX.Count)
                    {
                        other = dataStore.locationCacheX[index + 1].value; index++;
                        if (!other.Spawned || other.Destroyed) continue; // valid thing?

                        if (other.positionInt.DistanceTo(thing.positionInt) < distance) yield return other; // in radius?
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > distance) break; // are we still in bounds?
                    }

                    index = x;
                    while (index - 1 >= 0)
                    {
                        other = dataStore.locationCacheX[index - 1].value; index--;
                        if (!other.Spawned || other.Destroyed) continue;

                        if (other.positionInt.DistanceTo(thing.positionInt) < distance) yield return other; 
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > distance) break; 
                    }
                }
            }
        }
    }
}