using System;
using System.Collections.Generic;
using API.Interfaces;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH;
using API.Problems.NPComplete.NPC_UNRESTRICTEDLONGESTPATH.Solvers;
using Xunit;

namespace redux_tests.Problems;

public class UNRESTRICTEDLONGESTPATH_TeachingInstanceTests {
    private const string Fork = "(({1,2,3,4,5},{{1,2},{2,3},{2,4},{4,5}}),3)";
    private const string ForkTooLong = "(({1,2,3,4,5},{{1,2},{2,3},{2,4},{4,5}}),4)";
    private const string DisconnectedTriangles = "(({1,2,3,4,5,6},{{1,2},{2,3},{1,3},{4,5},{5,6},{4,6}}),3)";
    private const string UnbalancedBipartite = "(({1,2,3,4,5,6},{{1,3},{1,4},{1,5},{1,6},{2,3},{2,4},{2,5},{2,6}}),5)";
    private const string CompleteGraph = "(({1,2,3,4,5},{{1,2},{1,3},{1,4},{1,5},{2,3},{2,4},{2,5},{3,4},{3,5},{4,5}}),4)";

    [Fact]
    public void ParameterlessConstructorLoadsTheAdvertisedForkInstance() {
        var problem = new UNRESTRICTEDLONGESTPATH();

        Assert.Equal(Fork, problem.defaultInstance);
        Assert.Equal(Fork, problem.instance);
    }

    [Fact]
    public void ConfiguredDefaultSolverFindsAValidPathOnTheFork() {
        var problem = new UNRESTRICTEDLONGESTPATH();

        Assert.True(problem.defaultVerifier.verify(problem, problem.defaultSolver.solve(problem)));
    }

    public static IEnumerable<object[]> SolverCases() {
        var cases = new (string Name, string Instance, bool HasPath)[] {
            ("Fork", Fork, true),
            ("Fork with K = 4", ForkTooLong, false),
            ("Disconnected triangles", DisconnectedTriangles, false),
            ("Unbalanced bipartite graph", UnbalancedBipartite, false),
            ("Complete graph", CompleteGraph, true)
        };
        string[] solvers = {
            nameof(UnrestrictedLongestPathBruteForce),
            nameof(UnrestrictedLongestPathBacktracking),
            nameof(UnrestrictedLongestPathSubsetDP)
        };

        foreach (string solver in solvers) {
            foreach (var example in cases) {
                yield return new object[] { solver, example.Name, example.Instance, example.HasPath };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SolverCases))]
    public void SolverReturnsTheExpectedDecision(string solverName, string example, string instance, bool hasPath) {
        var problem = new UNRESTRICTEDLONGESTPATH(instance);
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
