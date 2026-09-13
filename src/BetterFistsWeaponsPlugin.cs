using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SwmarlyValheimBetterFistsWeapons
{
    /// <summary>
    /// Entry point for the mod. The plugin intentionally has no Jötunn dependency because
    /// it only edits vanilla item data and does not register new content.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    [BepInProcess("valheim_server.x86_64")]
    public sealed class BetterFistsWeaponsPlugin : BaseUnityPlugin
    {
        internal const string Guid = "com.swmarly.valheimbetterfistsweapons";
        internal const string Name = "Swmarly Valheim Better Fists Weapons";
        internal const string Version = "1.0.0";

        internal static ManualLogSource Log;

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            harmony = new Harmony(Guid);

            try
            {
                // ObjectDB is rebuilt for the actual world after the menu database. The
                // postfix therefore applies the change to whichever database Valheim builds.
                harmony.PatchAll(typeof(BetterFistsWeaponsPlugin).Assembly);
                Log.LogInfo(Name + " " + Version + " loaded.");
            }
            catch (Exception exception)
            {
                Log.LogError("Failed to install the ObjectDB patch. The mod will remain disabled: " + exception);
            }
        }

        private void OnDestroy()
        {
            if (harmony == null)
            {
                return;
            }

            harmony.UnpatchSelf();
            harmony = null;
        }
    }

    /// <summary>
    /// Runs after vanilla has populated ObjectDB.m_items. This is the point at which the
    /// vanilla knife and fist weapon shared data are available and safe to edit.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDbAwakePatch
    {
        private static void Postfix(ObjectDB __instance)
        {
            try
            {
                FistWeaponAttackReplacer.Apply(__instance);
            }
            catch (Exception exception)
            {
                BetterFistsWeaponsPlugin.Log.LogError("Failed to replace fist weapon secondary attacks: " + exception);
            }
        }
    }

    /// <summary>
    /// Copies the actual knife secondary attack rather than re-creating it by hand.
    ///
    /// This matters because an Attack contains much more than its animation: stamina,
    /// timing, range, hit type, damage multiplier, stagger multiplier, and other combat
    /// rules all live on the Attack object. Cloning the knife object keeps all of those
    /// values together and preserves the knife's special-attack damage boost.
    /// </summary>
    internal static class FistWeaponAttackReplacer
    {
        private const string PreferredKnifePrefab = "KnifeFlint";

        internal static void Apply(ObjectDB objectDb)
        {
            if (objectDb == null || objectDb.m_items == null || objectDb.m_items.Count == 0)
            {
                return;
            }

            Attack knifeSecondaryAttack = FindKnifeSecondaryAttack(objectDb);
            if (knifeSecondaryAttack == null)
            {
                BetterFistsWeaponsPlugin.Log.LogWarning(
                    "No vanilla knife with a secondary attack was found; fist weapons were left unchanged.");
                return;
            }

            int replacedCount = 0;
            foreach (GameObject prefab in objectDb.m_items)
            {
                // Valheim 1.0 can place non-item prefabs in m_items. Never assume every
                // entry has an ItemDrop component.
                if (prefab == null)
                {
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null)
                {
                    continue;
                }

                ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
                if (shared.m_skillType != Skills.SkillType.Unarmed)
                {
                    continue;
                }

                // Keep the original fist weapon's attack effects. The primary attack is
                // where vanilla stores the fist slash audio and its related VFX. Copy only
                // effect lists, not combat values, so the knife attack still supplies the
                // heavy-attack animation, timing, stamina cost, range, and damage boost.
                Attack fistAttack = shared.m_attack ?? shared.m_secondaryAttack;
                Attack replacementAttack = knifeSecondaryAttack.Clone();
                CopyAttackEffectLists(fistAttack, replacementAttack);

                // Valheim clones this object again when an attack starts, so each weapon
                // receives its own isolated copy while the shared definition stays stable.
                shared.m_secondaryAttack = replacementAttack;
                replacedCount++;

                BetterFistsWeaponsPlugin.Log.LogDebug(
                    "Applied the vanilla knife secondary attack to " + prefab.name + ".");
            }

            BetterFistsWeaponsPlugin.Log.LogInfo(
                "Applied the vanilla knife secondary attack to " + replacedCount + " Unarmed/Fists item(s).");
        }

        private static Attack FindKnifeSecondaryAttack(ObjectDB objectDb)
        {
            Attack fallback = null;

            foreach (GameObject prefab in objectDb.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null)
                {
                    continue;
                }

                ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
                if (shared.m_skillType != Skills.SkillType.Knives || shared.m_secondaryAttack == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(shared.m_secondaryAttack.m_attackAnimation))
                {
                    continue;
                }

                // Flint knife is the ordinary vanilla knife and is the least surprising
                // source. If it is absent, any real knife secondary attack is equivalent
                // for the purpose of this mod and is used as a compatibility fallback.
                if (prefab.name == PreferredKnifePrefab)
                {
                    return shared.m_secondaryAttack;
                }

                fallback = shared.m_secondaryAttack;
            }

            return fallback;
        }

        private static void CopyAttackEffectLists(Attack source, Attack target)
        {
            if (source == null || target == null)
            {
                return;
            }

            // EffectList contains the Unity effect prefabs that drive attack audio and VFX.
            // Copy every EffectList field so this remains compatible with effects added to
            // the Attack class by the current Valheim build, without copying damage or
            // animation fields from the fist's normal attack.
            FieldInfo[] fields = typeof(Attack).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(EffectList))
                {
                    field.SetValue(target, field.GetValue(source));
                }
            }
        }
    }
}
