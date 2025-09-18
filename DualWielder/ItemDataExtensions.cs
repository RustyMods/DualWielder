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
        public float primaryStamina;
        public float secondaryStamina;
        public float baseStamina;
        public float baseSecondaryStamina;
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

        public float GetAttackStamina()
        {
            var combinedStamina = baseStamina + primaryStamina;
            return combinedStamina * 0.75f;
        }

        public float GetSecondaryStamina()
        {
            var combinedStamina = baseSecondaryStamina + secondaryStamina;
            return combinedStamina * 0.75f;
        }
        public void Reset(ItemDrop.ItemData item)
        {
            item.m_shared.m_attack.m_attackStamina = baseStamina;
            item.m_shared.m_secondaryAttack.m_attackStamina = baseSecondaryStamina;
            leftItemQuality = 1;
            leftItemDamagesPerLevel = new();
            leftItemDamage = new();
            primaryStamina = 0f;
            secondaryStamina = 0f;
            baseStamina = 0f;
            baseSecondaryStamina = 0f;
        }
    }

    public static ExtraData GetExtraData(this ItemDrop.ItemData item) => extendedItems.GetOrCreateValue(item);

    private static HitData.DamageTypes GetLeftItemDamage(this ItemDrop.ItemData item, float worldLevel) =>
        item.GetExtraData().GetDamage(worldLevel);

    private static void SetLeftItemDamage(this ItemDrop.ItemData item, ItemDrop.ItemData leftItem)
    {
        ExtraData data = item.GetExtraData();
        data.leftItemDamage = leftItem.m_shared.m_damages;
        data.leftItemDamagesPerLevel = leftItem.m_shared.m_damagesPerLevel;
        data.leftItemQuality = leftItem.m_quality;
        data.primaryStamina = leftItem.m_shared.m_attack.m_attackStamina;
        data.secondaryStamina = leftItem.m_shared.m_secondaryAttack.m_attackStamina;
        data.baseStamina = item.m_shared.m_attack.m_attackStamina;
    }

    public static bool IsDualWielding(this ItemDrop.ItemData item) => item.GetExtraData().isDualWielding;

    public static void SetDualWielding(this ItemDrop.ItemData item, bool isDualWielding)
    {
        var data = item.GetExtraData();
        data.isDualWielding = isDualWielding;
        if (!isDualWielding)
        {
            data.Reset(item);
        }
    }

    public static void SetupDualWield(this ItemDrop.ItemData rightItem, ItemDrop.ItemData leftItem)
    {
        rightItem.SetLeftItemDamage(leftItem);
        rightItem.SetDualWielding(true);
        ExtraData data = rightItem.GetExtraData();
        rightItem.m_shared.m_attack.m_attackStamina = data.GetAttackStamina();
        rightItem.m_shared.m_secondaryAttack.m_attackStamina = data.GetSecondaryStamina();
    }

    public static HitData.DamageTypes GetTotalDamage(this ItemDrop.ItemData item, float worldLevel, HitData.DamageTypes defaultValue)
    {
        if (!DualWielderPlugin.CombineDamages) return defaultValue;
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