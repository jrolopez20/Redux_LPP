using System.Text.RegularExpressions;
using API.Interfaces;
using SPADE;

namespace API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Verifiers;

class UnrestrictedLongestPathVerifier : IVerifier<UNRESTRICTEDLONGESTPATH> {
    public const string CertificateGrammar =
        "{P | P is list}; write (v0,...,vl) with distinct declared vertices, adjacent consecutive vertices, and l >= k. " +
        "A single vertex (v) certifies k=0. () denotes no witness and verifies false.";
    public const string CertificateExample = "(1,2,3)";
    private static readonly Regex CertificateSyntax = new(
        @"\A\s*\(\s*(?:" + UNRESTRICTEDLONGESTPATH.VertexPattern + @"(?:\s*,\s*" +
        UNRESTRICTEDLONGESTPATH.VertexPattern + @")*)?\s*\)\s*\z",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    public string verifierName { get; } = "Default Unrestricted Longest Path Problem Verifier";
    public string verifierDefinition { get; } =
        "Checks that an ordered list is a simple path with at least k edges, allowing any endpoints.";
    public string source { get; } = "Direct verification of the simple-path certificate definition.";
    public string sourceFile { get; } = "Problems/NPComplete/NPC_UNRESTRICTEDLONGESTPATH/Verifiers/UnrestrictedLongestPathVerifier.cs";
    public string[] contributors { get; } = Array.Empty<string>();
    public string certificate { get; private set; } = "";

    public UnrestrictedLongestPathVerifier() { }

    public bool verify(UNRESTRICTEDLONGESTPATH problem, string certificate) {
        List<string> path;
        try {
            if (string.IsNullOrWhiteSpace(certificate) || !CertificateSyntax.IsMatch(certificate)) {
                throw new FormatException("expected an ordered vertex list, such as (1,2,3)");
            }
            StringParser parser = new("{P | P is list}");
            parser.parse(string.Concat(certificate.Where(c => !char.IsWhiteSpace(c))));
            path = parser["P"].ToList().Select(v => v.ToString()).ToList();
        } catch (Exception ex) when (ex is not CertificateParseException && ex is not OutOfMemoryException) {
            throw new CertificateParseException(problem, certificate, ex.Message, ex);
        }
        this.certificate = certificate;
        if (path.Count == 0 || path.Count - 1 < problem.K) return false;
        HashSet<string> vertices = new(problem.nodes, StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);
        if (path.Any(v => !vertices.Contains(v) || !visited.Add(v))) return false;
        HashSet<(string, string)> adjacency = new();
        foreach (var edge in problem.edges) {
            adjacency.Add((edge.Key, edge.Value));
            adjacency.Add((edge.Value, edge.Key));
        }
        for (int i = 1; i < path.Count; i++) {
            if (!adjacency.Contains((path[i - 1], path[i]))) return false;
        }
        return true;
    }
}
