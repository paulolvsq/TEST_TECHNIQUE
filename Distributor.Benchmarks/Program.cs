using System.Reflection;
using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());

switcher.Run(args);
