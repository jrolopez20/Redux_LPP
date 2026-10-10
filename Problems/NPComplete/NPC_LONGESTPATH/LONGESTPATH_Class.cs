using API.Interfaces;
using API.Problems.NPComplete.NPC_LONGESTPATH.Solvers;
using API.Problems.NPComplete.NPC_LONGESTPATH.Verifiers;
using API.Problems.NPComplete.NPC_LONGESTPATH.Visualizations;
using SPADE;

namespace API.Problems.NPComplete.NPC_LONGESTPATH;

class LONGESTPATH : IGraphProblem<LongestPathSolver, LongestPathVerifier, LongestPathVisualization, UtilCollectionGraph> {

    // --- Fields ---
    public string problemName { get; } = "Longest Path";
    public string problemLink { get; } = "https://en.wikipedia.org/wiki/Longest_path_problem";
    public string formalDefinition { get; } = "Longest Path Problem = {<G, k> | Exists a simple path in G with at least k edges}.";
    public string problemDefinition { get; } = "Longest Path Problem decision problem asks whether given a graph G = (V, E) and an integer k, whether G contains a simple path (no repeated vertices) with at least k edges.";
    public string inputDescription { get; } = "G, a graph, and k, the minimum number of edges in the path.";
    public string outputDescription { get; } = "True or False, whether G has a simple path with at least k edges.";
    public string source { get; } = "M. R. Garey and D. S. Johnson, Computers and Intractability: A Guide to the Theory of NP-Completeness. San Francisco, CA, USA: W. H.Freeman, 1979.";
    public string sourceLink { get; } = "https://books.google.com/books?id=fjxGAQAAIAAJ";

    private static readonly string _defaultInstance = "(({1,2,3,4,5,6},{{1,2},{1,3},{2,3},{2,4},{3,5},{4,5},{4,6},{5,6}}),5)";
    public const string InstanceGrammar =
        "{((N,E),K) | N is set, E subset N unorderedcross N, K is int}";
    public string defaultInstance { get; } = _defaultInstance;
    // Short descriptive sentence with a concrete example. Consumed by
    // /ProblemProvider/info and by parse-error responses; this is the
    // canonical place to tell an LLM (or a human, or the GUI) what
    // shape your instance/certificate strings take.
    public string instanceFormat { get; } = $"Format: {InstanceGrammar} Example: {_defaultInstance}";

    public string certificateFormat { get; } =
        $"Format: {LongestPathVerifier.CertificateGrammar}. Example: {LongestPathVerifier.CertificateExample}";
    public string instance { get; set; } = string.Empty;
    public string wikiName { get; } = "Longest path";

    public UtilCollectionGraph graph { get; set; }

    public LongestPathSolver defaultSolver { get; } = new LongestPathSolver();
    public LongestPathVerifier defaultVerifier { get; } = new LongestPathVerifier();
    public LongestPathVisualization defaultVisualization { get; } = new LongestPathVisualization();
    public string[] contributors { get; } = { "Alice Giola", "Sadman Hasan", "Javier Rodriguez Lopez" };
    // Declared, not derived: do NOT infer these from the Problems/<Folder>/ this file lives
    // in (the folder is a filing convention and is wrong for many existing problems).
    // Getting them wrong is a public correctness claim the API serves to callers.
    public ComplexityClass complexityClass { get; } = ComplexityClass.NPComplete;
    
    public ProblemType problemType { get; } = ProblemType.GraphTheory;

    // --- Properties ---
    private List<string> _nodes = new List<string>();
    public List<string> nodes {
        get { return _nodes; }
        set { _nodes = value; }
    }

    private List<KeyValuePair<string, string>> _edges = new List<KeyValuePair<string, string>>();
    public List<KeyValuePair<string, string>> edges {
        get { return _edges; }
        set { _edges = value; }
    }

    private int _K;
    public int K {
        get { return _K; }
        set { _K = value; }
    }

    // --- Methods and Constructors ---
    public LONGESTPATH() : this(_defaultInstance) { }

    public LONGESTPATH(string stringInstance) {
        instance = stringInstance;

        StringParser longestPathGraph = new(InstanceGrammar);
        longestPathGraph.parse(stringInstance);
        nodes = longestPathGraph["N"].ToList().Select(node => node.ToString()).ToList();
        edges = longestPathGraph["E"]
            .ToList()
            .Select(edge => {
                List<UtilCollection> cast = edge.ToList();
                return new KeyValuePair<string, string>(cast[0].ToString(), cast[1].ToString());
            })
            .ToList();
        _K = int.Parse(longestPathGraph["K"].ToString());

        graph = new UtilCollectionGraph(longestPathGraph["N"], longestPathGraph["E"]);
    }
}
