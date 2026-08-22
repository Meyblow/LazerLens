using System;
using System.Globalization;
using osu.Framework.Allocation;
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
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components
{
    public sealed partial class ArchiveSessionCard : OsuClickableContainer, IHasContextMenu
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public SessionSummary Summary { get; }
        public bool IsSelected { get; private set; }

        private readonly Action<Guid>? onOpenFolder;
        private readonly Action<Guid, bool>? onTogglePin;
        private readonly Action<Guid, string?>? onSetNote;
        private readonly Action<Guid>? onDelete;

        private Box background = null!;
        private Box selectionIndicator = null!;
        private Container mainContainer = null!;

        public MenuItem[]? ContextMenuItems => new MenuItem[]
        {
            new OsuMenuItem(LazerLensStrings.ContextMenuOpenInFolder, MenuItemType.Standard, () => onOpenFolder?.Invoke(Summary.Id)),
            new OsuMenuItem(Summary.IsPinned ? LazerLensStrings.ContextMenuUnpinSession : LazerLensStrings.ContextMenuPinSession, MenuItemType.Standard, () => onTogglePin?.Invoke(Summary.Id, !Summary.IsPinned)),
            new OsuMenuItem(string.IsNullOrEmpty(Summary.Note) ? LazerLensStrings.ContextMenuSetNote : LazerLensStrings.ContextMenuEditNote, MenuItemType.Standard, () => onSetNote?.Invoke(Summary.Id, Summary.Note)),
            new OsuMenuItem(LazerLensStrings.ContextMenuDeleteSession, MenuItemType.Destructive, () => onDelete?.Invoke(Summary.Id)),
        };

        public ArchiveSessionCard(
            SessionSummary summary,
            bool isSelected,
            Action action,
            Action<Guid>? onOpenFolder = null,
            Action<Guid, bool>? onTogglePin = null,
            Action<Guid, string?>? onSetNote = null,
            Action<Guid>? onDelete = null)
        {
            Summary = summary;
            IsSelected = isSelected;
            Action = action;
            this.onOpenFolder = onOpenFolder;
            this.onTogglePin = onTogglePin;
            this.onSetNote = onSetNote;
            this.onDelete = onDelete;

            RelativeSizeAxes = Axes.X;
            Height = 64;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            string dateStr = Summary.StartTime.ToLocalTime().ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
            string ppStr = Summary.TopPP > 0 ? $"{Summary.TopPP:F0} PP" : "-";
            string accStr = $"{Summary.AverageAccuracy:F2}%";
            string playsStr = LazerLensStrings.ArchivePlaysCount(Summary.PlayCount).ToString();

            string titleText = !string.IsNullOrWhiteSpace(Summary.Note) ? Summary.Note : dateStr;
            IconUsage titleIcon = !string.IsNullOrWhiteSpace(Summary.Note) ? FontAwesome.Solid.Tag : FontAwesome.Solid.CalendarAlt;

            Child = mainContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                BorderThickness = IsSelected ? 2 : 0,
                BorderColour = colourProvider.Highlight1,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = IsSelected ? colourProvider.Background3 : colourProvider.Background4,
                    },
                    selectionIndicator = new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Y,
                        Width = 4,
                        Colour = colourProvider.Highlight1,
                        Alpha = IsSelected ? 1 : 0,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Left = 14, Right = 10, Vertical = 9 },
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            // Top row: Title/Date + Pin icon + Top PP badge
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(6, 0),
                                        Children = new Drawable[]
                                        {
                                            new SpriteIcon
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Size = new Vector2(12),
                                                Icon = titleIcon,
                                                Colour = IsSelected ? colourProvider.Highlight1 : Color4.White.Opacity(0.7f),
                                            },
                                            new TruncatingSpriteText
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Text = titleText,
                                                Font = OsuFont.Torus.With(size: 13, weight: FontWeight.SemiBold),
                                                Colour = IsSelected ? Color4.White : Color4.White.Opacity(0.9f),
                                                MaxWidth = Summary.IsPinned ? 160 : 180,
                                            },
                                            new SpriteIcon
                                            {
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                                Size = new Vector2(11),
                                                Icon = FontAwesome.Solid.Thumbtack,
                                                Colour = Color4Extensions.FromHex("ffcc00"),
                                                Alpha = Summary.IsPinned ? 1 : 0,
                                            }
                                        }
                                    },
                                    new Container
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        AutoSizeAxes = Axes.Both,
                                        Masking = true,
                                        CornerRadius = 4,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = colourProvider.Background5,
                                            },
                                            new OsuSpriteText
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Text = ppStr,
                                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                                Colour = Color4Extensions.FromHex("ffcc00"),
                                                Margin = new MarginPadding { Horizontal = 6, Vertical = 2 },
                                            }
                                        }
                                    }
                                }
                            },

                            // Bottom row: Info (Plays, Acc, and Date if custom note is used)
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8, 0),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = playsStr,
                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                        Colour = Color4.White.Opacity(0.6f),
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = $"\u2022 {accStr} avg",
                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                        Colour = Color4.White.Opacity(0.6f),
                                    },
                                    new TruncatingSpriteText
                                    {
                                        Text = !string.IsNullOrWhiteSpace(Summary.Note) ? $"\u2022 {dateStr}" : "",
                                        Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Regular),
                                        Colour = Color4.White.Opacity(0.5f),
                                        MaxWidth = 110,
                                        Alpha = !string.IsNullOrWhiteSpace(Summary.Note) ? 1 : 0,
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public void SetSelected(bool selected)
        {
            if (IsDisposed) return;
            IsSelected = selected;
            mainContainer.BorderThickness = selected ? 2 : 0;
            selectionIndicator.FadeTo(selected ? 1 : 0, 100);
            background.FadeColour(selected ? colourProvider.Background3 : colourProvider.Background4, 100);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (!IsSelected)
                background.FadeColour(colourProvider.Background3.Opacity(0.8f), 100);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (!IsSelected)
                background.FadeColour(colourProvider.Background4, 100);
            base.OnHoverLost(e);
        }
    }
}
