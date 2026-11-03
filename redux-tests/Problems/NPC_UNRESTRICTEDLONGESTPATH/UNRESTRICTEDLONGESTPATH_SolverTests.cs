using System.Text.Json;
using API.Interfaces;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace redux_tests;
#pragma warning disable CS1591

public class UNRESTRICTEDLONGESTPATH_SolverTests {
    private static readonly string[] SolverNames = {
        nameof(UnrestrictedLongestPathBruteForce),
        nameof(UnrestrictedLongestPathBacktracking),
        nameof(UnrestrictedLongestPathSubsetDP)
    };

    private static ISolver CreateSolver(string name) => name switch {
        nameof(UnrestrictedLongestPathBruteForce) => new UnrestrictedLongestPathBruteForce(),
        nameof(UnrestrictedLongestPathBacktracking) => new UnrestrictedLongestPathBacktracking(),
        nameof(UnrestrictedLongestPathSubsetDP) => new UnrestrictedLongestPathSubsetDP(),
        _ => throw new ArgumentException("unknown solver", nameof(name))
    };

    public static IEnumerable<object[]> BoundaryCases() {
        var cases = new (string Input, bool Yes)[] {
            ("(({a},{}),0)", true),
            ("(({a},{}),1)", false),
            ("(({a,b,c},{}),0)", true),
            ("(({a,b,c},{}),1)", false),
            ("(({a,b},{{b,a}}),1)", true),
            ("(({a,b},{{a,b}}),2)", false),
            ("(({a,b,c,d},{{a,b},{b,c}}),2)", true),
            ("(({a,b,c,d},{{a,b},{c,d}}),2)", false),
            ("(({a,b,c,d,e},{{a,b},{a,c},{a,d},{a,e}}),2)", true),
            ("(({a,b,c,d,e},{{a,b},{a,c},{a,d},{a,e}}),3)", false),
            ("(({a,b,c,d},{{a,b},{b,c},{c,d},{d,a}}),3)", true),
            ("(({a,b,c,d},{{a,b},{b,c},{c,d},{d,a}}),4)", false),
            ("(({a,b,c,d,e},{{a,b},{c,d},{d,e}}),2)", true),
            ("(({a},{}),2147483647)", false),
            ("(({isolated,a,b,c},{{a,b},{b,c}}),2)", true)
        };
        foreach (string solver in SolverNames)
            foreach (var test in cases)
                yield return new object[] { solver, test.Input, test.Yes };
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Solver_BoundaryCasesReturnCorrectWitnessOrNoSolution(string solverName, string input, bool yes) {
        ISolver solver = CreateSolver(solverName);
        var problem = new UNRESTRICTEDLONGESTPATH(input);
        string answer = solver.solve(input);
        Assert.NotEqual("timeout", answer);
        Assert.Equal(yes, answer != "{}");
        Assert.Equal(yes, problem.defaultVerifier.verify(problem, answer));
        if (yes) AssertWitnessIndependently(problem, answer);
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce))]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking))]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP))]
    public void Solver_DefaultInstanceReturnsValidCertificate(string solverName) {
        var problem = new UNRESTRICTEDLONGESTPATH();
        string answer = CreateSolver(solverName).solve(problem.instance);
        Assert.True(problem.defaultVerifier.verify(problem, answer));
        AssertWitnessIndependently(problem, answer);
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce))]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking))]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP))]
    public void Solver_TimerExpirationAndResetAreDistinctFromNoSolution(string solverName) {
        ISolver solver = CreateSolver(solverName);
        const string instance = "(({a,b},{{a,b}}),1)";
        solver.TimerExpired();
        Assert.Equal("timeout", solver.solve(instance));
        solver.ResetTimer();
        var problem = new UNRESTRICTEDLONGESTPATH(instance);
        Assert.True(problem.defaultVerifier.verify(problem, solver.solve(instance)));
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce))]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking))]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP))]
    public async Task Solver_ObservesCancellationDuringHardSearch(string solverName) {
        // An unbalanced complete bipartite graph has no Hamiltonian path.
        // These instances leave ample search work, while the flag makes the test bounded.
        string[] edges = (from u in Enumerable.Range(0, 20)
                          from v in Enumerable.Range(20, 10)
                          select "{" + u + "," + v + "}").ToArray();
        string input = "(({" + string.Join(",", Enumerable.Range(0, 30)) + "},{" + string.Join(",", edges) + "}),29)";
        ISolver solver = CreateSolver(solverName);
        using ManualResetEventSlim started = new();
        Task<string> work = Task.Run(() => {
            started.Set();
            return solver.solve(input);
        }, TestContext.Current.CancellationToken);
        try {
            Assert.True(started.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await Task.Delay(10, TestContext.Current.CancellationToken);
            solver.TimerExpired();
            Assert.Equal("timeout", await work.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        } finally {
            solver.TimerExpired();
        }
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce))]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking))]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP))]
    public void Solver_HandlesMoreThan64VerticesWithoutTruncatedMasks(string solverName) {
        const int n = 70;
        string input = "(({" + string.Join(",", Enumerable.Range(0, n)) + "},{" +
            string.Join(",", Enumerable.Range(0, n - 1).Select(v => "{" + v + "," + (v + 1) + "}")) + "}),69)";
        var problem = new UNRESTRICTEDLONGESTPATH(input);
        string answer = CreateSolver(solverName).solve(input);
        Assert.True(problem.defaultVerifier.verify(problem, answer));
        AssertWitnessIndependently(problem, answer);
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce))]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking))]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP))]
    public void Solver_InvalidInstanceUsesReduxParseException(string solverName) {
        foreach (string input in new[] { "", "garbage", "(({a},{}),-1)", "(({a,b},{{a,c}}),1)" }) {
            Assert.Throws<ProblemParseException>(() => CreateSolver(solverName).solve(input));
        }
    }

    [Theory]
    [InlineData(nameof(UnrestrictedLongestPathBruteForce), SolverType.BruteForce, SolverComplexityBucket.Factorial)]
    [InlineData(nameof(UnrestrictedLongestPathBacktracking), SolverType.Backtracking, SolverComplexityBucket.Factorial)]
    [InlineData(nameof(UnrestrictedLongestPathSubsetDP), SolverType.DynamicProgramming, SolverComplexityBucket.Exponential)]
    public void Solver_MetadataAndRuntimeDiscoveryAreCompatible(string solverName, SolverType type, SolverComplexityBucket bucket) {
        ISolver solver = CreateSolver(solverName);
        Assert.Equal(type, solver.solverType);
        Assert.Equal(bucket, solver.complexityBucket);
        Assert.NotNull(solver.timeComplexity);
        Assert.NotNull(solver.spaceComplexity);
        Assert.True(solver.timeComplexity.Evaluate(("n", 5), ("m", 4)) > 0);
        Assert.True(solver.spaceComplexity.Evaluate(("n", 5), ("m", 4)) > 0);
        Assert.True(ProblemProvider.Solvers.ContainsKey(solverName.ToLowerInvariant()));
        Assert.IsType<ContentResult>(new ProblemProvider().info(solverName));
        var response = Assert.IsType<ContentResult>(new ProblemProvider().solve(solverName, "(({a,b},{{a,b}}),1)"));
        string answer = JsonSerializer.Deserialize<string>(response.Content!)!;
        var problem = new UNRESTRICTEDLONGESTPATH("(({a,b},{{a,b}}),1)");
        Assert.True(problem.defaultVerifier.verify(problem, answer));
    }

    [Fact]
    public void AllThreeSolvers_MatchPermutationOracleForEveryGraphThroughFiveVertices() {
        int graphs = 0, decisions = 0, solverRuns = 0;
        for (int n = 1; n <= 5; n++) {
            List<(int U, int V)> pairs = new();
            for (int u = 0; u < n; u++)
                for (int v = u + 1; v < n; v++) pairs.Add((u, v));
            for (int mask = 0; mask < (1 << pairs.Count); mask++) {
                graphs++;
                bool[,] adjacency = new bool[n, n];
                List<string> edges = new();
                for (int e = 0; e < pairs.Count; e++) {
                    if ((mask & (1 << e)) == 0) continue;
                    var (u, v) = pairs[e];
                    adjacency[u, v] = adjacency[v, u] = true;
                    edges.Add("{" + u + "," + v + "}");
                }
                int maximum = LongestRunInAnyPermutation(adjacency, n);
                for (int k = 0; k <= n + 1; k++) {
                    decisions++;
                    string input = "(({" + string.Join(",", Enumerable.Range(0, n)) + "},{" + string.Join(",", edges) + "})," + k + ")";
                    var problem = new UNRESTRICTEDLONGESTPATH(input);
                    foreach (string solverName in SolverNames) {
                        solverRuns++;
                        string answer = CreateSolver(solverName).solve(input);
                        Assert.NotEqual("timeout", answer);
                        Assert.Equal(maximum >= k, answer != "{}");
                        Assert.Equal(maximum >= k, problem.defaultVerifier.verify(problem, answer));
                        if (answer != "{}") AssertWitnessIndependently(problem, answer);
                    }
                }
            }
        }
        Assert.Equal(1099, graphs);
        Assert.Equal(7603, decisions);
        Assert.Equal(22809, solverRuns);
    }

    // Oracle uses complete permutations and scans all consecutive runs; no solver/helper is reused.
    private static int LongestRunInAnyPermutation(bool[,] adjacency, int n) {
        int[] order = Enumerable.Range(0, n).ToArray();
        int best = 0;
        void Permute(int position) {
            if (position == n) {
                int run = 0;
                for (int i = 1; i < n; i++) {
                    run = adjacency[order[i - 1], order[i]] ? run + 1 : 0;
                    best = Math.Max(best, run);
                }
                return;
            }
            for (int i = position; i < n; i++) {
                (order[position], order[i]) = (order[i], order[position]);
                Permute(position + 1);
                (order[position], order[i]) = (order[i], order[position]);
            }
        }
        Permute(0);
        return best;
    }

    private static void AssertWitnessIndependently(UNRESTRICTEDLONGESTPATH problem, string answer) {
        Assert.StartsWith("{", answer);
        Assert.EndsWith("}", answer);
        string[] path = answer[1..^1].Split(',');
        Assert.True(path.Length - 1 >= problem.K);
        Assert.Equal(path.Length, path.Distinct(StringComparer.Ordinal).Count());
        foreach (string vertex in path) Assert.Contains(vertex, problem.nodes);
        for (int i = 1; i < path.Length; i++) {
            string u = path[i - 1], v = path[i];
            Assert.Contains(problem.edges, e => (e.Key == u && e.Value == v) || (e.Key == v && e.Value == u));
        }
    }
}
