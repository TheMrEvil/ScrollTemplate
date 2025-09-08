using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using UnityEngine;

namespace ScrollTemplate
{
    public class Class1 : MelonMod
    {
        internal const string AugmentName = "Scroll of Ferocity";
        internal const string AugmentDetail = "Double your damage (+100% All Damage).";

        internal static bool HasDamageScroll = false; // Manual override flag (optional external control; passive logic is primary)
        internal static object CachedAugmentTree;      // Reference to the constructed or adopted augment tree instance
        private static bool _augmentRegistered;        // Indicates the augment has been fully integrated into GraphDB structures
        private static float _nextAttemptTime;         // Timestamp (RealtimeSinceStartup) when the next registration attempt is permitted
        private static int _attempts;                  // Total registration attempts performed this session
        private static bool _templateFallbackUsed;     // True if a fallback clone-based augment was produced instead of a bespoke instance
        private static bool _injected;                 // True once the augment has been inserted into the PlayerMods collection
        private static bool _dictInjected;             // True once the augment has been inserted into at least one string->AugmentTree lookup dictionary

        // Performance / reflection optimization cache fields
        private static bool _ferocityOwnedCached;      // Cached evaluation indicating if the local player currently owns the augment
        private static float _nextOwnershipCheck;      // Next time (RealtimeSinceStartup) at which augment ownership should be revalidated
        private static object _allDamageEnumValue;     // Cached enum value representing Passive.AbilityValue.AllDamage (for direct equality checks)
        internal static FieldInfo _effectPropsSourceControlField; // Cached FieldInfo for EffectProperties.SourceControl to minimize repeated reflection
        internal static Type _playerControlType;                   // Cached PlayerControl Type for repeated use
        internal static FieldInfo _playerMyInstanceField;          // Cached FieldInfo for PlayerControl.myInstance static reference

        public override void OnInitializeMelon()
        {
            HarmonyInstance.PatchAll();
            InitReflectionCache();
            TryRegisterCustomAugment(true);
        }

