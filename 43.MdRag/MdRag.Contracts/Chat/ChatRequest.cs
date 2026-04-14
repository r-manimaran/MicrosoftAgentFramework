using System;
using System.Collections.Generic;
using System.Text;

namespace MdRag.Contracts.Chat;

/// <summary>
/// Sent by the Blazor client to POST /chat.
/// </summary>
/// <param name="SessionId">
///   Stable GUID that identifies the conversation.
///   The API uses this to load conversation history from the session store,
///   enabling multi-turn context across requests.
/// </param>
/// <param name="Query">The user's input or question for the chat session.</param>
/// <param name="Filters">Optional filters to refine the chat response.</param>
public sealed record ChatRequest(Guid SessionId, string Query, ChatFilters? Filters = null);

/// <summary>
/// Optional retrieval filters scoped to the request.
/// All properties are ANDed together when non-null.
/// </summary>
/// <param name="FolderPath">
///   Restricts retrieval to chunks whose source file lives under this path prefix.
///   Example: "docs/api" will only search files ingested from that folder.
/// </param>
/// <param name="ModifiedAfter">
///   Only return chunks from files last modified after this UTC date.
/// </param>
/// <param name="ModifiedBefore">
///   Only return chunks from files last modified before this UTC date.
/// </param>
public sealed record ChatFilters(
    string? FolderPath= null,
    DateTime? ModifiedAfter = null,
    DateTime? ModifiedBefore = null
    );

