using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using RMReader;

using RMWriter;

namespace UnitTests
{
	public class UnitTests
	{
		//[SetUp]
		//public void Setup()
		//{
		//}

		const string TestDirectory = @"D:\CS_tests\rmwriter";
		readonly string TestOutputFile = Path.Combine(TestDirectory, "test_resource.r");
		//file structure
		//	header:
		//		count - int32
		const int COUNT_L = sizeof(int);
		//		data_size[] - int32[count-1] //except last
		const int DATA_SIZE_L = sizeof(int);
		//	data[] - (byte[])[count]

		[Test]
		public async Task ResourceManagerSlim_Pack()
		{
			if (File.Exists(TestOutputFile)) File.Delete(TestOutputFile);
			var rs = Directory.EnumerateFiles(TestDirectory).Select(f => new FileInfo(f)).ToArray();
			int totSize = (int)rs.Sum(f => f.Length) +
				(rs.Length - 1) * DATA_SIZE_L +
				COUNT_L;

			await ResourcePacker.SaveToSlim(TestOutputFile, rs);

			Assert.AreEqual(totSize, new FileInfo(TestOutputFile).Length);
			using (var mem = new MemoryStream(totSize))
			using (var bw = new BinaryWriter(mem))
			{
				bw.Write(rs.Length);
				foreach (var r in rs.SkipLast(1).Select(x => (int)x.Length))
				{
					bw.Write(r);
				}
				foreach (var r in rs)
				{
					await using (var fs = r.OpenRead())
					{
						await fs.CopyToAsync(mem);
					}
				}
				var manual_buff = mem.GetBuffer();
				var lib_buff = await File.ReadAllBytesAsync(TestOutputFile);
				AreArraysEqual(manual_buff, lib_buff);
			}
		}

		[Test]
		public async Task ResourceManagerSlim_Pack_Unpack()
		{
			if (File.Exists(TestOutputFile)) File.Delete(TestOutputFile);
			var rs = Directory.EnumerateFiles(TestDirectory).Select(f => new FileInfo(f)).ToArray();

			await ResourcePacker.SaveToSlim(TestOutputFile, rs);
			await using(var rup = new ResourceUnpackerSlim())
			{
				await rup.InitAsync(TestOutputFile);
				Assert.AreEqual(rup.Count, rs.Length);
				Memory<byte> buff = new byte[rup.MaxFileLength];
				for (int i = 0; i < rup.Count; i++)
				{
					var br = await rup.ReadAsync(i, buff);
					var lib_buff = buff[..br];
					var manual_buff = await File.ReadAllBytesAsync(rs[i].FullName);
					AreArraysEqual(manual_buff, lib_buff.Span);
				}
			}
		}

		private static void AreArraysEqual(Span<byte> expected, Span<byte> actual)
		{
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
			{
				if (expected[i] != actual[i])
				{
					Assert.Fail($"Wrong value at index {i},\nexpected: {expected[i]},\nbut was: {actual[i]}");
				}
			}
		}
	}
}