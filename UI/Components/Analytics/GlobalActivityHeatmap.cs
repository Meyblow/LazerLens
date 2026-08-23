using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
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
    public sealed partial class GlobalActivityHeatmap : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly GlobalAnalyticsData analytics;

        public GlobalActivityHeatmap(GlobalAnalyticsData analytics)
        {
            this.analytics = analytics;
            RelativeSizeAxes = Axes.X;
            Height = 160;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            DateTime today = DateTime.Now.Date;
            const int weeks_count = 52;
            const int days_count = weeks_count * 7;
            DateTime startDate = today.AddDays(-days_count + 1);

            FillFlowContainer weeksFlow;

            InternalChild = new Container
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
                            // Header Row: Title & Streaks Summary
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(8, 0),
                                        Children = new Drawable[]
                                        {
                                            new SpriteIcon
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Size = new Vector2(16),
                                                Icon = FontAwesome.Solid.CalendarCheck,
                                                Colour = colourProvider.Highlight1,
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.AnalyticsActivityHeatmapTitle,
                                                Font = OsuFont.Torus.With(size: 15, weight: FontWeight.Bold),
                                                Colour = Color4.White,
                                            }
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(16, 0),
                                        Children = new Drawable[]
                                        {
                                            createStatPill(FontAwesome.Solid.Fire, $"{analytics.CurrentStreakDays} d", LazerLensStrings.AnalyticsCurrentStreak, Color4Extensions.FromHex("#ff9800")),
                                            createStatPill(FontAwesome.Solid.Trophy, $"{analytics.MaxStreakDays} d", LazerLensStrings.AnalyticsMaxStreak, Color4Extensions.FromHex("#ffd54f")),
                                            createStatPill(FontAwesome.Solid.CalendarDay, $"{analytics.TotalActiveDays} d", LazerLensStrings.AnalyticsActiveDays, Color4Extensions.FromHex("#5cd4f3")),
                                        }
                                    }
                                }
                            },

                            // 52 Weeks Grid (Fit all 52 weeks cleanly across width)
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 84,
                                Child = weeksFlow = new FillFlowContainer
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(3f, 0),
                                }
                            },

                            // Footer Row: Legend
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(5, 0),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.AnalyticsLess,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                                Colour = colourProvider.Content2,
                                            },
                                            createLegendBox(Color4.White.Opacity(0.06f)),
                                            createLegendBox(Color4Extensions.FromHex("#216e39")),
                                            createLegendBox(Color4Extensions.FromHex("#30a14e")),
                                            createLegendBox(Color4Extensions.FromHex("#40c463")),
                                            createLegendBox(Color4Extensions.FromHex("#9be9a8")),
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.AnalyticsMore,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                                Colour = colourProvider.Content2,
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            for (int w = 0; w < weeks_count; w++)
            {
                var column = new FillFlowContainer
                {
                    Width = 11,
                    RelativeSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
                };

                for (int d = 0; d < 7; d++)
                {
                    DateTime date = startDate.AddDays(w * 7 + d);
                    int count = analytics.DayPlayCounts.TryGetValue(date, out int cnt) ? cnt : 0;
                    column.Add(new HeatmapCell(date, count));
                }

                weeksFlow.Add(column);
            }
        }

        private FillFlowContainer createStatPill(IconUsage icon, string value, LocalisableString label, Color4 accent)
        {
            return new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5, 0),
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(12),
                        Icon = icon,
                        Colour = accent,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = value,
                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                        Colour = Color4.White,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = label,
                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                        Colour = colourProvider.Content2,
                    }
                }
            };
        }

        private static Drawable createLegendBox(Color4 colour) => new Container
        {
            Size = new Vector2(9),
            Masking = true,
            CornerRadius = 2,
            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = colour,
            }
        };

        private sealed partial class HeatmapCell : CompositeDrawable, IHasTooltip
        {
            private readonly DateTime date;
            private readonly int count;

            public LocalisableString TooltipText => LazerLensStrings.ActivityPlaysCount(count, date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));

            public HeatmapCell(DateTime date, int count)
            {
                this.date = date;
                this.count = count;

                Size = new Vector2(9.5f);
                Masking = true;
                CornerRadius = 2;

                Color4 cellColor = count switch
                {
                    0 => Color4.White.Opacity(0.06f),
                    < 5 => Color4Extensions.FromHex("#216e39"),
                    < 15 => Color4Extensions.FromHex("#30a14e"),
                    < 30 => Color4Extensions.FromHex("#40c463"),
                    _ => Color4Extensions.FromHex("#9be9a8")
                };

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = cellColor,
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.4f, 100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1.0f, 100, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
