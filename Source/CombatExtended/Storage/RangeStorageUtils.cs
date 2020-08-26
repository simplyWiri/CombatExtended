using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.Storage
{
    public static class StorageUtils
    {
        public static IEnumerable<Thing> ThingsInRange(this Thing thing, float distance)
        {
            if (thing?.Map == null)
                yield return null;
            else if (thing.Spawned && !thing.Destroyed)
            {
                Thing other;
                RangeStorage dataStore = thing.Map.rangeStore;
                if (thing.indexValid)
                {
                    var x = thing.positionIndex_x;
                    var z = thing.positionIndex_z;

                    int a = x;
                    while (a + 1 < dataStore.locationCacheX.Count)
                    {
                        other = dataStore.locationCacheX[a + 1].value; a++;
                        if (!other.Spawned || other.Destroyed)
                            continue;

                        if (other.positionInt.DistanceTo(thing.positionInt) < distance)
                            yield return other;
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > distance)
                        {
                            break;
                        }
                    }

                    a = x;
                    while (a - 1 >= 0)
                    {
                        other = dataStore.locationCacheX[a - 1].value; a--;
                        if (!other.Spawned || other.Destroyed)
                            continue;

                        if (other.positionInt.DistanceTo(thing.positionInt) < distance)
                            yield return other;
                        else if (Math.Abs(other.positionInt.x - thing.positionInt.x) > distance)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}