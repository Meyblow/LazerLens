using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Toolbar;

namespace LazerLens.UI
{
    public partial class LazerLensToolbarButton : ToolbarOverlayToggleButton
    {
        public LazerLensToolbarButton(Action toggleOverlay, OverlayContainer? overlay = null)
        {
            SetIcon(FontAwesome.Solid.ChartBar);
            TooltipMain = LazerLensStrings.TooltipMain;
            TooltipSub = LazerLensStrings.TooltipSub;
            TooltipText = LazerLensStrings.TooltipMain;
            Action = toggleOverlay;

            if (overlay != null)
                StateContainer = overlay;
        }

        public override LocalisableString TooltipText => LazerLensStrings.TooltipMain;
        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}
