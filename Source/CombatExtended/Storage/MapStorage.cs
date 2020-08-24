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

        public Dictionary<int, int> PAWN_X_INDEX = new Dictionary<int, int>();
        public Dictionary<int, int> PAWN_Z_INDEX = new Dictionary<int, int>();

        public List<CacheSortable<Thing>> LOC_CACHE_X = new List<MapStorage.CacheSortable<Thing>>(100);
        public List<CacheSortable<Thing>> LOC_CACHE_Z = new List<MapStorage.CacheSortable<Thing>>(100);

        #endregion
        #region methods      

        public void UpdateThingPosition(Thing thing)
        {
            if (PAWN_X_INDEX.TryGetValue(thing.thingIDNumber, out int x) && PAWN_Z_INDEX.TryGetValue(thing.thingIDNumber, out int z))
            {
                if (x == thing.positionInt.x && z == thing.positionInt.z)
                {
                    return;
                }

                LOC_CACHE_X[x].weight = thing.positionInt.x;
                InsertionSort<Thing>(
                   ref LOC_CACHE_X,
                   x, 1, (p, nIndex, _) =>
                   {
                       PAWN_X_INDEX[p.value.thingIDNumber] = nIndex;
                   });

                LOC_CACHE_Z[z].weight = thing.positionInt.z;
                InsertionSort<Thing>(
                    ref LOC_CACHE_Z,
                    z, 1, (p, nIndex, _) =>
                    {
                        PAWN_Z_INDEX[p.value.thingIDNumber] = nIndex;
                    });

            }
            else if (thing.Spawned && thing.positionInt != null)
            {

                PAWN_X_INDEX[thing.thingIDNumber] = Math.Max(LOC_CACHE_X.Count, 0);
                LOC_CACHE_X.Add(CacheSortable<Thing>.Create(
                    thing, thing.positionInt.x));
                InsertionSort<Thing>(
                    ref LOC_CACHE_X,
                    LOC_CACHE_X.Count - 1, 1, (p, nIndex, _) =>
                    {
                        PAWN_X_INDEX[p.value.thingIDNumber] = nIndex;
                    });

                PAWN_Z_INDEX[thing.thingIDNumber] = Math.Max(LOC_CACHE_Z.Count, 0);
                LOC_CACHE_Z.Add(CacheSortable<Thing>.Create(
                    thing, thing.positionInt.z));
                InsertionSort<Thing>(
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
