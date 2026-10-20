using API.Interfaces;

namespace API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;

class UnrestrictedLongestPathSolver : ISolver<UNRESTRICTEDLONGESTPATH> {
    public string solverName { get; } = "Unrestricted Longest Path Problem Backtracking";
    public string solverDefinition { get; } =
        "Exact elementary search over simple paths starting at every vertex, stopping after finding k edges. " +
        "Returns an ordered vertex list on YES, () on NO, or Search timed out if interrupted.";
    public string source { get; } = "Exhaustive simple-path enumeration with backtracking.";
    public string sourceFile { get; } = SourceFile.Path();
    public string[] contributors { get; } = Array.Empty<string>();
    private volatile bool expired;
    public bool timerHasExpired { get => expired; set => expired = value; }
    public SolverType solverType { get; } = SolverType.Backtracking;
    public SolverComplexityBucket complexityBucket { get; } = SolverComplexityBucket.Factorial;
    public string complexity { get; } =
    "O(n * n! + m), n = |V|, m = |E|; excludes input parsing";
    public string spaceComplexity { get; } =
        "O(n + m), n = |V|, m = |E|; excludes input parsing";

    public UnrestrictedLongestPathSolver() { }

    public string solve(UNRESTRICTEDLONGESTPATH problem) {
        if (timerHasExpired) return "Search timed out";
        if (problem.K == 0) return $"({problem.nodes[0]})";
        if (problem.K > problem.nodes.Count - 1) return "()";

        Dictionary<string, List<string>> adjacency = problem.nodes.ToDictionary(v => v, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in problem.edges) {
            adjacency[edge.Key].Add(edge.Value);
            adjacency[edge.Value].Add(edge.Key);
        }
        // K fits in int after the |V|-1 bound above. An explicit stack avoids recursive stack overflow.
        int requiredVertices = (int)problem.K + 1;
        List<string> path = new();
        List<int> nextNeighbor = new();
        HashSet<string> used = new(StringComparer.Ordinal);
        foreach (string start in problem.nodes) {
            path.Add(start);
            nextNeighbor.Add(0);
            used.Add(start);
            while (path.Count != 0) {
                if (timerHasExpired) return "Search timed out";
                if (path.Count >= requiredVertices) return "(" + string.Join(",", path) + ")";
                int depth = path.Count - 1;
                string current = path[depth];
                if (nextNeighbor[depth] == adjacency[current].Count) {
                    used.Remove(current);
                    path.RemoveAt(depth);
                    nextNeighbor.RemoveAt(depth);
                    continue;
                }
                string neighbor = adjacency[current][nextNeighbor[depth]];
                nextNeighbor[depth]++;
                if (used.Add(neighbor)) {
                    path.Add(neighbor);
                    nextNeighbor.Add(0);
                }
            }
        }
        return timerHasExpired ? "Search timed out" : "()";
    }
}
