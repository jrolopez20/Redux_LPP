using System;
using System.Collections.Generic;
using API.Interfaces;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;
using Xunit;

namespace redux_tests.Problems;

public class UNRESTRICTEDLONGESTPATH_CliqueInstanceTests {
    private const string DefaultGraph =
        "(({1,2,3,4,5,6,7},{{1,2},{1,3},{1,4},{2,3},{2,4},{3,4},{1,5},{2,6},{3,7},{6,7}}),6)";
    private const string ConnectedNo =
        "(({1,2,3,4,5,6,7},{{1,2},{1,3},{1,4},{2,3},{2,4},{3,4},{1,5},{2,6},{3,7}}),6)";
    private const string DisconnectedNo =
        "(({1,2,3,4,5,6,7},{{1,2},{1,3},{1,4},{2,3},{2,4},{3,4},{5,6},{5,7},{6,7}}),6)";

    [Fact]
    public void ParameterlessConstructorLoadsTheAdvertisedDefaultGraph() {
        var problem = new UNRESTRICTEDLONGESTPATH();

        Assert.Equal(DefaultGraph, problem.defaultInstance);
        Assert.Equal(DefaultGraph, problem.instance);
    }

    [Fact]
    public void ConfiguredDefaultSolverReturnsAVerifiablePath() {
        var problem = new UNRESTRICTEDLONGESTPATH();
        string answer = problem.defaultSolver.solve(problem);

        Assert.True(problem.defaultVerifier.verify(problem, answer),
            $"The default solver returned an invalid path: {answer}");
    }

    public static IEnumerable<object[]> SolverCases() {
        var instances = new (string Name, string Input, bool HasPath)[] {
            ("Default graph", DefaultGraph, true),
            ("Connected graph without edge 6–7", ConnectedNo, false),
            ("Disconnected graph with the same edge count", DisconnectedNo, false)
        };
        string[] solvers = {
            nameof(UnrestrictedLongestPathBruteForce),
            nameof(UnrestrictedLongestPathBacktracking),
            nameof(UnrestrictedLongestPathSubsetDP)
        };

        foreach (string solver in solvers) {
            foreach (var instance in instances) {
                yield return new object[] { solver, instance.Name, instance.Input, instance.HasPath };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SolverCases))]
    public void SolverReturnsTheExpectedDecision(string solverName, string example, string input, bool hasPath) {
        var problem = new UNRESTRICTEDLONGESTPATH(input);
        ISolver<UNRESTRICTEDLONGESTPATH> solver = solverName switch {
            nameof(UnrestrictedLongestPathBruteForce) => new UnrestrictedLongestPathBruteForce(),
            nameof(UnrestrictedLongestPathBacktracking) => new UnrestrictedLongestPathBacktracking(),
            nameof(UnrestrictedLongestPathSubsetDP) => new UnrestrictedLongestPathSubsetDP(),
            _ => throw new ArgumentOutOfRangeException(nameof(solverName), solverName, "Unknown solver")
        };

        string answer = solver.solve(problem);

        if (hasPath) {
            Assert.True(problem.defaultVerifier.verify(problem, answer),
                $"{solverName} returned an invalid path for {example}: {answer}");
        } else {
            Assert.Equal("{}", answer);
        }
    }
}
