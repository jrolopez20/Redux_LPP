using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using API.DummyClasses;
using API.Interfaces;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Verifiers;
using SPADE;

namespace API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH;

class UNRESTRICTEDLONGESTPATH : IGraphProblem<UnrestrictedLongestPathSolver, UnrestrictedLongestPathVerifier, DummyVisualization, UtilCollectionGraph> {
    public const string InstanceGrammar = "{((N,E),K) | N is set, E subset N unorderedcross N, K is int}";
    private const string DefaultInstance = "(({1,2,3,4},{{1,2},{2,3}}),2)";
    internal const string VertexPattern = @"[^\s,{}()]+";
    private const string Vertex = "(?<vertex>" + VertexPattern + ")";
    private const string Edge = @"(?<edge>\{\s*" + VertexPattern + @"\s*,\s*" + VertexPattern + @"\s*\})";

    // Validate the entire input before SPADE can normalize sets or ignore surplus text.
    private static readonly Regex InstanceSyntax = new(
        @"\A\s*\(\s*\(\s*\{\s*" + Vertex + @"(?:\s*,\s*" + Vertex + @")*\s*\}\s*,\s*\{\s*(?:" +
        Edge + @"(?:\s*,\s*" + Edge + @")*)?\s*\}\s*\)\s*,\s*[0-9]+\s*\)\s*\z",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public string problemName { get; } = "Unrestricted Longest Path Problem";
    public string problemLink { get; } = "https://en.wikipedia.org/wiki/Longest_path_problem";
    public string formalDefinition { get; } =
        "{<G,k> | G=(V,E) is a finite nonempty simple undirected unweighted graph, k is a nonnegative integer, " +
        "and there exist distinct v0,...,vl in V with l >= k and {vi,vi+1} in E for every 0 <= i < l}";
    public string problemDefinition { get; } =
        "Decide whether the graph contains a simple path with at least k edges. Endpoints may be any vertices. " +
        "Disconnected graphs and isolated vertices are allowed; k=0 always succeeds for a nonempty graph.";
    public string inputDescription { get; } =
        "A finite nonempty simple undirected unweighted graph G=(V,E), and a nonnegative integer k.";
    public string outputDescription { get; } =
        "YES iff a simple path with at least k edges exists; otherwise NO. A YES certificate is an ordered vertex list.";
    public string source { get; } =
        "Garey, Michael R., and David S. Johnson. Computers and Intractability: A Guide to the Theory of NP-Completeness. " +
        "W. H. Freeman, 1979. NP-hardness also follows directly from undirected Hamiltonian Path by taking k=|V|-1; " +
        "a simple path is a polynomially checkable certificate.";
    public string sourceLink { get; } = "https://openlibrary.org/isbn/9780716710455";
    public string sourceFile { get; } = "Problems/NPComplete/NPC_UNRESTRICTEDLONGESTPATH/UNRESTRICTEDLONGESTPATH_Class.cs";
    public string wikiName { get; } = "";
    public ComplexityClass complexityClass { get; } = ComplexityClass.NPComplete;
    public ProblemType problemType { get; } = ProblemType.GraphTheory;
    // No human authorship is asserted for this generated implementation.
    public string[] contributors { get; } = Array.Empty<string>();
    public string defaultInstance { get; } = DefaultInstance;
    public string instance { get; }
    public string instanceFormat { get; } =
        $"Format: {InstanceGrammar}. Example: {DefaultInstance}. " +
        "Vertex labels are nonempty tokens without whitespace, commas, braces, or parentheses. " +
        "N is nonempty; duplicate vertices, duplicate undirected edges, loops, and undeclared endpoints are rejected. " +
        "K is a nonnegative decimal integer; whitespace between tokens is allowed.";
    public string certificateFormat { get; } =
        $"Format: {UnrestrictedLongestPathVerifier.CertificateGrammar} Example: {UnrestrictedLongestPathVerifier.CertificateExample}";
    public UnrestrictedLongestPathSolver defaultSolver { get; } = new();
    public UnrestrictedLongestPathVerifier defaultVerifier { get; } = new();
    public DummyVisualization defaultVisualization { get; } = new();
    public UtilCollectionGraph graph { get; }
    public IReadOnlyList<string> nodes { get; }
    public IReadOnlyList<KeyValuePair<string, string>> edges { get; }
    // Arbitrary precision preserves every nonnegative integer, including very large NO thresholds.
    [JsonConverter(typeof(UnrestrictedLongestPathIntegerConverter))]
    public BigInteger K { get; }

    public UNRESTRICTEDLONGESTPATH() : this(DefaultInstance) { }

    public UNRESTRICTEDLONGESTPATH(string stringInstance) {
        try {
            if (string.IsNullOrWhiteSpace(stringInstance)) {
                throw new FormatException("instance is empty");
            }
            Match syntax = InstanceSyntax.Match(stringInstance);
            if (!syntax.Success) {
                throw new FormatException("expected (({vertices},{{u,v},...}),k), with a nonempty vertex set and nonnegative decimal k");
            }

            StringParser parser = new(InstanceGrammar);
            parser.parse(string.Concat(stringInstance.Where(c => !char.IsWhiteSpace(c))));
            List<string> parsedNodes = parser["N"].ToList().Select(n => n.ToString()).ToList();
            List<UtilCollection> parsedEdges = parser["E"].ToList();
            BigInteger parsedK = BigInteger.Parse(parser["K"].ToString(), NumberStyles.None, CultureInfo.InvariantCulture);
            if (parsedNodes.Count == 0 || parsedNodes.Count != syntax.Groups["vertex"].Captures.Count ||
                parsedNodes.Distinct(StringComparer.Ordinal).Count() != parsedNodes.Count) {
                throw new FormatException("vertices must be nonempty and distinct");
            }
            if (parsedEdges.Count != syntax.Groups["edge"].Captures.Count) {
                throw new FormatException("duplicate undirected edges are not allowed");
            }

            HashSet<string> vertexSet = new(parsedNodes, StringComparer.Ordinal);
            HashSet<(string, string)> edgeSet = new();
            List<KeyValuePair<string, string>> edgeList = new();
            foreach (UtilCollection edge in parsedEdges) {
                List<UtilCollection> endpoints = edge.ToList();
                if (edge.IsValue() || edge.IsOrdered() || endpoints.Count != 2 || endpoints.Any(n => !n.IsValue())) {
                    throw new FormatException("each edge must be an unordered pair of distinct vertex labels");
                }
                string u = endpoints[0].ToString();
                string v = endpoints[1].ToString();
                if (u == v || !vertexSet.Contains(u) || !vertexSet.Contains(v)) {
                    throw new FormatException("loops and undeclared edge endpoints are not allowed");
                }
                var canonical = string.CompareOrdinal(u, v) < 0 ? (u, v) : (v, u);
                if (!edgeSet.Add(canonical)) {
                    throw new FormatException("duplicate undirected edges are not allowed");
                }
                edgeList.Add(new(u, v));
            }

            // Publish state only after every parse and validation step succeeds.
            graph = new UtilCollectionGraph(parser["N"], parser["E"]);
            nodes = parsedNodes.AsReadOnly();
            edges = edgeList.AsReadOnly();
            K = parsedK;
            instance = stringInstance;
        } catch (Exception ex) when (ex is not ProblemParseException && ex is not OutOfMemoryException) {
            throw new ProblemParseException("UNRESTRICTEDLONGESTPATH", stringInstance, ex.Message, ex);
        }
    }
}

// Keep Redux's reflected JSON output numeric instead of serializing BigInteger's helper properties.
class UnrestrictedLongestPathIntegerConverter : JsonConverter<BigInteger> {
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Number ||
            !BigInteger.TryParse(document.RootElement.GetRawText(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)) {
            throw new JsonException("expected a nonnegative integer");
        }
        return value;
    }

    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options) {
        writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture));
    }
}
