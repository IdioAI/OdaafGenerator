using OdaafGenerator.Core;

var dry = args.Contains("--dry");
var repair = args.Contains("--repair");
var projectFile = GeneratorProject.FindDefaultFile()
    ?? throw new FileNotFoundException("找不到 rapidcad-zextract.ogproj。");
var project = GeneratorProject.Load(projectFile);
var root = Path.GetDirectoryName(project.FilePath) ?? ".";
var notesPath = Path.Combine(root, "notes", "accepted-flow-notes.json");
var logPath = Path.Combine(root, "notes", "luxray-fill.log");
Directory.CreateDirectory(Path.GetDirectoryName(notesPath)!);
var notes = AcceptedNotes.Read(notesPath);
FlowNotes.ReplaceAccepted(notes);

var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
var catalog = Catalog.Create(projects, rows, host);
if (repair)
{
    var known = NameFill.Collect(catalog, project.SourceRoot, onlyMissing: false).Targets
        .ToDictionary(item => item.Call.Name, item => item.Call, StringComparer.Ordinal);
    var changed = 0;
    foreach (var raw in File.ReadAllLines(logPath))
    {
        if (!known.TryGetValue(raw.Split('\t')[0].Trim().Split(' ').Last(), out var call))
            continue;
        if (!notes.TryGetValue(call.Name, out var stored))
            continue;
        var parsed = NameProposals.Parse(raw, [call]);
        if (parsed.Count == 0)
            continue;
        var proposal = parsed[0];
        var title = stored.Title;
        var purpose = stored.Purpose;
        var parameters = stored.Parameters;
        var touch = false;
        if (proposal.Title.Length > title.Length && proposal.Title.StartsWith(title, StringComparison.Ordinal))
        {
            title = proposal.Title;
            touch = true;
        }

        if (purpose.Contains('=') && !proposal.Purpose.Contains('='))
        {
            purpose = proposal.Purpose;
            touch = true;
        }

        if (proposal.Parameters.Count > parameters.Count)
        {
            parameters = proposal.Parameters;
            touch = true;
        }

        if (!touch)
            continue;
        notes[call.Name] = new FlowNotes.Note(purpose, parameters, "", title);
        changed++;
    }

    var redo = notes.Where(pair => pair.Value.Purpose.Contains('=') && !pair.Value.Purpose.Contains('。'))
        .Select(pair => pair.Key)
        .ToList();
    foreach (var key in redo)
        notes.Remove(key);
    AcceptedNotes.Write(notesPath, notes);
    Console.WriteLine($"已按日志补全 {changed} 条，说明仍是参数表的 {redo.Count} 条留待重问。");
    return;
}

var plan = NameFill.Collect(catalog, project.SourceRoot);
Log(logPath, $"待填 {plan.Targets.Count}，同名冲突跳过 {plan.Ambiguous.Count}。");
foreach (var name in plan.Ambiguous)
    Log(logPath, "冲突 " + name);
if (dry)
{
    foreach (var item in plan.Targets)
        Console.WriteLine($"{item.Handler}\t{item.Call.Name}");
    return;

}

var saved = 0;
var failed = 0;
var batch = 0;
foreach (var group in plan.Targets.GroupBy(item => item.Handler))
{
    var calls = group.Select(item => item.Call).ToList();
    for (var offset = 0; offset < calls.Count; offset += 6)
    {
        var slice = calls.Skip(offset).Take(6).ToList();
        batch++;
        var prompt = NameProposals.Prompt(group.Key, NameFill.SkillFor(group.Key), project.SourceRoot, slice);
        var asked = await Ask(slice, prompt);
        if (asked.Proposals is null)
        {
            asked = await Ask(slice, prompt);
        }

        Log(logPath, $"批次 {batch} {group.Key} {slice.Count} 个：{asked.Error ?? "ok " + asked.Proposals!.Count}");
        if (!string.IsNullOrEmpty(asked.Raw))
            Log(logPath, asked.Raw);
        if (asked.Proposals is null)
        {
            failed += slice.Count;
            continue;
        }

        foreach (var proposal in asked.Proposals)
        {
            if (notes.ContainsKey(proposal.MethodName))
                continue;
            var call = slice.First(item => item.Name == proposal.MethodName);
            var prior = FlowNotes.Find(call.SyntaxPath, call.Name);
            if (!string.IsNullOrWhiteSpace(prior?.Title) || !string.IsNullOrWhiteSpace(prior?.Purpose))
                continue;
            notes[proposal.MethodName] = new FlowNotes.Note(
                proposal.Purpose,
                proposal.Parameters,
                "",
                proposal.Title);
            saved++;
        }

        AcceptedNotes.Write(notesPath, notes);
        FlowNotes.ReplaceAccepted(notes);
    }
}

Log(logPath, $"写入 {saved} 条，失败 {failed} 个。文件 {notesPath}");

static async Task<NameProposals.AskResult> Ask(IReadOnlyList<DirectCall> calls, string prompt)
{
    var model = Environment.GetEnvironmentVariable("ODAAF_LOCAL_MODEL");
    var endpoint = Environment.GetEnvironmentVariable("ODAAF_LOCAL_LLM");
    return await NameProposals.TryAskAsync(
        string.IsNullOrWhiteSpace(endpoint) ? NameProposals.LuxrayChat : endpoint,
        string.IsNullOrWhiteSpace(model) ? NameProposals.LuxrayModel : model,
        prompt,
        calls,
        CancellationToken.None);
}

static void Log(string path, string line)
{
    Console.WriteLine(line);
    File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss") + " " + line + Environment.NewLine);
}
