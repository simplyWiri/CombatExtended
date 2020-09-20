using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Threading;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Storage
{
    public class RangeStorage
    {
        #region header
        // Updated every tick to avoid call to a property.
        public static int ticksGame = 0;

        // Essential data.
        private Map map;
        private readonly int MAP_WIDTH;

        public static readonly int CACHE_MAX_AGE = 60;
        public static readonly int CACHE_MIN_AGE = 15;

        public RangeStorage(Map map)
        {
            this.map = map;
            this.MAP_WIDTH = map.Size.x;
        }

        #endregion
        #region structs

        public class CacheSortable<T>
        {
            public T value;

            public int weight;

            private CacheSortable(T value, int weight)
            {
                this.value = value;
                this.weight = weight;
            }

            public static CacheSortable<T> Create(T value, int weight) => new CacheSortable<T>(value, weight);
        }

        #endregion
        #region fields       

        public List<CacheSortable<Thing>> locationCacheX = new List<RangeStorage.CacheSortable<Thing>>(100);
        public List<CacheSortable<Thing>> locationCacheZ = new List<RangeStorage.CacheSortable<Thing>>(100);

        #endregion
        #region methods      

        public void Notify_ThingPositionChanged(Thing thing)
        {
            if (thing.indexValid)
            {
                var x = thing.positionIndex_x;
                var z = thing.positionIndex_z;

                // does this comparison... make sense?
                if (x == thing.positionInt.x && z == thing.positionInt.z) // no need to update if they haven't moved?
                    return;

                // fix respawning after a caravan                
                if (x < 0 || x >= locationCacheX.Count)
                {
                    thing.indexValid = false;

                    this.Notify_ThingPositionChanged(thing);
                    return;
                }

                // update weights
                locationCacheX[x].weight = thing.positionInt.x;
                locationCacheZ[z].weight = thing.positionInt.z;

                // update lists
                UpdateListPosition(ref locationCacheX, x, 1, (p, nIndex) =>
                {
                    p.value.positionIndex_x = nIndex;
                });

                UpdateListPosition(ref locationCacheZ, z, 1, (p, nIndex) =>
                {
                    p.value.positionIndex_z = nIndex;
                });

            }
            else if (thing.Spawned && thing.positionInt != null)
            {
                // set weights
                thing.positionIndex_x = Math.Max(locationCacheX.Count, 0);
                thing.positionIndex_z = Math.Max(locationCacheZ.Count, 0);

                // add to cache
                locationCacheX.Add(CacheSortable<Thing>.Create(thing, thing.positionInt.x));
                locationCacheZ.Add(CacheSortable<Thing>.Create(thing, thing.positionInt.z));

                // update lists
                UpdateListPosition(ref locationCacheX, locationCacheX.Count - 1, 1, (p, nIndex) =>
                {
                    p.value.positionIndex_x = nIndex;
                });

                UpdateListPosition(ref locationCacheZ, locationCacheZ.Count - 1, 1, (p, nIndex) =>
                {
                    p.value.positionIndex_z = nIndex;
                });

                thing.indexValid = true;
            }
        }

        #endregion
        #region static_methods

#if DEBUG && PERFORMANCE
        private static float avgInsertionTime = 0;
#endif
        // onSwap is a function which takes:
        // - The element currently being swapped
        // - The new array position it will move to
        private static void UpdateListPosition<T>(ref List<CacheSortable<T>> list, int startIndex = -1, int updates = 1, Action<CacheSortable<T>, int> onSwap = null)
        {
            bool couldDoUpdateAction = onSwap != null;
            int curUpdates = 0;
            int j = 0;

#if DEBUG && PERFORMANCE
            var stopWatch = new Stopwatch();
            stopWatch.Restart();
#endif
            for (int i = (startIndex >= 1 ? startIndex : 1); i < list.Count && curUpdates < updates; i++)
            {
                bool didUpdate = false;

                j = i;
                while (j > 0 && list[j - 1].weight > list[j].weight)
                {
                    SwapElements(list, j - 1, j, couldDoUpdateAction, onSwap);

                    j--;
                    didUpdate = true;
                }

                j = i;
                while (j < list.Count - 1 && list[j + 1].weight < list[j].weight)
                {
                    SwapElements(list, j, j + 1, couldDoUpdateAction, onSwap);

                    j++;
                    didUpdate = true;
                }

                if (!didUpdate)
                {
                    break;
                }

            }
#if DEBUG && !PERFORMANCE && TARCE
            Log.Message(">-------------------<");
            for (int i = 0; i < list.Count; i++)
                Log.Message(list[i].value + "\t" + list[i].weight);
#endif
#if DEBUG && PERFORMANCE
            stopWatch.Stop();

            AVG_INSERTION_TIME = avgInsertionTime * 0.9f + 0.1f * stopWatch.ElapsedTicks / Stopwatch.Frequency;
#if TRACE && DEBUG
            if (Prefs.DevMode && Find.Selector.NumSelected > 0)
            {
                Log.Message("Insertion sort: time:\t" + stopWatch.Elapsed + "\t" + avgInsertionTime + " Second");
                Log.Message("Insertion sort: element count:\t" + list.Count + "\t" + updates);
            }
#endif
#endif
        }

        private static void SwapElements<T>(List<CacheSortable<T>> list, int firstIndex, int secondIndex, bool onSwapAction = false, Action<CacheSortable<T>, int> onSwap = null)
        {
            CacheSortable<T> a = list[firstIndex];
            CacheSortable<T> b = list[secondIndex];
            if (onSwapAction)
            {
                onSwap(a, secondIndex);
                onSwap(b, firstIndex);
            }

            list[secondIndex] = a;
            list[firstIndex] = b;
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

        #endregion
    }
}
