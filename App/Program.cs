using App;
using Microsoft.Extensions.DependencyInjection;
using StringsGenerator;
using StringsSorter;
using System.Diagnostics;

// I would add to the project:
// Logging, Error handling
// CancellationToken for graceful completion of asynchronous methods
// To cover more cases with unit tests


IServiceCollection services = new ServiceCollection();
using var provider = services.ConfigureApp().BuildServiceProvider();

Stopwatch stopwatch = Stopwatch.StartNew();

// Run Generator
Console.WriteLine("Start.");
var generator = provider.GetRequiredService<Generator>();
await generator.GenerateAsync();
Console.WriteLine($"File was generated. Timestamp: {stopwatch.ElapsedMilliseconds / 1000.0}.");

// Run Sorter
var sorter = provider.GetRequiredService<Sorter>();
List<string> chunks = sorter.GetOrCreateChunks();
Console.WriteLine($"Chunks were created. Timestamp: {stopwatch.ElapsedMilliseconds / 1000.0}.");

await sorter.MergeSortedChunksAsync(chunks);
Console.WriteLine($"Sorted chunks were merged. Timestamp: {stopwatch.ElapsedMilliseconds / 1000.0}.{Environment.NewLine}Finish.");
sorter.RemoveChunks();

stopwatch.Stop();