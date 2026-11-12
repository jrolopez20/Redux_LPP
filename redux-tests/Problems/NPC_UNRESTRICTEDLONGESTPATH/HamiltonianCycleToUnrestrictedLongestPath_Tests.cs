using System;
using System.Collections.Generic;
using System.Linq;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.ReduceTo.NPC_HAMILTONIAN;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;
using Xunit;

namespace redux_tests;

public class HamiltonianCycleToUnrestrictedLongestPath_Tests {
    private const string Triangle = "({c,b,a},{{b,a},{c,b},{c,a}})";

    [Fact]
    public void TriangleProducesTheExpectedSplitGraphAndThreshold() {
        var reduction = new HamiltonianCycleToUnrestrictedLongestPath(Triangle);
        var target = reduction.reductionTo;
        var reparsed = new UNRESTRICTEDLONGESTPATH(target.instance);

        // Copies 0 and 3 share neighbors 1 and 2. Leaves 4 and 5 force the ends.
        Assert.Equal(new[] { "0", "1", "2", "3", "4", "5" }, reparsed.nodes.OrderBy(v => v));
        Assert.True(reparsed.K == 5);
        var edges = reparsed.edges.Select(e => string.CompareOrdinal(e.Key, e.Value) < 0
            ? (e.Key, e.Value)
            : (e.Value, e.Key));
        Assert.Equal(new[] {
            ("0", "1"), ("0", "2"), ("0", "4"), ("1", "2"), ("1", "3"), ("2", "3"), ("3", "5")
        }, edges.OrderBy(e => e));
    }

    [Fact]
    public void ReductionAndMappingStayStableAfterTheSourceSolverRuns() {
        var reduction = new HamiltonianCycleToUnrestrictedLongestPath();
        var target = reduction.reductionTo;
        var source = reduction.reductionFrom;
        string certificate = source.defaultSolver.solve(source);
        string mapped = reduction.mapSolutions(certificate);

        Assert.True(target.defaultVerifier.verify(target, mapped));
        Assert.Equal(target.instance, reduction.reduce().instance);
        Assert.Equal(mapped, reduction.mapSolutions(certificate));
    }

    [Theory]
    [InlineData("{a,b,c}")]
    [InlineData("{b,c,a,b}")]
    [InlineData("{c,b,a,c}")]
    public void MapsCyclesWithDifferentStartsOrientationsAndOptionalClosingVertex(string cycle) {
        var reduction = new HamiltonianCycleToUnrestrictedLongestPath(Triangle);
        var target = reduction.reductionTo;
        string answer = reduction.mapSolutions(cycle);

        Assert.True(target.defaultVerifier.verify(target, answer));
        Assert.StartsWith("{4,0,", answer);
        Assert.EndsWith(",3,5}", answer);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("a,b,c")]
    [InlineData("{a,b,unknown}")]
    [InlineData("{a,b,a}")]
    public void RejectsInvalidSourceCertificates(string certificate) {
        var reduction = new HamiltonianCycleToUnrestrictedLongestPath(Triangle);

        Assert.Throws<FormatException>(() => reduction.mapSolutions(certificate));
    }

    [Theory]
    [InlineData("({},{})", false)]
    [InlineData("({a},{})", false)]
    [InlineData("({a,b},{})", false)]
    [InlineData("({a,b},{{a,b}})", true)]
    public void PreservesReduxDecisionsForTinyInputs(string input, bool expected) {
        var reduction = new HamiltonianCycleToUnrestrictedLongestPath(input);
        var source = reduction.reductionFrom;
        var target = reduction.reductionTo;
        string sourceAnswer = source.defaultSolver.solve(source);
        string targetAnswer = new UnrestrictedLongestPathBacktracking().solve(target);

        Assert.Equal(expected, sourceAnswer != "{}");
        Assert.Equal(expected, targetAnswer != "{}");
        if (expected) {
            Assert.True(target.defaultVerifier.verify(target, targetAnswer));
            Assert.True(target.defaultVerifier.verify(target, reduction.mapSolutions(sourceAnswer)));
        }
    }

    [Fact]
    public void PreservesTheDecisionForEverySimpleGraphOnThreeThroughFiveVertices() {
        var solver = new UnrestrictedLongestPathBacktracking();

        for (int n = 3; n <= 5; n++) {
            var labels = Enumerable.Range(1, n).Select(i => $"v{i}").ToArray();
            var possibleEdges = new List<(int U, int V)>();
            for (int u = 0; u < n; u++) {
                for (int v = u + 1; v < n; v++) {
                    possibleEdges.Add((u, v));
                }
            }

            for (int mask = 0; mask < (1 << possibleEdges.Count); mask++) {
                var adjacency = new bool[n, n];
                var edges = new List<string>();
                for (int bit = 0; bit < possibleEdges.Count; bit++) {
                    if ((mask & (1 << bit)) == 0) continue;
                    var (u, v) = possibleEdges[bit];
                    adjacency[u, v] = adjacency[v, u] = true;
                    edges.Add("{" + labels[u] + "," + labels[v] + "}");
                }

                string input = "({" + string.Join(",", labels) + "},{" + string.Join(",", edges) + "})";
                bool expected = HasHamiltonianCycle(adjacency, 0, 1, 1);
                var reduction = new HamiltonianCycleToUnrestrictedLongestPath(input);
                var target = reduction.reductionTo;
                string answer = solver.solve(target);

                Assert.True(expected == (answer != "{}"), $"Unexpected answer for {input}: {answer}");
                if (expected) {
                    Assert.True(target.defaultVerifier.verify(target, answer), $"Invalid path for {input}: {answer}");
                }
            }
        }
    }

    // Independent oracle: start at vertex 0 and check whether a spanning walk can close.
    private static bool HasHamiltonianCycle(bool[,] adjacency, int last, int used, int count) {
        int n = adjacency.GetLength(0);
        if (count == n) return adjacency[last, 0];

        for (int next = 1; next < n; next++) {
            int bit = 1 << next;
            if ((used & bit) == 0 && adjacency[last, next] &&
                HasHamiltonianCycle(adjacency, next, used | bit, count + 1)) {
                return true;
            }
        }
        return false;
    }
}
