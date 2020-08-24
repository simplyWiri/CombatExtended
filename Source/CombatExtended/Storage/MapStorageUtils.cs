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
                MapStorage dataStore = thing.Map.CEDataStore;
                if (thing.indexValid)
                {
                    var x = thing.xIndex;
                    var z = thing.zIndex;

                    int a = x;
                    while (a + 1 < dataStore.LOC_CACHE_X.Count)
                    {
                        other = dataStore.LOC_CACHE_X[a + 1].value; a++;
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
                        other = dataStore.LOC_CACHE_X[a - 1].value; a--;
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