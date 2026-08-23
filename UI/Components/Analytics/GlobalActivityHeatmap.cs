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
            Height = 168;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            DateTime today = DateTime.Now.Date;
            const int weeks_count = 52;
            const int days_count = weeks_count * 7;
            DateTime startDate = today.AddDays(-days_count + 1);

            // Construct 53 columns (1 label column + 52 weeks)
            var colDims = new Dimension[53];
            colDims[0] = new Dimension(GridSizeMode.Absolute, 28);
            for (int i = 1; i <= 52; i++)
                colDims[i] = new Dimension(GridSizeMode.Distributed);

            // 8 rows (Row 0: Month labels, Rows 1-7: Day cells for Mon-Sun)
            var rowDims = new Dimension[8];
            rowDims[0] = new Dimension(GridSizeMode.Absolute, 14);
            for (int i = 1; i <= 7; i++)
                rowDims[i] = new Dimension(GridSizeMode.Distributed);

            var gridContent = new Drawable[8][];
            for (int r = 0; r < 8; r++)
                gridContent[r] = new Drawable[53];

            // Row 0, Col 0: empty
            gridContent[0][0] = Empty();

            // Col 0 Day Labels: Mon, Wed, Fri
            for (int d = 1; d <= 7; d++)
            {
                string dayLabel = d switch
                {
                    1 => "Mon",
                    3 => "Wed",
                    5 => "Fri",
                    _ => ""
                };

                gridContent[d][0] = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = dayLabel,
                        Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                        Colour = colourProvider.Content2,
                    }
                };
            }

            int lastLabeledMonth = -1;

            // Fill 52 weeks
            for (int w = 0; w < 52; w++)
            {
                int colIndex = w + 1;
                DateTime weekStartDate = startDate.AddDays(w * 7);

                // Month header label on first week of each month
                if (weekStartDate.Month != lastLabeledMonth && w < 50)
                {
                    lastLabeledMonth = weekStartDate.Month;
                    gridContent[0][colIndex] = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new OsuSpriteText
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.BottomLeft,
                            Text = weekStartDate.ToString("MMM", CultureInfo.InvariantCulture),
                            Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                            Colour = colourProvider.Content1,
                        }
                    };
                }
                else
                {
                    gridContent[0][colIndex] = Empty();
                }

                // 7 days of the week
                for (int d = 0; d < 7; d++)
                {
                    DateTime cellDate = startDate.AddDays(w * 7 + d);
                    int count = analytics.DayPlayCounts.TryGetValue(cellDate, out int cnt) ? cnt : 0;

                    gridContent[d + 1][colIndex] = new HeatmapCell(cellDate, count);
                }
            }

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                BorderThickness = 1,
                BorderColour = colourProvider.Background1,
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
                        Padding = new MarginPadding(12),
                        Children = new Drawable[]
                        {
                            // 1. Header Row: Title & Streaks Summary
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 24,
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
                                                Size = new Vector2(15),
                                                Icon = FontAwesome.Solid.CalendarCheck,
                                                Colour = colourProvider.Highlight1,
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.AnalyticsActivityHeatmapTitle,
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
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
                                        Spacing = new Vector2(14, 0),
                                        Children = new Drawable[]
                                        {
                                            createStatPill(FontAwesome.Solid.Fire, $"{analytics.CurrentStreakDays} d", LazerLensStrings.AnalyticsCurrentStreak, Color4Extensions.FromHex("#ff9800")),
                                            createStatPill(FontAwesome.Solid.Trophy, $"{analytics.MaxStreakDays} d", LazerLensStrings.AnalyticsMaxStreak, Color4Extensions.FromHex("#ffd54f")),
                                            createStatPill(FontAwesome.Solid.CalendarDay, $"{analytics.TotalActiveDays} d", LazerLensStrings.AnalyticsActiveDays, Color4Extensions.FromHex("#5cd4f3")),
                                        }
                                    }
                                }
                            },

                            // 2. Full-Width 52-Weeks Grid
                            new Container
                            {
                                Position = new Vector2(0, 28),
                                RelativeSizeAxes = Axes.X,
                                Height = 94,
                                Child = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ColumnDimensions = colDims,
                                    RowDimensions = rowDims,
                                    Content = gridContent
                                }
                            },

                            // 3. Footer Row: Legend
                            new Container
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                AutoSizeAxes = Axes.Both,
                                Child = new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = LazerLensStrings.AnalyticsLess,
                                            Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
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
                                            Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                            Colour = colourProvider.Content2,
                                        },
                                    }
                                }
                            }
                        }
                    }
                }
            };
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
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
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
            private Container cellBox = null!;

            public LocalisableString TooltipText => LazerLensStrings.ActivityPlaysCount(count, date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));

            public HeatmapCell(DateTime date, int count)
            {
                this.date = date;
                this.count = count;

                RelativeSizeAxes = Axes.Both;
                Padding = new MarginPadding(1.5f);

                Color4 cellColor = count switch
                {
                    0 => Color4.White.Opacity(0.06f),
                    < 5 => Color4Extensions.FromHex("#216e39"),
                    < 15 => Color4Extensions.FromHex("#30a14e"),
                    < 30 => Color4Extensions.FromHex("#40c463"),
                    _ => Color4Extensions.FromHex("#9be9a8")
                };

                InternalChild = cellBox = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 2,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = cellColor,
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                cellBox.ScaleTo(1.35f, 100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                cellBox.ScaleTo(1.0f, 100, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
