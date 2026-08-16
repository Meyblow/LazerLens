using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Toolbar;

namespace LazerLens.UI
{
    public partial class LazerLensToolbarButton : ToolbarButton
    {
        public LazerLensToolbarButton(Action toggleOverlay)
        {
            SetIcon(FontAwesome.Solid.ChartBar);
            TooltipMain = LazerLensStrings.TooltipMain;
            TooltipSub = LazerLensStrings.TooltipSub;
            Action = toggleOverlay;
        }

        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }

}
