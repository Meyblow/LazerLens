using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components.Analytics
{
    public sealed partial class DistributionBarChart : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly GlobalAnalyticsData analytics;

        public DistributionBarChart(GlobalAnalyticsData analytics)
        {
            this.analytics = analytics;
            RelativeSizeAxes = Axes.X;
            Height = 220;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.Absolute, 14),
                    new Dimension(GridSizeMode.Distributed),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        // Left Panel: Mod Distribution
                        buildModDistributionPanel(),
                        Empty(),
                        // Right Panel: Star Rating Spread
                        buildStarRatingPanel(),
                    }
                }
            };
        }

        private Container buildModDistributionPanel()
        {
            int total = analytics.ModPlayCounts.Values.Sum();
            var itemsFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
            };

            foreach (var kvp in analytics.ModPlayCounts)
            {
                if (kvp.Value == 0 && total > 0) continue;
                double pct = total > 0 ? (double)kvp.Value / total * 100.0 : 0;
                Color4 modColor = getModGroupColour(kvp.Key);
                itemsFlow.Add(createDistributionRow(kvp.Key, kvp.Value, pct, modColor));
            }

            return createCardContainer(FontAwesome.Solid.SlidersH, LazerLensStrings.AnalyticsModDistributionTitle, itemsFlow);
        }

        private Container buildStarRatingPanel()
        {
            int total = analytics.StarRatingBuckets.Values.Sum();
            var itemsFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
            };

            foreach (var kvp in analytics.StarRatingBuckets)
            {
                double pct = total > 0 ? (double)kvp.Value / total * 100.0 : 0;
                Color4 starColor = getStarGroupColour(kvp.Key);
                itemsFlow.Add(createDistributionRow(kvp.Key, kvp.Value, pct, starColor));
            }

            return createCardContainer(FontAwesome.Solid.Star, LazerLensStrings.AnalyticsStarDistributionTitle, itemsFlow);
        }

        private Container createCardContainer(IconUsage icon, LocalisableString title, Drawable content)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 10,
                BorderThickness = 1,
                BorderColour = colourProvider.Background1,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(14),
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8, 0),
                                Children = new Drawable[]
                                {
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(14),
                                        Icon = icon,
                                        Colour = colourProvider.Highlight1,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = title,
                                        Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                        Colour = Color4.White,
                                    }
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Child = content
                            }
                        }
                    }
                }
            };
        }

        private Drawable createDistributionRow(string label, int count, double pct, Color4 accent)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 18,
                Children = new Drawable[]
                {
                    // Label
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = label,
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                        Colour = Color4.White,
                        Width = 80,
                    },
                    // Progress Track & Bar
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(85, 0),
                        RelativeSizeAxes = Axes.X,
                        Padding = new MarginPadding { Right = 145 },
                        Height = 8,
                        Masking = true,
                        CornerRadius = 4,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.White.Opacity(0.06f),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = (float)Math.Clamp(pct / 100.0, 0, 1),
                                Colour = accent,
                            }
                        }
                    },
                    // Value & Percentage
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = count.ToString("N0", CultureInfo.InvariantCulture),
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                                Colour = Color4.White,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = $"({pct:F1}%)",
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                Colour = colourProvider.Content2,
                            }
                        }
                    }
                }
            };
        }

        private static Color4 getModGroupColour(string key) => key switch
        {
            "DT / NC" => Color4Extensions.FromHex("#b35cff"),
            "HR" => Color4Extensions.FromHex("#ff5252"),
            "HD" => Color4Extensions.FromHex("#ffd54f"),
            "FL" => Color4Extensions.FromHex("#4fc3f7"),
            "EZ / HT" => Color4Extensions.FromHex("#81c784"),
            "NoMod" => Color4Extensions.FromHex("#78909c"),
            _ => Color4Extensions.FromHex("#ff80ab")
        };

        private static Color4 getStarGroupColour(string key) => key switch
        {
            "< 4.0★" => Color4Extensions.FromHex("#4fc3f7"),
            "4.0 - 4.9★" => Color4Extensions.FromHex("#81c784"),
            "5.0 - 5.9★" => Color4Extensions.FromHex("#ffd54f"),
            "6.0 - 6.9★" => Color4Extensions.FromHex("#ff9800"),
            "7.0 - 7.9★" => Color4Extensions.FromHex("#ff5252"),
            _ => Color4Extensions.FromHex("#b35cff")
        };
    }
}
