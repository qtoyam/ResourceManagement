﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMWriter
{
	public class ResourcePacker
	{

		private const int HEADER_221_SIZE = sizeof(byte);
		private const int HEADER_count_SIZE = sizeof(int);
		private const int HEADER_size_keys_table_SIZE = sizeof(int);

		private const int HEADER_SIZE = 0 +
			HEADER_221_SIZE +
			HEADER_count_SIZE +
			HEADER_size_keys_table_SIZE;


		private const int KEY_CELL_size_name_SIZE = sizeof(byte);
		public const int MaxBytesPerName = byte.MaxValue;
		private const int KEY_CELL_pos_data_SIZE = sizeof(long);
		private const int KEY_CELL_size_data_SIZE = sizeof(long);

		private const int MAX_KEY_CELL_SIZE = KEY_CELL_size_name_SIZE //to store 'size_name'
			+ MaxBytesPerName //to store 'name'
			+ KEY_CELL_pos_data_SIZE //to store 'pos_data'
			+ KEY_CELL_size_data_SIZE;//to store 'size_data'

		/* file structure
		* header:
		*	'221' - byte
		*	count - int32
		*	size_keys_table - int32 //max >7kk keys, should be enough
		* keys_table:
		*	size_name - byte
		*	name - size_name bytes (max 255 bytes)
		*	pos_data - long
		*	size_data - long
		* data_raw:
		*	data - size_data bytes
		file structure */


		public static async Task SaveToAsync(string fullPath, ReadOnlyMemory<(string name, string filePath)> paths)
		{
			if (paths.Length < 1) throw new ArgumentException("Must be atleast 1 element", nameof(paths));
			int size_keys_table = (KEY_CELL_size_name_SIZE + KEY_CELL_pos_data_SIZE + KEY_CELL_size_data_SIZE) * paths.Length;
			for (int i = 0; i < paths.Length; i++)
			{
				var l = paths.Span[i].name.Length;
				if (l > MaxBytesPerName)
					throw new ArgumentOutOfRangeException(paths.Span[i].name, $"Max length of name is {MaxBytesPerName}, provided name is {l} length.");
				size_keys_table += l;
			}
			//using (var pin_memH = memH.Memory.Pin()) //useful?
			using (var memH = MemoryPool<byte>.Shared.Rent(4096 * 20)) //create temp buffer
			{
				var buff = memH.Memory;

				//write header
				int offset = 0;
				buff.Span[offset] = 221; //write '221'
				offset += HEADER_221_SIZE;

				WriteInt32(buff.Span.Slice(offset, HEADER_count_SIZE), paths.Length); //write 'count'
				offset += HEADER_count_SIZE;

				WriteInt32(buff.Span.Slice(offset, HEADER_size_keys_table_SIZE), size_keys_table); //write 'size_keys_table'
				offset += HEADER_size_keys_table_SIZE;

				//write keys_table
				FileStream[] files = new FileStream[paths.Length];
				try
				{
					int keyCellsPerCycle = (buff.Length - HEADER_SIZE) / MAX_KEY_CELL_SIZE;
					long pos_data = HEADER_SIZE + size_keys_table;
					int iCell = 0;
					while (true)
					{
						int remainedItems = paths.Length - iCell;
						int toI = remainedItems > keyCellsPerCycle ? keyCellsPerCycle : remainedItems;
						for (; iCell < toI; iCell++)
						{
							var p = paths.Span[iCell];

							//write key_cell
							buff.Span[offset] = (byte)p.name.Length; //write 'size_name' to buff, length already checked so cast is safe
							offset += KEY_CELL_size_name_SIZE;

							EncodeAscii(p.name, buff.Span.Slice(offset, ))
							encoder.Convert(paths.Span[iCell].name, buff.Span.Slice(offset + 1, MaxBytesPerName), //slice MaxBytePerName count cauze it will ALWAYS be atleast this free space
								true, out _, out _, out _); //write 'name' to buff



							offset += KEY_CELL_pos_data_SIZE; //skip 'pos_data' //DEFAULT

							WriteInt64(buff.Span.Slice(offset, KEY_CELL_size_data_SIZE), files[i].Length); //write 'size_data'
							offset += KEY_CELL_size_data_SIZE;

							files[iCell] = new FileStream(paths.Span[iCell].filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan);
						}
						if (i == paths.Length) //break if we are done in this cycle
						{
							break;
						}
						else //flush if more keys
						{
							await fsOut.WriteAsync(buff[..offset]);
							offset = 0;
						}
					}
				}
				finally
				{
					for (i = 0; i < paths.Length; i++)
					{
						if (files[i] != null) await files[i].DisposeAsync();
					}
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

		private static void EncodeAscii(in ReadOnlySpan<char> input, Span<byte> buffer)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] > sbyte.MaxValue) throw new ArgumentException($"Non-ascii char {input[i]}");
				buffer[i] = (byte)input[i];
			}
		}

		private static void WriteInt64(Span<byte> dest, long value)
		{
			dest[0] = (byte)value;
			dest[1] = (byte)(value >> 8);
			dest[2] = (byte)(value >> 16);
			dest[3] = (byte)(value >> 24);
			dest[4] = (byte)(value >> 32);
			dest[5] = (byte)(value >> 40);
			dest[6] = (byte)(value >> 48);
			dest[7] = (byte)(value >> 56);
		}
	}
}
