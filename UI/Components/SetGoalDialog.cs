using System;
using System.Globalization;
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
using LazerLens.Models;

namespace LazerLens.UI.Components
{
    public sealed partial class SetGoalDialog : VisibilityContainer
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly SessionGoal? currentGoal;
        private readonly Action<SessionGoal?> onSave;

        private Container dialogCard = null!;
        private OsuEnumDropdown<SessionGoalType> typeDropdown = null!;
        private OsuTextBox targetBox = null!;

        public SetGoalDialog(SessionGoal? currentGoal, Action<SessionGoal?> onSave)
        {
            this.currentGoal = currentGoal;
            this.onSave = onSave;

            RelativeSizeAxes = Axes.Both;
            Depth = -99999;
            State.Value = Visibility.Visible;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
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
                                            Icon = FontAwesome.Solid.Bullseye,
                                            Colour = colourProvider.Highlight1,
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = LazerLensStrings.GoalDialogTitle,
                                            Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                                            Colour = Color4.White,
                                        }
                                    }
                                },
                                new OsuSpriteText
                                {
                                    Text = LazerLensStrings.GoalDialogDesc,
                                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Regular),
                                    Colour = Color4.White.Opacity(0.7f),
                                },
                                new OsuSpriteText
                                {
                                    Text = LazerLensStrings.GoalTypeLabel,
                                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                    Colour = Color4.White.Opacity(0.9f),
                                },
                                typeDropdown = new OsuEnumDropdown<SessionGoalType>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Current = { Value = currentGoal?.Type ?? SessionGoalType.PlayCount }
                                },
                                new OsuSpriteText
                                {
                                    Text = LazerLensStrings.GoalTargetValueLabel,
                                    Font = OsuFont.Torus.With(size: 12, weight: FontWeight.SemiBold),
                                    Colour = Color4.White.Opacity(0.9f),
                                },
                                targetBox = new OsuTextBox
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 38,
                                    Text = currentGoal?.TargetValue > 0 ? currentGoal.TargetValue.ToString(CultureInfo.InvariantCulture) : "20",
                                },
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

            targetBox.OnCommit += (_, _) => submit();
        }

        private void submit()
        {
            var type = typeDropdown.Current.Value;
            if (type == SessionGoalType.None)
            {
                onSave(null);
                Hide();
                return;
            }

            if (double.TryParse(targetBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double target) ||
                double.TryParse(targetBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out target))
            {
                if (target > 0)
                {
                    onSave(new SessionGoal
                    {
                        Type = type,
                        TargetValue = target,
                        IsAchieved = false
                    });
                }
                else
                {
                    onSave(null);
                }
            }

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

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (State.Value == Visibility.Visible)
            {
                this.FadeIn(200, Easing.OutQuint);
                dialogCard?.ScaleTo(0.95f).ScaleTo(1.0f, 250, Easing.OutQuint);
            }
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);
            dialogCard?.ScaleTo(0.95f).ScaleTo(1.0f, 250, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(150, Easing.OutQuint);
            dialogCard?.ScaleTo(0.95f, 150, Easing.OutQuint);
            Expire();
        }
    }
}
