using System;
using UnityEngine;

namespace Game1.Items
{
    public enum ItemSize { S, M, L, XL }
    public enum ItemRarity { Common, Uncommon, Rare, Curse }
    public enum PhysicsProfile { Soft, Standard, Heavy, Delicate }

    /// <summary>Authoritative static data for a sellable prototype item.</summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "GAME1/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId = "new_item";
        [SerializeField] private string localizationKey = "item.new_item.name";
        [SerializeField, Min(0)] private int value;
        [SerializeField, Range(0.1f, 40f)] private float weightKg = 1f;
        [SerializeField] private ItemSize sizeClass;
        [SerializeField, Range(0, 100)] private int fragility;
        [SerializeField, Min(0f)] private float noiseRadius;
        [SerializeField] private ItemRarity rarity;
        [SerializeField] private string curseId;
        [SerializeField] private bool twoHanded;
        [SerializeField, Range(1, 2)] private int requiredCarriers = 1;
        [SerializeField] private PhysicsProfile physicsProfile = PhysicsProfile.Standard;
        [SerializeField, Min(0)] private int spawnWeight = 1;
        [SerializeField, Min(0)] private int durability = 100;
        [SerializeField, Range(0f, 1f)] private float brokenValueMultiplier = 0.5f;
        [SerializeField] private bool throwable = true;
        [SerializeField, Min(0f)] private float carryNoiseRadius;

        public string ItemId => itemId;
        public string LocalizationKey => localizationKey;
        public int Value => value;
        public float WeightKg => weightKg;
        public int RequiredCarriers => requiredCarriers;
        public bool Throwable => throwable;
        public int Durability => durability;
        public float BrokenValueMultiplier => brokenValueMultiplier;
        public int Fragility => fragility;
        public float NoiseRadius => noiseRadius;
        public bool TwoHanded => twoHanded;
        public ItemSize SizeClass => sizeClass;
        public ItemRarity Rarity => rarity;
        public PhysicsProfile PhysicsProfile => physicsProfile;

        /// <summary>Throws when the asset violates the specification constraints.</summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(itemId) || !IsSnakeCase(itemId)) throw new InvalidOperationException("Item id must be lower snake_case.");
            if (string.IsNullOrWhiteSpace(localizationKey)) throw new InvalidOperationException("Localization key is required.");
            if (value < 0 || weightKg < 0.1f || weightKg > 40f) throw new InvalidOperationException("Item value or weight is out of range.");
            if (requiredCarriers is < 1 or > 2) throw new InvalidOperationException("Required carriers must be one or two.");
            if (brokenValueMultiplier is < 0f or > 1f) throw new InvalidOperationException("Broken value multiplier must be between zero and one.");
            if (rarity != ItemRarity.Curse && !string.IsNullOrEmpty(curseId)) throw new InvalidOperationException("Only curse items may define a curse id.");
        }

        private static bool IsSnakeCase(string value)
        {
            foreach (var character in value)
                if (!(character == '_' || char.IsDigit(character) || character is >= 'a' and <= 'z')) return false;
            return value[0] != '_' && value[^1] != '_';
        }
    }
}
