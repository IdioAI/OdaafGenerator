using Microsoft.Data.Sqlite;

namespace OdaafGenerator.Core;

public static class DeclarationStore
{
    public const string DefaultDatabasePath = @"g:\myfuture\CodeSliceIndex\index\bheditor.db";

    public static readonly string[] HostProjects = ["BHEditor"];

    public static (IReadOnlyList<string> Projects, IReadOnlyList<RawDeclaration> FocusRows, IReadOnlyList<RawDeclaration> HostRows) Load(string databasePath)
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("找不到代码索引数据库。", databasePath);

        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();

        var projects = new List<string>();
        using (var names = connection.CreateCommand())
        {
            names.CommandText = "SELECT DISTINCT project_name FROM files ORDER BY project_name;";
            using var reader = names.ExecuteReader();
            while (reader.Read())
                projects.Add(reader.GetString(0));
        }

        var rows = ReadProjects(connection, XmlScene.FocusProjects);
        var host = ReadProjects(connection, HostProjects);
        return (projects, rows, host);
    }

    private static List<RawDeclaration> ReadProjects(SqliteConnection connection, IReadOnlyList<string> projectNames)
    {
        var rows = new List<RawDeclaration>();
        using var command = connection.CreateCommand();
        var names = projectNames.Select((_, index) => $"$p{index}").ToArray();
        command.CommandText =
            $"""
            SELECT d.file_path, d.start_byte, d.parent_start_byte, d.kind, d.syntax_path,
                   d.signature_text, d.start_line, d.end_line, f.project_name
            FROM declarations d
            JOIN files f ON f.file_path = d.file_path
            WHERE f.project_name IN ({string.Join(", ", names)});
            """;
        for (var i = 0; i < projectNames.Count; i++)
            command.Parameters.AddWithValue(names[i], projectNames[i]);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new RawDeclaration(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetInt32(6),
                reader.GetInt32(7),
                reader.GetString(8)));
        }

        return rows;
    }
}