        private static void InitReflectionCache()
        {
            try
            {
                var passiveAbilityEnum = AccessTools.TypeByName("Passive+AbilityValue") ?? AccessTools.TypeByName("Passive/AbilityValue");
                if (passiveAbilityEnum != null)
                {
                    try { _allDamageEnumValue = Enum.Parse(passiveAbilityEnum, "AllDamage"); } catch { _allDamageEnumValue = null; }
                }
                var effectPropsType = AccessTools.TypeByName("EffectProperties");
                if (effectPropsType != null)
                {
                    _effectPropsSourceControlField = effectPropsType.GetField("SourceControl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                _playerControlType = AccessTools.TypeByName("PlayerControl");
                if (_playerControlType != null)
                {
                    _playerMyInstanceField = _playerControlType.GetField("myInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
            }
            catch { }
        }

        public override void OnUpdate()
        {
            if (!_augmentRegistered)
            {
                if (Time.realtimeSinceStartup >= _nextAttemptTime && AccessTools.TypeByName("GraphDB") != null)
                {
                    TryRegisterCustomAugment(false);
                    float delay = Mathf.Min(5f, 0.5f + _attempts * 0.5f);
                    _nextAttemptTime = Time.realtimeSinceStartup + delay;
                }
            }
            else
            {
                // Ownership polling performed at a fixed interval to avoid expensive repeated dictionary queries in high-frequency paths
                if (Time.realtimeSinceStartup >= _nextOwnershipCheck)
                {
                    _ferocityOwnedCached = PlayerHasFerocity();
                    _nextOwnershipCheck = Time.realtimeSinceStartup + 0.5f;
                }
            }
        }

        public static void SetDamageScroll(bool enabled)
        {
            HasDamageScroll = enabled;
            MelonLogger.Msg($"Damage Scroll manual override {(enabled ? "ENABLED" : "DISABLED")}");
        }

        internal static void TryRegisterCustomAugment(bool first)
        {
            if (_augmentRegistered) return;
            _attempts++;
            try
            {
                var graphDbType = AccessTools.TypeByName("GraphDB");
                if (graphDbType == null) return;

                var getByName = AccessTools.Method(graphDbType, "GetAugmentByName", new[] { typeof(string) });
                object existing = null;
                if (getByName != null)
                {
                    try { existing = getByName.Invoke(null, new object[] { AugmentName }); } catch { }
                }

                var graphDbInstanceField = graphDbType.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var graphDbInstance = graphDbInstanceField?.GetValue(null);
                if (graphDbInstance == null) return;

                // Construct augment (or reuse an existing instance) if not already cached
                if (CachedAugmentTree == null)
                {
                    CachedAugmentTree = existing ?? BuildAugmentWithDiagnostics();
                    if (CachedAugmentTree == null) return;
                }

                // Register augment in player mod list to integrate with UI / selection surfaces
                var playerModsField = graphDbInstance.GetType().GetField("PlayerMods", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (playerModsField?.GetValue(graphDbInstance) is IList playerModsList)
                {
                    int idx = FindAugmentIndexByName(playerModsList, AugmentName);
                    if (idx >= 0)
                    {
                        CachedAugmentTree = playerModsList[idx];
                        _injected = true;
                    }
                    else if (!_injected)
                    {
                        playerModsList.Add(CachedAugmentTree);
                        _injected = true;
                        MelonLogger.Msg("[Ferocity] Added to PlayerMods list.");
                    }
                }

                // Register augment in any string->AugmentTree dictionaries used for lookup by name
                var augmentTreeType = CachedAugmentTree.GetType();
                foreach (var field in graphDbInstance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var fType = field.FieldType;
                    if (!fType.IsGenericType || fType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) continue;
                    var args = fType.GetGenericArguments();
                    if (args[0] == typeof(string) && args[1] == augmentTreeType)
                    {
                        var dict = field.GetValue(graphDbInstance);
                        if (dict == null) continue;
                        bool contains = false;
                        try { contains = (bool)fType.GetMethod("ContainsKey")!.Invoke(dict, new object[] { AugmentName }); } catch { }
                        if (!contains)
                        {
                            try { fType.GetMethod("Add")!.Invoke(dict, new object[] { AugmentName, CachedAugmentTree }); _dictInjected = true; MelonLogger.Msg("[Ferocity] Added to dictionary '" + field.Name + "'."); } catch { }
                        }
                        else
                        {
                            _dictInjected = true;
                        }
                    }
                }

                bool registered = false;
                if (getByName != null)
                {
                    try { registered = getByName.Invoke(null, new object[] { AugmentName }) != null; } catch { }
                }

                _augmentRegistered = registered && _injected;
                if (_augmentRegistered)
                {
                    MelonLogger.Msg($"[Ferocity] Registration successful (InjectedList={_injected} Dict={_dictInjected} Fallback={_templateFallbackUsed}).");
                }
            }
            catch { }
        }

        private static int FindAugmentIndexByName(IList list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var root = GetRootNode(list[i]);
                if (root == null) continue;
                var nameField = root.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nameField == null) continue;
                var nm = nameField.GetValue(root) as string;
                if (string.Equals(nm, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        internal static object BuildAugmentWithDiagnostics()
        {
            try
            {
                var augmentTreeType = AccessTools.TypeByName("AugmentTree");
                var augmentRootNodeType = AccessTools.TypeByName("AugmentRootNode");
                var modPassiveNodeType = AccessTools.TypeByName("ModPassiveNode");
                if (augmentTreeType == null || augmentRootNodeType == null || modPassiveNodeType == null)
                    return TemplateFallback();

                object root = Activator.CreateInstance(augmentRootNodeType);
                object passive = Activator.CreateInstance(modPassiveNodeType);

                // Root metadata initialization
                SetFieldIfExists(root, "Name", AugmentName);
                SetFieldIfExists(root, "Detail", AugmentDetail);
                var modTypeEnum = AccessTools.TypeByName("ModType");
                if (modTypeEnum != null) { try { SetFieldIfExists(root, "modType", Enum.Parse(modTypeEnum, "Player")); } catch { } }

                // Passive node configuration (+100% AllDamage multiplier)
                SetFieldIfExists(passive, "Multiplier", true);
                SetFieldIfExists(passive, "Value", 1f);
                var categoryEnum = modPassiveNodeType.GetNestedType("Category");
                if (categoryEnum != null) { try { SetFieldIfExists(passive, "category", Enum.Parse(categoryEnum, "Ability")); } catch { } }

                // Ability value assignment (AllDamage)
                var abilityPassiveWrapperField = modPassiveNodeType.GetField("abilityPassive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (abilityPassiveWrapperField != null)
                {
                    var wrapper = abilityPassiveWrapperField.GetValue(passive);
                    var passiveAbilityEnumInner = AccessTools.TypeByName("Passive+AbilityValue") ?? AccessTools.TypeByName("Passive/AbilityValue");
                    if (wrapper != null && passiveAbilityEnumInner != null)
                    {
                        var valField = wrapper.GetType().GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (valField != null) { try { valField.SetValue(wrapper, Enum.Parse(passiveAbilityEnumInner, "AllDamage")); abilityPassiveWrapperField.SetValue(passive, wrapper); } catch { } }
                    }
                }

                // Attach passive to root
                var passivesField = augmentRootNodeType.GetField("Passives", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (passivesField != null) { (passivesField.GetValue(root) as IList)?.Add(passive); }

                // Attempt constructor injection, fallback to field assignment if necessary
                object tree = null;
                foreach (var ctor in augmentTreeType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var pars = ctor.GetParameters();
                    if (pars.Length == 1 && pars[0].ParameterType == augmentRootNodeType)
                    { try { tree = ctor.Invoke(new object[] { root }); break; } catch { } }
                }

                if (tree == null)
                {
                    try { tree = Activator.CreateInstance(augmentTreeType, true); } catch { tree = null; }
                    if (tree != null)
                    {
                        bool rootSet = false;
                        foreach (var f in augmentTreeType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (f.FieldType == augmentRootNodeType)
                            { try { f.SetValue(tree, root); rootSet = true; break; } catch { } }
                        }
                        if (!rootSet) return TemplateFallback();
                    }
                    else return TemplateFallback();
                }

                MelonLogger.Msg("[Ferocity] Built standalone augment.");
                return tree;
            }
            catch { return TemplateFallback(); }
        }

        private static object TemplateFallback()
        {
            if (_templateFallbackUsed && CachedAugmentTree != null) return CachedAugmentTree;
            _templateFallbackUsed = true;
            try
            {
                var graphDbType = AccessTools.TypeByName("GraphDB");
                var modTypeEnum = AccessTools.TypeByName("ModType");
                if (graphDbType == null || modTypeEnum == null) return null;
                var getValidMods = AccessTools.Method(graphDbType, "GetValidMods", new[] { modTypeEnum });
                if (getValidMods == null) return null;
                var playerVal = Enum.Parse(modTypeEnum, "Player");
                var listObj = getValidMods.Invoke(null, new object[] { playerVal }) as IList;
                if (listObj == null || listObj.Count == 0) return null;
                var template = listObj[0];
                if (template == null) return null;
                object clone = null;
                try { clone = template.GetType().InvokeMember("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, template, null); } catch { clone = template; }
                object root = GetRootNode(clone) ?? GetRootNode(template);
                if (root != null)
                {
                    SetFieldIfExists(root, "Name", AugmentName);
                    SetFieldIfExists(root, "Detail", AugmentDetail);
                }
                MelonLogger.Msg("[Ferocity] Using template augment fallback.");
                return clone;
            }
            catch { return null; }
        }

        internal static object GetRootNode(object augmentTree)
        {
            if (augmentTree == null) return null;
            var t = augmentTree.GetType();
            var prop = t.GetProperty("Root", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                try { return prop.GetValue(augmentTree); } catch { }
            }
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.Name.Equals("Root", StringComparison.OrdinalIgnoreCase))
                {
                    try { return f.GetValue(augmentTree); } catch { }
                }
            }
            return null;
        }

        private static void SetFieldIfExists(object obj, string fieldName, object value)
        {
            if (obj == null || value == null) return;
            var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                try { f.SetValue(obj, value); } catch { }
            }
        }

        // Determines if the local player currently has the Ferocity augment in their augment list.
        internal static bool PlayerHasFerocity()
        {
            try
            {
                if (_playerControlType == null || _playerMyInstanceField == null) return false;
                var player = _playerMyInstanceField.GetValue(null);
                if (player == null) return false;
                var augField = _playerControlType.GetField("Augment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var augObj = augField?.GetValue(player);
                if (augObj == null) return false;
                var treesField = augObj.GetType().GetField("trees", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var dict = treesField?.GetValue(augObj) as IDictionary;
                if (dict == null) return false;
                foreach (var key in dict.Keys)
                {
                    var root = key;
                    var nameField = root.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var nm = nameField?.GetValue(root) as string;
                    if (string.Equals(nm, AugmentName, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return false;
        }

        internal static bool FerocityActiveFast() => HasDamageScroll || _ferocityOwnedCached;
        internal static bool IsAllDamageEnum(object val)
        {
            if (_allDamageEnumValue == null || val == null) return false;
            return val.Equals(_allDamageEnumValue);
        }
    }

    // Patch ModifyAbilityPassives so any AllDamage calculation is doubled when player has the Ferocity scroll.
    [HarmonyPatch]
    internal static class Ferocity_ModifyAbilityPassives_Patch
    {
        private static MethodBase TargetMethod()
        {
            var effectPropsType = AccessTools.TypeByName("EffectProperties");
            if (effectPropsType == null) return null;
            var passiveAbilityEnum = AccessTools.TypeByName("Passive+AbilityValue") ?? AccessTools.TypeByName("Passive/AbilityValue");
            if (passiveAbilityEnum == null) return null;
            return AccessTools.Method(effectPropsType, "ModifyAbilityPassives", new[] { passiveAbilityEnum, typeof(float) });
        }

        // Signature: float ModifyAbilityPassives(Passive.AbilityValue valueType, float startVal)
        private static void Postfix(object valueType, float startVal, ref float __result, object __instance)
        {
            try
            {
                if (!Class1.FerocityActiveFast()) return;            // Skip if augment not active
                if (!Class1.IsAllDamageEnum(valueType)) return;      // Only process AllDamage requests
                if (Class1._effectPropsSourceControlField == null || Class1._playerMyInstanceField == null) return;

                var sourceCtrl = Class1._effectPropsSourceControlField.GetValue(__instance);
                if (sourceCtrl == null) return;
                var localPlayer = Class1._playerMyInstanceField.GetValue(null);
                if (localPlayer == null || !ReferenceEquals(sourceCtrl, localPlayer)) return;

                float baseVal = startVal;
                if (Math.Abs(baseVal) < 0.0001f) return;
                // Apply a single +100% increment relative to the original baseline, unless already approximately doubled
                if (__result < baseVal * 1.95f)
                {
                    __result += baseVal;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"Ferocity passive patch failed: {e.Message}");
            }
        }
    }
}
