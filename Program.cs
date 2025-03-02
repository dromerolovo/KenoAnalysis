using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using CsvHelper;
using System.Globalization;
using System.Collections.Concurrent;
using System.Text;
using MathNet.Numerics.Distributions;


#region Set up

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//Fetch data root path. KenoCurrentYear and KenoPastYears extracted directories should be inside.
builder.Configuration.AddUserSecrets<Program>();

string dataRootPath = builder.Configuration["DATA_ROOT_PATH"]
    ?? throw new KeyNotFoundException("Directory not found");

string[] files = Directory.GetFiles(dataRootPath, "*", new EnumerationOptions
{
    RecurseSubdirectories = true
});

ConcurrentDictionary<int, int> frequencies = new ConcurrentDictionary<int, int>();
int rowsCounter = 0;

#endregion
#region Calc

Parallel.ForEach(files, file =>
{
    using (var reader = new StreamReader(file))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        csv.Read();
        csv.ReadHeader();

        int columns = csv.HeaderRecord.Length;

        while (csv.Read())
        {
            Interlocked.Increment(ref rowsCounter);

            for (int i = 4; i < 24; i++)
            {
                int number = csv.GetField<int>(i);

                frequencies.AddOrUpdate(number, 1, (key, oldValue) => Interlocked.Increment(ref oldValue));
            }
        }
    }
});

Dictionary<int, double> frequenciesPercentage = new Dictionary<int, double>();

foreach (var kv in frequencies)
{
    frequenciesPercentage[kv.Key] = Math.Round((double)kv.Value / rowsCounter, 5);
}

var sortedDict = from entry in frequenciesPercentage orderby entry.Value descending select entry;

double expectedFrequency = rowsCounter * 20 / 80;
double chiSquared = 0;

foreach (var kv in frequencies)
{
    var observedFrequency = kv.Value;
    chiSquared += Math.Pow(observedFrequency - expectedFrequency, 2) / expectedFrequency;
}

double pValue = ChiSquared.CDF(79, chiSquared);
#endregion
#region LatexBuilder
var latexBuilder = new StringBuilder();

var header = @"
\documentclass{article}
\usepackage{booktabs}
\usepackage{geometry}
\usepackage{amsmath}
\usepackage{supertabular}
\usepackage{multicol}
\geometry{margin=0in}

\begin{document}
\vspace{10cm}

\twocolumn
\centering
\tablehead{Number & Relative Frequency \\}
\begin{supertabular}{cc}
\toprule
\midrule
";

latexBuilder.AppendLine(header);

foreach (var kv in sortedDict)
{
    latexBuilder.AppendLine($"{kv.Key} & {kv.Value} \\\\");
}

latexBuilder.AppendLine(@"\bottomrule");
latexBuilder.AppendLine($"\\multicolumn{{2}}{{l}}{{D = {rowsCounter}; \\, N = 80;}}  \\\\");
latexBuilder.AppendLine(@"\multicolumn{2}{l}{k = 20} \\");
latexBuilder.AppendLine($"\\multicolumn{{2}}{{l}}{{$\\chi^2 = {Math.Round(chiSquared, 4)}$}} \\\\");
latexBuilder.AppendLine($"\\multicolumn{{2}}{{l}}{{\\text{{p - value}} = {Math.Round(pValue, 4)}}} \\\\");
latexBuilder.AppendLine(@"\end{supertabular}");
latexBuilder.AppendLine(@"\end{document}");

#endregion

File.WriteAllText("frequencies.tex", latexBuilder.ToString());