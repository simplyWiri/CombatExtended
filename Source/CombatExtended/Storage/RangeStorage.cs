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

        public List<CacheSortable<Thing>> LOC_CACHE_X = new List<RangeStorage.CacheSortable<Thing>>(100);
        public List<CacheSortable<Thing>> LOC_CACHE_Z = new List<RangeStorage.CacheSortable<Thing>>(100);

        #endregion
        #region methods      

        public void Notify_ThingPositionChanged(Thing thing)
        {
            if (thing.indexValid)
            {
                var x = thing.xIndex;
                var z = thing.zIndex;

                if (x == thing.positionInt.x && z == thing.positionInt.z)
                {
                    return;
                }

                LOC_CACHE_X[x].weight = thing.positionInt.x;
                InsertionSort<Thing>(
                   ref LOC_CACHE_X,
                   x, 1, (p, nIndex, _) =>
                   {
                       p.value.xIndex = nIndex;
                   });

                LOC_CACHE_Z[z].weight = thing.positionInt.z;
                InsertionSort<Thing>(
                    ref LOC_CACHE_Z,
                    z, 1, (p, nIndex, _) =>
                    {
                        p.value.zIndex = nIndex;
                    });

            }
            else if (thing.Spawned && thing.positionInt != null)
            {

                thing.xIndex = Math.Max(LOC_CACHE_X.Count, 0);
                LOC_CACHE_X.Add(CacheSortable<Thing>.Create(
                    thing, thing.positionInt.x));
                InsertionSort<Thing>(
                    ref LOC_CACHE_X,
                    LOC_CACHE_X.Count - 1, 1, (p, nIndex, _) =>
                    {
                        p.value.xIndex = nIndex;
                    });

                thing.zIndex = Math.Max(LOC_CACHE_Z.Count, 0);
                LOC_CACHE_Z.Add(CacheSortable<Thing>.Create(
                    thing, thing.positionInt.z));
                InsertionSort<Thing>(
                    ref LOC_CACHE_Z,
                    LOC_CACHE_Z.Count - 1, 1, (p, nIndex, _) =>
                    {
                        p.value.zIndex = nIndex;
                    });

                thing.indexValid = true;
            }
        }

        #endregion
        #region static_methods

        public static float AVG_INSERTION_TIME = 0;
        public static void InsertionSort<T>(
            ref List<CacheSortable<T>> list,
            int startIndex = -1,
            int updates = 1,

            Action<CacheSortable<T>, int, int> onUpdate = null)
        {
            var couldDoUpdateAction = onUpdate != null;
            var curUpdates = 0;

            CacheSortable<T> a;
            CacheSortable<T> b;

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
                    a = list[j - 1];
                    b = list[j];
                    if (couldDoUpdateAction)
                    {
                        onUpdate(b, j - 1, j);
                        onUpdate(a, j, j - 1);
                    }

                    list[j - 1] = b;
                    list[j] = a;

                    j -= 1; didUpdate = true;
                }

                j = i;
                while (j < list.Count - 1 && list[j + 1].weight < list[j].weight)
                {
                    a = list[j];
                    b = list[j + 1];
                    if (couldDoUpdateAction)
                    {
                        onUpdate(a, j + 1, j);
                        onUpdate(b, j, j + 1);
                    }

                    list[j + 1] = a;
                    list[j] = b;

                    j += 1; didUpdate = true;
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

            AVG_INSERTION_TIME = AVG_INSERTION_TIME * 0.9f + 0.1f * stopWatch.ElapsedTicks / Stopwatch.Frequency;
#if TRACE && DEBUG
            if (Prefs.DevMode && Find.Selector.NumSelected > 0)
            {
                Log.Message("Insertion sort: time:\t" + stopWatch.Elapsed + "\t" + AVG_INSERTION_TIME + " Second");
                Log.Message("Insertion sort: element count:\t" + list.Count + "\t" + updates);
            }
#endif
#endif
        }

        #endregion
    }
}
