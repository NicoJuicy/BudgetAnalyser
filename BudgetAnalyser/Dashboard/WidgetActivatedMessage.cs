﻿using BudgetAnalyser.Engine.Widgets;
using Rees.Wpf;

namespace BudgetAnalyser.Dashboard
{
    public class WidgetActivatedMessage : MessageBase
    {
        public WidgetActivatedMessage(Widget widget)
        {
            Widget = widget;
        }

        public bool Handled { get; set; }
        public Widget Widget { get; private set; }
    }
}
