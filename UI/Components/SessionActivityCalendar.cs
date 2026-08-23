using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using LazerLens.Models;

namespace LazerLens.UI.Components
{
    public sealed partial class SessionActivityCalendar : CompositeDrawable
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly IReadOnlyList<SessionSummary> summaries;

        public SessionActivityCalendar(IReadOnlyList<SessionSummary> summaries)
        {
            this.summaries = summaries;
            RelativeSizeAxes = Axes.X;
            Height = 110;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var dayCounts = new Dictionary<DateTime, int>();
            foreach (var s in summaries)
            {
                var day = s.StartTime.ToLocalTime().Date;
                if (!dayCounts.ContainsKey(day))
                    dayCounts[day] = 0;
                dayCounts[day] += s.PlayCount;
            }

            DateTime today = DateTime.Now.Date;
            const int total_days = 70; // 10 weeks of 7 days
            DateTime startDate = today.AddDays(-total_days + 1);

            FillFlowContainer weeksFlow;

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 8,
                BorderThickness = 1,
                BorderColour = colourProvider.Background1,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(12),
                        Spacing = new Vector2(0, 8),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(8, 0),
                                Children = new Drawable[]
                                {
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(13),
                                        Icon = FontAwesome.Solid.CalendarAlt,
                                        Colour = colourProvider.Highlight1,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = LazerLensStrings.ActivityCalendarTitle,
                                        Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Bold),
                                        Colour = Color4.White,
                                    }
                                }
                            },
                            weeksFlow = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 56,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(4, 0),
                            }
                        }
                    }
                }
            };

            // Build columns (weeks)
            for (int w = 0; w < 10; w++)
            {
                var dayColumn = new FillFlowContainer
                {
                    Width = 14,
                    RelativeSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 3),
                };

                for (int d = 0; d < 7; d++)
                {
                    DateTime cellDate = startDate.AddDays(w * 7 + d);
                    int count = dayCounts.TryGetValue(cellDate, out int cnt) ? cnt : 0;

                    dayColumn.Add(new CalendarCell(cellDate, count));
                }

                weeksFlow.Add(dayColumn);
            }
        }

        private sealed partial class CalendarCell : CompositeDrawable, IHasTooltip
        {
            private readonly DateTime date;
            private readonly int count;

            public LocalisableString TooltipText => LazerLensStrings.ActivityPlaysCount(count, date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));

            public CalendarCell(DateTime date, int count)
            {
                this.date = date;
                this.count = count;

                Size = new Vector2(10);
                Masking = true;
                CornerRadius = 2;

                Color4 cellColor = count switch
                {
                    0 => Color4.White.Opacity(0.06f),
                    < 5 => Color4Extensions.FromHex("216e39"),
                    < 15 => Color4Extensions.FromHex("30a14e"),
                    < 30 => Color4Extensions.FromHex("40c463"),
                    _ => Color4Extensions.FromHex("9be9a8")
                };

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = cellColor,
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.4f, 100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1.0f, 100, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
