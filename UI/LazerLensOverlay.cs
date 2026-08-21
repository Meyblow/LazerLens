using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
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

    public enum SessionStatusFilter
    {
        All,
        Ranked,
        Loved,
        Graveyard
    }

    public partial class LazerLensOverlay : OsuCcShearedOverlay
    {
        private float ItemHeight => service.CompactMode.Value ? 38f : 56f;
        private static float ItemSpacing => 6f;
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
        private LazerLensFilterControl filterControl = null!;

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
        private LazerLensSettingsModal settingsModal = null!;

        public static bool IsSettingsModalOpen { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.Title = LazerLensStrings.Name;
            Header.Description = LazerLensStrings.Description;
            Header.Icon = FontAwesome.Solid.ChartBar;

            AddInternal(settingsModal = new LazerLensSettingsModal(service));

            AddInternal(headerIconsContainer = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 80,
                Depth = float.MinValue,
                Alpha = 0,
                Y = -50,
                Child = new HeaderSettingsButton(openPluginSettings)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.Centre,
                    Position = new Vector2(-160, 32),
                }
            });

            MainAreaContent.AddRange(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourProvider.Background6.Opacity(0.96f),
                },
                new OverlayScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 12),
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
                                                        Text = LazerLensStrings.ArchiveBanner(""),
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
                                                    Text = LazerLensStrings.ReturnToLive,
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
                                            Child = timeCard = new MetricCard(FontAwesome.Solid.Clock, LazerLensStrings.OverlaySessionTime, "00:00:00", LazerLensStrings.TimeStartedJustNow)
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Horizontal = 3 },
                                            Child = playsCard = new MetricCard(FontAwesome.Solid.Play, LazerLensStrings.OverlayTotalPlays, "0", LazerLensStrings.PlaysPassFail(0, 0))
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Horizontal = 3 },
                                            Child = accCard = new MetricCard(FontAwesome.Solid.Percent, LazerLensStrings.OverlayAvgAccuracy, "0.00%", LazerLensStrings.PlaysRecorded(0))
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Left = 6 },
                                            Child = comboCard = new MetricCard(FontAwesome.Solid.Fire, LazerLensStrings.OverlayMaxCombo, "0x", LazerLensStrings.OverlaySessionPPGain("+0.0 pp"))
                                        }
                                    }
                                }
                            },

                            // 2. Session Best Score Banner (Clickable)
                            bestScoreBanner = new BestScoreBanner(openBestScoreBeatmap),

                            // 3. Play History Header
                            historyCountText = new OsuSpriteText
                            {
                                Text = LazerLensStrings.HistoryTitle(0),
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                Colour = ColourProvider.Colour1,
                                Margin = new MarginPadding { Top = 4 },
                            },

                            // 4. BeatmapListing-style Filter Control
                            filterControl = new LazerLensFilterControl(),

                            // 5. Play History Animated Container
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Margin = new MarginPadding { Top = 4 },
                                Children = new Drawable[]
                                {
                                    noHistoryText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        Margin = new MarginPadding { Top = 40, Bottom = 40 },
                                        Text = LazerLensStrings.HistoryEmpty,
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
                }
            });

            filterControl.SearchChanged += _ => RefreshData();
            filterControl.RulesetChanged += _ => RefreshData();
            filterControl.OutcomeChanged += _ => RefreshData();
            filterControl.StatusChanged += _ => RefreshData();
            filterControl.SortChanged += _ => RefreshData();
            filterControl.SortDirectionToggled += _ => RefreshData();

            service.OnSessionUpdated += onServiceSessionUpdated;

            service.CompactMode.BindValueChanged(_ => Schedule(() =>
            {
                if (IsDisposed) return;
                isDataDirty = true;
                if (State.Value == Visibility.Visible) RefreshData();
            }));

            service.ShowUR.BindValueChanged(_ => Schedule(() =>
            {
                if (IsDisposed) return;
                isDataDirty = true;
                if (State.Value == Visibility.Visible) RefreshData();
            }));
        }

        private void onServiceSessionUpdated()
        {
            Schedule(() =>
            {
                if (IsDisposed) return;
                isDataDirty = true;
                if (State.Value == Visibility.Visible)
                    RefreshData();
            });
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
                    timeCard.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(service.LiveState.SessionStart.ToString("HH:mm", CultureInfo.InvariantCulture)));
                }
            }
        }

        protected override void PopIn()
        {
            base.PopIn();

            settingsModal?.Hide();

            headerIconsContainer.FadeIn(250, Easing.OutQuint);
            headerIconsContainer.MoveToY(0, 250, Easing.OutQuint);

            if (isDataDirty)
                RefreshData();
        }

        protected override void PopOut()
        {
            base.PopOut();

            settingsModal?.Hide();

            headerIconsContainer.FadeOut(250, Easing.InQuint);
            headerIconsContainer.MoveToY(-50, 250, Easing.InQuint);
        }

        public void RefreshData()
        {
            if (IsDisposed) return;
            isDataDirty = false;
            var state = service.State;

            // Archive banner visibility
            if (archiveBanner != null)
            {
                if (service.IsViewingArchive)
                {
                    archiveBanner.FadeIn(200, Easing.OutQuint);
                    archiveBannerText.Text = LazerLensStrings.ArchiveBanner(state.SessionStart.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture));
                }
                else
                {
                    archiveBanner.FadeOut(150, Easing.InQuint);
                }
            }

            // 1. Session Duration
            if (service.IsViewingArchive)
            {
                var archivedDuration = state.Plays.Count > 0
                    ? state.Plays.Last().Timestamp - state.SessionStart
                    : TimeSpan.Zero;
                string timeStr = $"{(int)archivedDuration.TotalHours:D2}:{archivedDuration.Minutes:D2}:{archivedDuration.Seconds:D2}";
                timeCard?.UpdateValues(timeStr, LazerLensStrings.TimeArchived(state.SessionStart.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)));
            }
            else
            {
                var duration = state.SessionDuration;
                string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                timeCard?.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(state.SessionStart.ToString("HH:mm", CultureInfo.InvariantCulture)));
            }

            // 2. Total Plays
            playsCard?.UpdateValues(state.TotalPlays.ToString(CultureInfo.InvariantCulture), LazerLensStrings.PlaysPassFail(state.TotalPasses, state.TotalFails));

            // 3. Average Accuracy & UR
            var urPlays = state.Plays.Where(p => p.UnstableRate.HasValue && p.UnstableRate.Value > 0).ToList();
            string urAvgStr = urPlays.Count > 0 ? LazerLensStrings.AvgUr(urPlays.Average(p => p.UnstableRate!.Value).ToString("F1", CultureInfo.InvariantCulture)).ToString() : "";
            accCard?.UpdateValues($"{state.AverageAccuracy.ToString("F2", CultureInfo.InvariantCulture)}%", LazerLensStrings.AccPlaysUr(state.Plays.Count, urAvgStr));

            // 4. Max Combo / Session PP Gain
            string ppGainStr = state.SessionPPGain >= 0 ? $"+{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp" : $"{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp";
            comboCard?.UpdateValues($"{state.MaxCombo.ToString("N0", CultureInfo.InvariantCulture)}x", LazerLensStrings.OverlaySessionPPGain(ppGainStr));

            // 5. Best Score Banner
            bestScoreBanner?.UpdateScore(state.BestScore);

            if (filterControl == null) return;

            // 6. Filter by selected Ruleset, Outcome, Status, and Search
            var filteredPlays = state.Plays.Where(p =>
                matchesRuleset(p, filterControl.CurrentRuleset) &&
                matchesOutcome(p, filterControl.CurrentOutcome) &&
                matchesStatus(p, filterControl.CurrentStatus) &&
                matchesSearch(p, filterControl.SearchTextBox?.Current.Value ?? "")
            );

            // 7. Sort
            IEnumerable<SessionPlayRecord> sortedPlays = filterControl.CurrentSort switch
            {
                SessionSortMode.Score => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.TotalScore),

                SessionSortMode.Accuracy => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.Accuracy)
                    : filteredPlays.OrderByDescending(p => p.Accuracy),

                SessionSortMode.PP => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.PerformancePoints ?? 0).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.PerformancePoints ?? 0).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Combo => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.MaxCombo)
                    : filteredPlays.OrderByDescending(p => p.MaxCombo),

                SessionSortMode.Grade => filterControl.SortAscending
                    ? filteredPlays.OrderByDescending(p => getGradeRank(p.Rank)).ThenBy(p => p.Accuracy)
                    : filteredPlays.OrderBy(p => getGradeRank(p.Rank)).ThenByDescending(p => p.Accuracy),

                SessionSortMode.Difficulty => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.StarRating)
                    : filteredPlays.OrderByDescending(p => p.StarRating),

                _ => filterControl.SortAscending
                    ? filteredPlays.AsEnumerable() // Oldest first
                    : filteredPlays.AsEnumerable().Reverse() // Recent (Newest first)
            };

            var playList = sortedPlays.ToList();

            if (historyCountText != null)
                historyCountText.Text = LazerLensStrings.HistoryTitle(playList.Count);

            if (noHistoryText != null)
            {
                noHistoryText.Alpha = playList.Count == 0 ? 1 : 0;
                if (playList.Count == 0)
                {
                    noHistoryText.Text = state.Plays.Count > 0
                        ? LazerLensStrings.FilterEmpty
                        : LazerLensStrings.HistoryEmpty;
                }
            }

            if (historyContainer == null) return;

            // Remove stale items
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

            // Animate items smoothly to their new Y positions
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

        private static bool matchesStatus(SessionPlayRecord play, SessionStatusFilter filter) => filter switch
        {
            SessionStatusFilter.All => true,
            SessionStatusFilter.Ranked => play.Status.Equals("Ranked", StringComparison.OrdinalIgnoreCase) ||
                                          play.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                                          play.Status.Equals("Qualified", StringComparison.OrdinalIgnoreCase),
            SessionStatusFilter.Loved => play.Status.Equals("Loved", StringComparison.OrdinalIgnoreCase),
            SessionStatusFilter.Graveyard => play.Status.Equals("Graveyard", StringComparison.OrdinalIgnoreCase) ||
                                             play.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                                             play.Status.Equals("WIP", StringComparison.OrdinalIgnoreCase) ||
                                             play.Status.Equals("Local", StringComparison.OrdinalIgnoreCase),
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
            settingsModal.Toggle();
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

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            service.OnSessionUpdated -= onServiceSessionUpdated;
        }

        private sealed partial class BestScoreBanner : OsuClickableContainer, IHasTooltip
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public override LocalisableString TooltipText => currentBest != null && (currentBest.OnlineBeatmapID > 0 || currentBest.OnlineBeatmapSetID > 0)
                ? LazerLensStrings.TooltipViewBeatmap
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
                                                Text = LazerLensStrings.BestScoreTitle,
                                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                Colour = Color4.White,
                                            },
                                            subtitleText = new OsuSpriteText
                                            {
                                                Text = LazerLensStrings.BestScoreEmpty,
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
                    string urString = best.UnstableRate.HasValue && best.UnstableRate.Value > 0 ? $" \u2022 {best.UnstableRate.Value.ToString("F1", CultureInfo.InvariantCulture)} UR" : "";
                    string ppString = best.PerformancePoints.HasValue && best.PerformancePoints.Value > 0 ? $" \u2022 {best.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)}pp" : "";
                    subtitleText.Text = LazerLensStrings.BestScoreDetail(
                        best.Grade,
                        best.Accuracy.ToString("F2", CultureInfo.InvariantCulture),
                        best.TotalScore.ToString("N0", CultureInfo.InvariantCulture),
                        best.MaxCombo,
                        $"{urString}{ppString}",
                        best.Status
                    );
                }
                else
                {
                    titleText.Text = LazerLensStrings.BestScoreTitle;
                    subtitleText.Text = LazerLensStrings.BestScoreEmpty;
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

        private sealed partial class HeaderSettingsButton : ClickableContainer, IHasTooltip
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public LocalisableString TooltipText => LazerLensStrings.HeaderSettingsTooltip;

            private readonly Box background;
            private readonly SpriteIcon icon;

            public HeaderSettingsButton(Action action)
            {
                Size = new Vector2(36);
                Action = action;

                Child = new CircularContainer
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

        private sealed partial class LazerLensSettingsModal : CompositeDrawable
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly LazerLensService service;
            private Container modalCard = null!;

            public LazerLensSettingsModal(LazerLensService service)
            {
                this.service = service;
                RelativeSizeAxes = Axes.Both;
                Depth = -100;
                Alpha = 0;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    // Dim background (clicking closes modal, absorbs positional input to block tooltips underneath)
                    new ClickableContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = Hide,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black.Opacity(0.65f),
                        }
                    },
                    // Modal Card
                    modalCard = new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 520,
                        AutoSizeAxes = Axes.Y,
                        Masking = true,
                        CornerRadius = 12,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background5,
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    // Header
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 44,
                                        Padding = new MarginPadding { Horizontal = 16 },
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
                                                        Icon = FontAwesome.Solid.Cog,
                                                        Colour = colourProvider.Colour1,
                                                    },
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.CentreLeft,
                                                        Origin = Anchor.CentreLeft,
                                                        Text = LazerLensStrings.HeaderSettingsTooltip,
                                                        Font = OsuFont.Torus.With(size: 15, weight: FontWeight.Bold),
                                                        Colour = Color4.White,
                                                    }
                                                }
                                            },
                                            new OsuClickableContainer
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Size = new Vector2(24),
                                                Action = Hide,
                                                Child = new SpriteIcon
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Size = new Vector2(12),
                                                    Icon = FontAwesome.Solid.Times,
                                                    Colour = colourProvider.Content2,
                                                }
                                            }
                                        }
                                    },
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 1,
                                        Colour = colourProvider.Background4,
                                    },
                                    // Body
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Padding = new MarginPadding(16),
                                        Child = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 6),
                                            Children = new Drawable[]
                                            {
                                                // Section 1: Gameplay & Tracking
                                                new OsuSpriteText
                                                {
                                                    Text = LazerLensStrings.SettingsSectionGameplay,
                                                    Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                    Colour = colourProvider.Colour1,
                                                    Margin = new MarginPadding { Top = 2, Bottom = 2 },
                                                },
                                                new SettingsCheckbox
                                                {
                                                    LabelText = LazerLensStrings.SettingsNotificationsCaption,
                                                    TooltipText = LazerLensStrings.SettingsNotificationsSubtitle,
                                                    Current = service.NotifyOnPlay,
                                                },
                                                new SettingsCheckbox
                                                {
                                                    LabelText = LazerLensStrings.SettingsTrackRetriesCaption,
                                                    TooltipText = LazerLensStrings.SettingsTrackRetriesSubtitle,
                                                    Current = service.TrackRetries,
                                                },

                                                // Section 2: Interface & Visuals
                                                new OsuSpriteText
                                                {
                                                    Text = LazerLensStrings.SettingsSectionVisuals,
                                                    Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                    Colour = colourProvider.Colour1,
                                                    Margin = new MarginPadding { Top = 8, Bottom = 2 },
                                                },
                                                new SettingsCheckbox
                                                {
                                                    LabelText = LazerLensStrings.SettingsCompactHistoryCaption,
                                                    TooltipText = LazerLensStrings.SettingsCompactHistorySubtitle,
                                                    Current = service.CompactMode,
                                                },
                                                new SettingsCheckbox
                                                {
                                                    LabelText = LazerLensStrings.SettingsShowURCaption,
                                                    TooltipText = LazerLensStrings.SettingsShowURSubtitle,
                                                    Current = service.ShowUR,
                                                },

                                                // Section 3: Data & Storage
                                                new OsuSpriteText
                                                {
                                                    Text = LazerLensStrings.SettingsSectionData,
                                                    Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                    Colour = colourProvider.Colour1,
                                                    Margin = new MarginPadding { Top = 8, Bottom = 2 },
                                                },
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 36,
                                                    Margin = new MarginPadding { Top = 4 },
                                                    Child = new GridContainer
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        ColumnDimensions = new[]
                                                        {
                                                            new Dimension(GridSizeMode.Relative, 0.5f),
                                                            new Dimension(GridSizeMode.Relative, 0.5f),
                                                        },
                                                        Content = new[]
                                                        {
                                                            new Drawable[]
                                                            {
                                                                new Container
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Padding = new MarginPadding { Right = 4 },
                                                                    Child = new SettingsActionButton(LazerLensStrings.SettingsOpenDirectory, FontAwesome.Solid.FolderOpen, () => service.OpenSessionsDirectory())
                                                                },
                                                                new Container
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Padding = new MarginPadding { Left = 4 },
                                                                    Child = new SettingsActionButton(LazerLensStrings.SettingsExportCsv, FontAwesome.Solid.FileExport, () =>
                                                                    {
                                                                        LazerLensPlugin.Instance?.ExportSessionsToCsv();
                                                                    })
                                                                }
                                                            }
                                                        }
                                                    }
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

            public new void Show()
            {
                if (IsDisposed) return;
                IsSettingsModalOpen = true;
                this.FadeIn(200, Easing.OutQuint);
                modalCard.ScaleTo(0.95f).ScaleTo(1f, 250, Easing.OutQuint);
            }

            public new void Hide()
            {
                if (IsDisposed) return;
                IsSettingsModalOpen = false;
                this.FadeOut(150, Easing.InQuint);
                modalCard.ScaleTo(0.95f, 150, Easing.InQuint);
            }

            public void Toggle()
            {
                if (Alpha > 0.5f)
                    Hide();
                else
                    Show();
            }
        }

        private sealed partial class SettingsActionButton : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly LocalisableString label;
            private readonly IconUsage icon;
            private Box background = null!;

            public SettingsActionButton(LocalisableString label, IconUsage icon, Action action)
            {
                this.label = label;
                this.icon = icon;
                this.Action = action;

                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
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
                            Colour = colourProvider.Background4,
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Children = new Drawable[]
                            {
                                new SpriteIcon
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(14),
                                    Icon = icon,
                                    Colour = colourProvider.Colour1,
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = label,
                                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                    Colour = Color4.White,
                                }
                            }
                        }
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeColour(colourProvider.Background3, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeColour(colourProvider.Background4, 100);
                base.OnHoverLost(e);
            }
        }
    }
}
