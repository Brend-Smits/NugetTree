using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NugetTree.Core;
using NugetTree.Models;

namespace NugetTree
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            string targetSolution = default;
            List<OutputType> outputs = new List<OutputType>();
            foreach (string arg in args)
                if (arg.StartsWith("-"))
                    switch (arg)
                    {
                        case "-c":
                            outputs.Add(OutputType.Conflicts);
                            break;

                        case "-f":
                            outputs.Add(OutputType.Flat);
                            break;

                        case "-t":
                            outputs.Add(OutputType.Tree);
                            break;

                        case "-nr":
                            outputs.Add(OutputType.TopLevel);
                            break;

                        case "-fv":
                            outputs.Add(OutputType.Frameworks);
                            break;
                    }
                else
                    targetSolution = arg;

            if (string.IsNullOrEmpty(targetSolution))
            {
                Console.WriteLine("You must provide a path to a solution file");
                return;
            }

            if (!File.Exists(targetSolution))
            {
                Console.WriteLine($"Solution not found {targetSolution}");
                return;
            }

            if (!outputs.Any())
            {
                Console.WriteLine("You must provide at least one output");
                return;
            }

            IDependencyProcessor processor =
                await DependencyProcessorFactory.CreateDependencyProcessorAsync(LogLevel.Warning);

            await processor.ProcessSolution(targetSolution, outputs.ToArray());
        }
    }
}