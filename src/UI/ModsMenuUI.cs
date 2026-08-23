using System;
using ModLoader.UI;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;
using UnityEngine.UI;
using static BetterModMenu.Settings.Settings;

namespace BetterModMenu.UI;

public static class ModsMenuUI
{
    #region References and State

    private static ScrollElement _modsMenuScrollElement;
    private static bool _uiInjected;
    private static RectTransform _menuRect;

    #endregion

    #region UI Constants

    private const int FilterHeight = 40;
    private const int FilterSpacing = 90;
    private const float MenuHeightIncrease = 60f;

    #endregion


    #region UI Logic

    internal static void InjectUI(ModsMenu menu)
    {
        if (_uiInjected) return;
        _uiInjected = true;

        _menuRect = menu.menuHolder.Rect();

        _modsMenuScrollElement = menu.elementsHolder.GetComponent<ScrollElement>();
        
        var filterBar = new GameObject("BetterModMenu_FilterBar", typeof(RectTransform));
        filterBar.transform.SetParent(menu.menuHolder.transform, false);
        
        filterBar.Rect().anchoredPosition = new Vector2(0, (int)(_menuRect.sizeDelta.y/2) - MenuHeightIncrease/2);
        
        filterBar.Rect().sizeDelta = new Vector2(menu.menuHolder.Rect().sizeDelta.x - 50, MenuHeightIncrease);
        
        _menuRect.sizeDelta += new Vector2(0, MenuHeightIncrease);
        
        menu.menuHolder.gameObject.transform.Find("Scroll Window").Rect().anchoredPosition -= new Vector2(0, MenuHeightIncrease);
        menu.menuHolder.gameObject.transform.Find("Scroll Window").Rect().sizeDelta -= new Vector2(0, MenuHeightIncrease);
        
        var hlg = filterBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = FilterSpacing;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter; 

        var parent = filterBar.Rect();

        CreateFilterToggle(parent, "Texture Packs",
            () => Filters.showTexturePacks,
            v => { Filters.showTexturePacks = v; OnFilterChanged(); });

        CreateFilterToggle(parent, "Code Mods",
            () => Filters.showCodeMods,
            v => { Filters.showCodeMods = v; OnFilterChanged(); });

        CreateFilterToggle(parent, "Part Packs",
            () => Filters.showPartPacks,
            v => { Filters.showPartPacks = v; OnFilterChanged(); });

        CreateFilterToggle(parent, "Solar Systems",
            () => Filters.showCustomSolarSystems,
            v => { Filters.showCustomSolarSystems = v; OnFilterChanged(); });

        var smgButton = Builder.CreateButton(
            parent: menu.menuHolder.transform,
            width: (int)_menuRect.sizeDelta.x,
            height: 60,
            text: "SFS Modding Guide",
            onClick: () => Application.OpenURL("https://kojamori.github.io/SFS-Modding-Guide/")
        );
        smgButton.rectTransform.anchoredPosition = new Vector2(0, -_menuRect.sizeDelta.y/2 - 60);
    }

    private static void CreateFilterToggle(Transform parent, string label,
        Func<bool> getter, Action<bool> setter)
    {
        var filter = Builder.CreateToggleWithLabel(
            parent, width: -1, height: FilterHeight, labelText: label,
            getToggleValue: getter,
            onChange: () =>
            {
                setter(!getter());
                OnFilterChanged();
            }
        );

        var le = filter.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minWidth = 80;
        le.preferredHeight = FilterHeight;
    
        // CRITICAL: Clear any residual sizeDelta.x that Builder may have set
        filter.rectTransform.sizeDelta = new Vector2(0, filter.rectTransform.sizeDelta.y);
    }

    private static void OnFilterChanged()
    {
        ModsMenu.main?.DrawUI();
        _modsMenuScrollElement?.ResetPosition();
        Save();
    }

    #endregion
    
}
