using System;
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
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osucc.Client;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;

namespace LazerLens.UI.Components
{
    public partial class SessionPlayHistoryItem : OsuClickableContainer, IHasTooltip
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

        public override LocalisableString TooltipText => currentPlay.OnlineBeatmapID > 0 || currentPlay.OnlineBeatmapSetID > 0
            ? "Click to view beatmap info in overlay"
            : "Local beatmap (no online ID)";

        private SessionPlayRecord currentPlay;
        private readonly LazerLensService service;
        private Box background = null!;
        private Box hoverOverlay = null!;
        private OsuSpriteText statusText = null!;
        private OsuSpriteText ppTextSprite = null!;
        private OsuSpriteText hitsTextSprite = null!;
        private OsuSpriteText urTextSprite = null!;

        public SessionPlayHistoryItem(SessionPlayRecord play, LazerLensService service)
        {
            this.currentPlay = play;
            this.service = service;

            RelativeSizeAxes = Axes.X;

            Action = openBeatmapInfo;

            string modString = play.Mods.Length > 0 ? "+" + string.Join("", play.Mods) : "NoMod";
            string starPrefix = play.StarRating > 0 ? $"[\u2605 {play.StarRating.ToString("F2", CultureInfo.InvariantCulture)}] " : "";

            string ppDisplay = formatPp(play);

            Child = new Container
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
                    hoverOverlay = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 14, Vertical = 6 },
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 54),  // 1. Fixed Pill Rank Badge
                            new Dimension(GridSizeMode.Distributed),   // 2. Title & Status
                            new Dimension(GridSizeMode.Absolute, 85),  // 3. Acc & Mods
                            new Dimension(GridSizeMode.Absolute, 80),  // 4. Score
                            new Dimension(GridSizeMode.Absolute, 115), // 5. Hits & UR (300/100/50/0 & UR)
                            new Dimension(GridSizeMode.Absolute, 70),  // 6. Max Combo
                            new Dimension(GridSizeMode.Absolute, 95),  // 7. PP (XX / YY)
                            new Dimension(GridSizeMode.Absolute, 65),  // 8. Time
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                // 1. Vanilla Rank Badge strictly constrained to 48x24
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new Container
                                    {
                                        Size = new Vector2(48, 24),
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Masking = true,
                                        CornerRadius = 12,
                                        Child = new UpdateableRank(play.Rank)
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                        }
                                    }
                                },
                                // 2. Title & Subtitle
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Padding = new MarginPadding { Right = 10 },
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Text = $"{starPrefix}{play.BeatmapArtist} - {play.BeatmapTitle}",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                                RelativeSizeAxes = Axes.X,
                                            },
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(4, 0),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Text = $"[{play.DifficultyName}] ({play.RulesetName}) \u2022",
                                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                                        Colour = Color4.White.Opacity(0.55f),
                                                    },
                                                    statusText = new OsuSpriteText
                                                    {
                                                        Text = play.Status,
                                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                // 3. Accuracy & Mods (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = $"{play.Accuracy.ToString("F2", CultureInfo.InvariantCulture)}%",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = play.Accuracy >= 95 ? Color4.LightGreen : Color4.White,
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = modString,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                                                Colour = modString == "NoMod" ? Color4.White.Opacity(0.4f) : Color4.Yellow,
                                            }
                                        }
                                    }
                                },
                                // 4. Score (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = play.TotalScore.ToString("N0", CultureInfo.InvariantCulture),
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = Color4.White.Opacity(0.95f),
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = "SCORE",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.35f),
                                            }
                                        }
                                    }
                                },
                                // 5. Hits (300/100/0) & UR (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            hitsTextSprite = new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = formatHitsString(play),
                                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                                Colour = Color4.White.Opacity(0.95f),
                                            },
                                            urTextSprite = new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = formatUrString(play),
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.SemiBold),
                                                Colour = play.UnstableRate.HasValue ? Color4Extensions.FromHex("00ffcc") : Color4.White.Opacity(0.35f),
                                            }
                                        }
                                    }
                                },
                                // 6. Max Combo (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = $"{play.MaxCombo}x",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = Color4.White.Opacity(0.95f),
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = "COMBO",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.35f),
                                            }
                                        }
                                    }
                                },
                                // 7. PP: XX / YY (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            ppTextSprite = new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = ppDisplay,
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = getPpColour(play),
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = "PP (PLAY/PROF)",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.35f),
                                            }
                                        }
                                    }
                                },
                                // 8. Time (Baseline aligned)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = play.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.6f),
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopRight,
                                                Origin = Anchor.TopRight,
                                                Text = "TIME",
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.35f),
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


        public void UpdateData(SessionPlayRecord newPlay)
        {
            this.currentPlay = newPlay;

            if (ppTextSprite != null)
            {
                ppTextSprite.Text = formatPp(newPlay);
                ppTextSprite.Colour = getPpColour(newPlay);
            }

            if (hitsTextSprite != null)
            {
                hitsTextSprite.Text = formatHitsString(newPlay);
            }

            if (urTextSprite != null)
            {
                urTextSprite.Text = formatUrString(newPlay);
                urTextSprite.Colour = newPlay.UnstableRate.HasValue ? Color4Extensions.FromHex("00ffcc") : Color4.White.Opacity(0.35f);
            }
        }

        private static string formatHitsString(SessionPlayRecord play)
        {
            string ruleset = play.RulesetName.ToLowerInvariant();

            if (ruleset.Contains("taiko"))
            {
                return $"{play.CountGreat} / {play.CountOk} / {play.CountMiss}";
            }

            if (ruleset.Contains("catch") || ruleset.Contains("fruit"))
            {
                int misses = play.CountMiss + play.CountLargeTickMiss;
                return $"{play.CountGreat} / {play.CountLargeTickHit} / {play.CountSmallTickHit} / {misses}";
            }

            if (ruleset.Contains("mania"))
            {
                if (play.CountPerfect > 0)
                    return $"{play.CountPerfect} / {play.CountGreat} / {play.CountGood} / {play.CountMiss}";
                return $"{play.CountGreat} / {play.CountOk} / {play.CountMeh} / {play.CountMiss}";
            }

            // Standard osu! (std): Always strictly 300 / 100 / 50 / 0
            return $"{play.CountGreat} / {play.CountOk} / {play.CountMeh} / {play.CountMiss}";
        }

        private static string formatUrString(SessionPlayRecord play)
        {
            if (play.UnstableRate.HasValue && play.UnstableRate.Value > 0)
                return $"{play.UnstableRate.Value.ToString("F2", CultureInfo.InvariantCulture)} UR";

            return "HITS";
        }

        private static string formatPp(SessionPlayRecord play)
        {
            if (play.PerformancePoints.HasValue)
            {
                string xx = play.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture);
                string yy;

                if (play.Status is "Ranked" or "Approved")
                {
                    if (play.ProfilePerformancePoints.HasValue)
                    {
                        double prof = play.ProfilePerformancePoints.Value;
                        yy = prof > 0 ? $"+{prof.ToString("F0", CultureInfo.InvariantCulture)}" : prof.ToString("F0", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        yy = "0";
                    }
                }
                else
                {
                    yy = "0";
                }

                return $"{xx} / {yy}";
            }

            return "- / -";
        }

        private static Color4 getPpColour(SessionPlayRecord play)
        {
            if (play.ProfilePerformancePoints.HasValue)
            {
                if (play.ProfilePerformancePoints.Value > 0)
                    return Color4.Cyan;
                if (play.ProfilePerformancePoints.Value < 0)
                    return Color4.Coral;
            }

            if (play.PerformancePoints.HasValue && play.PerformancePoints.Value > 0)
                return Color4.White.Opacity(0.85f);

            return Color4.White.Opacity(0.4f);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = colourProvider.Background4;

            service.CompactMode.BindValueChanged(e => Height = e.NewValue ? 38 : 56, true);
            service.ShowUR.BindValueChanged(e => 
            {
                if (urTextSprite != null)
                {
                    urTextSprite.Alpha = e.NewValue ? 1 : 0;
                }
            }, true);

            // Determine status color
            statusText.Colour = currentPlay.Status switch
            {
                "Ranked" or "Approved" => Color4.LimeGreen,
                "Qualified" => Color4.LightSkyBlue,
                "Loved" => Color4.HotPink,
                "Pending" or "WIP" => Color4.Gold,
                _ => Color4.Gray
            };
        }

        private void openBeatmapInfo()
        {
            var overlay = beatmapSetOverlay ?? ClientApi.Game?.Dependencies?.Get(typeof(BeatmapSetOverlay)) as BeatmapSetOverlay;

            if (currentPlay.OnlineBeatmapID > 0)
            {
                overlay?.FetchAndShowBeatmap(currentPlay.OnlineBeatmapID);
            }
            else if (currentPlay.OnlineBeatmapSetID > 0)
            {
                overlay?.FetchAndShowBeatmapSet(currentPlay.OnlineBeatmapSetID);
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverOverlay.FadeTo(0.08f, 150);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverOverlay.FadeTo(0, 150);
            base.OnHoverLost(e);
        }
    }
}

