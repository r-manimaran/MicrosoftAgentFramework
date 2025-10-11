using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolCalling.Basic;

public static class Tools
{
    // comment this description. It may also consider in the token usage.
    [Description("Get the current date and time in the specified timezone.")]
    public static DateTime CurrentDateAndTime(TimeType type)
    {
        return type switch
        {
            TimeType.Local => DateTime.Now,
            TimeType.Utc => DateTime.UtcNow,         
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    [Description("Get the current timezone of the system.")]
    public static string CurrentTimezone()
    {
        return TimeZoneInfo.Local.DisplayName;
    }

    public enum TimeType
    {
        Local,
        Utc
    }
}
