using osu.Framework.Localisation;
using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components
{
    /// <summary>
    /// A dropdown-style session selector that sits in the overlay header area.
    /// Shows "● Live Session" when active, and lists past sessions in a popover.
    /// </summary>
    public sealed partial class SessionSelectorDropdown : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly Action<Guid?> onSessionSelected;
        private readonly Func<List<SessionSummary>> getSessions;

        private OsuSpriteText currentLabel = null!;
        private SpriteIcon arrowIcon = null!;
        private FillFlowContainer dropdownContent = null!;
        private Container dropdownContainer = null!;
        private bool isOpen;

        public Guid? CurrentSessionId { get; private set; }

        public SessionSelectorDropdown(Action<Guid?> onSessionSelected, Func<List<SessionSummary>> getSessions)
        {
            this.onSessionSelected = onSessionSelected;
            this.getSessions = getSessions;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                new SessionSelectorButton(this),
                dropdownContainer = new Container
                {
                    Width = 240,
                    AutoSizeAxes = Axes.Y,
                    BypassAutoSizeAxes = Axes.Both,
                    Position = new Vector2(0, 36),
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Depth = -100,
                    Masking = true,
                    CornerRadius = 8,
                    Alpha = 0,
                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                    {
                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                        Colour = Color4.Black.Opacity(0.45f),
                        Radius = 8,
                    },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background5,
                        },
                        dropdownContent = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Padding = new MarginPadding(6),
                        }
                    }
                }
            };

            currentLabel = new OsuSpriteText();
            arrowIcon = new SpriteIcon();
        }

        public void Toggle()
        {
            if (IsDisposed) return;
            isOpen = !isOpen;

            if (isOpen)
            {
                populateDropdown();
                dropdownContainer.FadeIn(200, Easing.OutQuint);
                arrowIcon.RotateTo(180, 200, Easing.OutQuint);
            }
            else
            {
                dropdownContainer.FadeOut(150, Easing.InQuint);
                arrowIcon.RotateTo(0, 200, Easing.OutQuint);
            }
        }

        public void SelectLive()
        {
            CurrentSessionId = null;
            updateLabel();
            close();
            onSessionSelected(null);
        }

        public void SelectSession(Guid sessionId)
        {
            CurrentSessionId = sessionId;
            updateLabel();
            close();
            onSessionSelected(sessionId);
        }

        public bool IsViewingArchive => CurrentSessionId.HasValue;

        private void close()
        {
            if (!isOpen) return;
            isOpen = false;
            dropdownContainer.FadeOut(150, Easing.InQuint);
            arrowIcon.RotateTo(0, 200, Easing.OutQuint);
        }

        private void updateLabel()
        {
            if (currentLabel != null)
            {
                currentLabel.Text = CurrentSessionId.HasValue ? LazerLensStrings.DropdownArchivedSession : LazerLensStrings.DropdownLiveSession;
                currentLabel.Colour = CurrentSessionId.HasValue ? Color4Extensions.FromHex("ffcc00") : colourProvider.Highlight1;
            }
        }

        private void populateDropdown()
        {
            dropdownContent.Clear();

            // Live session item
            dropdownContent.Add(new SessionDropdownItem(LazerLensStrings.DropdownLiveSession, LazerLensStrings.DropdownCurrentActive, !CurrentSessionId.HasValue, () => SelectLive()));

            var sessions = getSessions();
            foreach (var s in sessions)
            {
                string label = s.StartTime.ToLocalTime().ToString("dd MMM HH:mm", CultureInfo.InvariantCulture);
                var detail = LazerLensStrings.SessionSummaryDetail(s.PlayCount, s.TopPP, s.AverageAccuracy);
                bool isSelected = CurrentSessionId == s.Id;
                var id = s.Id;
                dropdownContent.Add(new SessionDropdownItem(label, detail, isSelected, () => SelectSession(id)));
            }
        }

        private sealed partial class SessionSelectorButton : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly SessionSelectorDropdown parent;
            private Box background = null!;
            private OsuSpriteText labelText = null!;
            private SpriteIcon arrow = null!;

            public SessionSelectorButton(SessionSelectorDropdown parent)
            {
                this.parent = parent;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                Height = 34;
                Action = () => parent.Toggle();

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
                            Colour = colourProvider.Background3,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Horizontal = 14 },
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
                                            Icon = FontAwesome.Solid.History,
                                            Colour = colourProvider.Colour1,
                                        },
                                        labelText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = parent.CurrentSessionId.HasValue ? LazerLensStrings.DropdownArchivedSession : LazerLensStrings.DropdownLiveSession,
                                            Font = OsuFont.Torus.With(size: 13, weight: FontWeight.SemiBold),
                                            Colour = parent.CurrentSessionId.HasValue ? Color4Extensions.FromHex("ffcc00") : colourProvider.Highlight1,
                                        },
                                    }
                                },
                                arrow = parent.arrowIcon = new SpriteIcon
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Size = new Vector2(10),
                                    Icon = FontAwesome.Solid.ChevronDown,
                                    Colour = Color4.White.Opacity(0.6f),
                                }
                            }
                        }
                    }
                };

                parent.currentLabel = labelText;
            }

            protected override void Update()
            {
                base.Update();
                if (labelText != null)
                {
                    labelText.Text = parent.CurrentSessionId.HasValue ? LazerLensStrings.DropdownArchivedSession : LazerLensStrings.DropdownLiveSession;
                    labelText.Colour = parent.CurrentSessionId.HasValue ? Color4Extensions.FromHex("ffcc00") : colourProvider.Highlight1;
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

        private sealed partial class SessionDropdownItem : OsuClickableContainer
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly LocalisableString label;
            private readonly LocalisableString detail;
            private readonly bool isSelected;
            private Box background = null!;

            public SessionDropdownItem(LocalisableString label, LocalisableString detail, bool isSelected, Action action)
            {
                this.label = label;
                this.detail = detail;
                this.isSelected = isSelected;

                RelativeSizeAxes = Axes.X;
                Height = 38;
                Action = action;
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
                            Colour = isSelected ? colourProvider.Colour0.Opacity(0.3f) : colourProvider.Background4,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Padding = new MarginPadding { Horizontal = 10 },
                            Spacing = new Vector2(0, 2),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = label,
                                    Font = OsuFont.Torus.With(size: 12, weight: isSelected ? FontWeight.Bold : FontWeight.SemiBold),
                                    Colour = isSelected ? colourProvider.Highlight1 : Color4.White,
                                },
                                new OsuSpriteText
                                {
                                    Text = detail,
                                    Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Regular),
                                    Colour = Color4.White.Opacity(0.5f),
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
                background.FadeColour(isSelected ? colourProvider.Colour0.Opacity(0.3f) : colourProvider.Background4, 100);
                base.OnHoverLost(e);
            }
        }
    }
}
