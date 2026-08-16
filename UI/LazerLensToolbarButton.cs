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
            TooltipMain = "Lazer Lens";
            TooltipSub = "View live session metrics & play history";
            Action = toggleOverlay;
        }

        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}

