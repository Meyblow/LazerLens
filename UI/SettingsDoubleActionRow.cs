using osu.Framework.Localisation;
using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;

namespace LazerLens.UI
{
    public partial class SettingsDoubleActionRow : Container
    {
        private readonly LocalisableString leftLabel;
        private readonly LocalisableString rightLabel;
        private readonly Action leftAction;
        private readonly Action rightAction;

        public SettingsDoubleActionRow(LocalisableString leftLabel, Action leftAction, LocalisableString rightLabel, Action rightAction)
        {
            this.leftLabel = leftLabel;
            this.leftAction = leftAction;
            this.rightLabel = rightLabel;
            this.rightAction = rightAction;

            RelativeSizeAxes = Axes.X;
            Height = 40;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.5f),
                    new Dimension(GridSizeMode.Relative, 0.5f)
                },
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 1f)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Right = 2.5f },
                            Child = new ActionButton(leftLabel, leftAction)
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Left = 2.5f },
                            Child = new ActionButton(rightLabel, rightAction)
                        }
                    }
                }
            };
        }

        private sealed partial class ActionButton : OsuClickableContainer
        {
            private Box background = null!;
            private readonly LocalisableString label;

            public ActionButton(LocalisableString label, Action action)
            {
                this.label = label;
                Action = action;
                RelativeSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 5;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colours.Gray2,
                        Alpha = 0
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = label,
                        Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 14)
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeIn(200, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeOut(200, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
