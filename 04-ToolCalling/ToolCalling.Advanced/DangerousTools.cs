using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolCalling.Advanced;

public class DangerousTools
{
    public static void SomethingDangerous (int value = 42)
    {
        // Simulate something dangerous
        Utils.WriteLineWarning($"[DANGEROUS OPERATION] Performing a dangerous operation with value: {value}");
    }
}
