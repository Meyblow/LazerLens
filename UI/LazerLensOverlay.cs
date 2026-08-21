using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
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
using osucc.Localisation;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;
using LazerLens.UI.Components;

namespace LazerLens.UI
{
    public enum LazerLensSection
    {
        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.TabSession))]
        Session,

        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.TabSettings))]
        Settings,
    }

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

    public partial class LazerLensOverlay : OsuCcWaveOverlay
    {
        private float ItemHeight => service.CompactMode.Value ? 40f : 58f;
        private static float ItemSpacing => 6f;
        private float SlotHeight => ItemHeight + ItemSpacing;

        private readonly LazerLensService service;
        private readonly Action? exportCsvAction;

        private readonly Bindable<LazerLensSection> currentSection = new(LazerLensSection.Session);
        private LazerLensTabControl tabControl = null!;

        [Resolved(canBeNull: true)]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

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

        private FillFlowContainer sessionContent = null!;
        private FillFlowContainer settingsContent = null!;
        private SessionSelectorDropdown sessionSelector = null!;
        private Container archiveBanner = null!;
        private OsuSpriteText archiveBannerText = null!;

        private bool isDataDirty = true;
        private int lastUpdatedSecond = -1;

        public LazerLensOverlay(LazerLensService service, Action? exportCsvAction = null)
            : base(OverlayColourScheme.Aquamarine)
        {
            this.service = service;
            this.exportCsvAction = exportCsvAction;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.TitleText = LazerLensStrings.Name;
            Header.DescriptionText = LazerLensStrings.Description;
            Header.HeaderIcon = FontAwesome.Solid.ChartBar;

            tabControl = new LazerLensTabControl
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
            };
            tabControl.Current.BindTo(currentSection);

            Header.ContentRow.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.Absolute, 220),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        tabControl,
                        Empty(),
                        sessionSelector = new SessionSelectorDropdown(
                            id => service.SelectSession(id),
                            () => service.GetAllSessionSummaries()
                        )
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                        }
                    }
                }
            });

            MainAreaContent.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    // Tab 1: Session Content
                    sessionContent = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                        Padding = new MarginPadding { Top = 10 },
                        Children = new Drawable[]
                        {
                            // Archive Banner
                            archiveBanner = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 36,
                                Masking = true,
                                CornerRadius = 6,
                                Alpha = 0,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4Extensions.FromHex("ffcc00").Opacity(0.2f),
                                    },
                                    archiveBannerText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.SemiBold),
                                        Colour = Color4Extensions.FromHex("ffcc00"),
                                    }
                                }
                            },

                            // 1. KPI Metric Cards Row
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 82,
                                Child = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
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
                                }
                            },

                            // 2. Best Score Banner (Clickable)
                            bestScoreBanner = new BestScoreBanner(openBestScoreBeatmap),

                            // 3. Play History Header
                            historyCountText = new OsuSpriteText
                            {
                                Text = LazerLensStrings.HistoryTitle(0),
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                Colour = ColourProvider.Colour1,
                                Margin = new MarginPadding { Top = 4 },
                            },

                            // 4. Filter Control
                            filterControl = new LazerLensFilterControl(),

                            // 5. Play History Items Container
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Margin = new MarginPadding { Top = 4 },
                                Children = new Drawable[]
                                {
                                    noHistoryText = new OsuSpriteText
                                    {
                                        Text = LazerLensStrings.HistoryEmpty,
                                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                        Colour = Color4.White.Opacity(0.5f),
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        Margin = new MarginPadding { Vertical = 24 },
                                        Alpha = 0,
                                    },
                                    historyContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                    }
                                }
                            }
                        }
                    },

                    // Tab 2: Settings Content
                    settingsContent = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 16),
                        Padding = new MarginPadding { Top = 16, Horizontal = 40 },
                        Alpha = 0,
                        Children = new Drawable[]
                        {
                            createSettingsSection(LazerLensStrings.SettingsSectionGameplay, new Drawable[]
                            {
                                new SettingsCheckbox
                                {
                                    LabelText = LazerLensStrings.SettingsNotificationsCaption,
                                    Current = service.NotifyOnPlay,
                                    Keywords = new[] { "notifications", "notify", "toast" },
                                },
                                new SettingsCheckbox
                                {
                                    LabelText = LazerLensStrings.SettingsTrackRetriesCaption,
                                    Current = service.TrackRetries,
                                    Keywords = new[] { "retries", "retry", "fail", "pass" },
                                },
                            }),
                            createSettingsSection(LazerLensStrings.SettingsSectionVisuals, new Drawable[]
                            {
                                new SettingsCheckbox
                                {
                                    LabelText = LazerLensStrings.SettingsCompactHistoryCaption,
                                    Current = service.CompactMode,
                                    Keywords = new[] { "compact", "history", "ui" },
                                },
                                new SettingsCheckbox
                                {
                                    LabelText = LazerLensStrings.SettingsShowURCaption,
                                    Current = service.ShowUR,
                                    Keywords = new[] { "ur", "unstable rate" },
                                },
                            }),
                            createSettingsSection(LazerLensStrings.SettingsSectionData, new Drawable[]
                            {
                                new SettingsButton
                                {
                                    Text = LazerLensStrings.SettingsOpenDirectory,
                                    Action = () => service.OpenSessionsDirectory(),
                                },
                                new SettingsButton
                                {
                                    Text = LazerLensStrings.SettingsExportCsv,
                                    Action = () => exportCsvAction?.Invoke(),
                                },
                            }),
                        }
                    }
                }
            });

            currentSection.BindValueChanged(e =>
            {
                if (e.NewValue == LazerLensSection.Session)
                {
                    sessionContent.FadeIn(200, Easing.OutQuint);
                    settingsContent.FadeOut(200, Easing.OutQuint);
                    sessionSelector.FadeIn(200, Easing.OutQuint);
                }
                else
                {
                    sessionContent.FadeOut(200, Easing.OutQuint);
                    settingsContent.FadeIn(200, Easing.OutQuint);
                    sessionSelector.FadeOut(200, Easing.OutQuint);
                }
            }, true);

            service.OnSessionUpdated += onServiceSessionUpdated;

            service.CompactMode.BindValueChanged(_ =>
            {
                if (IsDisposed) return;
                RefreshData();
            });

            service.ShowUR.BindValueChanged(_ =>
            {
                if (IsDisposed) return;
                RefreshData();
            });

            filterControl.SearchChanged += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };

            filterControl.RulesetChanged += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };

            filterControl.OutcomeChanged += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };

            filterControl.StatusChanged += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };

            filterControl.SortChanged += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };

            filterControl.SortDirectionToggled += _ =>
            {
                if (IsDisposed) return;
                RefreshData();
            };
        }

        private Container createSettingsSection(LocalisableString header, Drawable[] items)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 8,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 8),
                        Padding = new MarginPadding(16),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = header,
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                Colour = ColourProvider.Colour1,
                                Margin = new MarginPadding { Bottom = 4 },
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 6),
                                Children = items,
                            }
                        }
                    }
                }
            };
        }

        private void onServiceSessionUpdated()
        {
            if (IsDisposed) return;

            if (State.Value == Visibility.Visible)
            {
                Schedule(RefreshData);
            }
            else
            {
                isDataDirty = true;
            }
        }

        private static bool matchesRuleset(SessionPlayRecord play, SessionRulesetFilter filter)
        {
            return filter switch
            {
                SessionRulesetFilter.All => true,
                SessionRulesetFilter.Osu => play.RulesetName.Contains("osu", StringComparison.OrdinalIgnoreCase) && !play.RulesetName.Contains("taiko", StringComparison.OrdinalIgnoreCase) && !play.RulesetName.Contains("mania", StringComparison.OrdinalIgnoreCase) && !play.RulesetName.Contains("catch", StringComparison.OrdinalIgnoreCase) && !play.RulesetName.Contains("fruit", StringComparison.OrdinalIgnoreCase),
                SessionRulesetFilter.Mania => play.RulesetName.Contains("mania", StringComparison.OrdinalIgnoreCase),
                SessionRulesetFilter.Taiko => play.RulesetName.Contains("taiko", StringComparison.OrdinalIgnoreCase),
                SessionRulesetFilter.Catch => play.RulesetName.Contains("catch", StringComparison.OrdinalIgnoreCase) || play.RulesetName.Contains("fruit", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        private static bool matchesOutcome(SessionPlayRecord play, SessionOutcomeFilter filter)
        {
            return filter switch
            {
                SessionOutcomeFilter.All => true,
                SessionOutcomeFilter.Pass => play.Passed,
                SessionOutcomeFilter.Fail => !play.Passed,
                _ => true
            };
        }

        private static bool matchesStatus(SessionPlayRecord play, SessionStatusFilter filter)
        {
            return filter switch
            {
                SessionStatusFilter.All => true,
                SessionStatusFilter.Ranked => play.Status is "Ranked" or "Approved",
                SessionStatusFilter.Loved => play.Status == "Loved",
                SessionStatusFilter.Graveyard => play.Status is "Graveyard" or "Pending" or "WIP" or "Unranked",
                _ => true
            };
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
                    timeCard.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(service.LiveState.SessionStart.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));
                }
            }
        }

        protected override void PopIn()
        {
            base.PopIn();

            if (isDataDirty)
                RefreshData();
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
                    archiveBannerText.Text = LazerLensStrings.ArchiveBanner(state.SessionStart.ToLocalTime().ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture));
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
                timeCard?.UpdateValues(timeStr, LazerLensStrings.TimeArchived(state.SessionStart.ToLocalTime().ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)));
            }
            else
            {
                var duration = state.SessionDuration;
                string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                timeCard?.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(state.SessionStart.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));
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
                    ? filteredPlays.OrderBy(p => p.MaxCombo).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.MaxCombo).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Grade => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.Rank).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.Rank).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Difficulty => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.StarRating).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.StarRating).ThenByDescending(p => p.TotalScore),

                _ => filterControl.SortAscending
                    ? filteredPlays.OrderBy(p => p.Timestamp)
                    : filteredPlays.OrderByDescending(p => p.Timestamp)
            };

            var finalPlaysList = sortedPlays.ToList();

            // 8. Update count title
            if (historyCountText != null)
            {
                historyCountText.Text = LazerLensStrings.HistoryTitle(finalPlaysList.Count);
            }

            // 9. Update History Container
            if (historyContainer != null)
            {
                var currentVisibleIds = new HashSet<Guid>();
                float currentY = 0f;

                for (int i = 0; i < finalPlaysList.Count; i++)
                {
                    var play = finalPlaysList[i];
                    currentVisibleIds.Add(play.Id);

                    if (!itemMap.TryGetValue(play.Id, out var item))
                    {
                        item = new SessionPlayHistoryItem(play, service);
                        itemMap[play.Id] = item;
                        historyContainer.Add(item);
                    }
                    else
                    {
                        item.UpdateData(play);
                    }

                    float targetY = currentY;
                    item.MoveToY(targetY, 200, Easing.OutQuint);
                    item.FadeIn(150);

                    currentY += SlotHeight;
                }

                historyContainer.Height = currentY;

                // Fade out and remove items that are no longer visible
                var toRemove = new List<Guid>();
                foreach (var (id, item) in itemMap)
                {
                    if (!currentVisibleIds.Contains(id))
                    {
                        item.FadeOut(150).Expire();
                        toRemove.Add(id);
                    }
                }

                foreach (var id in toRemove)
                    itemMap.Remove(id);

                noHistoryText.Alpha = finalPlaysList.Count == 0 ? 1 : 0;
            }
        }

        private void openBestScoreBeatmap()
        {
            var best = service.LiveState.BestScore;
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
                    string starsStr = best.StarRating > 0 ? $"[{best.StarRating.ToString("F2", CultureInfo.InvariantCulture)}\u2605] " : "";
                    titleText.Text = $"{starsStr}{best.BeatmapArtist} - {best.BeatmapTitle} [{best.DifficultyName}]";

                    string ppStr = best.PerformancePoints.HasValue && best.PerformancePoints.Value > 0
                        ? $" \u2022 {best.PerformancePoints.Value.ToString("F0", CultureInfo.InvariantCulture)} PP"
                        : "";

                    subtitleText.Text = LazerLensStrings.BestScoreDetail(
                        best.Rank.ToString(),
                        best.Accuracy.ToString("F2", CultureInfo.InvariantCulture),
                        best.TotalScore.ToString("N0", CultureInfo.InvariantCulture),
                        best.MaxCombo,
                        ppStr,
                        best.Passed ? "PASS" : "FAIL"
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
        }

        private sealed partial class LazerLensTabControl : OverlayTabControl<LazerLensSection>
        {
            private const float bar_height = 2;

            public LazerLensTabControl()
            {
                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.X;
                Anchor = Anchor.BottomLeft;
                Origin = Anchor.BottomLeft;
                Height = 47;
                BarHeight = bar_height;
            }

            protected override TabItem<LazerLensSection> CreateTabItem(LazerLensSection value) => new LazerLensTabItem(value);

            protected override TabFillFlowContainer CreateTabFlow() => new TabFillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
            };

            private sealed partial class LazerLensTabItem : OverlayTabItem
            {
                public LazerLensTabItem(LazerLensSection value)
                    : base(value)
                {
                    Text.Text = value.GetLocalisableDescription().ToLower();
                    Text.Font = OsuFont.GetFont(size: 14);
                    Text.Margin = new MarginPadding { Vertical = 16.5f };
                    Bar.Margin = new MarginPadding { Bottom = bar_height };
                }
            }
        }
    }
}
