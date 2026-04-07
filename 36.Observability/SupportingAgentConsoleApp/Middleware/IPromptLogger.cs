using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Middleware;

public interface IPromptLogger
{
    Task LogAsync(PromptLogEntry entry);
}
