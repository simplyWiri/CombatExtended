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
    public class MapStorage
    {
        #region header
        // Updated every tick to avoid call to a property.
        public static int ticksGame = 0;

        // Essential data.
        private Map map;
        private readonly int MAP_WIDTH;

        public static readonly int CACHE_MAX_AGE = 60;
        public static readonly int CACHE_MIN_AGE = 15;

        public MapStorage(Map map)
        {
            this.map = map;
            this.MAP_WIDTH = map.Size.x;
        }

        #endregion
        #region structs

        public struct CacheUnit<T>
        {
            public int tick;
            public int flags;

            public T value;

            public bool ShouldRemove => tick + CACHE_MAX_AGE > ticksGame;
            public bool IsValid => tick + CACHE_MAX_AGE > ticksGame && CACHE_MIN_AGE > ticksGame;

            private CacheUnit(T value, int flags, int tick)
            {
                this.value = value;
                this.flags = flags;
                this.tick = tick;
            }

            public static CacheUnit<T> Create(T value, int flags = 0) => new CacheUnit<T>(value: value, flags: flags, tick: ticksGame);
        }

        public struct CacheKey<T>
        {
            private readonly int hash;

            private CacheKey(T key)
            {
                this.hash = key.GetHashCode();
            }

            public static CacheKey<T> Create(T key) => new CacheKey<T>(key);

            public override int GetHashCode() => hash;
            public override bool Equals(object obj) => hash == obj.GetHashCode();
        }

        public struct CacheKeyPair<T>
        {
            private readonly int hash;

            private CacheKeyPair(T key1, T key2)
            {
                this.hash = key2.GetHashCode() ^ (key1.GetHashCode() << 1);
            }

            public static CacheKeyPair<T> Create(T key1, T key2) => new CacheKeyPair<T>(key1, key2);

            public override int GetHashCode() => hash;
            public override bool Equals(object obj) => hash == obj.GetHashCode();
        }

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

        private Dictionary<int, int> PAWN_X_INDEX = new Dictionary<int, int>();
        private Dictionary<int, int> PAWN_Z_INDEX = new Dictionary<int, int>();

        private List<CacheSortable<Pawn>> LOC_CACHE_X = new List<MapStorage.CacheSortable<Pawn>>(100);
        private List<CacheSortable<Pawn>> LOC_CACHE_Z = new List<MapStorage.CacheSortable<Pawn>>(100);

        #endregion
        #region methods      

        public void UpdatePawnPos(Pawn pawn)
        {
            if (PAWN_X_INDEX.TryGetValue(pawn.thingIDNumber, out int x) && PAWN_Z_INDEX.TryGetValue(pawn.thingIDNumber, out int z))
            {
                LOC_CACHE_X[x].weight = pawn.positionInt.x;
                InsertionSort<Pawn>(
                   ref LOC_CACHE_X,
                   x, 1, (p, nIndex, _) =>
                   {
                       PAWN_X_INDEX[p.value.thingIDNumber] = nIndex;
#if DEBUG && PERFORMANCE
                       Log.Message("1.-" + p.value + "\t:x:" + nIndex);
#endif
                   });

                LOC_CACHE_Z[z].weight = pawn.positionInt.z;
                InsertionSort<Pawn>(
                    ref LOC_CACHE_Z,
                    z, 1, (p, nIndex, _) =>
                    {
                        PAWN_Z_INDEX[p.value.thingIDNumber] = nIndex;

#if DEBUG && PERFORMANCE
                        Log.Message("2.-" + p.value + "\t:z:" + nIndex);
#endif
                    });
#if DEBUG
                                if (LOC_CACHE_X.Count > 1)
                                {
                
                                    var f1 = false;
                                    var f2 = false;
                
                                    var minRange = 5;
                
                                    if (x + 1 <= LOC_CACHE_X.Count - 1)
                                    {
                                        f1 = Math.Abs(LOC_CACHE_X[x + 1].weight - LOC_CACHE_X[x].weight) < minRange;
                                    }
                                    if (x - 1 >= 0)
                                    {
                                        f2 = Math.Abs(LOC_CACHE_X[x].weight - LOC_CACHE_X[x - 1].weight) < minRange;
                                    }
                
                                    if (z + 1 <= LOC_CACHE_Z.Count - 1)
                                    {
                                        f1 = f1 && Math.Abs(LOC_CACHE_Z[z + 1].weight - LOC_CACHE_Z[z].weight) < minRange;
                                    }
                                    if (z - 1 >= 0)
                                    {
                                        f2 = f2 && Math.Abs(LOC_CACHE_Z[z].weight - LOC_CACHE_Z[z - 1].weight) < minRange;
                                    }
                
                                    if (f1 || f2)
                                        Log.Message("Inrange");
                                    else
                                        Log.Message("out of range");
                                }
#endif
            }
            else if (pawn.Spawned && pawn.positionInt != null)
            {

                PAWN_X_INDEX[pawn.thingIDNumber] = Math.Max(LOC_CACHE_X.Count, 0);
                LOC_CACHE_X.Add(CacheSortable<Pawn>.Create(
                    pawn, pawn.positionInt.x));
                InsertionSort<Pawn>(
                    ref LOC_CACHE_X,
                    LOC_CACHE_X.Count - 1, 1, (p, nIndex, _) =>
                    {
                        PAWN_X_INDEX[p.value.thingIDNumber] = nIndex;
                    });

                PAWN_Z_INDEX[pawn.thingIDNumber] = Math.Max(LOC_CACHE_Z.Count, 0);
                LOC_CACHE_Z.Add(CacheSortable<Pawn>.Create(
                    pawn, pawn.positionInt.z));
                InsertionSort<Pawn>(
                    ref LOC_CACHE_Z,
                    LOC_CACHE_Z.Count - 1, 1, (p, nIndex, _) =>
                    {
                        PAWN_Z_INDEX[p.value.thingIDNumber] = nIndex;
                    });
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
                    if (couldDoUpdateAction)
                    {
                        onUpdate(list[j], j - 1, j);
                        onUpdate(list[j - 1], j, j - 1);
                    }

                    var other = list[j - 1];
                    list[j - 1] = list[j];
                    list[j] = other;

                    j -= 1; didUpdate = true;
                }

                j = i;
                while (j < list.Count - 1 && list[j + 1].weight < list[j].weight)
                {
                    if (couldDoUpdateAction)
                    {
                        onUpdate(list[j], j + 1, j);
                        onUpdate(list[j + 1], j, j + 1);
                    }

                    var other = list[j + 1];
                    list[j + 1] = list[j];
                    list[j] = other;

                    j += 1; didUpdate = true;
                }

                if (didUpdate)
                    curUpdates += 1;
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
