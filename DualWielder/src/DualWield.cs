using HarmonyLib;
using UnityEngine;

namespace DualWielder;

public static class DualWield
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    private static class Load_DualWield_Animation
    {
        private static void Prefix(
            Attack __instance, 
            Humanoid character, 
            ref ItemDrop.ItemData weapon, 
            Attack previousAttack, 
            float timeSinceLastAttack)
        {
            if (character != Player.m_localPlayer) return;
            if (character.m_rightItem == null || character.m_leftItem == null) return;
            if (character.m_rightItem.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.OneHandedWeapon) return;
            if (character.m_leftItem.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.OneHandedWeapon) return;
            
            string normalAttack = __instance.m_attackAnimation;
            bool secondary = normalAttack.EndsWith("_secondary");

            if (character.m_rightItem.m_shared.m_skillType is Skills.SkillType.Spears && secondary) return;

            bool hasKnife = character.m_rightItem.m_shared.m_skillType is Skills.SkillType.Knives || character.m_leftItem.m_shared.m_skillType is Skills.SkillType.Knives;
            bool hasAxes = character.m_rightItem.m_shared.m_skillType is Skills.SkillType.Axes || character.m_leftItem.m_shared.m_skillType is Skills.SkillType.Axes;

            __instance.m_attackAnimation = secondary ? hasAxes ? "dualaxes_secondary" : "dual_knives_secondary" : hasKnife ? "dual_knives" : "dualaxes";
            __instance.m_attackChainLevels = secondary ? 1 : 4;

            if (previousAttack != null && previousAttack.m_attackAnimation == __instance.m_attackAnimation)
            {
                var previousChainLevel = previousAttack.m_nextAttackChainLevel;
                if (previousChainLevel >= __instance.m_attackChainLevels || timeSinceLastAttack > 0.20000000298023224)
                {
                    previousChainLevel = 0;
                }

                switch (previousChainLevel)
                {
                    case 1 or 3:
                        weapon = character.m_leftItem;
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class Equip_DualWield_Item
    {
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects,
            ref bool __result)
        {
            if (item.m_shared.m_name == "$item_spear_chitin" || __instance.m_rightItem?.m_shared.m_name == "$item_spear_chitin") return true;
            
            if (item.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.OneHandedWeapon) return true;
            if (__instance.m_rightItem?.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.OneHandedWeapon)
                return true;

            if (__instance.IsItemEquiped(item) || !__instance.m_inventory.ContainsItem(item) || __instance.InAttack() ||
                __instance.InDodge() || __instance.IsDead() || __instance.IsSwimming() || !__instance.IsOnGround() ||
                (item.m_shared.m_useDurability && item.m_durability <= 0.0)) return true;

            if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc)) return true;

            if (Game.m_worldLevel > 0 && item.m_worldLevel < Game.m_worldLevel &&
                (item.m_shared.m_itemType is ItemDrop.ItemData.ItemType.Trinket or ItemDrop.ItemData.ItemType.Utility))
            {
                return true;
            }

            if (__instance.m_leftItem != null)
            {
                __instance.UnequipItem(__instance.m_leftItem, triggerEquipEffects);
            }

            __instance.m_leftItem = item;
            __instance.m_leftItem.m_equipped = true;

            item.m_shared.m_equipEffect.Create(__instance.m_visEquipment.m_leftHand.position,
                __instance.m_visEquipment.m_leftHand.rotation);

            __instance.m_hiddenLeftItem = null;
            
            __instance.SetupEquipment();
            if (triggerEquipEffects) __instance.TriggerEquipEffect(item);
            
            __result = true;
            return true;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class Humanoid_UnequipItem_Prefix
    {
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (__instance.m_rightItem != item || __instance.m_leftItem == null) return;
            if (__instance.m_leftItem.m_shared.m_itemType is not ItemDrop.ItemData.ItemType.OneHandedWeapon) return;
            (__instance.m_rightItem, __instance.m_leftItem) = (__instance.m_leftItem, __instance.m_rightItem);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.Awake))]
    private static class Setup_Alternate_BackAttach_Joints
    {
        private static void Postfix(VisEquipment __instance)
        {
            if (__instance.m_backMelee == null || __instance.m_backTool == null) return;
            
            var leftBackMelee = Object.Instantiate(__instance.m_backMelee.gameObject, __instance.m_backMelee.transform.parent);
            leftBackMelee.name = "left_back_melee";

            leftBackMelee.transform.localRotation = Quaternion.Euler(123.57f, -82.526f, 86.67f);
            leftBackMelee.transform.localPosition = new Vector3(-0.00207f, 0.00451f, -0.00187f);
            
            var leftBackTool = Object.Instantiate(__instance.m_backTool.gameObject, __instance.m_backTool.transform.parent);
            leftBackTool.name = "left_back_tool";
            leftBackTool.transform.localRotation = Quaternion.Euler(101.664f, 90.17902f, -179.256f);
            leftBackTool.transform.localPosition = new Vector3(-0.00239f, 0.00187f, 0f);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachBackItem))]
    private static class Relocate_DualWield_BackAttach_Point
    {
        private static void Postfix(VisEquipment __instance, GameObject __result, int hash, bool rightHand)
        {
            if (rightHand) return;
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(hash);
            if (itemPrefab == null) return;
            ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
            switch (component.m_itemData.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None ? 
                        component.m_itemData.m_shared.m_attachOverride : 
                        component.m_itemData.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                    var leftBackMelee = Utils.FindChild(__instance.transform, "left_back_melee");
                    if (leftBackMelee != null)
                    {
                        __result.transform.SetParent(leftBackMelee);
                        __result.transform.localPosition = Vector3.zero;
                        __result.transform.localRotation = Quaternion.identity;
                    }
                    break;
                case ItemDrop.ItemData.ItemType.Tool:
                    var leftBackTool = Utils.FindChild(__instance.transform, "left_back_tool");
                    if (leftBackTool != null)
                    {
                        __result.transform.SetParent(leftBackTool);
                        __result.transform.localPosition = Vector3.zero;
                        __result.transform.localRotation = Quaternion.identity;
                    }
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    private static class Switch_DualWeapons_Hands
    {
        private static float lastSwitchTime;
        
        private static void Postfix(Player __instance)
        {
            if (Time.time - lastSwitchTime < 0.3f) return;
            
            if (DualWielderPlugin.SwitchKey.IsPressed())
            {
                if (__instance.m_rightItem != null && __instance.m_leftItem != null &&
                    __instance.m_rightItem.m_shared.m_itemType is ItemDrop.ItemData.ItemType.OneHandedWeapon &&
                    __instance.m_leftItem.m_shared.m_itemType is ItemDrop.ItemData.ItemType.OneHandedWeapon)
                {
                    (__instance.m_rightItem, __instance.m_leftItem) = (__instance.m_leftItem, __instance.m_rightItem);
                    __instance.SetupVisEquipment(__instance.m_visEquipment, false);
                    
                    __instance.TriggerEquipEffect(__instance.m_rightItem);
                }

                lastSwitchTime = Time.time;
            }
        }
    }
}