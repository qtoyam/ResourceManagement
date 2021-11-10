using System;
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
		public Dictionary<string, byte[]> Resources { get; } = new();

		private readonly Encoding _encoding;
		private readonly Encoder _encoder;

		public const int MaxBytePerName = byte.MaxValue;

		private const int HEADER_221_SIZE = sizeof(byte);
		private const int HEADER_codepage_SIZE = sizeof(int);
		private const int HEADER_count_SIZE = sizeof(int);
		private const int HEADER_size_keys_SIZE = sizeof(int);

		private const int HEADER_SIZE = 0 +
			HEADER_221_SIZE +
			HEADER_codepage_SIZE +
			HEADER_count_SIZE +
			HEADER_size_keys_SIZE;


		private const int KEY_HEADER_size_name_SIZE = sizeof(byte);
		private const int KEY_HEADER_pos_data_SIZE = sizeof(long);
		private const int KEY_HEADER_size_data_SIZE = sizeof(long);

		private const int MAX_KEY_HEADER_SIZE = MaxBytePerName //to store 'name'
			+ KEY_HEADER_size_name_SIZE //to store 'size_name'
			+ KEY_HEADER_pos_data_SIZE //to store 'pos_data'
			+ KEY_HEADER_size_data_SIZE;//to store 'size_data'

		public ResourcePacker() : this(Encoding.UTF8) { }

		public ResourcePacker(Encoding encoding)
		{
			_encoding = encoding;
			_encoder = _encoding.GetEncoder();
		}

		/* file structure
		* ---HEADER---
		* '221' - byte
		* codepage - int32
		* count - int32
		* size_keys - int32
		* seq keys:
		*	size_name - byte
		*	name - size_name bytes
		*	pos_data - long
		*	size_data - long
		* ---HEADER---
		* ---DATA---
		* seq data:
		*	data - size_data bytes
		* ---DATA---
		file structure */
		public static async Task SaveToAsync(string fullPath, ReadOnlyMemory<(string name, string filePath)> paths, Encoding encoding = null)
		{
			FileStream[] files = new FileStream[paths.Length];
			FileStream? fsOut = null;
			try
			{
				//open and lock files
				fsOut = new(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.Asynchronous);
				for (int i = 0; i < paths.Length; i++)
				{
					files[i] = new FileStream(paths.Span[i].filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
				}

				//find max file size
				long maxFile = MAX_KEY_HEADER_SIZE; //set minimum to key header size
				for (int i = 0; i < files.Length; i++)
				{
					long fL = files[i].Length;
					if (fL > maxFile) maxFile = fL;
				}

				//create buff
				const int DEF_BUFF_LENGTH = 4096 * 20;
				using (var memH = MemoryPool<byte>.Shared.Rent(
					//max rent: DEF_BUFF_LENGTH
					maxFile > DEF_BUFF_LENGTH ? DEF_BUFF_LENGTH : (int)maxFile
					))
				using (var pin_memH = memH.Memory.Pin())
				{
					var buff = memH.Memory;

					//write header
					int offset = 0;
					buff.Span[offset] = 221; //'221' byte
					offset += HEADER_221_SIZE;
					WriteInt32(buff.Span.Slice(offset, HEADER_codepage_SIZE), encoding.CodePage); //'codepage'
					offset += HEADER_codepage_SIZE;
					WriteInt32(buff.Span.Slice(offset, HEADER_count_SIZE), paths.Length); //'count'
					offset += HEADER_count_SIZE;
					WriteInt32(buff.Span.Slice(offset, HEADER_size_keys_SIZE), 0); //'size_keys' //DEFAULT
					offset += HEADER_size_keys_SIZE;

					//write keys
					encoding ??= Encoding.UTF8;
					var encoder = encoding.GetEncoder();
					for (int i = 0; i < paths.Length; i++)
					{
						//flush if no space atleast for key header
						if (buff.Length - offset /*available space*/ < MAX_KEY_HEADER_SIZE)
						{
							await fsOut.WriteAsync(buff[..offset]);
							offset = 0;
						}

						encoder.Convert(paths.Span[i].name, buff.Span[(offset + 1)..MaxBytePerName],
							true, out _, out int bUsed, out bool c); //write 'name' to buff
						if (!c) throw new ArgumentException($"Name of [{i}] element is too long!");
						buff.Span[offset++] = (byte)bUsed; //write 'size_name' to buff
						offset += bUsed; //move offset by 'name' byte-length
						offset += KEY_HEADER_pos_data_SIZE; //write 'pos_data' //DEFAULT
						WriteInt64(buff.Span.Slice(offset, KEY_HEADER_size_data_SIZE), files[i].Length); //write 'size_data'
					}

					//find KEY_HEADER actual size
					int key_header_actual_size = ((int)fsOut.Position + offset) - HEADER_SIZE; //file pos + buffer offset, cauze buffer can be not flushed to file at this step

					//write data
					for (int i = 0; i < paths.Length; i++)
					{
						int bRead;
						while (files[i].Position < files[i].Length)
						{
							bRead = await files[i].ReadAsync(buff[offset..]);
						}
					}
				}
			}
			finally
			{
				if (fsOut != null) await fsOut.DisposeAsync();
				for (int i = 0; i < files.Length; i++)
					if (files[i] != null) await files[i].DisposeAsync();
			}
		}

		private static void WriteInt32(Span<byte> dest, int value)
		{
			dest[0] = (byte)value;
			dest[1] = (byte)(value >> 8);
			dest[2] = (byte)(value >> 16);
			dest[3] = (byte)(value >> 24);
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
