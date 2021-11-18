using BenchmarkDotNet.Attributes;

using RMReader;

namespace Benchmarks
{
	[MemoryDiagnoser]
	//[ReturnValueValidator(true)]
	[BenchmarkDotNet.Diagnostics.Windows.Configs.NativeMemoryProfiler]
	[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
	[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput, targetCount: 10, invocationCount: 128)]
	public class BenchmarkRead
	{
	}
}
