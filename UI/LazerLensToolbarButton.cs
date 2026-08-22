using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays.Toolbar;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;

namespace LazerLens.UI
{
    public partial class LazerLensToolbarButton : ToolbarOverlayToggleButton
    {
        private readonly LazerLensService service;
        private Container badgeContainer = null!;
        private Box badgeBackground = null!;
        private OsuSpriteText badgeText = null!;

        public LazerLensToolbarButton(Action toggleOverlay, LazerLensService service, OverlayContainer? overlay = null)
        {
            this.service = service;
            SetIcon(FontAwesome.Solid.ChartBar);
            TooltipMain = LazerLensStrings.TooltipMain;
            TooltipSub = LazerLensStrings.TooltipSub;
            TooltipText = LazerLensStrings.TooltipMain;
            Action = toggleOverlay;

            if (overlay != null)
                StateContainer = overlay;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(badgeContainer = new Container
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.Centre,
                Position = new Vector2(-6, 8),
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 6,
                Alpha = 0,
                Children = new Drawable[]
                {
                    badgeBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex(service.ToolbarBadgeColor.Value),
                    },
                    badgeText = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.Torus.With(size: 9.5f, weight: FontWeight.Bold),
                        Colour = Color4.Black,
                        Padding = new MarginPadding { Horizontal = 3, Vertical = 1 },
                    }
                }
            });

            service.ToolbarBadge.BindValueChanged(_ => updateBadge(), true);
            service.ToolbarBadgeColor.BindValueChanged(e =>
            {
                if (IsDisposed) return;
                try
                {
                    badgeBackground.Colour = Color4Extensions.FromHex(e.NewValue);
                }
                catch
                {
                    badgeBackground.Colour = Color4Extensions.FromHex("#00d2ff");
                }
            }, true);
            service.OnSessionUpdated += updateBadge;
        }

        private void updateBadge()
        {
            if (IsDisposed) return;

            var mode = service.ToolbarBadge.Value;
            var state = service.LiveState;

            if (mode == ToolbarBadgeMode.None || state.TotalPlays == 0)
            {
                badgeContainer.FadeOut(150);
                return;
            }

            string text = mode switch
            {
                ToolbarBadgeMode.PlayCount => state.TotalPlays.ToString(CultureInfo.InvariantCulture),
                ToolbarBadgeMode.PpGain => state.SessionPPGain >= 0 ? $"+{state.SessionPPGain:F0}" : $"{state.SessionPPGain:F0}",
                ToolbarBadgeMode.Accuracy => $"{state.AverageAccuracy:F0}%",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(text))
            {
                badgeContainer.FadeOut(150);
                return;
            }

            badgeText.Text = text;
            badgeContainer.FadeIn(150);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            service.OnSessionUpdated -= updateBadge;
        }

        public override LocalisableString TooltipText => LazerLensStrings.TooltipMain;
        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}
