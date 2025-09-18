using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using SkillManager;

namespace DualWielder;

public class DualWield : MonoBehaviour
{
    private ItemDrop.ItemData? m_rightItem;
    private ItemDrop.ItemData? m_leftItem;
    private bool m_isDualWielding;
    private string m_lastLeftItem = "";
    private bool m_shouldChangeAttachPoint;

    private Transform? m_backLeftMelee;
    private readonly DualWieldState _state = new();

    private class DualWieldState
    {
        public bool wasDualWielding;
        public ItemDrop.ItemData? hiddenLeft;
        public ItemDrop.ItemData? hiddenRight;
    }
    
    public void Awake()
    {
        Transform? backOneHandAttach = Utils.FindChild(transform, "BackOneHanded_attach");
        Transform? go = Instantiate(backOneHandAttach, backOneHandAttach.parent);
        go.name = "BackOneHanded_left_attach";
        go.localPosition = new Vector3(-0.0028f, 0.0047f, -0.0022f);
        go.localRotation = Quaternion.Euler(117.401f, -91.143f, -85.99f);
        m_backLeftMelee = go;
    }
    
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            __instance.m_playerPrefab.AddComponent<DualWield>();
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    private static class Attack_Start_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Attack __instance, Humanoid character)
        {
            if (!character.IsPlayer()) return;
            if (character.GetRightItem() is not { m_shared.m_itemType: ItemDrop.ItemData.ItemType.OneHandedWeapon } rightItem 
                || character.GetLeftItem() is not { m_shared.m_itemType: ItemDrop.ItemData.ItemType.OneHandedWeapon } leftItem) return;

            string normalAttack = __instance.m_attackAnimation;

            bool hasKnife = rightItem.m_shared.m_skillType is Skills.SkillType.Knives || leftItem.m_shared.m_skillType is Skills.SkillType.Knives;
            bool hasAxes = rightItem.m_shared.m_skillType is Skills.SkillType.Axes || leftItem.m_shared.m_skillType is Skills.SkillType.Axes;
            
            __instance.m_attackAnimation = normalAttack.EndsWith("_secondary") ? hasAxes ? "dualaxes_secondary" : "dual_knives_secondary" : hasKnife ? "dual_knives" : "dualaxes";
            __instance.m_attackChainLevels = normalAttack.EndsWith("_secondary") ? 1 : 4;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.ModifyDamage))]
    private static class Attack_ModifyDamage_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Attack __instance, HitData hitData, float damageFactor)
        {
            if (!DualWielderPlugin.CombineDamages) return;
            if (__instance.m_character is not Player player) return;
            if (!player.gameObject.TryGetComponent(out DualWield component)) return;
            if (!component.m_isDualWielding) return;
            float skillFactor = 1 + player.GetSkillFactor("DualWielder") * (1 - DualWielderPlugin.DamageModifier);
            hitData.m_damage.Modify(skillFactor);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Character __instance, HitData? hit)
        {
            if (__instance.IsPlayer() || hit?.GetAttacker() is not {} attacker || attacker is not Player player) return;
            if (!player.gameObject.TryGetComponent(out DualWield component)) return;
            if (!component.m_isDualWielding) return;
            player.RaiseSkill("DualWielder");
        }
    }
    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
    private static class AttachItem_Override
    {
        [UsedImplicitly]
        private static void Prefix(VisEquipment __instance, int itemHash, ref Transform joint)
        {
            if (!__instance.TryGetComponent(out DualWield dualWield)) return;
            if (!dualWield.m_shouldChangeAttachPoint) return;
            if (joint != __instance.m_backMelee) return;
            if (ObjectDB.instance.GetItemPrefab(itemHash) is not { } item || !item.TryGetComponent(out ItemDrop component)) return;
            if (component.m_itemData.m_shared.m_name != dualWield.m_lastLeftItem) return;
            joint = dualWield.m_backLeftMelee == null ? __instance.m_backTool : dualWield.m_backLeftMelee;

            dualWield.m_lastLeftItem = "";
            dualWield.m_shouldChangeAttachPoint = false;
        }
    }
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class Humanoid_EquipItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
        {
            if (!__instance.TryGetComponent(out DualWield dualWield)) return true;
            if (__instance.GetRightItem() is not { m_shared.m_itemType: ItemDrop.ItemData.ItemType.OneHandedWeapon} rightItem) return true;
            if (item.IsHarpoon() || item.IsDualItem()) return true;
            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon) return true;
            
            if (__instance.GetLeftItem() is { } leftItem)
            {
                if (leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield) return true;
                __instance.UnequipItem(leftItem, false);
            }

            if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc))
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_dlcrequired");
                return false;
            }

            if (Game.m_worldLevel > 0 && item.m_worldLevel < Game.m_worldLevel &&
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_ng_item_too_low");
                return false;
            }
            
            __instance.m_leftItem = item;
            __instance.m_leftItem.m_equipped = true;
            __instance.m_hiddenLeftItem = null;
            __instance.m_hiddenRightItem = null;

            dualWield.m_rightItem = rightItem;
            dualWield.m_leftItem = item;

            rightItem.SetupDualWield(item);
            
            if (dualWield.m_rightItem.m_shared.m_attachOverride is not ItemDrop.ItemData.ItemType.Tool && dualWield.m_leftItem.m_shared.m_attachOverride is not ItemDrop.ItemData.ItemType.Tool)
            {
                dualWield.m_shouldChangeAttachPoint = true;
            }
            
            dualWield.m_isDualWielding = true;
            __result = true;

            if (__instance.IsItemEquiped(item)) item.m_equipped = true;
            __instance.SetupEquipment();
            if (triggerEquipEffects) __instance.TriggerEquipEffect(item);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
    private static class ItemDrop_ItemData_GetDamage_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, float worldLevel, ref HitData.DamageTypes __result)
        {
            if (!__instance.IsDualWielding()) return;
            __result = __instance.GetTotalDamage(worldLevel, __result);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class Humanoid_UnequipItem_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (__instance.IsDead()) return;
            if (!__instance.TryGetComponent(out DualWield dualWield)) return;
            if (dualWield.m_rightItem != item && dualWield.m_leftItem != item) return;
            dualWield.m_lastLeftItem = dualWield.m_leftItem?.m_shared.m_name ?? "";
            
            dualWield.m_rightItem?.SetDualWielding(false);
            
            if (item == dualWield.m_rightItem && dualWield.m_leftItem != null)
            {
                __instance.m_rightItem = dualWield.m_leftItem;
                __instance.m_leftItem = null;
                __instance.SetupVisEquipment(__instance.m_visEquipment, false);
            }

            dualWield.m_rightItem = null;
            dualWield.m_leftItem = null;
            dualWield.m_isDualWielding = false;
        }
    }
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.HideHandItems))]
    private static class Humanoid_HideHandItems_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Humanoid __instance)
        {
            if (!__instance.TryGetComponent(out DualWield dualWield) || !dualWield.m_isDualWielding) return;
            dualWield._state.wasDualWielding = true;
            dualWield._state.hiddenLeft = __instance.m_leftItem;
            dualWield._state.hiddenRight = __instance.m_rightItem;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.ShowHandItems))]
    private static class Humanoid_ShowHandItems_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid __instance, bool onlyRightHand, bool animation)
        {
            if (onlyRightHand) return true;
            if (!__instance.TryGetComponent(out DualWield dualWield) || !dualWield._state.wasDualWielding) return true;

            __instance.EquipItem(dualWield._state.hiddenRight); // needed to make sure right item was equipped first
            __instance.EquipItem(dualWield._state.hiddenLeft);
            
            dualWield._state.wasDualWielding = false;
            dualWield._state.hiddenLeft = null;
            dualWield._state.hiddenRight = null;

            if (animation) __instance.m_zanim.SetTrigger("equip_hip");
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    private static class ItemDrop_ItemData_GetTooltip_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            if (!item.IsDualWielding()) return;
            __result += $"\n<color=orange>❖ {DualWielderPlugin.DualWieldKey} ❖</color>";
        }
    }
}