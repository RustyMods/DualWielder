using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DualWielder;

public static class ItemDataExtensions
{
    private static readonly ConditionalWeakTable<ItemDrop.ItemData, ExtraData> extendedItems = new();

    [UsedImplicitly]
    public class ExtraData
    {
        public ItemDrop.ItemData? leftItem;
        public float baseStamina;
        public float baseSecondaryStamina;
        public float baseEitr;
        public float baseSecondaryEitr;
        public bool isDualWielding;

        public HitData.DamageTypes GetDamage(float worldLevel)
        {
            HitData.DamageTypes damages = leftItem?.m_shared.m_damages ?? new();
            if (leftItem?.m_quality > 1) damages.Add(leftItem.m_shared.m_damagesPerLevel, leftItem.m_quality - 1);
            if (worldLevel > 0.0)
            {
                damages.IncreaseEqually(worldLevel * Game.instance.m_worldLevelGearBaseDamage, true);
            }

            return damages;
        }

        public float GetAttackStamina()
        {
            float combinedStamina = baseStamina + leftItem?.m_shared.m_attack.m_attackStamina ?? 0f;
            return combinedStamina * 0.75f;
        }

        public float GetEitr()
        {
            float combinedEitr = baseEitr + leftItem?.m_shared.m_attack.m_attackEitr ?? 0f;
            return combinedEitr * 0.75f;
        }

        public float GetSecondaryEitr()
        {
            float combinedEitr = baseSecondaryEitr + leftItem?.m_shared.m_secondaryAttack.m_attackEitr ?? 0f;
            return combinedEitr * 0.75f;
        }

        public float GetSecondaryStamina()
        {
            float combinedStamina = baseSecondaryStamina + leftItem?.m_shared.m_secondaryAttack.m_attackStamina ?? 0f;
            return combinedStamina * 0.75f;
        }
        public void Reset(ItemDrop.ItemData item)
        {
            item.m_shared.m_attack.m_attackStamina = baseStamina;
            item.m_shared.m_secondaryAttack.m_attackStamina = baseSecondaryStamina;
            item.m_shared.m_attack.m_attackEitr = baseEitr;
            item.m_shared.m_secondaryAttack.m_attackEitr = baseSecondaryEitr;
            leftItem = null;
            baseStamina = 0f;
            baseSecondaryStamina = 0f;
            baseEitr = 0f;
            baseSecondaryEitr = 0f;
        }
    }

    public static ExtraData GetExtraData(this ItemDrop.ItemData item) => extendedItems.GetOrCreateValue(item);

    private static HitData.DamageTypes GetLeftItemDamage(this ItemDrop.ItemData item, float worldLevel) =>
        item.GetExtraData().GetDamage(worldLevel);
    public static bool IsDualWielding(this ItemDrop.ItemData item) => item.GetExtraData().isDualWielding;
    public static void ClearDualWield(this ItemDrop.ItemData item)
    {
        ExtraData data = item.GetExtraData();
        data.isDualWielding = false;
        data.Reset(item);
    }

    public static void SetupDualWield(this ItemDrop.ItemData rightItem, ItemDrop.ItemData leftItem)
    {
        ExtraData data = rightItem.GetExtraData();
        data.isDualWielding = true;
        data.leftItem = leftItem;
        data.baseStamina = rightItem.m_shared.m_attack.m_attackStamina;
        data.baseSecondaryStamina = rightItem.m_shared.m_secondaryAttack.m_attackStamina;
        data.baseEitr = rightItem.m_shared.m_attack.m_attackEitr;
        data.baseSecondaryEitr = rightItem.m_shared.m_secondaryAttack.m_attackEitr;
        rightItem.m_shared.m_attack.m_attackStamina = data.GetAttackStamina();
        rightItem.m_shared.m_secondaryAttack.m_attackStamina = data.GetSecondaryStamina();
        rightItem.m_shared.m_attack.m_attackEitr = data.GetEitr();
        rightItem.m_shared.m_secondaryAttack.m_attackEitr = data.GetSecondaryEitr();
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