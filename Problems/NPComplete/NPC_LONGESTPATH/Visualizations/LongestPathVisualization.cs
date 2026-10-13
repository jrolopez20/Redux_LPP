using API.Interfaces;
using API.Interfaces.JSON_Objects;
using API.Interfaces.Graphs.GraphParser;
using API.Interfaces.JSON_Objects.Graphs;
using API.Problems.NPComplete.NPC_LONGESTPATH;

namespace API.Problems.NPComplete.NPC_LONGESTPATH.Visualizations;

class LongestPathVisualization : IVisualization<LONGESTPATH, API_GraphJSON> {
    public string visualizationName { get; } = "Longest Path Visualization";
    public string visualizationDefinition { get; } = "This is a default visualization for longest path";
    public string source { get; } = "M. R. Garey and D. S. Johnson, Computers and Intractability: A Guide to the Theory of NP-Completeness. San Francisco, CA, USA: W. H.Freeman, 1979.";
    public string sourceLink { get; } = "https://books.google.com/books?id=fjxGAQAAIAAJ";
    public string[] contributors { get; } = { "Alice Giola", "Sadman Hasan", "Javier Rodriguez Lopez" };

    public VisualizationType visualizationType { get; } = VisualizationType.GraphD3;
    // The solver whose steps this visualization shows. Defaults to the problem's default solver.
    // Replace it with a specific solver if you prefer, e.g. `new MyBruteForce()`. This must stay
    // expression-bodied (=>): the problem's constructor builds its default visualization, so an
    // initializer (=) here would recurse forever.
    public ISolver solver => new LONGESTPATH().defaultSolver;

    public string sourceFile { get; } = SourceFile.Path();

    // --- Methods Including Constructors ---
    public LongestPathVisualization() {

    }
    public API_GraphJSON visualize(LONGESTPATH instance) {
        return instance.graph.ToAPIGraph();
    }

    public API_JSON SolvedVisualization(LONGESTPATH instance, string solution) {
        API_GraphJSON apiGraph = instance.graph.ToAPIGraph();
        if (string.IsNullOrWhiteSpace(solution) || solution.Trim() == "{}")
            return apiGraph;

        List<string> solutionList = GraphParser.parseNodeListWithStringFunctions(solution)
            .Where(node => !string.IsNullOrWhiteSpace(node))
            .ToList();
        if (solutionList.Count == 0)
            return apiGraph;

        // Highlight nodes that are part of the solution path.
        if (solutionList.Count > 0) {
            string firstNode = solutionList[0];
            string lastNode = solutionList[solutionList.Count - 1];

            foreach (var node in apiGraph.nodes) {
                if (solutionList.Contains(node.name)) {
                    if (node.name == firstNode || node.name == lastNode) {
                        node.color = "Red";
                    } else {
                        node.color = "Solution";
                    }
                }
                
            }
        }

        // Highlight edges that are part of the solution path.
        for (int i = 0; i < solutionList.Count - 1; i++) {
            string from = solutionList[i];
            string to = solutionList[i + 1];

            foreach (var link in apiGraph.links) {
                if ((link.source == from && link.target == to)
                    || (link.source == to && link.target == from)) {
                    link.color = "Solution";
                }
            }
        }

        return apiGraph;
    }
}