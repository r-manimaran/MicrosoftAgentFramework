using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Chat;

/// <summary>
/// A single token (or small token batch) pushed over SignalR from the API
/// to the Blazor client during a streaming chat response.
///
/// The client accumulates tokens into a string buffer and re-renders the
/// message bubble on each received token, giving a typewriter effect.
/// </summary>
/// <param name="SessionId">Links the token to the correct conversation.</param>
/// <param name="TokenText">
///   The text of this token. May be a single word-piece or a small batch
///   of several tokens flushed together for efficiency.
/// </param>
/// <param name="TokenIndex">
///   Zero-based sequence number. The client uses this to detect and handle
///   out-of-order delivery (rare but possible under load).
/// </param>
/// <param name="IsFinished">
///   True on the final token of the response. After receiving this, the
///   client should fetch the full ChatResponse (with citations and eval scores)
///   via a GET /chat/{sessionId}/last endpoint.
/// </param>
/// <param name="IsError">
///   True if the pipeline encountered an unrecoverable error. TokenText will
///   contain a user-friendly error message in this case.
/// </param>
public sealed record ChatStreamToken(
    Guid SessionId,
    string TokenText,
    int TokenIndex,
    bool IsFinished = false,
    bool IsError = false
);