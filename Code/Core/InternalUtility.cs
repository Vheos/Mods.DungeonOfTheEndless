namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using BepInEx;
    using Mods.Core;
    using UnityRandom = UnityEngine.Random;

    static internal class InternalUtility
    {
        /// <summary> Randomizes the indices of all elements in this list. </summary>
        static public void Shuffle<T>(this IList<T> t)
        {
            for (int i = 0; i < t.Count - 1; ++i)
            {
                int j = UnityRandom.Range(i, t.Count);
                T tmp = t[i];
                t[i] = t[j];
                t[j] = tmp;
            }
        }
        static public void Shuffle<T>(this IList<T> t, int tempSeed)
        {
            int previousSeed = UnityRandom.seed;
            UnityRandom.seed = tempSeed;
            for (int i = 0; i < t.Count - 1; ++i)
            {
                int j = UnityRandom.Range(i, t.Count);
                T tmp = t[i];
                t[i] = t[j];
                t[j] = tmp;
            }
            UnityRandom.seed = previousSeed;
        }
        static public void ApplyBoolOverride(this ref bool value, BoolOverride boolOverride, bool isReversed = false)
        {
            switch (boolOverride)
            {
                case BoolOverride.True: value = !isReversed; break;
                case BoolOverride.False: value = isReversed; break;
            }
        }

    }
}