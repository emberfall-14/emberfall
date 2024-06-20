﻿using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class TabContainerSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var tabContainerPanel = sheet.GetTexture("tabcontainer_panel.png").IntoPatch(StyleBox.Margin.All, 2);

        var tabContainerBoxActive = new StyleBoxFlat(sheet.SecondaryPalette[1]);
        tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
        var tabContainerBoxInactive = new StyleBoxFlat(sheet.SecondaryPalette[3]);
        tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        return new StyleRule[]
        {
            E<TabContainer>()
                .Prop(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel)
                .Prop(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive)
        };
    }
}
