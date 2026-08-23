using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;
using LazerLens.Services;

namespace LazerLens.UI.Components
{
    public sealed partial class SessionGoalWidget : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly LazerLensService service;
        private readonly Action onEditGoal;

        private Box progressBar = null!;
        private OsuSpriteText titleText = null!;
        private OsuSpriteText progressText = null!;
        private Container glowContainer = null!;
        private SpriteIcon iconSprite = null!;

        public SessionGoalWidget(LazerLensService service, Action onEditGoal)
        {
            this.service = service;
            this.onEditGoal = onEditGoal;

            RelativeSizeAxes = Axes.X;
            Height = 44;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    // Progress Bar Fill
                    progressBar = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 0,
                        Colour = colourProvider.Highlight1.Opacity(0.35f),
                    },
                    // Achieved Glow
                    glowContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4Extensions.FromHex("ffcc00").Opacity(0.15f),
                        }
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
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    iconSprite = new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(16),
                                        Icon = FontAwesome.Solid.Bullseye,
                                        Colour = colourProvider.Highlight1,
                                    },
                                    titleText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = LazerLensStrings.GoalHeader,
                                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                        Colour = Color4.White,
                                    },
                                    progressText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = OsuFont.Torus.With(size: 12, weight: FontWeight.Regular),
                                        Colour = Color4.White.Opacity(0.85f),
                                    }
                                }
                            },
                            new IconButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Icon = FontAwesome.Solid.Edit,
                                IconScale = new Vector2(0.8f),
                                Action = onEditGoal,
                                TooltipText = "Edit Goal"
                            }
                        }
                    }
                }
            };

            service.ActiveGoal.BindValueChanged(_ => UpdateState(), true);
            service.OnSessionUpdated += UpdateState;
        }

        public void UpdateState()
        {
            if (IsDisposed) return;

            var goal = service.ActiveGoal.Value;
            var session = service.LiveState;

            if (goal == null || goal.Type == SessionGoalType.None || goal.TargetValue <= 0)
            {
                progressText.Text = $"\u2022 {LazerLensStrings.GoalNoneActive}";
                progressBar.ResizeWidthTo(0, 200, Easing.OutQuint);
                glowContainer.FadeOut(200);
                iconSprite.Colour = colourProvider.Content2;
                return;
            }

            double progress = goal.GetProgress(session);
            string progressStr = goal.GetProgressString(session);
            bool isComplete = progress >= 1.0;

            if (isComplete)
            {
                progressText.Text = $"\u2022 {progressStr} ({LazerLensStrings.GoalAchieved})";
                progressBar.FadeColour(Color4Extensions.FromHex("ffcc00").Opacity(0.4f), 200);
                progressBar.ResizeWidthTo(DrawWidth, 300, Easing.OutQuint);
                glowContainer.FadeIn(300);
                iconSprite.Colour = Color4Extensions.FromHex("ffcc00");
                titleText.Colour = Color4Extensions.FromHex("ffcc00");
            }
            else
            {
                progressText.Text = $"\u2022 {progressStr} ({(progress * 100):F0}%)";
                progressBar.FadeColour(colourProvider.Highlight1.Opacity(0.35f), 200);
                progressBar.ResizeWidthTo((float)(DrawWidth * progress), 300, Easing.OutQuint);
                glowContainer.FadeOut(200);
                iconSprite.Colour = colourProvider.Highlight1;
                titleText.Colour = Color4.White;
            }
        }
    }
}
