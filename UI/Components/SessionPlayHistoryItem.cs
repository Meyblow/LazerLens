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
            new Dimension(GridSizeMode.Absolute, 80),  // 6. Hits
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

            Child = buildContent();
        }

        private Container buildContent()
        {
            string modString = currentPlay.Mods.Length > 0 ? "+" + string.Join("", currentPlay.Mods) : "NoMod";

            string ppPrimary = currentPlay.PerformancePoints.HasValue && currentPlay.PerformancePoints.Value > 0
                ? $"{currentPlay.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)} PP"
                : "-";

            string ppSecondary = "PP";
            Color4 ppSecondaryColour = colourProvider.Content2;
            bool isPpGain = false;

            if (currentPlay.ProfilePerformancePoints.HasValue)
            {
                double prof = currentPlay.ProfilePerformancePoints.Value;
                if (prof > 0)
                {
                    ppSecondary = $"+{prof.ToString("F0", CultureInfo.InvariantCulture)} PP";
                    ppSecondaryColour = Color4Extensions.FromHex("#00ff66");
                    isPpGain = true;
                }
                else if (prof < 0)
                {
                    ppSecondary = $"{prof.ToString("F0", CultureInfo.InvariantCulture)} PP";
                    ppSecondaryColour = Color4Extensions.FromHex("#ed4242");
                }
                else
                {
                    ppSecondary = "+0 PP";
                    ppSecondaryColour = colourProvider.Content2;
                }
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

            bool hasUr = currentPlay.UnstableRate.HasValue && currentPlay.UnstableRate.Value > 0;
            string urText = hasUr ? currentPlay.UnstableRate!.Value.ToString("F2", CultureInfo.InvariantCulture) : "-";
            Color4 urColour = hasUr ? colourProvider.Content1 : colourProvider.Content2;

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
                        Text = urText,
                        Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                        Colour = urColour,
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
                        string.Equals(r.Name, currentPlay.RulesetName, StringComparison.OrdinalIgnoreCase) ||
                        (currentPlay.RulesetName.Contains("taiko", StringComparison.OrdinalIgnoreCase) && r.ShortName == "taiko") ||
                        (currentPlay.RulesetName.Contains("catch", StringComparison.OrdinalIgnoreCase) && r.ShortName == "fruits") ||
                        (currentPlay.RulesetName.Contains("fruit", StringComparison.OrdinalIgnoreCase) && r.ShortName == "fruits") ||
                        (currentPlay.RulesetName.Contains("mania", StringComparison.OrdinalIgnoreCase) && r.ShortName == "mania"))
                    ?? rulesets.GetRuleset(0);

                if (rInfo != null)
                {
                    try
                    {
                        var iconDrawable = rInfo.CreateInstance()?.CreateIcon();
                        if (iconDrawable != null)
                        {
                            iconDrawable.Size = new Vector2(10);
                            iconDrawable.Anchor = Anchor.Centre;
                            iconDrawable.Origin = Anchor.Centre;
                            rulesetIcon = iconDrawable;
                        }
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
                    // Subtle geometric triangles pattern in the card background
                    new TrianglesV2
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = diffColour.Opacity(0.35f),
                    },
                    // Left colored accent rounded pill with ruleset icon
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
                                // Column 1: Rank Badge
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 4 },
                                    Child = new UpdateableRank(currentPlay.Rank)
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(40, 20),
                                    }
                                },
                                // Column 2: Beatmap & Difficulty Details (Vanilla SongSelect style)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = true,
                                    Padding = new MarginPadding { Left = 6, Right = 6 },
                                    Child = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 2),
                                        Padding = new MarginPadding { Right = 6 },
                                        Children = new Drawable[]
                                        {
                                            // Top Line: Title - Artist [Difficulty]
                                            new Container
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Child = new TruncatingSpriteText
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Text = $"{currentPlay.BeatmapArtist} - {currentPlay.BeatmapTitle} [{currentPlay.DifficultyName}]",
                                                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                                    Colour = Color4.White,
                                                }
                                            },
                                            // Bottom Line: StarRatingDisplay + OnlineStatus
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
                                                        Child = new StarRatingDisplay(new StarDifficulty(currentPlay.StarRating, 0), StarRatingDisplaySize.Small)
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                        }
                                                    },
                                                    statusContainer,
                                                }
                                            }
                                        }
                                    }
                                },
                                // Column 3: Accuracy
                                buildCell(
                                    $"{currentPlay.Accuracy.ToString("F2", CultureInfo.InvariantCulture)}%",
                                    modString,
                                    colourProvider.Content1,
                                    colourProvider.Content2,
                                    true
                                ),
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
                                    isPpGain
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

            int n300 = currentPlay.CountGreat;
            int n100 = currentPlay.CountOk;
            int n50 = currentPlay.CountMeh;
            int nMiss = currentPlay.CountMiss;

            flow.Add(new OsuSpriteText
            {
                Text = n300.ToString(CultureInfo.InvariantCulture),
                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                Colour = Color4Extensions.FromHex("#5cd4f3"),
            });

            flow.Add(new OsuSpriteText
            {
                Text = "/",
                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                Colour = colourProvider.Content2.Opacity(0.5f),
                Margin = new MarginPadding { Horizontal = 1 },
            });

            flow.Add(new OsuSpriteText
            {
                Text = n100.ToString(CultureInfo.InvariantCulture),
                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                Colour = Color4Extensions.FromHex("#87d332"),
            });

            flow.Add(new OsuSpriteText
            {
                Text = "/",
                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                Colour = colourProvider.Content2.Opacity(0.5f),
                Margin = new MarginPadding { Horizontal = 1 },
            });

            flow.Add(new OsuSpriteText
            {
                Text = n50.ToString(CultureInfo.InvariantCulture),
                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                Colour = Color4Extensions.FromHex("#e5a228"),
            });

            flow.Add(new OsuSpriteText
            {
                Text = "/",
                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                Colour = colourProvider.Content2.Opacity(0.5f),
                Margin = new MarginPadding { Horizontal = 1 },
            });

            flow.Add(new OsuSpriteText
            {
                Text = nMiss.ToString(CultureInfo.InvariantCulture),
                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                Colour = nMiss > 0 ? Color4Extensions.FromHex("#ed4242") : colourProvider.Content2,
            });

            return flow;
        }

        private static string formatPp(SessionPlayRecord p)
        {
            if (p.PerformancePoints.HasValue && p.PerformancePoints.Value > 0)
                return $"{p.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)} PP";

            return "-";
        }

        private static Container buildPpCell(string primary, string secondary, Color4 primaryColour, Color4 secondaryColour, bool isGain)
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
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = primary,
                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                            Colour = primaryColour,
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
                }
            };
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
