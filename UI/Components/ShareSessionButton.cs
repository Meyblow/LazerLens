using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;

namespace LazerLens.UI.Components
{
    public sealed partial class ShareSessionButton : OsuClickableContainer
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Clipboard? clipboard { get; set; }

        [Resolved(canBeNull: true)]
        private NotificationOverlay? notifications { get; set; }

        [Resolved(canBeNull: true)]
        private LazerLensService? service { get; set; }

        private readonly Func<SessionState> sessionProvider;
        private Container card = null!;
        private Box background = null!;

        public ShareSessionButton(Func<SessionState> sessionProvider)
        {
            this.sessionProvider = sessionProvider;
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = card = new Container
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
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(13),
                                Icon = FontAwesome.Solid.ShareAlt,
                                Colour = colourProvider.Highlight1,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = LazerLensStrings.ShareSessionButton,
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                Colour = Color4.White,
                            }
                        }
                    }
                }
            };

            Action = () =>
            {
                var session = sessionProvider();
                var format = service?.ShareFormatting.Value ?? ShareFormattingMode.Markdown;
                SessionShareCardExporter.ExportAndShare(session, format, clipboard, notifications);
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeColour(colourProvider.Background3, 100, Easing.OutQuint);
            card.ScaleTo(1.03f, 100, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background4, 100, Easing.OutQuint);
            card.ScaleTo(1.0f, 100, Easing.OutQuint);
            base.OnHoverLost(e);
        }
    }
}
