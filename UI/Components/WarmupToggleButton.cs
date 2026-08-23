using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osuTK;
using osuTK.Graphics;
using LazerLens.Services;

namespace LazerLens.UI.Components
{
    public sealed partial class WarmupToggleButton : OsuClickableContainer
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private NotificationOverlay? notifications { get; set; }

        private readonly LazerLensService service;
        private Box background = null!;
        private Container borderContainer = null!;
        private SpriteIcon icon = null!;
        private OsuSpriteText text = null!;

        public WarmupToggleButton(LazerLensService service)
        {
            this.service = service;
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = borderContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 6,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Padding = new MarginPadding { Horizontal = 10, Vertical = 6 },
                        Children = new Drawable[]
                        {
                            icon = new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(13),
                                Icon = FontAwesome.Solid.Coffee,
                                Colour = Color4Extensions.FromHex("#ff9800"),
                            },
                            text = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                Colour = Color4.White,
                            }
                        }
                    }
                }
            };

            Action = toggle;

            service.IsWarmupMode.BindValueChanged(v => updateVisualState(v.NewValue), true);
        }

        private void toggle()
        {
            bool next = !service.IsWarmupMode.Value;
            service.IsWarmupMode.Value = next;

            notifications?.Post(new SimpleNotification
            {
                Text = next ? LazerLensStrings.WarmupModeEnabled.ToString() : LazerLensStrings.WarmupModeDisabled.ToString(),
                Icon = FontAwesome.Solid.Coffee
            });
        }

        private void updateVisualState(bool isActive)
        {
            if (IsDisposed || background == null || text == null) return;

            if (isActive)
            {
                background.FadeColour(Color4Extensions.FromHex("#e65100"), 180, Easing.OutQuint);
                icon.FadeColour(Color4.White, 180, Easing.OutQuint);
                text.Text = LazerLensStrings.WarmupModeOn;
                text.Colour = Color4.White;
            }
            else
            {
                background.FadeColour(colourProvider.Background4, 180, Easing.OutQuint);
                icon.FadeColour(Color4Extensions.FromHex("#ff9800"), 180, Easing.OutQuint);
                text.Text = LazerLensStrings.WarmupModeOff;
                text.Colour = Color4.White;
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (!service.IsWarmupMode.Value)
                background.FadeColour(colourProvider.Background3, 100, Easing.OutQuint);
            borderContainer.ScaleTo(1.03f, 100, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (!service.IsWarmupMode.Value)
                background.FadeColour(colourProvider.Background4, 100, Easing.OutQuint);
            borderContainer.ScaleTo(1.0f, 100, Easing.OutQuint);
            base.OnHoverLost(e);
        }
    }
}
