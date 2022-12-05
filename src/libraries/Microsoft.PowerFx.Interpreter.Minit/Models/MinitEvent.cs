// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Interpreter.Minit.Models
{
    public class MinitEvent
    {
        public MinitEvent()
        {
        }

        public MinitEvent(int @case, int view, string activity, MinitUser user, int duration)
        {
            Case = @case;
            View = view;
            Activity = activity;
            User = user.ToString();
            Duration = duration;
        }

        public string User { get; set; }

        public string Activity { get; set; }

        public int Case { get; set; }

        public int View { get; set; }

        public int Duration { get; set; }
    }
}
