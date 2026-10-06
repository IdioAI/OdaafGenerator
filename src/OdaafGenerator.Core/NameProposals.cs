using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OdaafGenerator.Core;

public sealed class NameProposal : System.ComponentModel.INotifyPropertyChanged
{
    private bool _accept;

    public NameProposal(string methodName, string title, string purpose, IReadOnlyDictionary<string, string> parameters)
    {
        MethodName = methodName;
        Title = title;
        Purpose = purpose;
        Parameters = parameters;
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string MethodName { get; }
    public string Title { get; set; }
    public string Purpose { get; set; }
    public IReadOnlyDictionary<string, string> Parameters { get; }

    public bool Accept
    {
        get => _accept;
        set
        {
            if (_accept == value)
                return;
            _accept = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Accept)));
        }
    }
}

public static class NameProposals
{
    public const string LuxrayChat = "http://ai.luxray.hk.cn/v1/chat/completions";
    public const string LuxrayModel = "qwen3-coder-next";
    public const string LuxrayKey = "luxray";

    public static IReadOnlyList<DirectCall> Missing(CallLayerGraph layer)
    {
        return layer.Calls.Where(call =>
        {
            if (call.Kind == CallKind.System)
                return false;
            var note = FlowNotes.Find(call.SyntaxPath, call.Name);
            return string.IsNullOrWhiteSpace(note?.Title) && string.IsNullOrWhiteSpace(note?.Purpose);
        }).ToList();
    }

    public static NameProposal FromDoc(DirectCall call)
    {
        var doc = XmlDocComments.Read(call.FilePath, call.StartLine, call.Name);
        var purpose = FirstSentence(doc.Summary);
        return new NameProposal(call.Name, ShortTitle(purpose), purpose, doc.Params);
    }

    public static string Prompt(string handler, string? skill, string sourceRoot, IReadOnlyList<DirectCall> calls)
    {
        var lines = new List<string>
        {
            "为下面每个 C# 方法给一个四到十二个字的中文短名，和一句功能说明。短名要把词说完。",
            "只输出 TSV，列是：方法名、短名、说明、参数。参数列写成 name=含义，多个用分号隔开。没有参数就留空。",
            "每一行的方法名必须与 --- 后面那一行完全相同。",
            "不要解释，不要代码块。短名不要重复方法的英文名。",
            "这些调用出现在：" + handler
        };
        if (!string.IsNullOrWhiteSpace(skill))
        {
            lines.Add("这个菜单的说明：");
            lines.Add(skill);
        }

        foreach (var call in calls)
        {
            lines.Add("---");
            lines.Add(call.Name);
            lines.Add(call.Signature ?? "");
            var slice = Slice(sourceRoot, call);
            lines.Add(string.IsNullOrWhiteSpace(slice) ? "（没有源码正文。不要编造参数。）" : slice);
        }

        lines.Add("本批只能使用这些方法名，一个字都不要改：" + string.Join("、", calls.Select(call => call.Name)));
        return string.Join('\n', lines);
    }

    public static string Slice(string sourceRoot, DirectCall call)
    {
        var path = CallLayerGraph.ResolveSource(sourceRoot, call.FilePath ?? "");
        if (path is null)
            return "";
        var text = File.ReadAllLines(path);
        var start = Math.Max(0, call.StartLine - 1);
        var end = Math.Min(text.Length, start + 70);
        return end > start ? string.Join('\n', text[start..end]) : "";
    }

    public static IReadOnlyList<NameProposal> Parse(string text, IReadOnlyList<DirectCall> calls)
    {
        var known = calls.ToDictionary(call => call.Name, StringComparer.Ordinal);
        var result = new List<NameProposal>();
        var body = text.Replace("\r", "");
        var think = body.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (think >= 0)
            body = body[(think + "</think>".Length)..];
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("```", StringComparison.Ordinal))
                continue;
            if (line.Length > 9 && line[2] == ':' && line[5] == ':')
                line = line[9..].TrimStart();
            if (!line.Contains('\t') && line.Contains('|'))
                line = string.Join('\t', line.Split('|', StringSplitOptions.TrimEntries));
            var parts = line.Split('\t');
            if (parts.Length < 3 || !known.TryGetValue(parts[0].Trim(), out var call))
                continue;
            var title = parts[1].Trim().Trim('「', '」', '"');
            if (title.Length > 16)
                title = title[..16];
            if (title.Length == 0 || title.Equals(call.Name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!title.Any(ch => ch is >= '\u4e00' and <= '\u9fff'))
                continue;
            var purposeText = parts[2];
            var paramText = parts.Length > 3 ? parts[3] : "";
            var splitAt = purposeText.IndexOf('；');
            if (splitAt < 0)
                splitAt = purposeText.IndexOf(';');
            if (splitAt > 0 && purposeText[(splitAt + 1)..].Contains('='))
            {
                paramText = purposeText[(splitAt + 1)..] + ";" + paramText;
                purposeText = purposeText[..splitAt];
            }

            var allowed = SignatureParser.Parameters(call.Signature).Select(leaf => leaf.Name).ToHashSet(StringComparer.Ordinal);
            var meanings = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var piece in paramText.Split([';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                    var eq = piece.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    var key = piece[..eq].Trim();
                    if (allowed.Contains(key))
                        meanings[key] = piece[(eq + 1)..].Trim();
            }

            result.Add(new NameProposal(call.Name, title, FirstSentence(purposeText), meanings));
        }

        return result;
    }

    public sealed record AskResult(IReadOnlyList<NameProposal>? Proposals, string? Error, string Raw);

    public static async Task<AskResult> TryAskAsync(
        string endpoint,
        string model,
        string prompt,
        IReadOnlyList<DirectCall> calls,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (endpoint.Contains("luxray", StringComparison.OrdinalIgnoreCase))
            {
                var key = Environment.GetEnvironmentVariable("LUXRAY_API_KEY");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    string.IsNullOrWhiteSpace(key) ? LuxrayKey : key);
            }

            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.2,
                    max_tokens = 4000
                }),
                Encoding.UTF8,
                "application/json");
            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new AskResult(null, ((int)response.StatusCode) + " " + Trim(payload), payload);
            using var document = JsonDocument.Parse(payload);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
            var parsed = Parse(content, calls);
            return parsed.Count == 0
                ? new AskResult(null, "模型有文字，但没有对上方法名。", content)
                : new AskResult(parsed, null, content);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return new AskResult(null, ex.Message, "");
        }
    }

    private static string Trim(string text) =>
        text.Length <= 240 ? text : text[..240];

    private static string FirstSentence(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return "";
        var stop = trimmed.IndexOf('。');
        return stop < 0 ? trimmed : trimmed[..(stop + 1)];
    }

    private static string ShortTitle(string purpose)
    {
        var text = purpose.Trim().TrimEnd('。');
        if (text.Length == 0)
            return "";
        return text.Length <= 8 ? text : text[..8];
    }
}
