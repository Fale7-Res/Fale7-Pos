using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length is < 1 or > 2) throw new ArgumentException("Repository root and optional --facets-only required");
var facetsOnly=args.Length==2 && args[1]=="--facets-only";
var root = Path.GetFullPath(args[0]);
var work = Path.Combine(root, "Codex Work");
var records = new List<Dictionary<string, object?>>();
var facets = new List<Dictionary<string, object?>>();
var generated = new List<Dictionary<string, object?>>();
var errors = new List<string>();
int files = 0, bodyless = 0, small = 0;
var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols:
    ["TRACE", "RELEASE", "NET", "NET8_0", "NETCOREAPP", "NET8_0_OR_GREATER", "NET7_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER", "WINDOWS"]);
string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
foreach (var mapPath in Directory.EnumerateFiles(Path.Combine(work, "Knowledge", "Source-Symbols"), "*.json", SearchOption.AllDirectories))
{
    using var map = JsonDocument.Parse(File.ReadAllText(mapPath));
    var doc = map.RootElement;
    var path = doc.GetProperty("path").GetString()!;
    if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
    var raw = File.ReadAllBytes(Path.Combine(root, path));
    if (Hash(raw) != doc.GetProperty("worktree_sha256").GetString()) { errors.Add("Source changed: " + path); continue; }
    var text = Encoding.UTF8.GetString(raw).TrimStart('\uFEFF');
    var tree = CSharpSyntaxTree.ParseText(text, parseOptions, path);
    var syntaxErrors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    if (syntaxErrors.Length != 0) { errors.Add("Syntax errors: " + path); continue; }
    files++;
    var globals=tree.GetCompilationUnitRoot().Members.OfType<GlobalStatementSyntax>().ToArray();
    if(globals.Length>0)
    {
        var first=globals[0].GetLocation().GetLineSpan().StartLinePosition;
        var last=globals[^1].GetLocation().GetLineSpan().EndLinePosition;
        generated.Add(new(){["path"]=path,["source_git_blob_sha256"]=doc.GetProperty("git_blob_sha256").GetString(),
            ["worktree_sha256"]=doc.GetProperty("worktree_sha256").GetString(),["kind"]="CompilerTopLevelEntryPoint",
            ["name"]=path+"::<top-level-entry-point>",["qualifiedSymbol"]=path+"::<top-level-entry-point>",
            ["startLine"]=first.Line+1,["startColumn"]=first.Character+1,["endLine"]=last.Line+1,["endColumn"]=last.Character+1,
            ["origin_declaration_stable_id"]=null,["status"]="INDEXED_NOT_ANALYZED",
            ["scope"]="Compiler synthesized entry-point around actual global statements; not an invented source MethodDeclaration or per-callback review."});
    }
    foreach (var node in tree.GetRoot().DescendantNodesAndSelf())
    {
        var nodeSpan=node.GetLocation().GetLineSpan();
        JsonElement? Origin(SyntaxNode n)
        {
            var s=n.GetLocation().GetLineSpan();
            var hits=doc.GetProperty("symbols").EnumerateArray().Where(v=>
                v.GetProperty("kind").GetString()==n.Kind().ToString() &&
                v.GetProperty("startLine").GetInt32()==s.StartLinePosition.Line+1 &&
                v.GetProperty("startColumn").GetInt32()==s.StartLinePosition.Character+1 &&
                v.GetProperty("endLine").GetInt32()==s.EndLinePosition.Line+1 &&
                v.GetProperty("endColumn").GetInt32()==s.EndLinePosition.Character+1).ToArray();
            return hits.Length==1 ? hits[0] : null;
        }
        Dictionary<string,object?> Facet(SyntaxNode n, string kind, string suffix)
        {
            var origin=Origin(n);
            var ns=n.GetLocation().GetLineSpan();
            if(origin is null)errors.Add("Missing facet origin: "+path+":"+(ns.StartLinePosition.Line+1));
            string name=origin?.GetProperty("qualifiedSymbol").GetString() ?? origin?.GetProperty("name").GetString() ?? n.Kind().ToString();
            return new(){["path"]=path,["source_git_blob_sha256"]=doc.GetProperty("git_blob_sha256").GetString(),
                ["worktree_sha256"]=doc.GetProperty("worktree_sha256").GetString(),["kind"]=kind,["name"]=name+suffix,
                ["qualifiedSymbol"]=name+suffix,["startLine"]=ns.StartLinePosition.Line+1,["startColumn"]=ns.StartLinePosition.Character+1,
                ["endLine"]=ns.EndLinePosition.Line+1,["endColumn"]=ns.EndLinePosition.Character+1,
                ["origin_declaration_stable_id"]=origin?.GetProperty("stable_id").GetString(),["status"]="INDEXED_NOT_ANALYZED"};
        }
        ArrowExpressionClauseSyntax? arrow=node switch
        {
            PropertyDeclarationSyntax property=>property.ExpressionBody,
            IndexerDeclarationSyntax indexer=>indexer.ExpressionBody,
            _=>null
        };
        if(arrow is not null)
        {
            var f=Facet(node,node is IndexerDeclarationSyntax?"ExpressionBodiedIndexerGetter":"ExpressionBodiedPropertyGetter","::get()");
            var bs=arrow.Expression.GetLocation().GetLineSpan();
            f["body_span"]=new[]{bs.StartLinePosition.Line+1,bs.StartLinePosition.Character+1,bs.EndLinePosition.Line+1,bs.EndLinePosition.Character+1};
            f["isCallable"]=true;f["facet"]="get";facets.Add(f);
        }
        if(node is RecordDeclarationSyntax record)
        {
            var r=Facet(node,"CompilerRecordBehavior","::record-generated-behavior");
            r["positional_parameters"]=record.ParameterList?.Parameters.Select(p=>new{name=p.Identifier.Text,type=p.Type?.ToString(),modifiers=p.Modifiers.ToString()}).ToArray();
            r["scope"]="Compiler-provided record constructors/properties/equality/copy/printing behavior; no invented count of generated methods.";
            generated.Add(r);
        }
        else if(node is TypeDeclarationSyntax { ParameterList:not null } primary)
        {
            var r=Facet(node,"PrimaryConstructorBehavior","::primary-constructor-behavior");
            r["positional_parameters"]=primary.ParameterList.Parameters.Select(p=>new{name=p.Identifier.Text,type=p.Type?.ToString()}).ToArray();
            generated.Add(r);
        }
        if(node is FieldDeclarationSyntax field && field.AttributeLists.SelectMany(x=>x.Attributes).Any(a=>a.Name.ToString().EndsWith("ObservableProperty")||a.Name.ToString().EndsWith("ObservablePropertyAttribute")))
            foreach(var variable in field.Declaration.Variables)
            {
                var r=Facet(variable,"ToolkitObservablePropertyGeneratorInput","::observable-property-generator-input");
                r["scope"]="Source-generator input only; generated API/bindings/hooks need contextual review; not an individually counted authored method.";generated.Add(r);
            }
        if(node is MethodDeclarationSyntax method && method.AttributeLists.SelectMany(x=>x.Attributes).Any(a=>a.Name.ToString().EndsWith("RelayCommand")||a.Name.ToString().EndsWith("RelayCommandAttribute")))
            generated.Add(Facet(method,"ToolkitRelayCommandGeneratorInput","::relay-command-generator-input"));
        if(facetsOnly)continue;
        bool callable = node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax or AnonymousFunctionExpressionSyntax;
        if (!callable) continue;
        SyntaxNode? body = node switch
        {
            BaseMethodDeclarationSyntax n => (SyntaxNode?)n.Body ?? n.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax n => (SyntaxNode?)n.Body ?? n.ExpressionBody?.Expression,
            AccessorDeclarationSyntax n => (SyntaxNode?)n.Body ?? n.ExpressionBody?.Expression,
            AnonymousFunctionExpressionSyntax n => n.Body,
            _ => null
        };
        if (body is null) { bodyless++; continue; }
        var tokens = body.DescendantTokens().ToArray();
        if (tokens.Length < 20) { small++; continue; }
        // Keep identifiers and literals; discard trivia only. This is lexical, not semantic equivalence.
        var fingerprint = Hash(Encoding.UTF8.GetBytes(string.Join("\0", tokens.Select(t => t.RawKind + ":" + t.Text))));
        var span = node.GetLocation().GetLineSpan();
        var match = doc.GetProperty("symbols").EnumerateArray().Where(s =>
            s.GetProperty("kind").GetString() == node.Kind().ToString() &&
            s.GetProperty("startLine").GetInt32() == span.StartLinePosition.Line + 1 &&
            s.GetProperty("startColumn").GetInt32() == span.StartLinePosition.Character + 1 &&
            s.GetProperty("endLine").GetInt32() == span.EndLinePosition.Line + 1 &&
            s.GetProperty("endColumn").GetInt32() == span.EndLinePosition.Character + 1).ToArray();
        if (match.Length != 1) { errors.Add("Missing exact stable symbol: " + path + ":" + (span.StartLinePosition.Line + 1)); continue; }
        records.Add(new()
        {
            ["path"] = path, ["stable_id"] = match[0].GetProperty("stable_id").GetString(),
            ["kind"] = node.Kind().ToString(), ["source_git_blob_sha256"] = doc.GetProperty("git_blob_sha256").GetString(),
            ["start_line"] = span.StartLinePosition.Line + 1, ["end_line"] = span.EndLinePosition.Line + 1,
            ["token_count"] = tokens.Length, ["body_token_sha256"] = fingerprint,
            ["constructor_initializer_excluded"] = node is ConstructorDeclarationSyntax { Initializer: not null },
            ["category"] = path.Contains("/Migrations/") ? "GENERATED_MODEL_OR_MIGRATION" : path.StartsWith("tests/") ? "TEST_SOURCE" : "APPLICATION_OR_TOOL_SOURCE"
        });
    }
}
if(facetsOnly)
{
    var folder=Path.Combine(work,"Knowledge","Source-Symbols-CallableFacets");Directory.CreateDirectory(folder);
    var target=Path.Combine(folder,"ast-facets.raw.json");var temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
    File.WriteAllText(temp,JsonSerializer.Serialize(new{at_utc=DateTimeOffset.UtcNow,files,facets,generated_behavior_inputs=generated,errors},new JsonSerializerOptions{WriteIndented=true}),new UTF8Encoding(false));
    using(JsonDocument.Parse(File.ReadAllText(temp))){}
    File.Move(temp,target,true);
    Console.WriteLine(JsonSerializer.Serialize(new{files,expression_getter_facets=facets.Count,generated_behavior_inputs=generated.Count,errors}));
    if(errors.Count!=0)Environment.ExitCode=1;
    return;
}
var groups = records.GroupBy(r => (string)r["body_token_sha256"]!).Where(g => g.Count() > 1)
    .OrderByDescending(g => (int)g.First()["token_count"]! * g.Count())
    .Select(g => new { body_token_sha256 = g.Key, token_count = g.First()["token_count"], occurrences = g.ToArray(), review_status = "CANDIDATE_NOT_SEMANTICALLY_VERIFIED" }).ToArray();
