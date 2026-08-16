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
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Scoring;
using osucc.Client;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;
using LazerLens.UI.Components;

namespace LazerLens.UI
{
    public enum SessionSortMode
    {
        Recent,
        Score,
        Accuracy,
        PP,
        Combo,
        Grade,
        Difficulty
    }

    public enum SessionRulesetFilter
    {
        All,
        Osu,
        Mania,
        Taiko,
        Catch
    }

    public enum SessionOutcomeFilter
    {
        All,
        Pass,
        Fail
    }

    public partial class LazerLensOverlay : OsuCcShearedOverlay
    {
        private float ItemHeight => service.CompactMode.Value ? 38f : 56f;
        private float ItemSpacing => 6f;
        private float SlotHeight => ItemHeight + ItemSpacing;

        private readonly LazerLensService service;

        [Resolved(canBeNull: true)]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private SettingsOverlay? settingsOverlay { get; set; }

        private MetricCard timeCard = null!;
        private MetricCard playsCard = null!;
        private MetricCard accCard = null!;
        private MetricCard comboCard = null!;

        private BestScoreBanner bestScoreBanner = null!;
        private Container historyContainer = null!;
        private readonly Dictionary<Guid, SessionPlayHistoryItem> itemMap = new();
        private OsuSpriteText noHistoryText = null!;
        private OsuSpriteText historyCountText = null!;
        private OsuTextBox searchTextBox = null!;

        private SessionSortMode currentSort = SessionSortMode.Recent;
        private bool sortAscending;
        private SessionRulesetFilter currentRulesetFilter = SessionRulesetFilter.All;
        private SessionOutcomeFilter currentOutcomeFilter = SessionOutcomeFilter.All;

        private readonly List<SortPillButton> sortButtons = new();
        private readonly List<RulesetPillButton> rulesetButtons = new();
        private readonly List<OutcomePillButton> outcomeButtons = new();

        private bool isDataDirty = true;
        private int lastUpdatedSecond = -1;

        public LazerLensOverlay(LazerLensService service)
            : base(OverlayColourScheme.Aquamarine)
        {
            this.service = service;
        }

        private SessionSelectorDropdown sessionSelector = null!;
        private Container archiveBanner = null!;
        private OsuSpriteText archiveBannerText = null!;
        private Container headerIconsContainer = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.Title = "Lazer Lens";
            Header.Description = "Live performance tracking, session metrics and play history";

