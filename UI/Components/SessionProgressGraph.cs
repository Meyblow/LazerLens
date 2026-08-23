using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components
{
    public enum GraphMetric
    {
        PerformancePoints,
        Accuracy,
        StarRating
    }

    public sealed partial class SessionProgressGraph : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly IReadOnlyList<SessionPlayRecord> plays;
        private GraphMetric currentMetric = GraphMetric.PerformancePoints;

        private Container chartArea = null!;
        private osu.Framework.Graphics.Lines.Path linePath = null!;
        private Container dataPointsContainer = null!;
        private OsuSpriteText titleLabel = null!;
        private OsuSpriteText minLabel = null!;
        private OsuSpriteText maxLabel = null!;

        public SessionProgressGraph(IReadOnlyList<SessionPlayRecord> plays)
        {
            this.plays = plays;
            RelativeSizeAxes = Axes.X;
            Height = 160;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
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
                            // Top Row: Title + Metric Selection Buttons
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
                                                Size = new Vector2(14),
                                                Icon = FontAwesome.Solid.ChartLine,
                                                Colour = colourProvider.Highlight1,
                                            },
                                            titleLabel = new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = LazerLensStrings.GraphTitle,
                                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
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
                                        Spacing = new Vector2(6, 0),
                                        Children = new Drawable[]
                                        {
                                            createMetricButton(GraphMetric.PerformancePoints, LazerLensStrings.GraphMetricPP),
                                            createMetricButton(GraphMetric.Accuracy, LazerLensStrings.GraphMetricAcc),
                                            createMetricButton(GraphMetric.StarRating, LazerLensStrings.GraphMetricSR),
                                        }
                                    }
                                }
                            },

                            // Chart Plot Container
                            chartArea = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Margin = new MarginPadding { Top = 30, Bottom = 10, Left = 36, Right = 10 },
                                Children = new Drawable[]
                                {
                                    // Subtle Grid lines
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
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
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
                                        PathRadius = 2.0f,
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
                                Colour = Color4.White.Opacity(0.5f),
                            },
                            minLabel = new OsuSpriteText
                            {
                                Position = new Vector2(0, 130),
                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.SemiBold),
                                Colour = Color4.White.Opacity(0.5f),
                            }
                        }
                    }
                }
            };

            rebuildGraph();
        }

        private Drawable createMetricButton(GraphMetric metric, LocalisableString text)
        {
            return new OsuClickableContainer
            {
                AutoSizeAxes = Axes.Both,
                Action = () =>
                {
                    currentMetric = metric;
                    rebuildGraph();
                },
                Child = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 4,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = currentMetric == metric ? colourProvider.Highlight1 : colourProvider.Background5,
                        },
                        new OsuSpriteText
                        {
                            Text = text,
                            Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                            Colour = currentMetric == metric ? Color4.Black : Color4.White.Opacity(0.8f),
                            Margin = new MarginPadding { Horizontal = 8, Vertical = 3 },
                        }
                    }
                }
            };
        }

        protected override void Update()
        {
            base.Update();
            if (chartArea.DrawWidth > 0 && linePath.Vertices.Count == 0 && plays.Count > 0)
            {
                rebuildGraph();
            }
        }

        private void rebuildGraph()
        {
            if (IsDisposed || chartArea == null) return;

            dataPointsContainer.Clear();
            linePath.ClearVertices();

            var validPlays = plays.ToList();
            if (validPlays.Count == 0)
            {
                minLabel.Text = "";
                maxLabel.Text = "";
                return;
            }

            float width = chartArea.DrawWidth;
            float height = chartArea.DrawHeight;
            if (width <= 0) width = 800;
            if (height <= 0) height = 80;

            List<double> values = new();
            double runningPp = 0;

            foreach (var p in validPlays)
            {
                switch (currentMetric)
                {
                    case GraphMetric.PerformancePoints:
                        runningPp += p.ProfilePerformancePoints ?? (p.Passed ? (p.PerformancePoints ?? 0) * 0.05 : 0);
                        values.Add(runningPp);
                        break;
                    case GraphMetric.Accuracy:
                        values.Add(p.Accuracy);
                        break;
                    case GraphMetric.StarRating:
                        values.Add(p.StarRating);
                        break;
                }
            }

            double min = values.Min();
            double max = values.Max();
            if (Math.Abs(max - min) < 0.001)
            {
                min = Math.Max(0, min - 1);
                max += 1;
            }

            minLabel.Text = currentMetric switch
            {
                GraphMetric.PerformancePoints => $"{min:+0;-0;0}pp",
                GraphMetric.Accuracy => $"{min:F1}%",
                GraphMetric.StarRating => $"{min:F1}★",
                _ => min.ToString("F1")
            };

            maxLabel.Text = currentMetric switch
            {
                GraphMetric.PerformancePoints => $"{max:+0;-0;0}pp",
                GraphMetric.Accuracy => $"{max:F1}%",
                GraphMetric.StarRating => $"{max:F1}★",
                _ => max.ToString("F1")
            };

            Color4 lineColor = currentMetric switch
            {
                GraphMetric.PerformancePoints => Color4Extensions.FromHex("00d2ff"),
                GraphMetric.Accuracy => Color4Extensions.FromHex("88e23b"),
                GraphMetric.StarRating => Color4Extensions.FromHex("ffcc00"),
                _ => Color4.White
            };

            linePath.Colour = lineColor;

            int count = values.Count;
            for (int i = 0; i < count; i++)
            {
                float x = count == 1 ? width / 2 : (float)(i * (width / (count - 1)));
                float normalized = (float)((values[i] - min) / (max - min));
                float y = height - (normalized * (height - 12)) - 6;

                Vector2 pt = new Vector2(x, y);
                linePath.AddVertex(pt);

                // Add interactive hover dot
                var play = validPlays[i];
                double val = values[i];
                string valStr = currentMetric switch
                {
                    GraphMetric.PerformancePoints => play.PerformancePoints.HasValue && play.PerformancePoints.Value > 0
                        ? $"{play.PerformancePoints.Value:F0}pp ({val:+0.0;-0.0;0.0} pp)"
                        : $"{val:+0.0;-0.0;0.0} pp",
                    GraphMetric.Accuracy => $"{val:F2}% acc",
                    GraphMetric.StarRating => $"{val:F2}★ diff",
                    _ => val.ToString("F1", CultureInfo.InvariantCulture)
                };

                dataPointsContainer.Add(new GraphPoint(play, valStr, lineColor)
                {
                    Position = pt,
                });
            }
        }

        private sealed partial class GraphPoint : CompositeDrawable, IHasTooltip
        {
            private readonly SessionPlayRecord play;
            private readonly string valueText;
            private readonly Color4 pointColor;

            public LocalisableString TooltipText => $"{play.BeatmapArtist} - {play.BeatmapTitle} [{play.DifficultyName}]\n{play.Grade} • {play.Accuracy:F2}% • {valueText}";

            public GraphPoint(SessionPlayRecord play, string valueText, Color4 pointColor)
            {
                this.play = play;
                this.valueText = valueText;
                this.pointColor = pointColor;

                Size = new Vector2(8);
                Origin = Anchor.Centre;
                InternalChild = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 2,
                    BorderColour = Color4.White,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = pointColor,
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.6f, 150, Easing.OutElastic);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1.0f, 150, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
