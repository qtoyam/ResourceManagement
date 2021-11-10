using BenchmarkDotNet.Running;

namespace Benchmarks
{
	class Program
	{
		static void Main()
		{
			try
			{
				BenchmarkRunner.Run<BenchmarkRead>();
			}
			finally
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("End of benchmark, enter any line...");
				Console.ReadLine();
			}
		}
	}
}
