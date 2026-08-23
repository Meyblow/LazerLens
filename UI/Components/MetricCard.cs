using osu.Framework.Localisation;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace LazerLens.UI.Components
{
    public partial class MetricCard : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly IconUsage icon;
        private readonly LocalisableString title;
        private readonly OsuSpriteText valueText;
        private readonly OsuSpriteText subtitleText;

        private Box background = null!;
        private Container cardContainer = null!;
        private Container iconContainer = null!;
        private SpriteIcon spriteIcon = null!;

        public MetricCard(IconUsage icon, LocalisableString title, string initialValue = "0", LocalisableString initialSubtitle = default)
        {
            this.icon = icon;
            this.title = title;

            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                cardContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 8,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(12),
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.Distributed),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    iconContainer = new CircularContainer
                                    {
                                        Size = new Vector2(48),
                                        Masking = true,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Margin = new MarginPadding { Right = 10 },
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.Black.Opacity(0.3f),
                                            },
                                            spriteIcon = new SpriteIcon
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Size = new Vector2(22),
                                                Icon = icon,
                                            }
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 2),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Text = title,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                                Colour = Color4.White.Opacity(0.6f),
                                            },
                                            valueText = new OsuSpriteText
                                            {
                                                Text = initialValue,
                                                Font = OsuFont.Torus.With(size: 20, weight: FontWeight.Bold),
                                                Colour = Color4.White,
                                            },
                                            subtitleText = new OsuSpriteText
                                            {
                                                Text = initialSubtitle,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.5f),
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = colourProvider.Background4;
            spriteIcon.Colour = colourProvider.Colour1;
        }

        public void UpdateValues(string value, LocalisableString subtitle = default)
        {
            valueText.Text = value;
            subtitleText.Text = subtitle;
        }
    }
}






