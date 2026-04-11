using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Evaluation;

public record EvalResult(
    string RunId,
    string TicketId,
    double RelevanceScore,
    double HallucinationScore,
    double SafetyScore,
    string HallucinationRisk,
    string SafetyViolations,
    DateTime EvaluatedAt);

