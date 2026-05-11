using FsEvdTools.Gen.Properties;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FsEvdTools.Gen;

internal class Program
{
    private static string INPUT_DIR;
    private static string OUTPUT_DIR;

    static void Main(string[] args)
    {
        INPUT_DIR = args[0];
        OUTPUT_DIR = args[1];
        if (Directory.Exists(OUTPUT_DIR))
            Directory.Delete(OUTPUT_DIR, true);
        Directory.CreateDirectory(OUTPUT_DIR);

        Generate("ac6-common.emedf.json", "EvdActionBuilderArmoredCore6");
        Generate("bb-common.emedf.json", "EvdActionBuilderBloodborne");
        Generate("ds1-common.emedf.json", "EvdActionBuilderDarkSouls");
        Generate("ds2-common.emedf.json", "EvdActionBuilderDarkSouls2");
        Generate("ds2scholar-common.emedf.json", "EvdActionBuilderDarkSouls2Scholar");
        Generate("ds3-common.emedf.json", "EvdActionBuilderDarkSouls3");
        Generate("er-common.emedf.json", "EvdActionBuilderEldenRing");
        Generate("nr-common.emedf.json", "EvdActionBuilderNightreign");
        Generate("sekiro-common.emedf.json", "EvdActionBuilderSekiro");
    }

    private static void Generate(string json, string name)
    {
        var emedf = LoadEmedf(Path.Join(INPUT_DIR, json));
        var actions = new List<string>();
        foreach (var category in emedf.main_classes)
        {
            foreach (var action in category.instrs)
            {
                var readProperties = new List<string>();
                var readStatements = new List<string>();
                var writeParameters = new List<string>();
                var writeArguments = new List<string>();
                int position = 0;
                for (int i = 0; i < action.args.Count; i++)
                {
                    EmedfDark.EmArgument arg = action.args[i];
                    string type = arg.type switch
                    {
                        0 => "byte",
                        1 => "ushort",
                        2 => "uint",
                        3 => "sbyte",
                        4 => "short",
                        5 => "int",
                        6 => "float",
                        8 => "int",
                        _ => throw new NotImplementedException()
                    };
                    if (arg.vararg)
                    {
                        writeParameters.Add($"params object[] {arg.name}");
                        writeArguments.Add($".. {arg.name}");
                    }
                    else
                    {
                        int argSize = type switch
                        {
                            "sbyte" or "byte" => 1,
                            "short" or "ushort" => 2,
                            "int" or "uint" or "float" => 4,
                            _ => throw new NotImplementedException()
                        };
                        string readMethod = type switch
                        {
                            "sbyte" => "ReadSByte",
                            "byte" => "ReadByte",
                            "short" => "ReadInt16",
                            "ushort" => "ReadUInt16",
                            "int" => "ReadInt32",
                            "uint" => "ReadUInt32",
                            "float" => "ReadSingle",
                            _ => throw new NotImplementedException()
                        };
                        readProperties.Add($"public {type} {arg.name} {{ get; }}");
                        if (position % argSize != 0)
                        {
                            readStatements.Add($"br.Align({argSize});");
                            position += argSize - (position % argSize);
                        }
                        readStatements.Add($"{arg.name} = br.{readMethod}();");
                        writeParameters.Add($"{type} {arg.name}");
                        writeArguments.Add(arg.name);
                        position += argSize;
                    }
                }
                actions.Add(FormatEvdActionBuilderAction(category.index, action.index, action.name, readProperties, readStatements, writeParameters, writeArguments));
            }
        }
        string builder = FormatEvdActionBuilder(name, actions);
        File.WriteAllText(Path.Join(OUTPUT_DIR, $"{name}.cs"), builder);
    }

    private static EmedfDark LoadEmedf(string path)
    {
        string json = File.ReadAllText(path);
        var options = new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        var emedf = JsonSerializer.Deserialize<EmedfDark>(json, options);
        foreach (var category in emedf.main_classes)
        {
            foreach (var action in category.instrs)
            {
                action.name = NormalizeName(action.name, true);
                action.args.ForEach(arg => arg.name = NormalizeName(arg.name, false));
                var ambiguous = action.args.GroupBy(arg => arg.name).Where(group => group.Count() > 1);
                foreach (var group in ambiguous)
                {
                    var args = group.ToArray();
                    for (int i = 0; i < args.Length; i++)
                        args[i].name += $"_{i}";
                }

                if (action.args.Count > 1 && action.args[..^1].Any(arg => arg.vararg))
                    throw new InvalidDataException("Varargs found before final argument.");
            }
        }
        return emedf;
    }

    private static string NormalizeName(string name, bool titleCase)
    {
        var words = Regex.Split(name, @"[^\w\d]")
            .Select(word => word.Trim())
            .Where(word => word != "")
            .Select(word => $"{char.ToUpper(word[0])}{word[1..].ToLower()}");
        name = string.Concat(words);
        if (!titleCase)
            name = $"{char.ToLower(name[0])}{name[1..]}";
        return name;
    }

    private static string FormatEvdActionBuilder(string name, IEnumerable<string> actions)
    {
        return string.Format(Resources.EvdActionBuilderTemplate,
            name,
            string.Join("\n\n", actions)
            );
    }

    private static string FormatEvdActionBuilderAction(int categoryId, int actionId, string name, IEnumerable<string> readProperties, IEnumerable<string> readStatements, IEnumerable<string> writeParameters, IEnumerable<string> writeArguments)
    {
        return string.Format(Resources.EvdActionBuilderActionTemplate,
            categoryId,
            actionId,
            name,
            string.Join("\n\t\t", readProperties),
            string.Join("\n\t\t\t", readStatements),
            string.Join(", ", writeParameters),
            string.Join(", ", writeArguments)
            );
    }
}
