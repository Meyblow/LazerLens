using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace LazerLens.UI.Components
{
    public sealed partial class SessionNoteDialog : VisibilityContainer
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly string initialNote;
        private readonly Action<string?> onSave;
        private OsuTextBox textBox = null!;
        private Container dialogCard = null!;

        public SessionNoteDialog(string? currentNote, Action<string?> onSave)
        {
            initialNote = currentNote ?? string.Empty;
            this.onSave = onSave;

            RelativeSizeAxes = Axes.Both;
            State.Value = Visibility.Hidden;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                // Semi-transparent clickable backdrop
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
                // Centered Dialog Card
                dialogCard = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 460,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    CornerRadius = 10,
                    BorderThickness = 2,
                    BorderColour = colourProvider.Highlight1,
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
                            Spacing = new Vector2(0, 14),
                            Padding = new MarginPadding(20),
                            Children = new Drawable[]
                            {
                                // Header: Icon + Title
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Children = new Drawable[]
                                    {
                                        new SpriteIcon
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Size = new Vector2(18),
                                            Icon = FontAwesome.Solid.Tag,
                                            Colour = colourProvider.Highlight1,
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = LazerLensStrings.DialogSetNoteTitle,
                                            Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                                            Colour = Color4.White,
                                        }
                                    }
                                },
                                // Subtitle
                                new OsuSpriteText
                                {
                                    Text = LazerLensStrings.DialogSetNoteDescription,
                                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                    Colour = Color4.White.Opacity(0.7f),
                                },
                                // Text Box
                                textBox = new OsuTextBox
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 38,
                                    PlaceholderText = LazerLensStrings.DialogSetNotePlaceholder.ToString(),
                                    Text = initialNote,
                                },
                                // Action Buttons (Save / Cancel)
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 36,
                                    Margin = new MarginPadding { Top = 6 },
                                    Children = new Drawable[]
                                    {
                                        new FillFlowContainer
                                        {
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(10, 0),
                                            Children = new Drawable[]
                                            {
                                                new RoundedButton
                                                {
                                                    Width = 100,
                                                    Height = 36,
                                                    Text = LazerLensStrings.DialogCancel,
                                                    Action = Hide,
                                                },
                                                new RoundedButton
                                                {
                                                    Width = 110,
                                                    Height = 36,
                                                    Text = LazerLensStrings.DialogSave,
                                                    BackgroundColour = colourProvider.Highlight1,
                                                    Action = submit,
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

            textBox.OnCommit += (_, _) => submit();
        }

        private void submit()
        {
            var note = textBox.Text?.Trim();
            onSave(string.IsNullOrEmpty(note) ? null : note);
            Hide();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
                return true;
            }

            return base.OnKeyDown(e);
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);
            dialogCard.ScaleTo(0.95f).ScaleTo(1.0f, 250, Easing.OutQuint);
            Schedule(() =>
            {
                if (IsDisposed) return;
                GetContainingFocusManager()?.ChangeFocus(textBox);
            });
        }

        protected override void PopOut()
        {
            this.FadeOut(150, Easing.OutQuint);
            dialogCard.ScaleTo(0.95f, 150, Easing.OutQuint);
            Expire();
        }
    }
}
