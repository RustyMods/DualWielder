using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DualWielder;

public static class ItemDataExtensions
{
    private static readonly ConditionalWeakTable<ItemDrop.ItemData, ExtraData> extendedItems = new();

    [UsedImplicitly]
    public class ExtraData
    {
        public int leftItemQuality = 1;
        public HitData.DamageTypes leftItemDamagesPerLevel = new();
        public HitData.DamageTypes leftItemDamage = new();
        public bool isDualWielding;

        public HitData.DamageTypes GetDamage(float worldLevel)
        {
            HitData.DamageTypes damages = leftItemDamage;
            if (leftItemQuality > 1) damages.Add(leftItemDamagesPerLevel, leftItemQuality - 1);
            if (worldLevel > 0.0)
            {
                damages.IncreaseEqually(worldLevel * Game.instance.m_worldLevelGearBaseDamage, true);
            }

            return damages;
        }

        public void Clear()
        {
            leftItemQuality = 1;
            leftItemDamagesPerLevel = new();
            leftItemDamage = new();
        }
    }

    private static ExtraData GetExtraData(this ItemDrop.ItemData item) => extendedItems.GetOrCreateValue(item);

    private static HitData.DamageTypes GetLeftItemDamage(this ItemDrop.ItemData item, float worldLevel) =>
        item.GetExtraData().GetDamage(worldLevel);

    private static void SetLeftItemDamage(this ItemDrop.ItemData item, HitData.DamageTypes damageType, HitData.DamageTypes damagePerLevel, int quality)
    {
        ExtraData data = item.GetExtraData();
        data.leftItemDamage = damageType;
        data.leftItemDamagesPerLevel = damagePerLevel;
        data.leftItemQuality = quality;
    }

    public static bool IsDualWielding(this ItemDrop.ItemData item) => item.GetExtraData().isDualWielding;

    public static void SetDualWielding(this ItemDrop.ItemData item, bool isDualWielding)
    {
        var data = item.GetExtraData();
        data.isDualWielding = isDualWielding;
        if (!isDualWielding) data.Clear();
    }

    public static void SetupDualWield(this ItemDrop.ItemData rightItem, ItemDrop.ItemData leftItem)
    {
        rightItem.SetLeftItemDamage(leftItem.m_shared.m_damages, leftItem.m_shared.m_damagesPerLevel, leftItem.m_quality);
        rightItem.SetDualWielding(true);
    }

    public static HitData.DamageTypes GetTotalDamage(this ItemDrop.ItemData item, float worldLevel, HitData.DamageTypes defaultValue)
    {
        if (!DualWielderPlugin.ApplyLeftHandedDamage) return defaultValue;
        if (!item.IsDualWielding()) return defaultValue;
        HitData.DamageTypes totalDamage = defaultValue.Clone();
        totalDamage.Add(item.GetLeftItemDamage(worldLevel));
        totalDamage.Modify(DualWielderPlugin.DamageModifier);
        return totalDamage;
    }
    
    public static bool IsHarpoon(this ItemDrop.ItemData itemData) => itemData.m_shared.m_name == "$item_spear_chitin";
    public static bool IsDualItem(this ItemDrop.ItemData itemData) => IsDualItem(itemData.m_shared.m_name);
    public static bool IsDualItem(string name) => name.StartsWith("Dual") || IsBerzekr(name) || IsSkollAndHati(name);
    public static bool IsBerzekr(string name) => name.StartsWith("AxeBerzerkr") || name.StartsWith("$item_axe_berzerkr") ||  name is "AxeBerzerkr";
    public static bool IsSkollAndHati(string name) => name is "KnifeSkollAndHati" or "$item_knife_skollandhati";
}