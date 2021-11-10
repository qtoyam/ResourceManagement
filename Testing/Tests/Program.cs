
using RMReader;

using RMWriter;

namespace Tests
{
	class Program
	{
		static void Main()
		{
			var outResourcePath = @"D:\TestCore_testfolder\res1";
			var r = new Random(42);
			for (int i = 0; i < 512; i++)
			{
				var d1 = new byte[r.Next(1, 32)]; r.NextBytes(d1);
				var d2 = new byte[r.Next(1, 2_097_152)]; r.NextBytes(d2);
				var d1n = RandStr(1, 128, r);
				var d2n = RandStr(1, 128, r);
				{
					var rp = new ResourcePacker();
					rp.Resources.Add(d1n, d1);
					rp.Resources.Add(d2n, d2);
					rp.SaveTo(outResourcePath);
				}
				using (var ru = new ResourceUnpacker(outResourcePath))
				{
					ru.ReadNames();
					var d1_fromRU = ru.Read(d1n);
					if (!d1.AsSpan().SequenceEqual(d1_fromRU)) { Console.WriteLine($"d1 not equals, i:{i}"); return; }
					var d2_fromRU = ru.Read(d2n);
					if (!d2.AsSpan().SequenceEqual(d2_fromRU)) { Console.WriteLine($"d2 not equals, i:{i}"); return; }
				}
				Console.WriteLine($"Pass i:{i}");
			}


			Console.WriteLine("End, enter any line..");
			Console.ReadLine();
		}

		static string RandStr(int minL, int maxL, Random r) =>
			new(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", r.Next(minL, maxL)).Select(s => s[r.Next(s.Length)]).ToArray());
	}
}
