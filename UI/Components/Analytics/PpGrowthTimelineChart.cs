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
using LazerLens.Services;

namespace LazerLens.UI.Components.Analytics
{
    public sealed partial class PpGrowthTimelineChart : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly GlobalAnalyticsData? analyticsData;
        private readonly IReadOnlyList<SessionTimelineEntry> initialTimeline;
        private IReadOnlyList<SessionTimelineEntry> currentTimeline;
        private string currentRuleset = "all";

        private Container chartArea = null!;
        private osu.Framework.Graphics.Lines.Path linePath = null!;
        private Container dataPointsContainer = null!;
        private OsuSpriteText maxLabel = null!;
        private OsuSpriteText midLabel = null!;
        private OsuSpriteText minLabel = null!;
        private OsuSpriteText startDateLabel = null!;
        private OsuSpriteText endDateLabel = null!;
        private OsuSpriteText summaryPillText = null!;
        private FillFlowContainer rulesetButtonsFlow = null!;
        private readonly List<(string Key, Box Bg, OsuSpriteText Text)> rulesetButtons = new();

        private const float CHART_PADDING_Y = 12f;

        public PpGrowthTimelineChart(GlobalAnalyticsData data)
        {
            analyticsData = data;
            initialTimeline = data.PpTimeline;
            currentTimeline = data.PpTimeline;
            RelativeSizeAxes = Axes.X;
            Height = 175;
        }

