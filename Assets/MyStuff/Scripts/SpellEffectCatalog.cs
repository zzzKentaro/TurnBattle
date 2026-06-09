using System;
using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedBattle
{
    [Serializable]
    public class SpellEffectBinding
    {
        public SpellData spell;
        public List<SpellEffectBase> effects = new List<SpellEffectBase>();
    }

    [CreateAssetMenu(fileName = "SpellEffectCatalog", menuName = "TurnBattle/Spell Effect Catalog")]
    public class SpellEffectCatalog : ScriptableObject
    {
        [SerializeField] private List<SpellEffectBinding> bindings = new List<SpellEffectBinding>();

        public IReadOnlyList<SpellEffectBase> GetEffects(SpellData spell)
        {
            if (spell == null)
            {
                return Array.Empty<SpellEffectBase>();
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                SpellEffectBinding binding = bindings[i];
                if (binding != null && binding.spell == spell)
                {
                    return binding.effects;
                }
            }

            return Array.Empty<SpellEffectBase>();
        }
    }
}