var output = new
{
    at_utc = DateTimeOffset.UtcNow, baseline = "c35816ecae49633674775dd32061c6ee92f94aaf", parser = "Roslyn syntax AST, configured Release NET8 WINDOWS",
    csharp_files_parsed = files, executable_body_records_at_least20_tokens = records.Count, bodyless_callables = bodyless,
    bodies_below20_tokens = small, candidate_groups = groups.Length, errors,
    limits = new[] { "Candidate lexical body equivalence only: signatures, types, bindings, captured context, constructor initializers and surrounding lifecycle may differ.",
        "Generated model repetitions and test helpers are classified separately; no refactor/defect or function-review promotion inferred.",
        "Bodies under20 tokens and inactive preprocessor regions are not examined for clone grouping; this is not exhaustive semantic duplicate detection.",
        "Only source parsed; no POS assembly, handler, database, printer or runtime workload executed." }, groups
};
var directory = Path.Combine(work, "Deep-Audit", "Duplication-and-Architecture");
Directory.CreateDirectory(directory);
var destination = Path.Combine(directory, "ast-duplicate-candidates.json");
var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
File.WriteAllText(temporary, JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
using (JsonDocument.Parse(File.ReadAllText(temporary))) { }
File.Move(temporary, destination, overwrite: true);
Console.WriteLine(JsonSerializer.Serialize(new { files, records = records.Count, bodyless, small, candidate_groups = groups.Length, errors }));
if (errors.Count != 0) Environment.ExitCode = 1;
