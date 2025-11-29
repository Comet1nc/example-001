namespace DbMetaTool;

public class SqlScriptsUtil
{
    public static IEnumerable<string> GetScriptsPathFromDirectory(string directory)
    {
        var files = Directory.GetFiles(directory, "*.sql")
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            throw new FileNotFoundException("Brak skryptów .sql do wykonania.");
        }

        return files;
    }
}