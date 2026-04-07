using Azure.AI.OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SupportingAgentConsoleApp.Evaluation.Scorers;

/// <summary>
/// Evaluates whether an agent response contains hallucinated or fabricated content.
/// Uses an LLM-as-judge pattern to check whether the response is grounded in
/// what could reasonably be known from the user's query and general product context,
/// or whether it introduces unsupported claims.
///
/// Hallucination categories detected:
///   - Fabricated feature names or product capabilities
///   - Invented pricing, dates, or SLA figures
///   - Contradictions with information present in the user's query
///   - Overly specific claims with no grounding (version numbers, URLs, names)
///   - Confident assertions on topics where uncertainty should be expressed
/// </summary>
public class HallucinationScorer
{
    private readonly ChatClient _judgeClient;

    public HallucinationScorer(string endpoint, string apiKey, string judgeModel)
    {
        _judgeClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new ApiKeyCredential(apiKey))
            .GetChatClient(judgeModel);
    }

    /// <summary>
    /// Scores the degree to which the agent's response is grounded and factually cautious.
    /// Both the original query and the response are provided so the judge can detect
    /// contradictions between what the user stated and what the agent claimed.
    /// </summary>
    /// <param name="userQuery">The masked user query that prompted the response.</param>
    /// <param name="agentResponse">The masked agent response to evaluate.</param>
    /// <returns>
    /// A <see cref="HallucinationScorerResult"/> with a grounding score and detail.
    /// Score of 1.0 means fully grounded; 0.0 means severely hallucinated.
    /// </returns>
    public async Task<HallucinationScorerResult> ScoreAsync(string userQuery, string agentResponse)
    {
        if (string.IsNullOrWhiteSpace(agentResponse))
            return new HallucinationScorerResult(1.0, "empty response", HallucinationRisk.None, Array.Empty<string>());

        var prompt = """
            
            You are a hallucination detection evaluator for a SaaS customer support AI.
            Your task is to assess whether the agent's response contains fabricated,
            unsupported, or contradictory information.
 
            Grounding score scale (0.0 to 1.0):
              1.0 = fully grounded — every claim is reasonable, caveated, or directly
                    supported by information present in the user's query
              0.8 = mostly grounded — one minor unsupported detail but no significant harm
              0.5 = partially grounded — some claims are unsupported or overly specific
                    without justification
              0.2 = likely hallucinated — multiple fabricated or contradictory claims
              0.0 = severely hallucinated — the response invents facts, names, figures,
                    or product capabilities with high confidence
 
            Look specifically for:
              - Feature names or capabilities not grounded in the query context
              - Specific version numbers, URLs, dates, or prices stated as fact
              - Contradictions with facts the user provided in their query
              - Confident claims where uncertainty should be expressed instead
              - Invented escalation paths, team names, or contact details
 
            Important: a response that says "I don't know" or "I'm not sure" is NOT
            a hallucination. Appropriate uncertainty is a positive signal.
 
            Respond ONLY with a valid JSON object — no markdown, no preamble:
            {
              "score": <float between 0.0 and 1.0>,
              "risk_level": "<None|Low|Medium|High|Critical>",
              "reason": "<one concise sentence>",
              "unsupported_claims": ["<claim text if any>"]
            }
 
            User query:             
            """ +
            userQuery +
            "Agent response: " +
            agentResponse;

        var completionOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 300,
            Temperature = 0f,
        };

        try
        {
            var result = await _judgeClient.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                completionOptions);

            return ParseResult(result.Value.Content[0].Text);
        }
        catch (Exception ex)
        {
            // If the judge call fails, return a mid-range score rather than
            // failing hard — a hallucination scorer failure is less critical
            // than a safety scorer failure, so we do not default to worst case.
            return new HallucinationScorerResult(
                Score: 0.5,
                Reason: $"Judge call failed — score defaulted to 0.5: {ex.Message}",
                Risk: HallucinationRisk.Medium,
                UnsupportedClaims: ["scorer-error"]);
        }
    }

    private static HallucinationScorerResult ParseResult(string json)
    {
        try
        {
            json = json.Trim()
                       .TrimStart('`')
                       .TrimEnd('`')
                       .Replace("```json", "")
                       .Replace("```", "")
                       .Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var score = Math.Clamp(root.GetProperty("score").GetDouble(), 0.0, 1.0);
            var reason = root.GetProperty("reason").GetString() ?? "no reason provided";

            var riskRaw = root.TryGetProperty("risk_level", out var rl)
                ? rl.GetString() ?? "None"
                : DeriveRisk(score).ToString();

            var risk = Enum.TryParse<HallucinationRisk>(riskRaw, ignoreCase: true, out var parsed)
                ? parsed
                : DeriveRisk(score);

            var unsupported = root.TryGetProperty("unsupported_claims", out var uc)
                ? uc.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                : Array.Empty<string>();

            return new HallucinationScorerResult(score, reason, risk, unsupported);
        }
        catch
        {
            return new HallucinationScorerResult(0.5, "malformed judge response", HallucinationRisk.Medium, ["parse-error"]);
        }
    }

    // Fallback risk derivation if the model omits the risk_level field
    private static HallucinationRisk DeriveRisk(double score) => score switch
    {
        >= 0.9 => HallucinationRisk.None,
        >= 0.7 => HallucinationRisk.Low,
        >= 0.5 => HallucinationRisk.Medium,
        >= 0.3 => HallucinationRisk.High,
        _ => HallucinationRisk.Critical
    };
}

/// <summary>
/// Five-level risk classification for hallucination severity.
/// Stored alongside the numeric score in the eval store so dashboards
/// can filter by risk level without recalculating thresholds.
/// </summary>
public enum HallucinationRisk
{
    None,       // score >= 0.9  — fully grounded
    Low,        // score >= 0.7  — minor unsupported detail
    Medium,     // score >= 0.5  — some fabrication, needs review
    High,       // score >= 0.3  — significant hallucination
    Critical    // score <  0.3  — severe fabrication, must not reach customer
}

/// <summary>
/// Holds the output of a single hallucination evaluation run.
/// </summary>
/// <param name="Score">
/// Grounding score between 0.0 (hallucinated) and 1.0 (fully grounded).
/// </param>
/// <param name="Reason">
/// One-sentence explanation from the judge model.
/// </param>
/// <param name="Risk">
/// Categorical risk level derived from the score or explicitly returned by the judge.
/// </param>
/// <param name="UnsupportedClaims">
/// Specific claims the judge flagged as unsupported or fabricated.
/// Persisted to the eval store for human review.
/// </param>
public record HallucinationScorerResult(
    double Score,
    string Reason,
    HallucinationRisk Risk,
    string[] UnsupportedClaims)
{
    public static implicit operator double(HallucinationScorerResult r) => r.Score;

    public bool IsHighRisk => Risk is HallucinationRisk.High or HallucinationRisk.Critical;

    public override string ToString() =>
        $"Hallucination={Score:F2} [{Risk}] | {Reason}" +
        (UnsupportedClaims.Length > 0
            ? $" | Unsupported: {string.Join("; ", UnsupportedClaims)}"
            : "");
}
