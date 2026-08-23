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
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
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

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private RulesetStore? rulesets { get; set; }

        [Resolved(canBeNull: true)]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

        public override LocalisableString TooltipText => currentPlay.OnlineBeatmapID > 0 || currentPlay.OnlineBeatmapSetID > 0
            ? LazerLensStrings.TooltipViewBeatmap
            : LazerLensStrings.TooltipLocalBeatmap;

        private SessionPlayRecord currentPlay;
        private readonly LazerLensService service;
        private Box background = null!;
        private Box hoverOverlay = null!;
        private GridContainer grid = null!;

        public SessionPlayHistoryItem(SessionPlayRecord play, LazerLensService service)
        {
            this.currentPlay = play;
            this.service = service;

            RelativeSizeAxes = Axes.X;
            Action = openBeatmapInfo;
        }

        public void UpdateData(SessionPlayRecord play)
        {
            if (ReferenceEquals(this.currentPlay, play) ||
                (this.currentPlay.Id == play.Id &&
                 this.currentPlay.TotalScore == play.TotalScore &&
                 this.currentPlay.Accuracy == play.Accuracy &&
                 this.currentPlay.Rank == play.Rank &&
                 this.currentPlay.UnstableRate == play.UnstableRate &&
                 this.currentPlay.MaxCombo == play.MaxCombo &&
                 this.currentPlay.Passed == play.Passed &&
                 this.currentPlay.PerformancePoints == play.PerformancePoints &&
                 this.currentPlay.ProfilePerformancePoints == play.ProfilePerformancePoints))
            {
                return;
            }

            this.currentPlay = play;
            Clear();
            Child = buildContent();
        }

        private Dimension[] getColumnDimensions() => new[]
        {
            new Dimension(GridSizeMode.Absolute, 52),  // 1. Rank Badge
            new Dimension(GridSizeMode.Distributed),   // 2. Title, Difficulty & Stars
            new Dimension(GridSizeMode.Absolute, 80),  // 3. Acc
            new Dimension(GridSizeMode.Absolute, 80),  // 4. Score
            service.ShowUR.Value
                ? new Dimension(GridSizeMode.Absolute, 80)
                : new Dimension(GridSizeMode.Absolute, 0),  // 5. UR
            new Dimension(GridSizeMode.Absolute, 96),  // 6. Hits (expanded for 6-grade Mania and custom rulesets)
            new Dimension(GridSizeMode.Absolute, 80),  // 7. Combo
            new Dimension(GridSizeMode.Absolute, 80),  // 8. PP
            new Dimension(GridSizeMode.Absolute, 80),  // 9. Time
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            service.CompactMode.BindValueChanged(e =>
            {
                if (IsDisposed) return;
                Height = e.NewValue ? 40 : 58;
            }, true);

            service.PpDisplay.BindValueChanged(_ => updateVisuals());
            service.HighlightUR.BindValueChanged(_ => updateVisuals());
            service.ShowModsInHistory.BindValueChanged(_ => updateVisuals());
            service.ShowDifficultyRating.BindValueChanged(_ => updateVisuals());
            service.ShowUR.BindValueChanged(_ => updateVisuals());

            Child = buildContent();
        }

        private void updateVisuals()
        {
            if (IsDisposed) return;
            Clear();
            Child = buildContent();
        }

        public void FlashHighlight()
        {
            if (IsDisposed) return;

            hoverOverlay?.FadeColour(Color4Extensions.FromHex("#5cd4f3"), 0)
                         .FadeTo(0.45f, 80)
                         .Then()
                         .FadeTo(0f, 1000, Easing.OutQuint);

            this.ScaleTo(1.025f, 100, Easing.OutQuad)
                .Then()
                .ScaleTo(1f, 350, Easing.OutQuint);
        }

        private Container buildContent()
        {
            string ppPrimary;
            string ppSecondary;
            Color4 ppSecondaryColour = colourProvider.Content2;
            bool isPpGain = false;

            double? rawPp = currentPlay.PerformancePoints;
            string rawPpStr = (rawPp.HasValue && rawPp.Value > 0)
                ? $"{rawPp.Value.ToString("F0", CultureInfo.InvariantCulture)} PP"
                : "-";

            string gainStr = "+0 PP";
            if (currentPlay.ProfilePerformancePoints.HasValue)
            {
                double prof = currentPlay.ProfilePerformancePoints.Value;
                if (prof > 0)
                {
                    gainStr = $"+{prof.ToString("F0", CultureInfo.InvariantCulture)} PP";
                    ppSecondaryColour = Color4Extensions.FromHex("#00ff66");
                    isPpGain = true;
                }
                else if (prof < 0)
                {
                    gainStr = $"{prof.ToString("F0", CultureInfo.InvariantCulture)} PP";
                    ppSecondaryColour = Color4Extensions.FromHex("#ed4242");
                }
                else
                {
                    gainStr = "+0 PP";
                    ppSecondaryColour = colourProvider.Content2;
                }
            }

            switch (service.PpDisplay.Value)
            {
                case PpDisplayMode.ProfileGainOnly:
                    ppPrimary = gainStr;
                    ppSecondary = rawPpStr != "-" ? rawPpStr : "Profile";
                    break;

                case PpDisplayMode.ScorePpOnly:
                    ppPrimary = rawPpStr;
                    ppSecondary = "PP";
                    break;

                default: // PpDisplayMode.Both
                    ppPrimary = rawPpStr;
                    ppSecondary = gainStr;
                    break;
            }

            string rulesetName = currentPlay.RulesetName == "osu" ? "osu!" : currentPlay.RulesetName;

            var statusContainer = new Container
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.Both,
            };
            if (Enum.TryParse<BeatmapOnlineStatus>(currentPlay.Status, true, out var onlineStatus))
            {
                statusContainer.Child = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Scale = new Vector2(0.72f),
                    Child = new BeatmapSetOnlineStatusPill
                    {
                        Status = onlineStatus,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    }
                };
            }
            else
            {
                statusContainer.Child = new OsuSpriteText
                {
                    Text = currentPlay.Status,
                    Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                    Colour = colourProvider.Highlight1,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                };
            }

            var urFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 3),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = currentPlay.UnstableRate.HasValue
                            ? currentPlay.UnstableRate.Value.ToString("F1", CultureInfo.InvariantCulture)
                            : "-",
                        Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                        Colour = getUrColour(currentPlay.UnstableRate),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "UR",
                        Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                        Colour = colourProvider.Content2,
                    }
                }
            };

            var urContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = service.ShowUR.Value ? 1 : 0,
                Child = urFlow
            };

            service.ShowUR.BindValueChanged(v =>
            {
                if (IsDisposed) return;
                urContainer.Alpha = v.NewValue ? 1 : 0;
                if (grid != null)
                    grid.ColumnDimensions = getColumnDimensions();
            }, true);

            Color4 diffColour = colours != null ? colours.ForStarDifficulty(currentPlay.StarRating) : colourProvider.Highlight1;

            Drawable? rulesetIcon = null;
            if (rulesets != null)
            {
                var rInfo = rulesets.GetRuleset(currentPlay.RulesetName)
                    ?? rulesets.AvailableRulesets.FirstOrDefault(r =>
                        string.Equals(r.ShortName, currentPlay.RulesetName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.Name, currentPlay.RulesetName, StringComparison.OrdinalIgnoreCase));

                if (rInfo != null)
                {
                    try
                    {
                        rulesetIcon = rInfo.CreateInstance().CreateIcon();
                        if (rulesetIcon != null)
                            rulesetIcon.Size = new Vector2(10);
                    }
                    catch { }
                }
            }

            if (rulesetIcon == null)
            {
                rulesetIcon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(8),
                    Icon = FontAwesome.Solid.Circle,
                };
            }

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 10,
                BorderThickness = 1.5f,
                BorderColour = diffColour.Opacity(0.9f),
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4
                    },
                    new TrianglesV2
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = diffColour.Opacity(0.35f),
                    },
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Y,
                        Height = 0.72f,
                        Width = 18,
                        Masking = true,
                        CornerRadius = 9,
                        Margin = new MarginPadding { Left = 5 },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = diffColour,
                            },
                            new Container
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(10),
                                Colour = Color4.Black.Opacity(0.8f),
                                Child = rulesetIcon,
                            }
                        }
                    },
                    hoverOverlay = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    grid = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Left = 26, Right = 8, Vertical = 4 },
                        ColumnDimensions = getColumnDimensions(),
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                // Column 1: Rank
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new UpdateableRank(currentPlay.Rank)
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(36, 18),
                                    }
                                },
                                // Column 2: Beatmap Info
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 6 },
                                    Child = new OsuClickableContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Action = openBeatmapInfo,
                                        TooltipText = currentPlay.OnlineBeatmapID > 0 || currentPlay.OnlineBeatmapSetID > 0
                                            ? LazerLensStrings.TooltipViewBeatmap
                                            : LazerLensStrings.TooltipLocalBeatmap,
                                        Child = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2),
                                            Children = new Drawable[]
                                            {
                                                new TruncatingSpriteText
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Text = $"{currentPlay.BeatmapArtist} - {currentPlay.BeatmapTitle}",
                                                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                                    Colour = Color4.White.Opacity(0.95f),
                                                },
                                                new FillFlowContainer
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(6, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            AutoSizeAxes = Axes.Both,
                                                            Scale = new Vector2(0.80f),
                                                            Alpha = service.ShowDifficultyRating.Value ? 1 : 0,
                                                            Child = new StarRatingDisplay(new StarDifficulty(currentPlay.StarRating, 0), StarRatingDisplaySize.Small)
                                                            {
                                                                Anchor = Anchor.CentreLeft,
                                                                Origin = Anchor.CentreLeft,
                                                            }
                                                        },
                                                        statusContainer,
                                                        currentPlay.IsWarmup ? new Container
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            AutoSizeAxes = Axes.Both,
                                                            Masking = true,
                                                            CornerRadius = 3,
                                                            Child = new Box
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Colour = Color4Extensions.FromHex("#ff9800"),
                                                            },
                                                            Children = new Drawable[]
                                                            {
                                                                new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = Color4Extensions.FromHex("#ff9800"),
                                                                },
                                                                new OsuSpriteText
                                                                {
                                                                    Text = LazerLensStrings.WarmupBadge,
                                                                    Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Bold),
                                                                    Colour = Color4.Black,
                                                                    Padding = new MarginPadding { Horizontal = 4, Vertical = 1 },
                                                                }
                                                            }
                                                        } : Empty()
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                // Column 3: Accuracy & Mods
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 2),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopCentre,
                                                Origin = Anchor.TopCentre,
                                                Text = $"{currentPlay.Accuracy.ToString("F2", CultureInfo.InvariantCulture)}%",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = colourProvider.Content1,
                                            },
                                            buildModsFlow(),
                                        }
                                    }
                                },
                                // Column 4: Score
                                buildCell(
                                    currentPlay.TotalScore.ToString("N0", CultureInfo.InvariantCulture),
                                    LazerLensStrings.SortScore,
                                    Color4.White.Opacity(0.95f),
                                    colourProvider.Content2
                                ),
                                // Column 5: UR
                                urContainer,
                                // Column 6: Hits
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 3),
                                        Children = new Drawable[]
                                        {
                                            buildHitsFlow(),
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopCentre,
                                                Origin = Anchor.TopCentre,
                                                Text = LazerLensStrings.SortHits,
                                                Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                                Colour = colourProvider.Content2,
                                            }
                                        }
                                    }
                                },
                                // Column 7: Combo
                                buildCell(
                                    $"{currentPlay.MaxCombo.ToString("N0", CultureInfo.InvariantCulture)}x",
                                    LazerLensStrings.SortCombo,
                                    colourProvider.Content1,
                                    colourProvider.Content2
                                ),
                                // Column 8: PP
                                buildPpCell(
                                    ppPrimary,
                                    ppSecondary,
                                    colourProvider.Content1,
                                    ppSecondaryColour,
                                    isPpGain,
                                    currentPlay
                                ),
                                // Column 9: Time
                                buildCell(
                                    currentPlay.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                                    currentPlay.Timestamp.ToLocalTime().ToString("dd MMM", CultureInfo.InvariantCulture),
                                    colourProvider.Content1,
                                    colourProvider.Content2
                                )
                            }
                        }
                    }
                }
            };
        }

        private Drawable buildModsFlow()
        {
            if (!service.ShowModsInHistory.Value || currentPlay.Mods.Length == 0)
            {
                return new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = currentPlay.Mods.Length == 0 ? "NoMod" : "+" + string.Join("", currentPlay.Mods),
                    Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                    Colour = colourProvider.Content2.Opacity(0.7f),
                };
            }

            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(2, 0),
            };

            foreach (var mod in currentPlay.Mods)
            {
                Color4 bgColour = getModColour(mod);
                flow.Add(new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 3,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = bgColour,
                        },
                        new OsuSpriteText
                        {
                            Text = mod,
                            Font = OsuFont.Torus.With(size: 9.5f, weight: FontWeight.Bold),
                            Colour = Color4.White,
                            Padding = new MarginPadding { Horizontal = 3, Vertical = 1 },
                        }
                    }
                });
            }

            return flow;
        }

        private static Color4 getModColour(string mod)
        {
            string upper = mod.ToUpperInvariant();
            if (upper.Contains("DT") || upper.Contains("NC")) return Color4Extensions.FromHex("#b35cff");
            if (upper.Contains("HR")) return Color4Extensions.FromHex("#ff5252");
            if (upper.Contains("HD")) return Color4Extensions.FromHex("#ffd54f");
            if (upper.Contains("FL")) return Color4Extensions.FromHex("#4fc3f7");
            if (upper.Contains("EZ") || upper.Contains("HT")) return Color4Extensions.FromHex("#81c784");
            if (upper.Contains("NF")) return Color4Extensions.FromHex("#4db6ac");
            if (upper.Contains("RX") || upper.Contains("AP") || upper.Contains("AT")) return Color4Extensions.FromHex("#ff80ab");
            if (upper.EndsWith("K") || upper.Contains("KEY") || upper.Contains("DS") || upper.Contains("MR")) return Color4Extensions.FromHex("#00bcd4");
            return Color4Extensions.FromHex("#78909c");
        }

        private Color4 getUrColour(double? ur)
        {
            if (!ur.HasValue) return colourProvider.Content2;
            if (!service.HighlightUR.Value) return colourProvider.Content1;

            if (ur.Value < 80) return Color4Extensions.FromHex("#87d332");
            if (ur.Value <= 110) return Color4Extensions.FromHex("#e5a228");
            return colourProvider.Content2;
        }

        private FillFlowContainer buildHitsFlow()
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(1, 0),
            };

            string ruleset = (currentPlay.RulesetName ?? "").ToLowerInvariant();

            if (ruleset.Contains("taiko"))
            {
                // Taiko: Great / Ok / Miss (3 counts)
                addHitCount(flow, currentPlay.CountGreat, "#5cd4f3", 12.5f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountOk, "#87d332", 12.5f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountMiss, "#ed4242", 12.5f, isMiss: true);
            }
            else if (ruleset.Contains("catch") || ruleset.Contains("fruit"))
            {
                // Catch: Fruits / Drops / Droplets / Miss (4 counts)
                addHitCount(flow, currentPlay.CountGreat, "#5cd4f3", 11.5f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountLargeTickHit, "#87d332", 11.5f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountSmallTickHit, "#e5a228", 11.5f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountMiss, "#ed4242", 11.5f, isMiss: true);
            }
            else if (ruleset.Contains("mania"))
            {
                // Mania: MAX / 300 / 200 / 100 / 50 / Miss (6 counts)
                flow.Spacing = new Vector2(0.5f, 0);
                addHitCount(flow, currentPlay.CountPerfect, "#ffffff", 10f);
                addSlash(flow, 9.5f);
                addHitCount(flow, currentPlay.CountGreat, "#5cd4f3", 10f);
                addSlash(flow, 9.5f);
                addHitCount(flow, currentPlay.CountGood, "#87d332", 10f);
                addSlash(flow, 9.5f);
                addHitCount(flow, currentPlay.CountOk, "#e5a228", 10f);
                addSlash(flow, 9.5f);
                addHitCount(flow, currentPlay.CountMeh, "#ba68c8", 10f);
                addSlash(flow, 9.5f);
                addHitCount(flow, currentPlay.CountMiss, "#ed4242", 10f, isMiss: true);
            }
            else if (ruleset.Equals("osu", StringComparison.OrdinalIgnoreCase) || ruleset.Contains("standard") || ruleset.Equals("osu!", StringComparison.OrdinalIgnoreCase))
            {
                // Standard: 300 / 100 / 50 / Miss (4 counts)
                addHitCount(flow, currentPlay.CountGreat, "#5cd4f3", 12f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountOk, "#87d332", 12f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountMeh, "#e5a228", 12f);
                addSlash(flow);
                addHitCount(flow, currentPlay.CountMiss, "#ed4242", 12f, isMiss: true);
            }
            else
            {
                // Custom rulesets (Sentakki, Tau, Soyokaze, Swing, etc.)
                if (currentPlay.Statistics.Count > 0)
                {
                    bool first = true;
                    foreach (var (result, count) in currentPlay.Statistics)
                    {
                        if (count == 0 && result != HitResult.Miss) continue;
                        if (result is HitResult.None or HitResult.IgnoreHit or HitResult.IgnoreMiss or HitResult.SmallBonus or HitResult.LargeBonus) continue;

                        if (!first) addSlash(flow, 9.5f);
                        first = false;

                        string color = result switch
                        {
                            HitResult.Perfect => "#ffffff",
                            HitResult.Great => "#5cd4f3",
                            HitResult.Good or HitResult.Ok or HitResult.LargeTickHit => "#87d332",
                            HitResult.Meh or HitResult.SmallTickHit => "#e5a228",
                            HitResult.Miss => "#ed4242",
                            _ => "#ffffff"
                        };

                        addHitCount(flow, count, color, 10.5f, isMiss: result == HitResult.Miss);
                    }
                }
                else
                {
                    addHitCount(flow, currentPlay.CountGreat, "#5cd4f3", 12f);
                    addSlash(flow);
                    addHitCount(flow, currentPlay.CountOk, "#87d332", 12f);
                    addSlash(flow);
                    addHitCount(flow, currentPlay.CountMiss, "#ed4242", 12f, isMiss: true);
                }
            }

            return flow;
        }

        private void addHitCount(FillFlowContainer flow, int count, string hexColour, float size = 13, bool isMiss = false)
        {
            flow.Add(new OsuSpriteText
            {
                Text = count.ToString(CultureInfo.InvariantCulture),
                Font = OsuFont.Torus.With(size: size, weight: FontWeight.Bold),
                Colour = (isMiss && count == 0) ? colourProvider.Content2.Opacity(0.5f) : Color4Extensions.FromHex(hexColour),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        private void addSlash(FillFlowContainer flow, float size = 12)
        {
            flow.Add(new OsuSpriteText
            {
                Text = "/",
                Font = OsuFont.Torus.With(size: size, weight: FontWeight.Regular),
                Colour = colourProvider.Content2.Opacity(0.4f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        private static string formatPp(SessionPlayRecord p)
        {
            if (p.PerformancePoints.HasValue && p.PerformancePoints.Value > 0)
                return $"{p.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)} PP";

            return "-";
        }

        private static Container buildPpCell(string primary, string secondary, Color4 primaryColour, Color4 secondaryColour, bool isGain, SessionPlayRecord play)
        {
            var content = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(3, 0),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = primary,
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                Colour = primaryColour,
                            },
                            play.IsChoke ? new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(10),
                                Icon = FontAwesome.Solid.HeartBroken,
                                Colour = Color4Extensions.FromHex("#ff4081"),
                            } : Empty()
                        }
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = secondary,
                        Font = OsuFont.Torus.With(size: isGain ? 11 : 10, weight: isGain ? FontWeight.Bold : FontWeight.Regular),
                        Colour = secondaryColour,
                    }
                }
            };

            if (play.IsChoke)
            {
                double ifFc = play.IfFcPerformancePoints ?? 0;
                double lost = ifFc - (play.PerformancePoints ?? 0);
                return new ChokeTooltipContainer(LazerLensStrings.ChokeTooltip(ifFc, lost))
                {
                    Child = content
                };
            }

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = content
            };
        }

        private sealed partial class ChokeTooltipContainer : Container, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public ChokeTooltipContainer(LocalisableString tooltip)
            {
                TooltipText = tooltip;
                RelativeSizeAxes = Axes.Both;
            }
        }

        private static Container buildCell(string primary, LocalisableString secondary, Color4 primaryColour, Color4 secondaryColour, bool primaryBold = false)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = primary,
                            Font = OsuFont.Torus.With(size: 14, weight: primaryBold ? FontWeight.Bold : FontWeight.SemiBold),
                            Colour = primaryColour,
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = secondary,
                            Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                            Colour = secondaryColour,
                        }
                    }
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeColour(colourProvider.Background3, 100);
            hoverOverlay.FadeTo(0.04f, 100);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background4, 100);
            hoverOverlay.FadeTo(0, 100);
            base.OnHoverLost(e);
        }

        private void openBeatmapInfo()
        {
            var overlay = beatmapSetOverlay;

            if (currentPlay.OnlineBeatmapID > 0)
                overlay?.FetchAndShowBeatmap(currentPlay.OnlineBeatmapID);
            else if (currentPlay.OnlineBeatmapSetID > 0)
                overlay?.FetchAndShowBeatmapSet(currentPlay.OnlineBeatmapSetID);
        }
    }
}