        public PpGrowthTimelineChart(IReadOnlyList<SessionTimelineEntry> timeline)
        {
            initialTimeline = timeline;
            currentTimeline = timeline;
            RelativeSizeAxes = Axes.X;
            Height = 175;
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
                        Padding = new MarginPadding(14),
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
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(8, 0),
                                        Children = new Drawable[]
                                        {
                                            rulesetButtonsFlow = new FillFlowContainer
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(4, 0),
                                            },
                                            summaryPillText = new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                                Colour = colourProvider.Highlight1,
                                            }
                                        }
                                    }
                                }
                            },

                            // 2. Chart area wrapper (Padding constrains ChildSize for RelativeSizeAxes children)
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Top = 30, Bottom = 22 },
                                Children = new Drawable[]
                                {
                                    // Y-Axis Labels (Aligned with gridlines)
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 44,
                                        Children = new Drawable[]
                                        {
                                            maxLabel = new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
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
                                                Origin = Anchor.BottomRight,
                                                Font = OsuFont.Torus.With(size: 9, weight: FontWeight.SemiBold),
                                                Colour = colourProvider.Content2,
                                            },
                                        }
                                    },

                                    // Chart Canvas
                                    chartArea = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Left = 52, Right = 14 },
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
                            },

                            // 3. X-Axis Date Labels (Positioned at bottom of card)
                            startDateLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Margin = new MarginPadding { Left = 52, Bottom = 2 },
                                Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Regular),
                                Colour = colourProvider.Content2,
                            },
                            endDateLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                Margin = new MarginPadding { Right = 14, Bottom = 2 },
                                Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Regular),
                                Colour = colourProvider.Content2,
                            },
                        }
                    }
                }
            };

            populateRulesetButtons();
        }

        private void populateRulesetButtons()
        {
            rulesetButtonsFlow.Clear();
            rulesetButtons.Clear();

            var available = getAvailableRulesets();

            // Only show ruleset selector if player has played more than 1 mode or has active data
            if (available.Count <= 1 && (analyticsData?.AllPlays == null || analyticsData.AllPlays.Count == 0))
                return;

            foreach (var (key, label) in available)
            {
                Box bg = null!;
                OsuSpriteText text = null!;
                bool isActive = key == currentRuleset;

                var btn = new OsuClickableContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Action = () => UpdateTimeline(key),
                    Child = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 4,
                        Children = new Drawable[]
                        {
                            bg = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = isActive ? colourProvider.Highlight1 : Color4.White.Opacity(0.06f),
                            },
                            text = new OsuSpriteText
                            {
                                Text = label,
                                Font = OsuFont.Torus.With(size: 10, weight: isActive ? FontWeight.Bold : FontWeight.SemiBold),
                                Colour = isActive ? Color4.Black : colourProvider.Content2,
                                Padding = new MarginPadding { Horizontal = 6, Vertical = 2 },
                            }
                        }
                    }
                };

                rulesetButtons.Add((key, bg, text));
                rulesetButtonsFlow.Add(btn);
            }
        }

        private List<(string Key, LocalisableString Label)> getAvailableRulesets()
        {
            var list = new List<(string Key, LocalisableString Label)>
            {
                ("all", LazerLensStrings.FilterAll)
            };

            if (analyticsData?.AllPlays == null || analyticsData.AllPlays.Count == 0)
                return list;

            var modes = new HashSet<string>();
            foreach (var p in analyticsData.AllPlays)
            {
                modes.Add(LazerLensAnalyticsEngine.NormalizeRuleset(p.RulesetName));
            }

            if (modes.Contains("osu")) list.Add(("osu", "osu!"));
            if (modes.Contains("taiko")) list.Add(("taiko", "osu!taiko"));
            if (modes.Contains("catch")) list.Add(("catch", "osu!catch"));
            if (modes.Contains("mania")) list.Add(("mania", "osu!mania"));

            return list;
        }

        public void UpdateTimeline(string? ruleset)
        {
            currentRuleset = ruleset ?? "all";

            // 1. Update button visual states
            foreach (var (key, bg, text) in rulesetButtons)
            {
                bool active = key == currentRuleset;
                bg.Colour = active ? colourProvider.Highlight1 : Color4.White.Opacity(0.06f);
                text.Colour = active ? Color4.Black : colourProvider.Content2;
                text.Font = OsuFont.Torus.With(size: 10, weight: active ? FontWeight.Bold : FontWeight.SemiBold);
            }

            // 2. Filter raw plays and recalculate cumulative PP strictly from 0
            if (currentRuleset == "all")
            {
                currentTimeline = initialTimeline;
            }
            else if (analyticsData?.AllPlays != null && analyticsData.AllPlays.Count > 0)
            {
                currentTimeline = LazerLensAnalyticsEngine.BuildTimelineFromPlays(analyticsData.AllPlays, currentRuleset);
            }
            else
            {
                currentTimeline = Array.Empty<SessionTimelineEntry>();
            }

            // 3. Redraw chart
            rebuildTimeline();
        }

        protected override void Update()
        {
            base.Update();
            if (chartArea.ChildSize.X > 20 && linePath.Vertices.Count == 0 && currentTimeline.Count > 0)
            {
                rebuildTimeline();
            }
        }

        private void rebuildTimeline()
        {
            if (chartArea.ChildSize.X <= 0 || chartArea.ChildSize.Y <= 0)
                return;

            dataPointsContainer.Clear();
            linePath.ClearVertices();

            if (currentTimeline == null || currentTimeline.Count == 0)
            {
                maxLabel.Text = "10 PP";
                midLabel.Text = "5 PP";
                minLabel.Text = "0 PP";
                startDateLabel.Text = "";
                endDateLabel.Text = "";
                summaryPillText.Text = "Total: +0 PP  •  0.00% avg";
                return;
            }

            var sorted = currentTimeline.OrderBy(t => t.Date).ToList();
            double rawMax = sorted.Count > 0 ? sorted.Max(t => t.CumulativePp) : 0.0;
            double maxVal = Math.Max(10.0, rawMax);
            double midVal = maxVal / 2.0;

            maxLabel.Text = $"{maxVal:F0} PP";
            midLabel.Text = $"{midVal:F0} PP";
            minLabel.Text = "0 PP";

            startDateLabel.Text = sorted.First().Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            endDateLabel.Text = sorted.Last().Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

            double totalGain = sorted.Last().CumulativePp;
            double avgAcc = sorted.Average(s => s.SessionAccuracy);
            summaryPillText.Text = $"Total: +{totalGain:F0} PP  •  {avgAcc:F2}% avg";

            float w = chartArea.ChildSize.X;
            float h = chartArea.ChildSize.Y;

            float usableH = h - CHART_PADDING_Y * 2;

            linePath.Colour = colourProvider.Highlight1;

            var rawPoints = new List<Vector2>();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                float x = sorted.Count > 1 ? (float)i / (sorted.Count - 1) * w : w / 2f;
                float normalized = maxVal > 0.001 ? (float)(entry.CumulativePp / maxVal) : 0f;
                float y = (h - CHART_PADDING_Y) - (normalized * usableH);
                y = Math.Clamp(y, CHART_PADDING_Y, h - CHART_PADDING_Y);

                var pt = new Vector2(x, y);
                rawPoints.Add(pt);

                dataPointsContainer.Add(new TimelinePoint(entry, colourProvider.Highlight1)
                {
                    Position = pt,
                });
            }

            var splineVertices = generateSmoothSpline(rawPoints, w, h, CHART_PADDING_Y);
            foreach (var vertex in splineVertices)
            {
                linePath.AddVertex(vertex);
            }
        }

        private static List<Vector2> generateSmoothSpline(IReadOnlyList<Vector2> points, float w, float h, float padY, int segmentsPerPoint = 12)
        {
            var result = new List<Vector2>();
            if (points.Count == 0) return result;
            if (points.Count <= 2)
            {
                foreach (var p in points)
                    result.Add(new Vector2(Math.Clamp(p.X, 0, w), Math.Clamp(p.Y, padY, h - padY)));
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
                    result.Add(new Vector2(Math.Clamp(p1.X, 0, w), Math.Clamp(p1.Y, padY, h - padY)));
                    continue;
                }

                // If vertical difference is flat (plateau), keep it perfectly straight without Catmull-Rom bounce
                if (Math.Abs(p1.Y - p2.Y) < 0.5f)
                {
                    result.Add(new Vector2(Math.Clamp(p1.X, 0, w), Math.Clamp(p1.Y, padY, h - padY)));
                    result.Add(new Vector2(Math.Clamp(p2.X, 0, w), Math.Clamp(p2.Y, padY, h - padY)));
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

                    result.Add(new Vector2(Math.Clamp(x, 0, w), Math.Clamp(y, padY, h - padY)));
                }
            }

            result.Add(new Vector2(Math.Clamp(points[^1].X, 0, w), Math.Clamp(points[^1].Y, padY, h - padY)));
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
                Anchor = Anchor.TopLeft;

                InternalChild = dot = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
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
