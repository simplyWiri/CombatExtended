using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;
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

        struct CacheUnit<T>
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

        struct CacheKey<T>
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

        struct CacheKeyPair<T>
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

        #endregion
        #region fields


        #endregion
        #region methods        

        #endregion
        #region static_methods     

        #endregion
    }
}
