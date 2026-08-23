using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public sealed partial class PpGrowthTimelineChart : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly IReadOnlyList<SessionTimelineEntry> timeline;

        private Container chartArea = null!;
        private osu.Framework.Graphics.Lines.Path linePath = null!;
        private Container dataPointsContainer = null!;
        private OsuSpriteText maxLabel = null!;
        private OsuSpriteText midLabel = null!;
        private OsuSpriteText minLabel = null!;
        private OsuSpriteText startDateLabel = null!;
        private OsuSpriteText endDateLabel = null!;
        private OsuSpriteText summaryPillText = null!;

        public PpGrowthTimelineChart(IReadOnlyList<SessionTimelineEntry> timeline)
        {
            this.timeline = timeline;
            RelativeSizeAxes = Axes.X;
            Height = 150;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
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
                            // 1. Title Row
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 22,
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
                                                Size = new Vector2(14),
                                                Icon = FontAwesome.Solid.ChartLine,
                                                Colour = colourProvider.Highlight1,
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.AnalyticsPpGrowthTitle,
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = Color4.White,
                                            }
                                        }
                                    },
                                    summaryPillText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                        Colour = colourProvider.Highlight1,
                                    }
                                }
                            },

                            // 2. Y-Axis Labels (Aligned exactly with Top, Mid, Bottom gridlines)
                            new Container
                            {
                                Position = new Vector2(0, 30),
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Bottom = 38 },
                                Width = 48,
                                Children = new Drawable[]
                                {
                                    maxLabel = new OsuSpriteText
                                    {
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.CentreRight,
                                        Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                                        Colour = colourProvider.Content2,
                                    },
                                    midLabel = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                                        Colour = colourProvider.Content2,
                                    },
                                    minLabel = new OsuSpriteText
                                    {
                                        Anchor = Anchor.BottomRight,
                                        Origin = Anchor.CentreRight,
                                        Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                                        Colour = colourProvider.Content2,
                                    },
                                }
                            },

                            // 3. X-Axis Date Labels (Positioned neatly in the bottom footer)
                            startDateLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Position = new Vector2(56, 0),
                                Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Regular),
                                Colour = colourProvider.Content2,
                            },
                            endDateLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                Position = new Vector2(-20, 0),
                                Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Regular),
                                Colour = colourProvider.Content2,
                            },

                            // 4. Chart Canvas
                            chartArea = new Container
                            {
                                Position = new Vector2(56, 30),
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Right = 20, Bottom = 38 },
                                Children = new Drawable[]
                                {
                                    // Top Grid Line
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = Color4.White.Opacity(0.06f),
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                    },
                                    // Mid Grid Line
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = Color4.White.Opacity(0.06f),
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                    },
                                    // Bottom Grid Line
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = Color4.White.Opacity(0.06f),
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                    },
                                    linePath = new osu.Framework.Graphics.Lines.Path
                                    {
                                        PathRadius = 2f,
                                    },
                                    dataPointsContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();
            if (chartArea.DrawWidth > 20 && linePath.Vertices.Count == 0 && timeline.Count > 0)
            {
                rebuildTimeline();
            }
        }

        private void rebuildTimeline()
        {
            if (timeline.Count == 0 || chartArea.DrawWidth <= 0 || chartArea.DrawHeight <= 0)
                return;

            dataPointsContainer.Clear();
            linePath.ClearVertices();

            var sorted = timeline.OrderBy(t => t.Date).ToList();
            double minVal = sorted.Min(t => t.CumulativePp);
            double maxVal = sorted.Max(t => t.CumulativePp);

            if (Math.Abs(maxVal - minVal) < 0.001)
            {
                minVal = Math.Max(0, minVal - 10);
                maxVal += 10;
            }

            double midVal = (maxVal + minVal) / 2.0;

            maxLabel.Text = $"{maxVal:F0} PP";
            midLabel.Text = $"{midVal:F0} PP";
            minLabel.Text = $"{minVal:F0} PP";

            startDateLabel.Text = sorted.First().Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            endDateLabel.Text = sorted.Last().Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

            double totalGain = sorted.Last().CumulativePp;
            double avgAcc = sorted.Average(s => s.SessionAccuracy);
            summaryPillText.Text = $"Total: +{totalGain:F0} PP  •  {avgAcc:F2}% avg";

            float w = chartArea.DrawWidth;
            float h = chartArea.DrawHeight;

            linePath.Colour = colourProvider.Highlight1;

            var rawPoints = new List<Vector2>();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                float x = sorted.Count > 1 ? (float)i / (sorted.Count - 1) * w : w / 2f;
                float normalized = (float)((entry.CumulativePp - minVal) / (maxVal - minVal));
                float y = h - (normalized * h);

                var pt = new Vector2(x, y);
                rawPoints.Add(pt);

                dataPointsContainer.Add(new TimelinePoint(entry, colourProvider.Highlight1)
                {
                    Position = pt,
                });
            }

            var splineVertices = generateSmoothSpline(rawPoints);
            foreach (var vertex in splineVertices)
            {
                linePath.AddVertex(vertex);
            }
        }

        private static List<Vector2> generateSmoothSpline(IReadOnlyList<Vector2> points, int segmentsPerPoint = 12)
        {
            var result = new List<Vector2>();
            if (points.Count == 0) return result;
            if (points.Count <= 2)
            {
                result.AddRange(points);
                return result;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = i > 0 ? points[i - 1] : points[i];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = i + 2 < points.Count ? points[i + 2] : p2;

                if (Vector2.DistanceSquared(p1, p2) < 0.001f)
                {
                    result.Add(p1);
                    continue;
                }

                for (int step = 0; step < segmentsPerPoint; step++)
                {
                    float t = (float)step / segmentsPerPoint;
                    float t2 = t * t;
                    float t3 = t2 * t;

                    float x = 0.5f * ((2 * p1.X) +
                                      (-p0.X + p2.X) * t +
                                      (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                                      (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);

                    float y = 0.5f * ((2 * p1.Y) +
                                      (-p0.Y + p2.Y) * t +
                                      (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                                      (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

                    result.Add(new Vector2(x, y));
                }
            }

            result.Add(points[^1]);
            return result;
        }

        private sealed partial class TimelinePoint : CompositeDrawable, IHasTooltip
        {
            private readonly SessionTimelineEntry entry;
            private readonly Color4 accent;
            private Container dot = null!;

            public LocalisableString TooltipText
            {
                get
                {
                    string dateStr = entry.Date.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
                    string playCountStr = entry.PlayCount > 0 ? $" • {entry.PlayCount} plays" : "";
                    string noteStr = !string.IsNullOrWhiteSpace(entry.SessionTitle) && !entry.SessionTitle.Contains(dateStr, StringComparison.OrdinalIgnoreCase)
                        ? $"\nNote: {entry.SessionTitle}"
                        : "";

                    return $"{dateStr}\n+{entry.SessionPpGain:F0} PP (Total: {entry.CumulativePp:F0} PP)\n{entry.SessionAccuracy:F2}% avg{playCountStr}{noteStr}";
                }
            }

            public TimelinePoint(SessionTimelineEntry entry, Color4 accent)
            {
                this.entry = entry;
                this.accent = accent;

                Size = new Vector2(8);
                Origin = Anchor.Centre;

                InternalChild = dot = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 4,
                    BorderThickness = 1.5f,
                    BorderColour = Color4.White,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = accent,
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                dot.ScaleTo(1.6f, 120, Easing.OutQuint);
                dot.BorderThickness = 2.5f;
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                dot.ScaleTo(1.0f, 120, Easing.OutQuint);
                dot.BorderThickness = 1.5f;
                base.OnHoverLost(e);
            }
        }
    }
}
