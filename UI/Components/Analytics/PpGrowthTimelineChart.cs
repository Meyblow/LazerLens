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

        private readonly IReadOnlyList<(DateTime Date, double CumulativePp, double DayAccuracy)> timeline;

        private Container chartArea = null!;
        private osu.Framework.Graphics.Lines.Path linePath = null!;
        private Container dataPointsContainer = null!;
        private OsuSpriteText maxLabel = null!;
        private OsuSpriteText minLabel = null!;

        public PpGrowthTimelineChart(IReadOnlyList<(DateTime Date, double CumulativePp, double DayAccuracy)> timeline)
        {
            this.timeline = timeline;
            RelativeSizeAxes = Axes.X;
            Height = 200;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
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
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(14),
                        Children = new Drawable[]
                        {
                            // Title Row
                            new FillFlowContainer
                            {
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
                                        Icon = FontAwesome.Solid.ChartLine,
                                        Colour = colourProvider.Highlight1,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = LazerLensStrings.AnalyticsPpGrowthTitle,
                                        Font = OsuFont.Torus.With(size: 15, weight: FontWeight.Bold),
                                        Colour = Color4.White,
                                    }
                                }
                            },

                            // Chart Canvas
                            chartArea = new Container
                            {
                                Position = new Vector2(50, 32),
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Right = 60, Bottom = 42 },
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = Color4.White.Opacity(0.08f),
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                    },
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = Color4.White.Opacity(0.08f),
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                    },
                                    linePath = new osu.Framework.Graphics.Lines.Path
                                    {
                                        PathRadius = 2.5f,
                                    },
                                    dataPointsContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    }
                                }
                            },

                            // Y-Axis Labels
                            maxLabel = new OsuSpriteText
                            {
                                Position = new Vector2(0, 30),
                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Content2,
                            },
                            minLabel = new OsuSpriteText
                            {
                                Position = new Vector2(0, 160),
                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Content2,
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
                minVal -= 10;
                maxVal += 10;
            }

            maxLabel.Text = $"{maxVal:F0} PP";
            minLabel.Text = $"{minVal:F0} PP";

            float w = chartArea.DrawWidth;
            float h = chartArea.DrawHeight;

            linePath.Colour = colourProvider.Highlight1;

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                float x = sorted.Count > 1 ? (float)i / (sorted.Count - 1) * w : w / 2f;
                float normalized = (float)((entry.CumulativePp - minVal) / (maxVal - minVal));
                float y = h - (normalized * (h - 14) + 7);

                var pt = new Vector2(x, y);
                linePath.AddVertex(pt);

                dataPointsContainer.Add(new TimelinePoint(entry.Date, entry.CumulativePp, entry.DayAccuracy, colourProvider.Highlight1)
                {
                    Position = pt,
                });
            }
        }

        private sealed partial class TimelinePoint : CompositeDrawable, IHasTooltip
        {
            private readonly DateTime date;
            private readonly double pp;
            private readonly double acc;

            public LocalisableString TooltipText => $"{date:dd MMM yyyy}\n+{pp:F0} PP • {acc:F2}% avg";

            public TimelinePoint(DateTime date, double pp, double acc, Color4 accent)
            {
                this.date = date;
                this.pp = pp;
                this.acc = acc;

                Size = new Vector2(8);
                Origin = Anchor.Centre;
                Masking = true;
                CornerRadius = 4;

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = accent,
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.6f, 100, Easing.OutQuint);
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
