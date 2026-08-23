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

        [Resolved]
        private osu.Game.Rulesets.RulesetStore? rulesets { get; set; }

        public SearchTextBox SearchTextBox { get; set; } = new SearchTextBox
        {
            PlaceholderText = LazerLensStrings.SearchPlaceholder,
        };

        public event Action<string>? SearchChanged;
        public event Action<HashSet<string>>? RulesetsChanged;
        public event Action<HashSet<SessionOutcomeFilter>>? OutcomesChanged;
        public event Action<HashSet<SessionStatusFilter>>? StatusesChanged;
        public event Action<SessionSortMode>? SortChanged;
        public event Action<bool>? SortDirectionToggled;

        public HashSet<string> SelectedRulesets { get; } = new() { "all" };
        public HashSet<SessionOutcomeFilter> SelectedOutcomes { get; } = new() { SessionOutcomeFilter.All };
        public HashSet<SessionStatusFilter> SelectedStatuses { get; } = new() { SessionStatusFilter.All };
        public SessionSortMode CurrentSort { get; private set; } = SessionSortMode.Recent;
        public bool SortAscending { get; private set; }

        private readonly List<FilterTabButton<string>> rulesetButtons = new();
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
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            // 1. Ruleset Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryRuleset,
                                createRulesetButtons()
                            ),

                            // 2. Outcome Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryOutcome,
                                createOutcomeButtons()
                            ),

                            // 3. Beatmap Status Row
                            createFilterRow(
                                LazerLensStrings.FilterCategoryStatus,
                                createStatusButtons()
                            ),

                            // 4. Sort By Row + Order Toggle
                            createFilterRow(
                                LazerLensStrings.FilterCategorySort,
                                createSortButtons(),
                                directionButton = new SortDirectionButton(SortAscending, toggleSortDirection)
                            )
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

        private Container createFilterRow(LocalisableString categoryLabel, IEnumerable<Drawable> buttons, Drawable? rightSlot = null)
        {
            var container = new Container
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
                        Width = 72,
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Padding = new MarginPadding { Left = 76 },
                        ChildrenEnumerable = buttons,
                    }
                }
            };

            if (rightSlot != null)
            {
                rightSlot.Anchor = Anchor.CentreRight;
                rightSlot.Origin = Anchor.CentreRight;
                container.Add(rightSlot);
            }

            return container;
        }

        private IEnumerable<Drawable> createRulesetButtons()
        {
            var items = new List<(string Filter, LocalisableString Label, IconUsage? Icon, Drawable? CustomIcon)>();
            items.Add(("all", LazerLensStrings.FilterAll, null, null));

            if (rulesets != null)
            {
                foreach (var r in rulesets.AvailableRulesets)
                {
                    Drawable? rIcon = null;
                    try
                    {
                        rIcon = r.CreateInstance().CreateIcon();
                        if (rIcon != null) rIcon.Size = new Vector2(11);
                    }
                    catch { }

                    items.Add((r.ShortName, r.Name, null, rIcon));
                }
            }
            else
            {
                items.Add(("osu", "osu!", (IconUsage?)OsuIcon.RulesetOsu, null));
                items.Add(("taiko", "osu!taiko", (IconUsage?)OsuIcon.RulesetTaiko, null));
                items.Add(("fruits", "osu!catch", (IconUsage?)OsuIcon.RulesetCatch, null));
                items.Add(("mania", "osu!mania", (IconUsage?)OsuIcon.RulesetMania, null));
                items.Add(("custom", LazerLensStrings.FilterRulesetCustom, (IconUsage?)FontAwesome.Solid.Gamepad, null));
            }

            foreach (var (filter, label, icon, customIcon) in items)
            {
                var btn = new FilterTabButton<string>(filter, label, icon, SelectedRulesets.Contains(filter), f =>
                {
                    if (f == "all")
                    {
                        SelectedRulesets.Clear();
                        SelectedRulesets.Add("all");
                    }
                    else
                    {
                        SelectedRulesets.Remove("all");
                        if (!SelectedRulesets.Remove(f))
                            SelectedRulesets.Add(f);

                        if (SelectedRulesets.Count == 0)
                            SelectedRulesets.Add("all");
                    }

                    foreach (var b in rulesetButtons)
                        b.SetActive(SelectedRulesets.Contains(b.Value));

                    RulesetsChanged?.Invoke(SelectedRulesets);
                }, customIcon);
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
                var btn = new FilterTabButton<SessionOutcomeFilter>(filter, label, null, SelectedOutcomes.Contains(filter), f =>
                {
                    if (f == SessionOutcomeFilter.All)
                    {
                        SelectedOutcomes.Clear();
                        SelectedOutcomes.Add(SessionOutcomeFilter.All);
                    }
                    else
                    {
                        SelectedOutcomes.Remove(SessionOutcomeFilter.All);
                        if (!SelectedOutcomes.Remove(f))
                            SelectedOutcomes.Add(f);

                        if (SelectedOutcomes.Count == 0)
                            SelectedOutcomes.Add(SessionOutcomeFilter.All);
                    }

                    foreach (var b in outcomeButtons)
                        b.SetActive(SelectedOutcomes.Contains(b.Value));

                    OutcomesChanged?.Invoke(SelectedOutcomes);
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
                var btn = new FilterTabButton<SessionStatusFilter>(filter, label, null, SelectedStatuses.Contains(filter), f =>
                {
                    if (f == SessionStatusFilter.All)
                    {
                        SelectedStatuses.Clear();
                        SelectedStatuses.Add(SessionStatusFilter.All);
                    }
                    else
                    {
                        SelectedStatuses.Remove(SessionStatusFilter.All);
                        if (!SelectedStatuses.Remove(f))
                            SelectedStatuses.Add(f);

                        if (SelectedStatuses.Count == 0)
                            SelectedStatuses.Add(SessionStatusFilter.All);
                    }

                    foreach (var b in statusButtons)
                        b.SetActive(SelectedStatuses.Contains(b.Value));

                    StatusesChanged?.Invoke(SelectedStatuses);
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
                var btn = new FilterTabButton<SessionSortMode>(mode, label, null, mode == CurrentSort, m =>
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
            private readonly IconUsage? icon;
            private readonly Drawable? customIcon;
            private readonly Action<T> onSelect;
            private bool isActive;

            private Box background = null!;
            private SpriteIcon? iconSprite;
            private OsuSpriteText textSprite = null!;

            public FilterTabButton(T value, LocalisableString label, IconUsage? icon, bool active, Action<T> onSelect, Drawable? customIcon = null)
            {
                Value = value;
                this.label = label;
                this.icon = icon;
                this.customIcon = customIcon;
                isActive = active;
                this.onSelect = onSelect;

                AutoSizeAxes = Axes.Both;
                Action = () => onSelect(Value);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                var contentFlow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };

                if (customIcon != null)
                {
                    customIcon.Anchor = Anchor.CentreLeft;
                    customIcon.Origin = Anchor.CentreLeft;
                    contentFlow.Add(customIcon);
                }
                else if (icon.HasValue)
                {
                    contentFlow.Add(iconSprite = new SpriteIcon
                    {
                        Icon = icon.Value,
                        Size = new Vector2(11),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    });
                }

                contentFlow.Add(textSprite = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = label,
                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                });

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
                            Child = contentFlow,
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
                    if (iconSprite != null) iconSprite.Colour = Color4.White;
                }
                else
                {
                    background.Colour = Color4.Transparent;
                    textSprite.Colour = colourProvider.Content1;
                    if (iconSprite != null) iconSprite.Colour = colourProvider.Content2;
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
