using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [Serializable]
    public class ElementStackTrack
    {
        [SerializeField] private ElementType elementType;
        [SerializeField] private int[] turnBuckets = new int[3];

        public ElementType ElementType => elementType;

        public ElementStackTrack(ElementType type)
        {
            elementType = type;
            turnBuckets = new int[3];
        }

        public int GetTotal()
        {
            int total = 0;
            for (int i = 0; i < turnBuckets.Length; i++)
            {
                total += turnBuckets[i];
            }

            return total;
        }

        public void AddStacks(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            turnBuckets[2] += amount;
        }

        public void AdvanceTurn()
        {
            turnBuckets[0] = turnBuckets[1];
            turnBuckets[1] = turnBuckets[2];
            turnBuckets[2] = 0;
        }

        public void Clear()
        {
            for (int i = 0; i < turnBuckets.Length; i++)
            {
                turnBuckets[i] = 0;
            }
        }
    }

    [Serializable]
    public class ElementStackController
    {
        [SerializeField] private List<ElementStackTrack> tracks = new List<ElementStackTrack>();

        public void EnsureInitialized()
        {
            EnsureTrack(ElementType.Fire);
            EnsureTrack(ElementType.Water);
            EnsureTrack(ElementType.Wind);
            EnsureTrack(ElementType.Earth);
            EnsureTrack(ElementType.Thunder);
        }

        public void ClearAll()
        {
            EnsureInitialized();
            foreach (ElementStackTrack track in tracks)
            {
                track.Clear();
            }
        }

        public void AddStacks(ElementType elementType, int amount)
        {
            if (elementType == ElementType.None || amount <= 0)
            {
                return;
            }

            ElementStackTrack track = GetOrCreateTrack(elementType);
            track.AddStacks(amount);
        }

        public int GetStacks(ElementType elementType)
        {
            if (elementType == ElementType.None)
            {
                return 0;
            }

            ElementStackTrack track = GetOrCreateTrack(elementType);
            return track.GetTotal();
        }

        public void AdvanceTurn()
        {
            EnsureInitialized();
            foreach (ElementStackTrack track in tracks)
            {
                track.AdvanceTurn();
            }
        }

        private void EnsureTrack(ElementType elementType)
        {
            if (FindTrack(elementType) == null)
            {
                tracks.Add(new ElementStackTrack(elementType));
            }
        }

        private ElementStackTrack GetOrCreateTrack(ElementType elementType)
        {
            ElementStackTrack found = FindTrack(elementType);
            if (found != null)
            {
                return found;
            }

            ElementStackTrack created = new ElementStackTrack(elementType);
            tracks.Add(created);
            return created;
        }

        private ElementStackTrack FindTrack(ElementType elementType)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].ElementType == elementType)
                {
                    return tracks[i];
                }
            }

            return null;
        }
    }
}
