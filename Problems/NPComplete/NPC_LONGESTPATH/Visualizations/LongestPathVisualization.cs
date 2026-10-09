using API.Interfaces;
using API.Interfaces.JSON_Objects;
using API.Interfaces.Graphs.GraphParser;
using API.Interfaces.JSON_Objects.Graphs;
using API.Problems.NPComplete.NPC_LONGESTPATH;

namespace API.Problems.NPComplete.NPC_LONGESTPATH.Visualizations;

class LongestPathVisualization : IVisualization<LONGESTPATH, API_GraphJSON> {
    public string visualizationName { get; } = "Longest Path Visualization";
    public string visualizationDefinition { get; } = "This is a default visualization for longest path";
    public string source { get; } = "";
    public string sourceLink { get; } = "TODO";
    public string[] contributors { get; } = { "Alice Giola", "Sadman Hasan", "Javier Rodriguez Lopez" };
    // TODO: replace Unimplemented with the VisualizationType matching the API_JSON shape you
    // return below. A test in redux-tests/Metadata (VisualizationType_Tests, NoNewUndeclared)
    // fails until you do, on purpose. If no existing renderer fits, make the problem's default
    // visualization DummyVisualization instead of this class. Adding a NEW VisualizationType
    // member means updating Documentation/visualization-types.json (checked by
    // VisualizationType_Tests.ManifestMatchesEnum) and adding a renderer in the Redux_GUI repo
    // (checked there by `npm run check:visualizations`).
    public VisualizationType visualizationType { get; } = VisualizationType.GraphD3;
    // The solver whose steps this visualization shows. Defaults to the problem's default solver.
    // Replace it with a specific solver if you prefer, e.g. `new MyBruteForce()`. This must stay
    // expression-bodied (=>): the problem's constructor builds its default visualization, so an
    // initializer (=) here would recurse forever.
    public ISolver solver => new LONGESTPATH().defaultSolver;

    // --- Methods Including Constructors ---
    public LongestPathVisualization() {

    }
    public API_GraphJSON visualize(LONGESTPATH instance) {
        return instance.graph.ToAPIGraph();
    }

    public API_GraphJSON SolvedVisualization(LONGESTPATH instance, string solution) {
        List<string> solutionList = GraphParser.parseNodeListWithStringFunctions(solution);

        API_GraphJSON apiGraph = instance.graph.ToAPIGraph();

        for (int i = 0; i < apiGraph.nodes.Count; i++) {
            if (solutionList.Contains(apiGraph.nodes[i].name)) {
                apiGraph.nodes[i].color = "Solution";
            } else {
                apiGraph.nodes[i].color = "Background";
            }
        }
        return apiGraph;
    }
}