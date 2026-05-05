using BepInEx;
using HarmonyLib;
using Ostranauts.UI.MegaToolTip;
using Ostranauts.UI.MegaToolTip.DataModules;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

[BepInPlugin("com.bemused.conditiontooltip", "Condition Tooltip", "0.7.0")]
public class ConditionTooltipPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("com.bemused.conditiontooltip").PatchAll();
        Logger.LogInfo("Condition Tooltip 0.7.0 loaded.");
    }
}

internal static class TooltipUtil
{
    public static T GetPrivateField<T>(object obj, string fieldName)
    {
        Type type = obj.GetType();

        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
                return (T)field.GetValue(obj);

            type = type.BaseType;
        }

        return default(T);
    }
}

#region Name + Descriptor + Font Scaling

[HarmonyPatch(typeof(ItemModule), "SetData")]
public static class Patch_ItemModule_SetData
{
    private static void Postfix(ItemModule __instance, CondOwner co)
    {
        try
        {
            if (__instance.GetType() != typeof(ItemModule))
                return;

            TMP_Text txtName = TooltipUtil.GetPrivateField<TMP_Text>(__instance, "_txtFullName");
            TMP_Text txtDesc = TooltipUtil.GetPrivateField<TMP_Text>(__instance, "_txtDescription");

            if (txtName == null || txtDesc == null || co == null)
                return;

            string descriptor = co.GetDamageDescriptor();
            descriptor = descriptor?.Trim().Trim('(', ')');

            txtName.text = string.IsNullOrWhiteSpace(descriptor)
            ? co.strNameFriendly
            : "<color=#00ff00>" + descriptor + "</color>\n" + co.strNameFriendly;

            txtName.fontSize *= 0.85f;
            txtDesc.fontSize *= 0.65f;

            var img = TooltipUtil.GetPrivateField<UnityEngine.UI.RawImage>(__instance, "_imgCO");

            if (img != null)
            {
                GameObject target = img.gameObject;

                if (img.transform.parent != null)
                    target = img.transform.parent.gameObject;

                target.SetActive(false);

                var layout = target.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout == null)
                    layout = target.AddComponent<UnityEngine.UI.LayoutElement>();

                layout.ignoreLayout = true;
                layout.minHeight = 0f;
                layout.preferredHeight = 0f;
                layout.flexibleHeight = 0f;

                var rect = target.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ConditionTooltip] ItemModule patch failed: " + e);
        }
    }
}

#endregion

#region Always Expanded + Remove Show Button

[HarmonyPatch(typeof(ToggleMoreModule), "Start")]
public static class Patch_ToggleMoreModule_Start
{
    private static void Postfix(ToggleMoreModule __instance)
    {
        try
        {
            typeof(ModuleHost)
            .GetProperty("ShowExpandedTooltip", BindingFlags.Public | BindingFlags.Static)
            ?.SetValue(null, true);

            typeof(ModuleHost)
            .GetField("<ShowExpandedTooltip>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static)
            ?.SetValue(null, true);

            __instance.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError("[ConditionTooltip] ToggleMoreModule patch failed: " + e);
        }
    }
}

#endregion

#region Remove $$$ Value Safely

[HarmonyPatch(typeof(ValueModule), "SetData")]
public static class Patch_ValueModule_SetData
{
    private static void Postfix(ValueModule __instance)
    {
        try
        {
            TMP_Text txt = TooltipUtil.GetPrivateField<TMP_Text>(__instance, "_Text");
            if (txt != null)
                txt.text = "";

            var layout = __instance.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layout == null)
                layout = __instance.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();

            layout.ignoreLayout = true;
            layout.preferredHeight = 0f;
            layout.minHeight = 0f;
            layout.flexibleHeight = 0f;

            var cg = __instance.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = __instance.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        catch (Exception e)
        {
            Debug.LogError("[ConditionTooltip] ValueModule patch failed: " + e);
        }
    }
}

#endregion

#region Tag Font Scaling

[HarmonyPatch(typeof(Ostranauts.UI.MegaToolTip.DataModules.SubElements.CondElement), "SetData")]
public static class Patch_CondElement_SetData
{
    private static void Postfix(object __instance)
    {
        try
        {
            TMP_Text[] texts = ((Component)__instance).GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text txt in texts)
            {
                txt.fontSize *= 0.75f;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ConditionTooltip] CondElement patch failed: " + e);
        }
    }
}

#endregion
