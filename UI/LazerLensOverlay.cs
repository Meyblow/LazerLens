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
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Cursor;
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

        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.TabArchive))]
        Archive,

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
        Catch,
        Custom
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

    public enum SessionArchiveSortMode
    {
        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.SortSessionDate))]
        Date,

        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.SortSessionPP))]
        TopPP,

        [LocalisableDescription(typeof(LazerLensStrings), nameof(LazerLensStrings.SortSessionPlays))]
        PlayCount,
    }

    public partial class LazerLensOverlay : OsuCcWaveOverlay
    {
        private float ItemHeight => service.CompactMode.Value ? 40f : 58f;
        private static float ItemSpacing => 6f;
        private float SlotHeight => ItemHeight + ItemSpacing;

        private readonly LazerLensService service;
        private readonly Action? exportCsvAction;

        private readonly Bindable<LazerLensSection> currentSection = new(LazerLensSection.Session);
        private TabControlOverlayHeader<LazerLensSection>.OverlayHeaderTabControl tabControl = null!;

        [Resolved(canBeNull: true)]
        private BeatmapSetOverlay? beatmapSetOverlay { get; set; }

        // Live Tab Components
        private FillFlowContainer liveContent = null!;
        private MetricCard liveTimeCard = null!;
        private MetricCard livePlaysCard = null!;
        private MetricCard liveAccCard = null!;
        private MetricCard liveComboCard = null!;
        private BestScoreBanner liveBestScoreBanner = null!;
        private Container liveHistoryContainer = null!;
        private readonly Dictionary<Guid, SessionPlayHistoryItem> liveItemMap = new();
        private OsuSpriteText liveNoHistoryText = null!;
        private OsuSpriteText liveHistoryCountText = null!;
        private LazerLensFilterControl liveFilterControl = null!;

        // Archive Tab Components
        private Container archiveContent = null!;
        private OsuContextMenuContainer? archiveContextMenuContainer;
        private FillFlowContainer archiveCardsList = null!;
        private readonly List<ArchiveSessionCard> archiveCards = new();
        private Guid? selectedArchiveSessionId;
        private SessionState? currentArchivedState;
        private OsuSpriteText archiveListHeader = null!;
        private OsuEnumDropdown<SessionArchiveSortMode> archiveSortDropdown = null!;
        private Container archiveEmptyContainer = null!;
        private FillFlowContainer archiveDetailContent = null!;
        private OsuScrollContainer archiveDetailScroll = null!;
        private MetricCard archiveTimeCard = null!;
        private MetricCard archivePlaysCard = null!;
        private MetricCard archiveAccCard = null!;
        private MetricCard archiveComboCard = null!;
        private BestScoreBanner archiveBestScoreBanner = null!;
        private Container archiveHistoryContainer = null!;
        private readonly Dictionary<Guid, SessionPlayHistoryItem> archiveItemMap = new();
        private OsuSpriteText archiveNoHistoryText = null!;
        private OsuSpriteText archiveHistoryCountText = null!;
        private LazerLensFilterControl archiveFilterControl = null!;

        // Settings Tab Components
        private FillFlowContainer settingsContent = null!;

        // Modal Dialog Layer
        private Container dialogContainer = null!;

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
            Header.DescriptionText = string.Empty;
            Header.HeaderIcon = FontAwesome.Solid.ChartBar;

            tabControl = new TabControlOverlayHeader<LazerLensSection>.OverlayHeaderTabControl
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };
            tabControl.Current.BindTo(currentSection);

            Header.ContentRow.Add(tabControl);

            MainAreaContent.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    // TAB 1: Live Session Content
                    liveContent = buildLiveContent(),

                    // TAB 2: Archive Sessions Content
                    archiveContent = buildArchiveContent(),

                    // TAB 3: Settings Content
                    settingsContent = buildSettingsContent(),
                }
            });

            AddInternal(dialogContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Depth = -10000,
            });

            currentSection.BindValueChanged(e =>
            {
                bool isSession = e.NewValue == LazerLensSection.Session;
                bool isArchive = e.NewValue == LazerLensSection.Archive;
                bool isSettings = e.NewValue == LazerLensSection.Settings;

                liveContent.BypassAutoSizeAxes = isSession ? Axes.None : Axes.Both;
                archiveContent.BypassAutoSizeAxes = isArchive ? Axes.None : Axes.Both;
                settingsContent.BypassAutoSizeAxes = isSettings ? Axes.None : Axes.Both;

                if (isSession)
                {
                    archiveContent.Hide();
                    settingsContent.Hide();
                    liveContent.FadeIn(180, Easing.OutQuint);
                }
                else if (isArchive)
                {
                    liveContent.Hide();
                    settingsContent.Hide();
                    archiveContent.FadeIn(180, Easing.OutQuint);
                    refreshArchiveList();
                }
                else if (isSettings)
                {
                    liveContent.Hide();
                    archiveContent.Hide();
                    settingsContent.FadeIn(180, Easing.OutQuint);
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

            service.SearchPosition.BindValueChanged(e =>
            {
                if (IsDisposed) return;
                var anchor = e.NewValue == SearchBarPosition.Centre ? Anchor.Centre : Anchor.CentreRight;
                var origin = e.NewValue == SearchBarPosition.Centre ? Anchor.Centre : Anchor.CentreRight;

                if (liveFilterControl?.SearchTextBox != null)
                {
                    liveFilterControl.SearchTextBox.Anchor = anchor;
                    liveFilterControl.SearchTextBox.Origin = origin;
                }

                if (archiveFilterControl?.SearchTextBox != null)
                {
                    archiveFilterControl.SearchTextBox.Anchor = anchor;
                    archiveFilterControl.SearchTextBox.Origin = origin;
                }
            }, true);

            archiveSortDropdown.Current.BindValueChanged(_ =>
            {
                if (IsDisposed) return;
                reorderArchiveCards();
            });

            bindFilter(liveFilterControl);
            bindFilter(archiveFilterControl);
        }

        private void bindFilter(LazerLensFilterControl filter)
        {
            filter.SearchChanged += _ => { if (!IsDisposed) RefreshData(); };
            filter.RulesetsChanged += _ => { if (!IsDisposed) RefreshData(); };
            filter.OutcomesChanged += _ => { if (!IsDisposed) RefreshData(); };
            filter.StatusesChanged += _ => { if (!IsDisposed) RefreshData(); };
            filter.SortChanged += _ => { if (!IsDisposed) RefreshData(); };
            filter.SortDirectionToggled += _ => { if (!IsDisposed) RefreshData(); };
        }

        private FillFlowContainer buildLiveContent()
        {
            liveFilterControl = new LazerLensFilterControl();

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 14),
                Padding = new MarginPadding { Top = 12 },
                Children = new Drawable[]
                {
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
                                new Dimension(GridSizeMode.Distributed),
                                new Dimension(GridSizeMode.Absolute, 12),
                                new Dimension(GridSizeMode.Distributed),
                                new Dimension(GridSizeMode.Absolute, 12),
                                new Dimension(GridSizeMode.Distributed),
                                new Dimension(GridSizeMode.Absolute, 12),
                                new Dimension(GridSizeMode.Distributed),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    liveTimeCard = new MetricCard(FontAwesome.Solid.Clock, LazerLensStrings.OverlaySessionTime, "00:00:00", LazerLensStrings.TimeStartedJustNow),
                                    Empty(),
                                    livePlaysCard = new MetricCard(FontAwesome.Solid.Play, LazerLensStrings.OverlayTotalPlays, "0", LazerLensStrings.PlaysPassFail(0, 0)),
                                    Empty(),
                                    liveAccCard = new MetricCard(FontAwesome.Solid.Percent, LazerLensStrings.OverlayAvgAccuracy, "0.00%", LazerLensStrings.PlaysRecorded(0)),
                                    Empty(),
                                    liveComboCard = new MetricCard(FontAwesome.Solid.Fire, LazerLensStrings.OverlayMaxCombo, "0x", LazerLensStrings.OverlaySessionPPGain("+0.0 pp")),
                                }
                            }
                        }
                    },

                    // 2. Best Score Banner (Clickable)
                    liveBestScoreBanner = new BestScoreBanner(() => openBeatmap(service.LiveState.BestScore)),

                    // 3. Play History Header Row (Title on Left, Search on Right)
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 34,
                        Children = new Drawable[]
                        {
                            liveHistoryCountText = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = LazerLensStrings.HistoryTitle(0),
                                Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                Colour = ColourProvider.Colour1,
                            },
                            liveFilterControl.SearchTextBox = new SearchTextBox
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Width = 420,
                                Height = 34,
                                PlaceholderText = LazerLensStrings.SearchPlaceholder,
                            }
                        }
                    },

                    // 4. Filter Control
                    liveFilterControl,

                    // 5. Play History Items Container
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            liveNoHistoryText = new OsuSpriteText
                            {
                                Text = LazerLensStrings.HistoryEmpty,
                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                Colour = Color4.White.Opacity(0.5f),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Vertical = 24 },
                                Alpha = 0,
                            },
                            liveHistoryContainer = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                            }
                        }
                    }
                }
            };
        }

        private Container buildArchiveContent()
        {
            archiveFilterControl = new LazerLensFilterControl();

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 650,
                Alpha = 0,
                BypassAutoSizeAxes = Axes.Both,
                Padding = new MarginPadding { Top = 12 },
                Child = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 340),
                        new Dimension(GridSizeMode.Distributed),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            // Left Column: Sessions List (Solid panel with integrated header and sorting)
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Right = 14 },
                                Child = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Masking = true,
                                    CornerRadius = 8,
                                    BorderThickness = 1,
                                    BorderColour = ColourProvider.Background1,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ColourProvider.Background5,
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(10),
                                            Child = new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                RowDimensions = new[]
                                                {
                                                    new Dimension(GridSizeMode.Absolute, 32),
                                                    new Dimension(GridSizeMode.Distributed),
                                                },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            Depth = -10, // Renders dropdown menu over the scroll list
                                                            Children = new Drawable[]
                                                            {
                                                                archiveListHeader = new OsuSpriteText
                                                                {
                                                                    Text = LazerLensStrings.ArchiveSavedSessions(0),
                                                                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                                    Colour = ColourProvider.Colour1,
                                                                    Anchor = Anchor.CentreLeft,
                                                                    Origin = Anchor.CentreLeft,
                                                                },
                                                                archiveSortDropdown = new OsuEnumDropdown<SessionArchiveSortMode>
                                                                {
                                                                    Anchor = Anchor.CentreRight,
                                                                    Origin = Anchor.CentreRight,
                                                                    Width = 110,
                                                                }
                                                            }
                                                        }
                                                    },
                                                    new Drawable[]
                                                    {
                                                        (archiveContextMenuContainer = new OsuContextMenuContainer
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            Margin = new MarginPadding { Top = 6 },
                                                            Child = new OsuScrollContainer
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Padding = new MarginPadding { Right = 18 },
                                                                ScrollbarVisible = true,
                                                                Child = archiveCardsList = new FillFlowContainer
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    AutoSizeAxes = Axes.Y,
                                                                    Direction = FillDirection.Vertical,
                                                                    Spacing = new Vector2(0, 6),
                                                                }
                                                            }
                                                        })
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },

                            // Right Column: Selected Session Details (Parallel independent scroll pane)
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    archiveEmptyContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 260,
                                        Masking = true,
                                        CornerRadius = 8,
                                        BorderThickness = 1,
                                        BorderColour = ColourProvider.Background1,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = ColourProvider.Background4,
                                            },
                                            new FillFlowContainer
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(0, 8),
                                                Children = new Drawable[]
                                                {
                                                    new SpriteIcon
                                                    {
                                                        Anchor = Anchor.Centre,
                                                        Origin = Anchor.Centre,
                                                        Size = new Vector2(36),
                                                        Icon = FontAwesome.Solid.Archive,
                                                        Colour = ColourProvider.Content2,
                                                    },
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.Centre,
                                                        Origin = Anchor.Centre,
                                                        Text = LazerLensStrings.ArchiveEmptyTitle,
                                                        Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                                                        Colour = ColourProvider.Content1,
                                                    },
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.Centre,
                                                        Origin = Anchor.Centre,
                                                        Text = LazerLensStrings.ArchiveEmptySubtitle,
                                                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                                        Colour = ColourProvider.Content2,
                                                    }
                                                }
                                            }
                                        }
                                    },

                                    archiveDetailScroll = new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Right = 24 },
                                        ScrollbarVisible = true,
                                        Child = archiveDetailContent = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 14),
                                            Alpha = 0,
                                            Children = new Drawable[]
                                            {
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
                                                            new Dimension(GridSizeMode.Distributed),
                                                            new Dimension(GridSizeMode.Absolute, 12),
                                                            new Dimension(GridSizeMode.Distributed),
                                                            new Dimension(GridSizeMode.Absolute, 12),
                                                            new Dimension(GridSizeMode.Distributed),
                                                            new Dimension(GridSizeMode.Absolute, 12),
                                                            new Dimension(GridSizeMode.Distributed),
                                                        },
                                                        Content = new[]
                                                        {
                                                            new Drawable[]
                                                            {
                                                                archiveTimeCard = new MetricCard(FontAwesome.Solid.Clock, LazerLensStrings.OverlaySessionTime, "00:00:00", ""),
                                                                Empty(),
                                                                archivePlaysCard = new MetricCard(FontAwesome.Solid.Play, LazerLensStrings.OverlayTotalPlays, "0", LazerLensStrings.PlaysPassFail(0, 0)),
                                                                Empty(),
                                                                archiveAccCard = new MetricCard(FontAwesome.Solid.Percent, LazerLensStrings.OverlayAvgAccuracy, "0.00%", LazerLensStrings.PlaysRecorded(0)),
                                                                Empty(),
                                                                archiveComboCard = new MetricCard(FontAwesome.Solid.Fire, LazerLensStrings.OverlayMaxCombo, "0x", LazerLensStrings.OverlaySessionPPGain("+0.0 pp")),
                                                            }
                                                        }
                                                    }
                                                },

                                                // 2. Best Score Banner
                                                archiveBestScoreBanner = new BestScoreBanner(() => openBeatmap(currentArchivedState?.BestScore)),

                                                // 3. Play History Header Row (Title on Left, Search on Right)
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 34,
                                                    Children = new Drawable[]
                                                    {
                                                        archiveHistoryCountText = new OsuSpriteText
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            Text = LazerLensStrings.HistoryTitle(0),
                                                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                                                            Colour = ColourProvider.Colour1,
                                                        },
                                                        archiveFilterControl.SearchTextBox = new SearchTextBox
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight,
                                                            Width = 420,
                                                            Height = 34,
                                                            PlaceholderText = LazerLensStrings.SearchPlaceholder,
                                                        }
                                                    }
                                                },

                                                // 4. Filter Control
                                                archiveFilterControl,

                                                // 5. Play History Items Container
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        archiveNoHistoryText = new OsuSpriteText
                                                        {
                                                            Text = LazerLensStrings.HistoryEmpty,
                                                            Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                                            Colour = Color4.White.Opacity(0.5f),
                                                            Anchor = Anchor.TopCentre,
                                                            Origin = Anchor.TopCentre,
                                                            Margin = new MarginPadding { Vertical = 24 },
                                                            Alpha = 0,
                                                        },
                                                        archiveHistoryContainer = new Container
                                                        {
                                                            RelativeSizeAxes = Axes.X,
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

        private FillFlowContainer buildSettingsContent()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 16),
                Padding = new MarginPadding { Top = 16, Horizontal = 40 },
                Alpha = 0,
                BypassAutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // 1. Metrics & Display
                    createSettingsSection(LazerLensStrings.SettingsSectionGameplay, new Drawable[]
                    {
                        new SettingsEnumDropdown<DefaultSortMode>
                        {
                            LabelText = LazerLensStrings.SettingsDefaultSortCaption,
                            Current = service.DefaultSort,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsEnumDropdown<PpDisplayMode>
                        {
                            LabelText = LazerLensStrings.SettingsPpDisplayCaption,
                            Current = service.PpDisplay,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsEnumDropdown<AccuracyCalculationMode>
                        {
                            LabelText = LazerLensStrings.SettingsAccCalcCaption,
                            Current = service.AccuracyCalculation,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsHighlightURCaption,
                            Current = service.HighlightUR,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "ur", "unstable rate", "color" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsShowModsCaption,
                            Current = service.ShowModsInHistory,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "mods", "badges", "history" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsShowDiffCaption,
                            Current = service.ShowDifficultyRating,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "stars", "difficulty", "rating" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsCompactHistoryCaption,
                            Current = service.CompactMode,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "compact", "history", "ui" },
                        },
                    }),

                    // 2. Session Management
                    createSettingsSection(LazerLensStrings.SettingsSectionData, new Drawable[]
                    {
                        new SettingsEnumDropdown<SessionSplitThreshold>
                        {
                            LabelText = LazerLensStrings.SettingsSessionSplitCaption,
                            Current = service.SessionSplit,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsEnumDropdown<AfkPauseTimeout>
                        {
                            LabelText = LazerLensStrings.SettingsAfkPauseCaption,
                            Current = service.AfkPause,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsEnablePauseCaption,
                            Current = service.EnableSessionPause,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "pause", "resume", "session" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsAutoExportCsvCaption,
                            Current = service.AutoExportCsv,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "export", "csv", "backup" },
                        },
                        new SettingsEnumDropdown<ArchiveRetentionLimit>
                        {
                            LabelText = LazerLensStrings.SettingsRetentionLimitCaption,
                            Current = service.ArchiveRetention,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsActionButton(FontAwesome.Solid.SyncAlt, LazerLensStrings.SettingsResetSession, handleResetLiveSession),
                        new SettingsActionButton(FontAwesome.Solid.FolderOpen, LazerLensStrings.SettingsOpenDirectory, () => service.OpenSessionsDirectory()),
                        new SettingsActionButton(FontAwesome.Solid.FileCsv, LazerLensStrings.SettingsExportCsv, () => exportCsvAction?.Invoke()),
                    }),

                    // 3. Recording Filters
                    createSettingsSection(LazerLensStrings.SettingsSectionFilters, new Drawable[]
                    {
                        new SettingsCheckbox
                        {
                            LabelText = "Track osu! (Standard) plays",
                            Current = service.TrackStandard,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = "Track osu!taiko plays",
                            Current = service.TrackTaiko,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = "Track osu!catch plays",
                            Current = service.TrackCatch,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = "Track osu!mania plays",
                            Current = service.TrackMania,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsTrackCustomCaption,
                            Current = service.TrackCustomRulesets,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "custom", "rulesets", "sentakki", "tau" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsTrackRetriesCaption,
                            Current = service.TrackRetries,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "retries", "retry", "fail", "pass" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsIgnoreNoFailCaption,
                            Current = service.IgnoreNoFailPlays,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "nofail", "nf", "practice" },
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsRankedLovedOnlyCaption,
                            Current = service.RankedLovedOnly,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "ranked", "loved", "graveyard", "unranked" },
                        },
                    }),

                    // 4. Notifications & Milestones
                    createSettingsSection(LazerLensStrings.SettingsSectionNotifications, new Drawable[]
                    {
                        new SettingsEnumDropdown<PlayNotificationFilter>
                        {
                            LabelText = LazerLensStrings.SettingsNotifFilterCaption,
                            Current = service.PlayNotifFilter,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsNotifySessionBestCaption,
                            Current = service.NotifySessionBest,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "best", "record", "celebrate" },
                        },
                        new SettingsEnumDropdown<MilestoneNotificationMode>
                        {
                            LabelText = LazerLensStrings.SettingsMilestonesCaption,
                            Current = service.Milestones,
                            ShowsDefaultIndicator = false,
                        },
                    }),

                    // 5. Overlay & Toolbar
                    createSettingsSection(LazerLensStrings.SettingsSectionOverlay, new Drawable[]
                    {
                        new SettingsCheckbox
                        {
                            LabelText = LazerLensStrings.SettingsAutoOpenOverlayCaption,
                            Current = service.AutoOpenOverlayOnPass,
                            ShowsDefaultIndicator = false,
                            Keywords = new[] { "auto", "open", "pass", "overlay" },
                        },
                        new SettingsEnumDropdown<ToolbarBadgeMode>
                        {
                            LabelText = LazerLensStrings.SettingsToolbarBadgeCaption,
                            Current = service.ToolbarBadge,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsEnumDropdown<SearchBarPosition>
                        {
                            LabelText = LazerLensStrings.SettingsSearchBarPositionCaption,
                            Current = service.SearchPosition,
                            ShowsDefaultIndicator = false,
                        },
                        new SettingsTextBox
                        {
                            LabelText = LazerLensStrings.SettingsToolbarBadgeColorCaption,
                            Current = service.ToolbarBadgeColor,
                            ShowsDefaultIndicator = false,
                        },
                    }),
                }
            };
        }

        private void handleResetLiveSession()
        {
            var dialog = new OsuCcConfirmDialog(
                LazerLensStrings.DialogResetSessionTitle,
                LazerLensStrings.DialogResetSessionBody,
                () =>
                {
                    service.ResetLiveSession();
                    currentSection.Value = LazerLensSection.Session;
                    RefreshData();
                }
            );

            ClientDialogs.Push(dialog);
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

        private void reorderArchiveCards()
        {
            if (IsDisposed || archiveCards.Count == 0) return;

            var sortMode = archiveSortDropdown?.Current.Value ?? SessionArchiveSortMode.Date;

            IOrderedEnumerable<ArchiveSessionCard> sorted;

            switch (sortMode)
            {
                case SessionArchiveSortMode.TopPP:
                    sorted = archiveCards.OrderByDescending(c => c.Summary.IsPinned)
                                         .ThenByDescending(c => c.Summary.TopPP)
                                         .ThenByDescending(c => c.Summary.StartTime);
                    break;

                case SessionArchiveSortMode.PlayCount:
                    sorted = archiveCards.OrderByDescending(c => c.Summary.IsPinned)
                                         .ThenByDescending(c => c.Summary.PlayCount)
                                         .ThenByDescending(c => c.Summary.StartTime);
                    break;

                case SessionArchiveSortMode.Date:
                default:
                    sorted = archiveCards.OrderByDescending(c => c.Summary.IsPinned)
                                         .ThenByDescending(c => c.Summary.StartTime);
                    break;
            }

            var sortedList = sorted.ToList();

            archiveCardsList.Clear(false);
            foreach (var card in sortedList)
            {
                archiveCardsList.Add(card);
            }
        }

        private void refreshArchiveList()
        {
            if (IsDisposed) return;

            var summaries = service.GetAllSessionSummaries();
            archiveListHeader.Text = LazerLensStrings.ArchiveSavedSessions(summaries.Count);

            archiveCardsList.Clear();
            archiveCards.Clear();

            if (summaries.Count == 0)
            {
                archiveEmptyContainer.FadeIn(200);
                archiveDetailContent.FadeOut(200);
                selectedArchiveSessionId = null;
                currentArchivedState = null;
                return;
            }

            archiveEmptyContainer.FadeOut(150);
            archiveDetailContent.FadeIn(200);

            if (!selectedArchiveSessionId.HasValue || !summaries.Any(s => s.Id == selectedArchiveSessionId.Value))
            {
                selectedArchiveSessionId = summaries.First().Id;
            }

            foreach (var summary in summaries)
            {
                bool isSelected = summary.Id == selectedArchiveSessionId;
                var id = summary.Id;

                var card = new ArchiveSessionCard(
                    summary,
                    isSelected,
                    action: () => selectArchivedSession(id),
                    onOpenFolder: handleOpenFolder,
                    onTogglePin: handleTogglePin,
                    onSetNote: handleSetNote,
                    onDelete: handleDeleteSession
                );

                archiveCards.Add(card);
            }

            reorderArchiveCards();
            loadArchivedSessionDetail(selectedArchiveSessionId.Value);
        }

        private void handleOpenFolder(Guid id)
        {
            service.StorageService?.OpenSessionFile(id);
        }

        private void handleTogglePin(Guid id, bool pinned)
        {
            service.StorageService?.SetSessionPinned(id, pinned);

            var card = archiveCards.FirstOrDefault(c => c.Summary.Id == id);
            if (card != null)
            {
                card.UpdatePinned(pinned);
                reorderArchiveCards();
            }
        }

        private void handleSetNote(Guid id, string? currentNote)
        {
            dialogContainer.Clear();
            var dialog = new SessionNoteDialog(currentNote, newNote =>
            {
                service.StorageService?.SetSessionNote(id, newNote);

                var card = archiveCards.FirstOrDefault(c => c.Summary.Id == id);
                if (card != null)
                {
                    card.UpdateNote(newNote);
                }
            });

            dialogContainer.Add(dialog);
            dialog.Show();
        }

        private void handleDeleteSession(Guid id)
        {
            var dialog = new OsuCcConfirmDialog(
                LazerLensStrings.DialogDeleteConfirmTitle,
                LazerLensStrings.DialogDeleteConfirmBody,
                () =>
                {
                    service.StorageService?.DeleteSession(id);

                    var card = archiveCards.FirstOrDefault(c => c.Summary.Id == id);
                    if (card != null)
                    {
                        card.AnimateRemoval(() =>
                        {
                            if (IsDisposed) return;
                            archiveCards.Remove(card);
                            archiveCardsList.Remove(card, false);

                            archiveListHeader.Text = LazerLensStrings.ArchiveSavedSessions(archiveCards.Count);

                            if (archiveCards.Count == 0)
                            {
                                selectedArchiveSessionId = null;
                                currentArchivedState = null;
                                archiveEmptyContainer.FadeIn(200);
                                archiveDetailContent.FadeOut(200);
                            }
                            else if (selectedArchiveSessionId == id)
                            {
                                selectArchivedSession(archiveCards.First().Summary.Id);
                            }
                        });
                    }
                }
            );

            ClientDialogs.Push(dialog);
        }

        private void selectArchivedSession(Guid id)
        {
            if (selectedArchiveSessionId == id) return;

            selectedArchiveSessionId = id;

            foreach (var card in archiveCards)
            {
                card.SetSelected(card.Summary.Id == id);
            }

            loadArchivedSessionDetail(id);
        }

        private void loadArchivedSessionDetail(Guid id)
        {
            currentArchivedState = service.StorageService?.LoadSession(id);
            refreshArchiveDetail();
        }

        private void refreshArchiveDetail()
        {
            if (currentArchivedState == null) return;
            var state = currentArchivedState;

            // 1. Duration
            var archivedDuration = state.Plays.Count > 0
                ? state.Plays.Last().Timestamp - state.SessionStart
                : TimeSpan.Zero;
            string timeStr = $"{(int)archivedDuration.TotalHours:D2}:{archivedDuration.Minutes:D2}:{archivedDuration.Seconds:D2}";
            archiveTimeCard?.UpdateValues(timeStr, LazerLensStrings.TimeArchived(state.SessionStart.ToLocalTime().ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)));

            // 2. Plays
            archivePlaysCard?.UpdateValues(state.TotalPlays.ToString(CultureInfo.InvariantCulture), LazerLensStrings.PlaysPassFail(state.TotalPasses, state.TotalFails));

            // 3. Acc & UR
            var urPlays = state.Plays.Where(p => p.UnstableRate.HasValue && p.UnstableRate.Value > 0).ToList();
            string urAvgStr = urPlays.Count > 0 ? LazerLensStrings.AvgUr(urPlays.Average(p => p.UnstableRate!.Value).ToString("F1", CultureInfo.InvariantCulture)).ToString() : "";
            archiveAccCard?.UpdateValues($"{state.AverageAccuracy.ToString("F2", CultureInfo.InvariantCulture)}%", LazerLensStrings.AccPlaysUr(state.Plays.Count, urAvgStr));

            // 4. Max Combo & Top PP
            string ppGainStr = state.SessionPPGain >= 0 ? $"+{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp" : $"{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp";
            archiveComboCard?.UpdateValues($"{state.MaxCombo.ToString("N0", CultureInfo.InvariantCulture)}x", LazerLensStrings.OverlaySessionPPGain(ppGainStr));

            // 5. Best Score Banner
            archiveBestScoreBanner?.UpdateScore(state.BestScore);

            // 6. Play History
            renderHistoryItems(state.Plays, archiveFilterControl, archiveHistoryContainer, archiveHistoryCountText, archiveNoHistoryText, archiveItemMap);
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

        protected override void Update()
        {
            base.Update();

            if (State.Value == Visibility.Visible && liveTimeCard != null)
            {
                var duration = service.LiveState.SessionDuration;
                int totalSeconds = (int)duration.TotalSeconds;

                if (totalSeconds != lastUpdatedSecond)
                {
                    lastUpdatedSecond = totalSeconds;
                    string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                    liveTimeCard.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(service.LiveState.SessionStart.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));
                }
            }
        }

        protected override void PopIn()
        {
            base.PopIn();

            if (isDataDirty)
                RefreshData();

            if (currentSection.Value == LazerLensSection.Archive)
                refreshArchiveList();
        }

        public void RefreshData()
        {
            if (IsDisposed) return;
            isDataDirty = false;
            var state = service.LiveState;

            // 1. Session Duration
            var duration = state.SessionDuration;
            string timeStr = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            liveTimeCard?.UpdateValues(timeStr, LazerLensStrings.TimeStartedAt(state.SessionStart.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));

            // 2. Total Plays
            livePlaysCard?.UpdateValues(state.TotalPlays.ToString(CultureInfo.InvariantCulture), LazerLensStrings.PlaysPassFail(state.TotalPasses, state.TotalFails));

            // 3. Average Accuracy & UR
            var urPlays = state.Plays.Where(p => p.UnstableRate.HasValue && p.UnstableRate.Value > 0).ToList();
            string urAvgStr = urPlays.Count > 0 ? LazerLensStrings.AvgUr(urPlays.Average(p => p.UnstableRate!.Value).ToString("F1", CultureInfo.InvariantCulture)).ToString() : "";
            liveAccCard?.UpdateValues($"{state.AverageAccuracy.ToString("F2", CultureInfo.InvariantCulture)}%", LazerLensStrings.AccPlaysUr(state.Plays.Count, urAvgStr));

            // 4. Max Combo / Session PP Gain
            string ppGainStr = state.SessionPPGain >= 0 ? $"+{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp" : $"{state.SessionPPGain.ToString("F1", CultureInfo.InvariantCulture)} pp";
            liveComboCard?.UpdateValues($"{state.MaxCombo.ToString("N0", CultureInfo.InvariantCulture)}x", LazerLensStrings.OverlaySessionPPGain(ppGainStr));

            // 5. Best Score Banner
            liveBestScoreBanner?.UpdateScore(state.BestScore);

            // 6. Play History
            renderHistoryItems(state.Plays, liveFilterControl, liveHistoryContainer, liveHistoryCountText, liveNoHistoryText, liveItemMap);

            // 7. Refresh archive if viewing archive
            if (currentSection.Value == LazerLensSection.Archive && currentArchivedState != null)
            {
                refreshArchiveDetail();
            }
        }

        private void renderHistoryItems(
            List<SessionPlayRecord> plays,
            LazerLensFilterControl filter,
            Container container,
            OsuSpriteText countText,
            OsuSpriteText emptyText,
            Dictionary<Guid, SessionPlayHistoryItem> map)
        {
            if (filter == null || container == null) return;

            if (plays.Count == 0)
            {
                filter.FadeTo(0, 150);
                filter.BypassAutoSizeAxes = Axes.Both;
            }
            else
            {
                filter.FadeTo(1, 150);
                filter.BypassAutoSizeAxes = Axes.None;
            }

            var filteredPlays = plays.Where(p =>
                matchesRuleset(p, filter.SelectedRulesets) &&
                matchesOutcome(p, filter.SelectedOutcomes) &&
                matchesStatus(p, filter.SelectedStatuses) &&
                matchesSearch(p, filter.SearchTextBox?.Current.Value ?? "")
            );

            IEnumerable<SessionPlayRecord> sortedPlays = filter.CurrentSort switch
            {
                SessionSortMode.Score => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.TotalScore),

                SessionSortMode.Accuracy => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.Accuracy)
                    : filteredPlays.OrderByDescending(p => p.Accuracy),

                SessionSortMode.PP => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.PerformancePoints ?? 0).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.PerformancePoints ?? 0).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Combo => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.MaxCombo).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.MaxCombo).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Grade => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.Rank).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.Rank).ThenByDescending(p => p.TotalScore),

                SessionSortMode.Difficulty => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.StarRating).ThenBy(p => p.TotalScore)
                    : filteredPlays.OrderByDescending(p => p.StarRating).ThenByDescending(p => p.TotalScore),

                _ => filter.SortAscending
                    ? filteredPlays.OrderBy(p => p.Timestamp)
                    : filteredPlays.OrderByDescending(p => p.Timestamp)
            };

            var finalPlaysList = sortedPlays.ToList();

            if (countText != null)
                countText.Text = LazerLensStrings.HistoryTitle(finalPlaysList.Count);

            var currentVisibleIds = new HashSet<Guid>();
            float currentY = 0f;

            for (int i = 0; i < finalPlaysList.Count; i++)
            {
                var play = finalPlaysList[i];
                currentVisibleIds.Add(play.Id);

                if (!map.TryGetValue(play.Id, out var item))
                {
                    item = new SessionPlayHistoryItem(play, service);
                    map[play.Id] = item;
                    container.Add(item);
                }
                else
                {
                    item.UpdateData(play);
                }

                float targetY = currentY;
                item.MoveToY(targetY, 200, Easing.OutQuint);
                item.FadeIn(150);

                currentY += (service.CompactMode.Value ? 40 : 58) + 6;
            }

            container.Height = currentY;

            // Remove items no longer in filter
            foreach (var kvp in map.ToList())
            {
                if (!currentVisibleIds.Contains(kvp.Key))
                {
                    container.Remove(kvp.Value, true);
                    map.Remove(kvp.Key);
                }
            }

            if (emptyText != null)
                emptyText.Alpha = finalPlaysList.Count == 0 ? 1 : 0;
        }

        private static bool matchesRuleset(SessionPlayRecord play, HashSet<string> filters)
        {
            if (filters.Contains("all")) return true;

            string name = (play.RulesetName ?? "").ToLowerInvariant();
            bool isTaiko = name.Contains("taiko");
            bool isCatch = name.Contains("catch") || name.Contains("fruit");
            bool isMania = name.Contains("mania");
            bool isOsu = name.Equals("osu") || name.Contains("standard") || name.Equals("osu!");
            bool isCustom = !isTaiko && !isCatch && !isMania && !isOsu;

            foreach (var f in filters)
            {
                if (string.Equals(f, name, StringComparison.OrdinalIgnoreCase)) return true;
                if ((f == "fruits" || f == "catch") && isCatch) return true;
                if (f == "taiko" && isTaiko) return true;
                if (f == "mania" && isMania) return true;
                if (f == "osu" && isOsu) return true;
                if (f == "custom" && isCustom) return true;
                if (name.Contains(f, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static bool matchesOutcome(SessionPlayRecord play, HashSet<SessionOutcomeFilter> filters)
        {
            if (filters.Contains(SessionOutcomeFilter.All)) return true;
            if (filters.Contains(SessionOutcomeFilter.Pass) && play.Passed) return true;
            if (filters.Contains(SessionOutcomeFilter.Fail) && !play.Passed) return true;
            return false;
        }

        private static bool matchesStatus(SessionPlayRecord play, HashSet<SessionStatusFilter> filters)
        {
            if (filters.Contains(SessionStatusFilter.All)) return true;
            if (filters.Contains(SessionStatusFilter.Ranked) && play.Status is "Ranked" or "Approved") return true;
            if (filters.Contains(SessionStatusFilter.Loved) && play.Status == "Loved") return true;
            if (filters.Contains(SessionStatusFilter.Graveyard) && play.Status is "Graveyard" or "Pending" or "WIP" or "Unranked") return true;
            return false;
        }

        private static bool matchesSearch(SessionPlayRecord play, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            return (play.BeatmapTitle?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                   (play.BeatmapArtist?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                   (play.DifficultyName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void openBeatmap(SessionPlayRecord? score)
        {
            if (score == null) return;

            var overlay = beatmapSetOverlay;

            if (score.OnlineBeatmapID > 0)
                overlay?.FetchAndShowBeatmap(score.OnlineBeatmapID);
            else if (score.OnlineBeatmapSetID > 0)
                overlay?.FetchAndShowBeatmapSet(score.OnlineBeatmapSetID);
        }

        public void HighlightPlay(Guid id)
        {
            if (IsDisposed) return;

            Schedule(() =>
            {
                if (liveItemMap.TryGetValue(id, out var item))
                {
                    item.FlashHighlight();
                }
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            service.OnSessionUpdated -= onServiceSessionUpdated;
        }

        private sealed partial class SettingsActionButton : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly IconUsage icon;
            private readonly LocalisableString text;
            private Box background = null!;

            public SettingsActionButton(IconUsage icon, LocalisableString text, Action action)
            {
                this.icon = icon;
                this.text = text;
                Action = action;

                RelativeSizeAxes = Axes.X;
                Height = 38;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 6,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background3,
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10, 0),
                            Children = new Drawable[]
                            {
                                new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(14),
                                    Icon = icon,
                                    Colour = colourProvider.Highlight1,
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Text = text,
                                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.SemiBold),
                                    Colour = Color4.White,
                                }
                            }
                        }
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeColour(colourProvider.Background2, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeColour(colourProvider.Background3, 100);
                base.OnHoverLost(e);
            }
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
                    BorderThickness = 1,
                    BorderColour = Color4Extensions.FromHex("ffcc00").Opacity(0.35f),
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
    }
}
