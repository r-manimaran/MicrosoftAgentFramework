using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MdRag.Shared.Telemetry
{
    public static class ActivitySources
    {
        // The string names must match what is registered in AddRagObservability().
        // Changing a name here without updating the registration will silently drop traces.

        /// <summary>Spans originating in MdRag.Api (HTTP endpoints, middleware).</summary>
        public static readonly ActivitySource Api = new ActivitySource("MdRag.Api", version: "1.0");

        /// <summary>Spans originating in MdRag.AgentCore (agent invocations, workflows).</summary>
        public static readonly ActivitySource Agent = new ActivitySource("MdRag.AgentCore", version: "1.0");

        /// <summary>Spans originating in MdRag.Ingestion (file watch, parse, embed, upsert).</summary>
        public static readonly ActivitySource Ingestion = new ActivitySource("MdRag.Ingestion", version: "1.0");

        /// <summary>Spans originating in MdRag.Infrastructure (Qdrant, Redis, SQL operations).</summary>
        public static readonly ActivitySource Infrastructure = new ActivitySource("MdRag.Infrastructure", version: "1.0");

        /// <summary>
        /// All source names as an array — passed to AddRagObservability() so the
        /// OTel tracer provider subscribes to all of them in a single call.
        /// </summary>
        public static readonly string[] AllSourceNames = [
            Api.Name,
            Agent.Name,
            Ingestion.Name,
            Infrastructure.Name
            ];
    }
}
