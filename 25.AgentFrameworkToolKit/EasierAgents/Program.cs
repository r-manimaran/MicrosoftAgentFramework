
using EasierAgents;

Console.Clear();
Utils.WriteLineSuccess("Before");
await Before.RunAsync();


Utils.Separator();

Utils.WriteLineSuccess("After");
await After.RunAsync();