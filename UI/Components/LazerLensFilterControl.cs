using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace LazerLens.UI.Components
{
    public sealed partial class LazerLensFilterControl : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public SearchTextBox SearchTextBox { get; private set; } = null!;

        public event Action<string>? SearchChanged;
        public event Action<SessionRulesetFilter>? RulesetChanged;
        public event Action<SessionOutcomeFilter>? OutcomeChanged;
        public event Action<SessionStatusFilter>? StatusChanged;
        public event Action<SessionSortMode>? SortChanged;
        public event Action<bool>? SortDirectionToggled;

        public SessionRulesetFilter CurrentRuleset { get; private set; } = SessionRulesetFilter.All;
        public SessionOutcomeFilter CurrentOutcome { get; private set; } = SessionOutcomeFilter.All;
        public SessionStatusFilter CurrentStatus { get; private set; } = SessionStatusFilter.All;
        public SessionSortMode CurrentSort { get; private set; } = SessionSortMode.Recent;
        public bool SortAscending { get; private set; }

        private readonly List<FilterTabButton<SessionRulesetFilter>> rulesetButtons = new();
        private readonly List<FilterTabButton<SessionOutcomeFilter>> outcomeButtons = new();
        private readonly List<FilterTabButton<SessionStatusFilter>> statusButtons = new();
        private readonly List<FilterTabButton<SessionSortMode>> sortButtons = new();

        private SortDirectionButton directionButton = null!;

        public LazerLensFilterControl()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6),
                        Padding = new MarginPadding(12),
                        Children = new Drawable[]
                        {
                            // 1. Search Box
                            SearchTextBox = new SearchTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 36,
                                PlaceholderText = LazerLensStrings.SearchPlaceholder,
                            },

                            // Subtle separator
                            new Box
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 1,
                                Colour = colourProvider.Background3.Opacity(0.6f),
                                Margin = new MarginPadding { Vertical = 3 },
                            },

                            // 2. Ruleset Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryRuleset,
                                createRulesetButtons()
                            ),

                            // 3. Outcome Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryOutcome,
                                createOutcomeButtons()
                            ),

                            // 4. Beatmap Status Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryStatus,
                                createStatusButtons()
                            ),

                            // 5. Sort By Row + Order Toggle
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    createFilterRow(
                                        LazerLensStrings.FilterCategorySort,
                                        createSortButtons()
                                    ),
                                    directionButton = new SortDirectionButton(SortAscending, toggleSortDirection)
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                    }
                                }
                            }
                        }
                    }
                }
            };

            SearchTextBox.Current.BindValueChanged(v =>
            {
                if (IsDisposed) return;
                SearchChanged?.Invoke(v.NewValue);
            });
        }

        private Container createFilterRow(LocalisableString categoryLabel, IEnumerable<Drawable> buttons)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = categoryLabel,
                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Bold),
                        Colour = colourProvider.Content2,
                        Width = 80,
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Padding = new MarginPadding { Left = 84 },
                        ChildrenEnumerable = buttons,
                    }
                }
            };
        }

        private IEnumerable<Drawable> createRulesetButtons()
        {
            var items = new (SessionRulesetFilter Filter, LocalisableString Label)[]
            {
                (SessionRulesetFilter.All, LazerLensStrings.FilterAll),
                (SessionRulesetFilter.Osu, "osu!"),
                (SessionRulesetFilter.Taiko, "osu!taiko"),
                (SessionRulesetFilter.Catch, "osu!catch"),
                (SessionRulesetFilter.Mania, "osu!mania"),
            };

            foreach (var (filter, label) in items)
            {
                var btn = new FilterTabButton<SessionRulesetFilter>(filter, label, filter == CurrentRuleset, f =>
                {
                    if (CurrentRuleset == f) return;
                    CurrentRuleset = f;
                    foreach (var b in rulesetButtons)
                        b.SetActive(b.Value == CurrentRuleset);
                    RulesetChanged?.Invoke(CurrentRuleset);
                });
                rulesetButtons.Add(btn);
                yield return btn;
            }
        }

        private IEnumerable<Drawable> createOutcomeButtons()
        {
            var items = new (SessionOutcomeFilter Filter, LocalisableString Label)[]
            {
                (SessionOutcomeFilter.All, LazerLensStrings.FilterAll),
                (SessionOutcomeFilter.Pass, LazerLensStrings.FilterPass),
                (SessionOutcomeFilter.Fail, LazerLensStrings.FilterFail),
            };

            foreach (var (filter, label) in items)
            {
                var btn = new FilterTabButton<SessionOutcomeFilter>(filter, label, filter == CurrentOutcome, f =>
                {
                    if (CurrentOutcome == f) return;
                    CurrentOutcome = f;
                    foreach (var b in outcomeButtons)
                        b.SetActive(b.Value == CurrentOutcome);
                    OutcomeChanged?.Invoke(CurrentOutcome);
                });
                outcomeButtons.Add(btn);
                yield return btn;
            }
        }

        private IEnumerable<Drawable> createStatusButtons()
        {
            var items = new (SessionStatusFilter Filter, LocalisableString Label)[]
            {
                (SessionStatusFilter.All, LazerLensStrings.FilterStatusAll),
                (SessionStatusFilter.Ranked, LazerLensStrings.FilterStatusRanked),
                (SessionStatusFilter.Loved, LazerLensStrings.FilterStatusLoved),
                (SessionStatusFilter.Graveyard, LazerLensStrings.FilterStatusGraveyard),
            };

            foreach (var (filter, label) in items)
            {
                var btn = new FilterTabButton<SessionStatusFilter>(filter, label, filter == CurrentStatus, f =>
                {
                    if (CurrentStatus == f) return;
                    CurrentStatus = f;
                    foreach (var b in statusButtons)
                        b.SetActive(b.Value == CurrentStatus);
                    StatusChanged?.Invoke(CurrentStatus);
                });
                statusButtons.Add(btn);
                yield return btn;
            }
        }

        private IEnumerable<Drawable> createSortButtons()
        {
            var items = new (SessionSortMode Mode, LocalisableString Label)[]
            {
                (SessionSortMode.Recent, LazerLensStrings.SortRecent),
                (SessionSortMode.Score, LazerLensStrings.SortScore),
                (SessionSortMode.Accuracy, LazerLensStrings.SortAccuracy),
                (SessionSortMode.PP, LazerLensStrings.SortPP),
                (SessionSortMode.Combo, LazerLensStrings.SortCombo),
                (SessionSortMode.Grade, LazerLensStrings.SortGrade),
                (SessionSortMode.Difficulty, LazerLensStrings.SortDifficulty),
            };

            foreach (var (mode, label) in items)
            {
                var btn = new FilterTabButton<SessionSortMode>(mode, label, mode == CurrentSort, m =>
                {
                    if (CurrentSort == m) return;
                    CurrentSort = m;
                    foreach (var b in sortButtons)
                        b.SetActive(b.Value == CurrentSort);
                    SortChanged?.Invoke(CurrentSort);
                });
                sortButtons.Add(btn);
                yield return btn;
            }
        }

        private void toggleSortDirection()
        {
            SortAscending = !SortAscending;
            directionButton.SetAscending(SortAscending);
            SortDirectionToggled?.Invoke(SortAscending);
        }

        private sealed partial class FilterTabButton<T> : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public T Value { get; }
            private readonly LocalisableString label;
            private readonly Action<T> onSelect;
            private bool isActive;

            private Box background = null!;
            private OsuSpriteText textSprite = null!;

            public FilterTabButton(T value, LocalisableString label, bool active, Action<T> onSelect)
            {
                Value = value;
                this.label = label;
                isActive = active;
                this.onSelect = onSelect;

                AutoSizeAxes = Axes.Both;
                Action = () => onSelect(Value);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new CircularContainer
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
                            Padding = new MarginPadding { Horizontal = 10, Vertical = 4 },
                            Child = textSprite = new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = label,
                                Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                            }
                        }
                    }
                };

                updateVisuals();
            }

            public void SetActive(bool active)
            {
                if (isActive == active) return;
                isActive = active;
                updateVisuals();
            }

            private void updateVisuals()
            {
                if (colourProvider == null || textSprite == null || background == null) return;

                if (isActive)
                {
                    background.Colour = colourProvider.Colour0;
                    textSprite.Colour = Color4.White;
                }
                else
                {
                    background.Colour = Color4.Transparent;
                    textSprite.Colour = colourProvider.Content1;
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!isActive && colourProvider != null)
                {
                    background.FadeColour(colourProvider.Background3, 100);
                    textSprite.FadeColour(Color4.White, 100);
                }
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (!isActive && colourProvider != null)
                {
                    background.FadeColour(Color4.Transparent, 100);
                    textSprite.FadeColour(colourProvider.Content1, 100);
                }
                base.OnHoverLost(e);
            }
        }

        private sealed partial class SortDirectionButton : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private bool isAscending;
            private readonly Action onToggle;

            private Box background = null!;
            private SpriteIcon icon = null!;
            private OsuSpriteText textSprite = null!;

            public SortDirectionButton(bool ascending, Action onToggle)
            {
                isAscending = ascending;
                this.onToggle = onToggle;

                AutoSizeAxes = Axes.Both;
                Action = onToggle;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Child = new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background3,
                        },
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 10, Vertical = 4 },
                            Child = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(5, 0),
                                Children = new Drawable[]
                                {
                                    icon = new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(10),
                                        Icon = isAscending ? FontAwesome.Solid.ArrowUp : FontAwesome.Solid.ArrowDown,
                                        Colour = colourProvider.Colour1,
                                    },
                                    textSprite = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = isAscending ? LazerLensStrings.FilterOrderAsc : LazerLensStrings.FilterOrderDesc,
                                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                        Colour = Color4.White,
                                    }
                                }
                            }
                        }
                    }
                };
            }

            public void SetAscending(bool ascending)
            {
                isAscending = ascending;
                if (icon != null && textSprite != null)
                {
                    icon.Icon = isAscending ? FontAwesome.Solid.ArrowUp : FontAwesome.Solid.ArrowDown;
                    textSprite.Text = isAscending ? LazerLensStrings.FilterOrderAsc : LazerLensStrings.FilterOrderDesc;
                }
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
    }
}
