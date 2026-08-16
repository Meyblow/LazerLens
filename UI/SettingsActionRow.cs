using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;

namespace LazerLens.UI
{
    public partial class SettingsActionRow : OsuClickableContainer
    {
        private readonly string label;
        private Box background = null!;

        public SettingsActionRow(string label)
        {
            this.label = label;
            RelativeSizeAxes = Axes.X;
            Height = 40; // Default SettingsItem height
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
                    Colour = colours.Gray2, // approximate SettingsItem background
                    Alpha = 0
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Text = label,
                    Margin = new MarginPadding { Left = 20 },
                    Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 16)
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

