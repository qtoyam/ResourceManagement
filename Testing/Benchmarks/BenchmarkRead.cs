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
		/*Data sizes:
		 * data1 = ?
		 * data2 = ?
		 */

		private readonly string data_name = "data2";
		private readonly string path_resource = @"D:\TestCore_testfolder\res1";

		[Benchmark(Baseline = true)]
		public void Read()
		{
			using (var ru = new ResourceUnpacker(path_resource))
			{
				ru.ReadNames();
				ru.Read(data_name);
			}
		}

		[Benchmark]
		public async Task ReadAsync()
		{
			using (var ru = new ResourceUnpacker(path_resource))
			{
				await ru.ReadNamesAsync();
				await ru.ReadAsync(data_name);
			}
		}
	}
}
