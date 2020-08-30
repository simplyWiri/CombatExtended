using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended.Storage
{
    public static class StorageUtils
    {
        public static IEnumerable<Thing> ThingsAround(this IntVec3 cell, int radius, Map map)
        {
            if (map == null || !cell.InBounds(map)) yield return null;
            else if (map.rangeStore != null)
            {
                Thing other;
                RangeStorage dataStore = map.rangeStore;

                var x = BinaryFindIndex(dataStore.locationCacheX, cell.x, map);

                int index = x;
                while (index + 1 < dataStore.locationCacheX.Count)
                {
                    other = dataStore.locationCacheX[index + 1].value; index++;
                    if (!other.Spawned || other.Destroyed) continue; // valid thing?

                    if (other.positionInt.DistanceTo(cell) < radius) yield return other; // in radius?
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius) break; // are we still in bounds?
                }

                index = x;
                while (index - 1 >= 0)
                {
                    other = dataStore.locationCacheX[index - 1].value; index--;
                    if (!other.Spawned || other.Destroyed) continue;

                    if (other.positionInt.DistanceTo(cell) < radius) yield return other;
                    else if (Math.Abs(other.positionInt.x - cell.x) > radius) break;
                }
            }
        }

        public static int BinaryFindIndex(List<RangeStorage.CacheSortable<Thing>> list, int targetValue, Map map, int currentRangeUpper = -1, int currentRangeLower = -1)
        {
            var rangeUpper = currentRangeUpper != -1 ? currentRangeUpper : list.Count - 1;
            var rangeLower = currentRangeLower != -1 ? currentRangeLower : 0;
            var middle = rangeLower + (rangeUpper - rangeLower) / 2;
            if (rangeUpper - rangeLower <= 1)
                return middle;
            var pos = list[middle].value.positionInt;
            if (pos.x > targetValue)
            {
                return BinaryFindIndex(list, targetValue, map, middle - 1, currentRangeLower);
            }
            else if (pos.x < targetValue)
            {
                return BinaryFindIndex(list, targetValue, map, currentRangeUpper, middle + 1);
            }
            else
            {
                return middle;
            }
        }

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