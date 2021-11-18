using System;
using System.Buffers;

namespace RMWriter
{
	public class ResourcePacker
	{

		//file structure
		//	header:
		//		count - int32
		private const int COUNT_L = sizeof(int);
		//		data_size[] - int32[count-1] //except last
		private const int DATA_SIZE_L = sizeof(int);
		//	data[] - (byte[])[count]

		public static async Task SaveToSlim(string path, IReadOnlyCollection<FileInfo> resources)
		{
			if (resources.Count < 1) throw new ArgumentException("No resources to save.", nameof(resources));
			const int MIN_BUFFER_LENGTH = 4096 * 20;
			int header_size = COUNT_L + ((resources.Count - 1) * DATA_SIZE_L); //no last data_size (cauze it can be calculated)
			using (var memH = MemoryPool<byte>.Shared.Rent(MIN_BUFFER_LENGTH))
			{
				var buff = memH.Memory;
				if (header_size > buff.Length) throw new ArgumentOutOfRangeException(nameof(resources), "Too much elements.");
				var curr_buff = buff;
				#region write 'header'
				WriteInt32(curr_buff.Span, resources.Count); //write 'count'
				WriteInt32(curr_buff.Span, COUNT_L, resources.SkipLast(1).Select(static f =>
				{
					//check file length
					var l = f.Length;
					if (l > int.MaxValue) throw new ArgumentOutOfRangeException($"{f.FullName} length", $"Max file length {int.MaxValue}");
					return (int)l;
				})); //write 'data_size[]', skip last 'data_size'
				curr_buff = curr_buff[header_size..];
				#endregion //write 'header'
				bool fileCreated = false;
				try
				{
					await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
					{
						fileCreated = true;
						if (curr_buff.Length == 0) //flush if fulled, can fail later if we dont check this before write 'data[]'
						{
							await fs.WriteAsync(buff); //flush full buffer
							curr_buff = buff; //reset current
						}
						foreach (var r in resources)
						{
							await using (var fsR = new FileStream(r.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan))
							{
								#region write 'data[i]'
								while (true)
								{
									var br = await fsR.ReadAsync(curr_buff);
									curr_buff = curr_buff[br..];
									if (curr_buff.Length > 0) //file end
									{
										break;
									}
									else //more data
									{
										await fs.WriteAsync(buff); //flush full buffer
										curr_buff = buff; //reset current

									}
								}
								#endregion //write 'data[i]'
							}
						}
						if (curr_buff.Length > 0) await fs.WriteAsync(buff[..^curr_buff.Length]); //flush if some data
					}


				}
				catch
				{
					if (fileCreated) File.Delete(path); //clean up if failed
					throw;
				}
			}
		}

		private static void WriteInt32(Span<byte> dest, int value)
		{
			dest[0] = (byte)value;
			dest[1] = (byte)(value >> 8);
			dest[2] = (byte)(value >> 16);
			dest[3] = (byte)(value >> 24);
		}

		private static void WriteInt32(Span<byte> dest, int offset, IEnumerable<int> values)
		{
			int c = offset;
			foreach (var v in values)
			{
				dest[c++] = (byte)v;
				dest[c++] = (byte)(v >> 8);
				dest[c++] = (byte)(v >> 16);
				dest[c++] = (byte)(v >> 24);
			}
		}
	}
}
