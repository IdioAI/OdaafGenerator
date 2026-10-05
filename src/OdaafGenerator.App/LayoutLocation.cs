using System.IO;

namespace OdaafGenerator.App;

public static class LayoutLocation
{
    public static string DefaultFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "OdaafGenerator.sln")))
            {
                var layouts = Path.Combine(dir.FullName, "layouts");
                Directory.CreateDirectory(layouts);
                return Path.Combine(layouts, "rapidcad-zextract-xml.json");
            }

            dir = dir.Parent;
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OdaafGenerator",
            "layouts");
        Directory.CreateDirectory(fallback);
        return Path.Combine(fallback, "rapidcad-zextract-xml.json");
    }
}