            AddInternal(headerIconsContainer = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 80,
                Depth = float.MinValue,
                Alpha = 0,
                Y = -50,
                Children = new Drawable[]
                {
                    // Left Icon in Header (Placed closer to the text)
                    new Container
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.Centre,
                        Position = new Vector2(120, 32),
                        Size = new Vector2(44),
                        Child = new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(28),
                            Icon = FontAwesome.Solid.ChartBar,
                            Colour = ColourProvider.Colour1,
                        }
                    },
                    // Right Settings Button (Placed further left from the Close button)
                    new HeaderSettingsButton(openPluginSettings)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.Centre,
                        Position = new Vector2(-160, 32),
                    }
                }
            });

            MainAreaContent.Add(new OverlayScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 16),
                    Padding = new MarginPadding
                    {
                        Horizontal = Padding * 2,
                        Vertical = Padding,
                    },
                    Children = new Drawable[]
                    {
                        // 0. Session Selector Dropdown
                        sessionSelector = new SessionSelectorDropdown(
                            onSessionSelected: id => service.SelectSession(id),
                            getSessions: () => service.GetAllSessionSummaries()
                        ),

                        // 0.5 Archive Banner (hidden by default)
                        archiveBanner = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 32,
                            Masking = true,
                            CornerRadius = 6,
                            Alpha = 0,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4Extensions.FromHex("ffcc00").Opacity(0.15f),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Horizontal = 12 },
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
                                                    Size = new Vector2(12),
                                                    Icon = FontAwesome.Solid.Archive,
                                                    Colour = Color4Extensions.FromHex("ffcc00"),
                                                },
                                                archiveBannerText = new OsuSpriteText
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                                    Colour = Color4Extensions.FromHex("ffcc00"),
                                                    Text = "Viewing archived session",
                                                }
                                            }
                                        },
                                        new OsuClickableContainer
                                        {
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            AutoSizeAxes = Axes.Both,
                                            Action = () =>
                                            {
                                                sessionSelector.SelectLive();
                                            },
                                            Child = new OsuSpriteText
                                            {
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                Colour = Color4Extensions.FromHex("00ffcc"),
                                                Text = "[ Return to Live ]",
                                            }
                                        }
                                    }
                                }
                            }
                        },

                        // 1. KPI Cards Grid (4 columns)
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 90,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Relative, 0.25f),
                                new Dimension(GridSizeMode.Relative, 0.25f),
                                new Dimension(GridSizeMode.Relative, 0.25f),
                                new Dimension(GridSizeMode.Relative, 0.25f),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Right = 6 },
                                        Child = timeCard = new MetricCard(FontAwesome.Solid.Clock, "Session Time", "00:00:00", "Started just now")
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Horizontal = 3 },
                                        Child = playsCard = new MetricCard(FontAwesome.Solid.Play, "Total Plays", "0", "0 passes \u2022 0 fails")
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Horizontal = 3 },
                                        Child = accCard = new MetricCard(FontAwesome.Solid.Percent, "Avg Accuracy", "0.00%", "Across all plays")
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Left = 6 },
                                        Child = comboCard = new MetricCard(FontAwesome.Solid.Fire, "Max Combo", "0x", "Session peak")
                                    }
                                }
                            }
                        },

                        // 2. Session Best Score Banner (Clickable)
                        bestScoreBanner = new BestScoreBanner(openBestScoreBeatmap),

                        // 3. Play History Header + Ruleset Filter + Outcome Filter + Sort Tabs
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 36,
                            Margin = new MarginPadding { Top = 8 },
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize), // Left (Filters)
                                new Dimension(GridSizeMode.Distributed), // Middle (Search bar)
                                new Dimension(GridSizeMode.AutoSize) // Right (Sort Tabs)
                            },
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Relative, 1f)
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    // Left: History Title + Ruleset Filters + Outcome Filters
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(8, 0),
                                        Children = new Drawable[]
                                        {
                                            historyCountText = new OsuSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = "PLAY HISTORY (0 PLAYS)",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = ColourProvider.Colour1,
                                                Margin = new MarginPadding { Right = 4 },
                                            },
                                            // Ruleset Filters: all, osu!, osu!mania, osu!taiko, osu!catch
                                            new FillFlowContainer
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(3, 0),
                                                Children = createRulesetFilterButtons()
                                            },
                                            // Outcome Filters: all, pass, fail
                                            new FillFlowContainer
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(3, 0),
                                                Children = createOutcomeFilterButtons()
                                            }
                                        }
                                    },
                                    // Middle: Search Bar (stretches, with margins)
                                    searchTextBox = new SearchTextBox
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        RelativeSizeAxes = Axes.X,
                                        Height = 30,
                                        Margin = new MarginPadding { Horizontal = 8 },
                                        PlaceholderText = "Search maps..."
                                    },
                                    // Right: Sort Tabs
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(4, 0),
                                        Children = createSortButtons()
                                    }
                                }
                            }
                        },

                        // 4. Play History Animated Container
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                noHistoryText = new OsuSpriteText
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Margin = new MarginPadding { Top = 40, Bottom = 40 },
                                    Text = "No beatmaps played in this session yet. Go set some scores!",
                                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Regular),
                                    Colour = Color4.White.Opacity(0.4f),
                                },
                                historyContainer = new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                }
                            }
                        }
                    }
                }
            });

            searchTextBox.Current.ValueChanged += _ => RefreshData();

            service.OnSessionUpdated += () =>
            {
                Schedule(() =>
                {
                    isDataDirty = true;
                    if (State.Value == Visibility.Visible)
                        RefreshData();
                });
            };

            service.CompactMode.BindValueChanged(_ => Schedule(() => { isDataDirty = true; if (State.Value == Visibility.Visible) RefreshData(); }));
            service.ShowUR.BindValueChanged(_ => Schedule(() => { isDataDirty = true; if (State.Value == Visibility.Visible) RefreshData(); }));
        }

        private static bool matchesSearch(SessionPlayRecord play, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return (play.BeatmapTitle?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                   (play.BeatmapArtist?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                   (play.DifficultyName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        }

        protected override void Update()
        {
            base.Update();

            if (State.Value == Visibility.Visible && timeCard != null && !service.IsViewingArchive)
            {
                var duration = service.LiveState.SessionDuration;
                int totalSeconds = (int)duration.TotalSeconds;

                if (totalSeconds != lastUpdatedSecond)
                {
                    lastUpdatedSecond = totalSeconds;
                    string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    timeCard.UpdateValues(timeStr, $"Started at {service.LiveState.SessionStart.ToString("HH:mm", CultureInfo.InvariantCulture)}");
                }
            }
        }

        private Drawable[] createRulesetFilterButtons()
        {
            var filters = new[]
            {
                (SessionRulesetFilter.All, "all"),
                (SessionRulesetFilter.Osu, "osu!"),
                (SessionRulesetFilter.Mania, "osu!mania"),
                (SessionRulesetFilter.Taiko, "osu!taiko"),
                (SessionRulesetFilter.Catch, "osu!catch"),
            };

            var list = new List<Drawable>();

            foreach (var (filter, label) in filters)
            {
                var btn = new RulesetPillButton(filter, label, filter == currentRulesetFilter, onRulesetFilterSelected);
                rulesetButtons.Add(btn);
                list.Add(btn);
            }

            return list.ToArray();
        }

        private void onRulesetFilterSelected(SessionRulesetFilter filter)
        {
            if (currentRulesetFilter == filter) return;

            currentRulesetFilter = filter;

            foreach (var btn in rulesetButtons)
                btn.SetActive(btn.Filter == currentRulesetFilter);

            RefreshData();
        }

        private Drawable[] createOutcomeFilterButtons()
        {
            var filters = new[]
            {
                (SessionOutcomeFilter.All, "all"),
                (SessionOutcomeFilter.Pass, "pass"),
                (SessionOutcomeFilter.Fail, "fail"),
            };

            var list = new List<Drawable>();

            foreach (var (filter, label) in filters)
            {
                var btn = new OutcomePillButton(filter, label, filter == currentOutcomeFilter, onOutcomeFilterSelected);
                outcomeButtons.Add(btn);
                list.Add(btn);
            }

            return list.ToArray();
        }

        private void onOutcomeFilterSelected(SessionOutcomeFilter filter)
        {
            if (currentOutcomeFilter == filter) return;

            currentOutcomeFilter = filter;

            foreach (var btn in outcomeButtons)
                btn.SetActive(btn.Filter == currentOutcomeFilter);

            RefreshData();
        }

        private Drawable[] createSortButtons()
        {
            var modes = new[]
            {
                SessionSortMode.Recent,
                SessionSortMode.Score,
                SessionSortMode.Accuracy,
                SessionSortMode.PP,
                SessionSortMode.Combo,
                SessionSortMode.Grade,
                SessionSortMode.Difficulty,
            };

            var list = new List<Drawable>();

            foreach (var mode in modes)
            {
                var btn = new SortPillButton(mode, mode == currentSort, sortAscending, onSortSelected);
                sortButtons.Add(btn);
                list.Add(btn);
            }

            return list.ToArray();
        }

        private void onSortSelected(SessionSortMode mode)
        {
            if (currentSort == mode)
            {
                sortAscending = !sortAscending;
            }
            else
            {
                currentSort = mode;
                sortAscending = false;
            }

            foreach (var btn in sortButtons)
                btn.UpdateState(btn.Mode == currentSort, sortAscending);

            RefreshData();
        }

        protected override void PopIn()
        {
            base.PopIn();

            headerIconsContainer.FadeIn(250, Easing.OutQuint);
            headerIconsContainer.MoveToY(0, 250, Easing.OutQuint);

            if (isDataDirty)
                RefreshData();
        }

        protected override void PopOut()
        {
            base.PopOut();

            headerIconsContainer.FadeOut(250, Easing.InQuint);
            headerIconsContainer.MoveToY(-50, 250, Easing.InQuint);
        }

        public void RefreshData()
        {
            isDataDirty = false;
            var state = service.State;

            // Archive banner visibility
            if (archiveBanner != null)
            {
                if (service.IsViewingArchive)
                {
                    archiveBanner.FadeIn(200, Easing.OutQuint);
                    archiveBannerText.Text = $"Viewing archived session from {state.SessionStart.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture)}";
                }
                else
                {
                    archiveBanner.FadeOut(150, Easing.InQuint);
                }
            }

            // 1. Session Duration
            if (service.IsViewingArchive)
            {
                // For archived sessions, show the stored duration
                var archivedDuration = state.Plays.Count > 0
                    ? state.Plays.Last().Timestamp - state.SessionStart
                    : TimeSpan.Zero;
                string timeStr = $"{(int)archivedDuration.TotalHours:D2}:{archivedDuration.Minutes:D2}:{archivedDuration.Seconds:D2}";
                timeCard?.UpdateValues(timeStr, $"Archived: {state.SessionStart.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)}");
            }
            else
            {
                var duration = state.SessionDuration;
                string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                timeCard?.UpdateValues(timeStr, $"Started at {state.SessionStart.ToString("HH:mm", CultureInfo.InvariantCulture)}");
            }

            // 2. Total Plays
            playsCard?.UpdateValues(state.TotalPlays.ToString(CultureInfo.InvariantCulture), $"{state.TotalPasses} pass \u2022 {state.TotalFails} fail");

            // 3. Average Accuracy & UR
            var urPlays = state.Plays.Where(p => p.UnstableRate.HasValue && p.UnstableRate.Value > 0).ToList();
            string urAvgStr = urPlays.Count > 0 ? $" \u2022 {urPlays.Average(p => p.UnstableRate!.Value):F1} avg UR" : "";
            accCard?.UpdateValues($"{state.AverageAccuracy.ToString("F2", CultureInfo.InvariantCulture)}%", $"{state.Plays.Count} recorded plays{urAvgStr}");

            // 4. Max Combo / Session PP Gain
            string ppGainStr = state.SessionPPGain >= 0 ? $"+{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp" : $"{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp";
            comboCard?.UpdateValues($"{state.MaxCombo.ToString("N0", CultureInfo.InvariantCulture)}x", $"Session PP: {ppGainStr}");

            // 5. Best Score Banner
            bestScoreBanner?.UpdateScore(state.BestScore);

            // 6. Filter by selected Ruleset and Outcome
            var filteredPlays = state.Plays.Where(p =>
                matchesRuleset(p, currentRulesetFilter) &&
                matchesOutcome(p, currentOutcomeFilter) &&
                matchesSearch(p, searchTextBox.Current.Value)
            );

            // 7. Smoothly animate and rearrange existing Play History Items
            IEnumerable<SessionPlayRecord> sortedPlays = currentSort switch
            {
                SessionSortMode.Score => sortAscending
                    ? filteredPlays.OrderBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.TotalScore),

                SessionSortMode.Accuracy => sortAscending
                    ? filteredPlays.OrderBy(p => p.Accuracy)
                    : filteredPlays.OrderByDescending(p => p.Accuracy),

                SessionSortMode.PP => sortAscending
                    ? filteredPlays.OrderBy(p => p.PerformancePoints ?? 0).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.PerformancePoints ?? 0).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Combo => sortAscending
                    ? filteredPlays.OrderBy(p => p.MaxCombo)
                    : filteredPlays.OrderByDescending(p => p.MaxCombo),

                SessionSortMode.Grade => sortAscending
                    ? filteredPlays.OrderByDescending(p => getGradeRank(p.Rank)).ThenBy(p => p.Accuracy)
                    : filteredPlays.OrderBy(p => getGradeRank(p.Rank)).ThenByDescending(p => p.Accuracy),

                SessionSortMode.Difficulty => sortAscending
                    ? filteredPlays.OrderBy(p => p.StarRating)
                    : filteredPlays.OrderByDescending(p => p.StarRating),

                _ => sortAscending
                    ? filteredPlays.AsEnumerable() // Oldest first
                    : filteredPlays.AsEnumerable().Reverse() // Recent (Newest first)
            };

            var playList = sortedPlays.ToList();

            if (historyCountText != null)
                historyCountText.Text = $"PLAY HISTORY ({playList.Count} PLAYS)";

            if (noHistoryText != null)
                noHistoryText.Alpha = playList.Count == 0 ? 1 : 0;

            if (historyContainer == null) return;

            // Remove stale items (e.g. on Filter change)
            var currentIds = new HashSet<Guid>(playList.Select(p => p.Id));
            var toRemove = itemMap.Keys.Where(k => !currentIds.Contains(k)).ToList();
            foreach (var id in toRemove)
            {
                if (itemMap.TryGetValue(id, out var item))
                {
                    item.FadeOut(180, Easing.OutQuint).Expire();
                    itemMap.Remove(id);
                }
            }

            // Animate items smoothly to their new Y positions without disposing / recreating
            for (int i = 0; i < playList.Count; i++)
            {
                var play = playList[i];
                float targetY = i * SlotHeight;

                if (!itemMap.TryGetValue(play.Id, out var item))
                {
                    item = new SessionPlayHistoryItem(play, service)
                    {
                        Y = targetY,
                        Alpha = 0,
                    };
                    historyContainer.Add(item);
                    itemMap[play.Id] = item;
                    item.FadeIn(250, Easing.OutQuint);
                }
                else
                {
                    // Existing item - update and animate it to the correct row
                    item.UpdateData(play);
                    item.MoveToY(targetY, 300, Easing.OutQuint);
                }
            }

            historyContainer.ResizeHeightTo(playList.Count * SlotHeight, 300, Easing.OutQuint);
        }

        private static bool matchesRuleset(SessionPlayRecord play, SessionRulesetFilter filter) => filter switch
        {
            SessionRulesetFilter.All => true,
            SessionRulesetFilter.Osu => play.RulesetName.Contains("osu!", StringComparison.OrdinalIgnoreCase) && !play.RulesetName.Contains("taiko") && !play.RulesetName.Contains("catch") && !play.RulesetName.Contains("mania"),
            SessionRulesetFilter.Taiko => play.RulesetName.Contains("taiko", StringComparison.OrdinalIgnoreCase),
            SessionRulesetFilter.Catch => play.RulesetName.Contains("catch", StringComparison.OrdinalIgnoreCase) || play.RulesetName.Contains("fruits", StringComparison.OrdinalIgnoreCase),
            SessionRulesetFilter.Mania => play.RulesetName.Contains("mania", StringComparison.OrdinalIgnoreCase),
            _ => true
        };

        private static bool matchesOutcome(SessionPlayRecord play, SessionOutcomeFilter filter) => filter switch
        {
            SessionOutcomeFilter.All => true,
            SessionOutcomeFilter.Pass => play.Passed,
            SessionOutcomeFilter.Fail => !play.Passed,
            _ => true
        };

        private static int getGradeRank(ScoreRank rank) => rank switch
        {
            ScoreRank.XH => 0,
            ScoreRank.X => 1,
            ScoreRank.SH => 2,
            ScoreRank.S => 3,
            ScoreRank.A => 4,
            ScoreRank.B => 5,
            ScoreRank.C => 6,
            ScoreRank.D => 7,
            ScoreRank.F => 8,
            _ => 9
        };

        private void openPluginSettings()
        {
            osucc.UI.Plugins.PluginsOverlayComponent.Instance?.ShowDetails("lazer-lens");
            Hide();
        }

        private void openBestScoreBeatmap()
        {
            var best = service.State.BestScore;
            if (best == null) return;

            var overlay = beatmapSetOverlay ?? ClientApi.Game?.Dependencies?.Get(typeof(BeatmapSetOverlay)) as BeatmapSetOverlay;

            if (best.OnlineBeatmapID > 0)
                overlay?.FetchAndShowBeatmap(best.OnlineBeatmapID);
            else if (best.OnlineBeatmapSetID > 0)
                overlay?.FetchAndShowBeatmapSet(best.OnlineBeatmapSetID);
        }

        private sealed partial class BestScoreBanner : OsuClickableContainer, IHasTooltip
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public override LocalisableString TooltipText => currentBest != null && (currentBest.OnlineBeatmapID > 0 || currentBest.OnlineBeatmapSetID > 0)
                ? "Click to view beatmap info in overlay"
                : string.Empty;

            private SessionPlayRecord? currentBest;
            private readonly Box background;
            private readonly Box hoverOverlay;
            private readonly OsuSpriteText titleText;
            private readonly OsuSpriteText subtitleText;

            public BestScoreBanner(Action action)
            {
                RelativeSizeAxes = Axes.X;
                Height = 70;
                Action = action;

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
                            Padding = new MarginPadding(12),
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.Distributed),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new CircularContainer
                                    {
                                        Size = new Vector2(46),
                                        Masking = true,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Margin = new MarginPadding { Right = 14 },
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.Gold.Opacity(0.2f),
                                            },
                                            new SpriteIcon
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Size = new Vector2(24),
                                                Icon = FontAwesome.Solid.Trophy,
                                                Colour = Color4.Gold,
                                            }
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 2),
                                        Children = new Drawable[]
                                        {
                                            titleText = new OsuSpriteText
                                            {
                                                Text = "SESSION BEST SCORE",
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = Color4.White,
                                            },
                                            subtitleText = new OsuSpriteText
                                            {
                                                Text = "No scores recorded yet in this session",
                                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Regular),
                                                Colour = Color4.White.Opacity(0.6f),
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                background.Colour = colourProvider.Background4;
            }

            public void UpdateScore(SessionPlayRecord? best)
            {
                currentBest = best;

                if (best != null)
                {
                    string bestStarPrefix = best.StarRating > 0 ? $"[\u2605 {best.StarRating.ToString("F2", CultureInfo.InvariantCulture)}] " : "";
                    titleText.Text = $"{bestStarPrefix}{best.BeatmapArtist} - {best.BeatmapTitle} [{best.DifficultyName}]";
                    string ppString = best.PerformancePoints.HasValue && best.PerformancePoints.Value > 0 ? $" \u2022 {best.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)}pp" : "";
                    subtitleText.Text = $"Grade: {best.Grade} \u2022 {best.Accuracy.ToString("F2", CultureInfo.InvariantCulture)}% \u2022 {best.TotalScore.ToString("N0", CultureInfo.InvariantCulture)} pts \u2022 {best.MaxCombo}x combo{ppString} \u2022 {best.Status}";
                }
                else
                {
                    titleText.Text = "SESSION BEST SCORE";
                    subtitleText.Text = "No scores recorded yet in this session";
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (currentBest != null)
                    hoverOverlay.FadeTo(0.08f, 150);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hoverOverlay.FadeTo(0, 150);
                base.OnHoverLost(e);
            }
        }

        private sealed partial class RulesetPillButton : ClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SessionRulesetFilter Filter { get; }
            private readonly string label;
            private readonly Action<SessionRulesetFilter> onSelect;
            private bool isActive;

            private readonly Box background;
            private readonly OsuSpriteText textSprite;

            public RulesetPillButton(SessionRulesetFilter filter, string label, bool active, Action<SessionRulesetFilter> onSelect)
            {
                Filter = filter;
                this.label = label;
                isActive = active;
                this.onSelect = onSelect;

                AutoSizeAxes = Axes.Both;
                Action = () => onSelect(Filter);

                InternalChild = new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 8, Vertical = 4 },
                            Child = textSprite = new OsuSpriteText
                            {
                                Text = label,
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                updateVisuals();
            }

            public void SetActive(bool active)
            {
                isActive = active;
                updateVisuals();
            }

            private void updateVisuals()
            {
                if (colourProvider == null) return;

                if (isActive)
                {
                    background.Colour = colourProvider.Colour1;
                    textSprite.Colour = Color4.White;
                }
                else
                {
                    background.Colour = colourProvider.Background4;
                    textSprite.Colour = Color4.White.Opacity(0.55f);
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background2, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background4, 100);
                base.OnHoverLost(e);
            }
        }

        private sealed partial class OutcomePillButton : ClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SessionOutcomeFilter Filter { get; }
            private readonly string label;
            private readonly Action<SessionOutcomeFilter> onSelect;
            private bool isActive;

            private readonly Box background;
            private readonly OsuSpriteText textSprite;

            public OutcomePillButton(SessionOutcomeFilter filter, string label, bool active, Action<SessionOutcomeFilter> onSelect)
            {
                Filter = filter;
                this.label = label;
                isActive = active;
                this.onSelect = onSelect;

                AutoSizeAxes = Axes.Both;
                Action = () => onSelect(Filter);

                InternalChild = new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 8, Vertical = 4 },
                            Child = textSprite = new OsuSpriteText
                            {
                                Text = label,
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                updateVisuals();
            }

            public void SetActive(bool active)
            {
                isActive = active;
                updateVisuals();
            }

            private void updateVisuals()
            {
                if (colourProvider == null) return;

                if (isActive)
                {
                    background.Colour = Filter switch
                    {
                        SessionOutcomeFilter.Pass => Color4.LimeGreen,
                        SessionOutcomeFilter.Fail => Color4.Coral,
                        _ => colourProvider.Colour0
                    };
                    textSprite.Colour = Color4.White;
                }
                else
                {
                    background.Colour = colourProvider.Background4;
                    textSprite.Colour = Color4.White.Opacity(0.55f);
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background2, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background4, 100);
                base.OnHoverLost(e);
            }
        }

        private sealed partial class SortPillButton : ClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SessionSortMode Mode { get; }
            private readonly Action<SessionSortMode> onSelect;
            private bool isActive;
            private bool isAscending;

            private readonly Box background;
            private readonly OsuSpriteText textSprite;

            public SortPillButton(SessionSortMode mode, bool active, bool ascending, Action<SessionSortMode> onSelect)
            {
                Mode = mode;
                isActive = active;
                isAscending = ascending;
                this.onSelect = onSelect;

                AutoSizeAxes = Axes.Both;
                Action = () => onSelect(Mode);

                InternalChild = new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 9, Vertical = 4 },
                            Child = textSprite = new OsuSpriteText
                            {
                                Text = getDisplayText(),
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.SemiBold),
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                updateVisuals();
            }

            public void UpdateState(bool active, bool ascending)
            {
                isActive = active;
                isAscending = ascending;
                updateVisuals();
            }

            private string getDisplayText()
            {
                if (!isActive) return Mode.ToString();
                return isAscending ? $"{Mode} \u25B2" : $"{Mode} \u25BC";
            }

            private void updateVisuals()
            {
                if (colourProvider == null) return;

                textSprite.Text = getDisplayText();

                if (isActive)
                {
                    background.Colour = colourProvider.Colour0;
                    textSprite.Colour = Color4.White;
                }
                else
                {
                    background.Colour = colourProvider.Background3;
                    textSprite.Colour = Color4.White.Opacity(0.6f);
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background2, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!isActive)
                    background.FadeColour(colourProvider.Background3, 100);
                base.OnHoverLost(e);
            }
        }

        private sealed partial class HeaderSettingsButton : ClickableContainer, IHasTooltip
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public LocalisableString TooltipText => "Lazer Lens Settings";

            private readonly Box background;
            private readonly SpriteIcon icon;

            public HeaderSettingsButton(Action action)
            {
                Size = new Vector2(36);
                Action = action;

                InternalChild = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        icon = new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(18),
                            Icon = FontAwesome.Solid.Cog,
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                background.Colour = colourProvider.Background4;
                icon.Colour = colourProvider.Colour1;
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeColour(colourProvider.Background2, 100);
                icon.RotateTo(45, 200, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeColour(colourProvider.Background4, 100);
                icon.RotateTo(0, 200, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}


