using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components.Analytics
{
    public sealed partial class TopFavoritesLeaderboard : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private OsuColour? colours { get; set; }

        private readonly GlobalAnalyticsData analytics;
        private readonly Action<int>? onOpenBeatmap;

        public TopFavoritesLeaderboard(GlobalAnalyticsData analytics, Action<int>? onOpenBeatmap = null)
        {
            this.analytics = analytics;
            this.onOpenBeatmap = onOpenBeatmap;
            RelativeSizeAxes = Axes.X;
            Height = 350;
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
                        // Left: Top Played Beatmaps
                        buildTopBeatmapsPanel(),
                        Empty(),
                        // Right: Top Mappers
                        buildTopMappersPanel(),
                    }
                }
            };
        }

        private Container buildTopBeatmapsPanel()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Padding = new MarginPadding { Right = 10 },
            };

            for (int i = 0; i < analytics.TopBeatmaps.Count; i++)
            {
                var map = analytics.TopBeatmaps[i];
                flow.Add(createBeatmapRow(i + 1, map));
            }

            if (analytics.TopBeatmaps.Count == 0)
            {
                flow.Add(new OsuSpriteText
                {
                    Text = LazerLensStrings.AnalyticsNoData,
                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Regular),
                    Colour = colourProvider.Content2,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Margin = new MarginPadding { Vertical = 16 }
                });
            }

            return createLeaderboardCard(FontAwesome.Solid.Music, LazerLensStrings.AnalyticsTopBeatmapsTitle, flow);
        }

        private Container buildTopMappersPanel()
        {
            var flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                Padding = new MarginPadding { Right = 10 },
            };

            for (int i = 0; i < analytics.TopMappers.Count; i++)
            {
                var mapper = analytics.TopMappers[i];
                flow.Add(createMapperRow(i + 1, mapper));
            }

            if (analytics.TopMappers.Count == 0)
            {
                flow.Add(new OsuSpriteText
                {
                    Text = LazerLensStrings.AnalyticsNoData,
                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Regular),
                    Colour = colourProvider.Content2,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Margin = new MarginPadding { Vertical = 16 }
                });
            }

            return createLeaderboardCard(FontAwesome.Solid.UserEdit, LazerLensStrings.AnalyticsTopMappersTitle, flow);
        }

        private Container createLeaderboardCard(IconUsage icon, LocalisableString title, Drawable content)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(14),
                        Children = new Drawable[]
                        {
                            // 1. Fixed Header
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 22,
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

                            // 2. Scrollable Content Pane
                            new Container
                            {
                                Position = new Vector2(0, 28),
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Bottom = 28 },
                                Child = new OsuScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ScrollbarVisible = true,
                                    Child = content
                                }
                            }
                        }
                    }
                }
            };
        }

        private Drawable createBeatmapRow(int rank, TopBeatmapStat map)
        {
            string ppStr = map.BestPp.HasValue && map.BestPp.Value > 0 ? $"{map.BestPp.Value:F0} PP" : "-";

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 36,
                Masking = true,
                CornerRadius = 6,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Padding = new MarginPadding { Horizontal = 8 },
                        Spacing = new Vector2(8, 0),
                        Children = new Drawable[]
                        {
                            // Rank Number
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = $"#{rank}",
                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                Colour = rank switch
                                {
                                    1 => Color4Extensions.FromHex("#ffd54f"),
                                    2 => Color4Extensions.FromHex("#cfd8dc"),
                                    3 => Color4Extensions.FromHex("#ffab91"),
                                    _ => colourProvider.Content2
                                },
                                Width = 24,
                            },
                            // Map Title & Artist
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Padding = new MarginPadding { Right = 140 },
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Text = $"{map.Artist} - {map.Title}",
                                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                        Colour = Color4.White,
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(4, 0),
                                        Children = new Drawable[]
                                        {
                                            new TruncatingSpriteText
                                            {
                                                Text = $"[{map.Difficulty}]",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = colourProvider.Content2,
                                                MaxWidth = 200,
                                            },
                                            new OsuSpriteText
                                            {
                                                Text = $"• {map.StarRating:F2}★",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.SemiBold),
                                                Colour = colours != null ? colours.ForStarDifficulty(map.StarRating) : colourProvider.Highlight1,
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    // Right: Play Count & Best PP
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Margin = new MarginPadding { Right = 8 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = $"{map.PlayCount} plays",
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                                Colour = colourProvider.Highlight1,
                            },
                            new Container
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                AutoSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 3,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.Black.Opacity(0.4f),
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = ppStr,
                                        Font = OsuFont.Torus.With(size: 10.5f, weight: FontWeight.Bold),
                                        Colour = Color4Extensions.FromHex("#ffcc00"),
                                        Padding = new MarginPadding { Horizontal = 4, Vertical = 2 }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private Drawable createMapperRow(int rank, TopMapperStat mapper)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 36,
                Masking = true,
                CornerRadius = 6,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Padding = new MarginPadding { Horizontal = 8 },
                        Spacing = new Vector2(8, 0),
                        Children = new Drawable[]
                        {
                            // Rank Number
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = $"#{rank}",
                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                Colour = rank switch
                                {
                                    1 => Color4Extensions.FromHex("#ffd54f"),
                                    2 => Color4Extensions.FromHex("#cfd8dc"),
                                    3 => Color4Extensions.FromHex("#ffab91"),
                                    _ => colourProvider.Content2
                                },
                                Width = 24,
                            },
                            // Mapper Name
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Padding = new MarginPadding { Right = 140 },
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Text = mapper.MapperName,
                                        Font = OsuFont.Torus.With(size: 12.5f, weight: FontWeight.SemiBold),
                                        Colour = Color4.White,
                                    },
                                    new TruncatingSpriteText
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Text = $"{mapper.MapsCount} maps • {mapper.AverageAccuracy:F2}% avg",
                                        Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                        Colour = colourProvider.Content2,
                                    }
                                }
                            }
                        }
                    },
                    // Right: Play Count
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(8, 0),
                        Margin = new MarginPadding { Right = 8 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Text = $"{mapper.PlayCount} plays",
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                                Colour = colourProvider.Highlight1,
                            }
                        }
                    }
                }
            };
        }
    }
}
