using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using KetoAnalyzer;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//Fetch data root path. KenoCurrentYear and KenoPastYears extracted directories should be inside.
builder.Configuration.AddUserSecrets<Program>();

string dataRootPath = builder.Configuration["DATA_ROOT_PATH"]
    ?? throw new KeyNotFoundException("Directory not found");

var latexBuilder = FrequenciesAnalysis.RunAnalysis(dataRootPath);
latexBuilder = MonteCarloSimulation.RunSimulation(latexBuilder);


File.WriteAllText("kenoAnalysis.tex", latexBuilder.ToString());
