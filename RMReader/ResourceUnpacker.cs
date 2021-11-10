#define ENABLE_CACHE

using System.Buffers;
using System.Text;

namespace RMReader
{
	public sealed class ResourceUnpacker : IDisposable, IAsyncDisposable
	{
		public const int MaxBytesPerName = byte.MaxValue;

		private string[]? _keys;
		private IMemoryOwner<byte>[] _values; //dispose array elements?
		private int[] _dataPoses;
		private int[] _dataSizes;
		private readonly FileStream _fs; //dispose

		private int _count = 0;

		//2

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public ResourceUnpacker(string fullPath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
			_fs = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			if (_fs.ReadByte() != 221) throw new ArgumentException("Path is not resource management file.", nameof(fullPath)); //221
		}

		/* file structure
		* '221' byte
		* codepage int32
		* count int32
		* seq keys:
		*	size_name byte
		*	name size_name bytes
		*	pos_data int
		*	size_data int
		* seq data:
		*	data size_data bytes
		*/
		public void ReadNames()
		{
			if (_keys != null) throw new InvalidOperationException("Keys already inited.");
			//already at '221' after ctor
			Span<byte> buff = stackalloc byte[MaxBytesPerName];
			var tbuff_int = buff[..sizeof(int)];
			#region count
			_fs.Read(tbuff_int); //count
			_count = BitConverter.ToInt32(tbuff_int);
			#endregion //count
			_keys = new string[_count];
			_values = new IMemoryOwner<byte>[_count];
			_dataPoses = new int[_count];
			_dataSizes = new int[_count];
			#region codepage
			_fs.Read(tbuff_int); //codepage
			var dec = Encoding.GetEncoding(BitConverter.ToInt32(tbuff_int)).GetDecoder();
			#endregion //codepage
			Span<char> charBuff = stackalloc char[MaxBytesPerName];
			int size_name;
			for (int i = 0; i < _count; i++)
			{
				#region size_name
				size_name = _fs.ReadByte(); //size_name
				var tbuff_name = buff[..size_name];
				#endregion //size_name
				#region name
				_fs.Read(tbuff_name);//name
				dec.Convert(tbuff_name, charBuff, true, out _, out int cUsed, out bool completed);
				if (!completed)
				{
					throw new InvalidDataException($"{_fs.Name} corrupted.");
				}
				_keys[i] = charBuff[..cUsed].ToString();
				#endregion //name
				#region pos_data
				_fs.Read(tbuff_int); //pos_data
				_dataPoses[i] = BitConverter.ToInt32(tbuff_int);
				#endregion //pos_data
				#region size_data
				_fs.Read(tbuff_int);
				_dataSizes[i] = BitConverter.ToInt32(tbuff_int);
				#endregion //size_data
			}
		}

		public async Task ReadNamesAsync()
		{
			if (_keys != null) throw new InvalidOperationException("Keys already inited.");
			//already at '221' after ctor
			Memory<byte> buff = new byte[MaxBytesPerName];
			var tbuff_int = buff[..sizeof(int)];
			#region count
			await _fs.ReadAsync(tbuff_int); //count
			_count = BitConverter.ToInt32(tbuff_int.Span);
			#endregion //count
			_keys = new string[_count];
			_values = new IMemoryOwner<byte>[_count];
			_dataPoses = new int[_count];
			_dataSizes = new int[_count];
			#region codepage
			await _fs.ReadAsync(tbuff_int); //codepage
			var dec = Encoding.GetEncoding(BitConverter.ToInt32(tbuff_int.Span)).GetDecoder();
			#endregion //codepage
			Memory<char> charBuff = new char[MaxBytesPerName];
			Memory<byte> tbuff_name;
			int size_name;
			for (int i = 0; i < _count; i++)
			{
				#region size_name
				size_name = _fs.ReadByte(); //size_name
				tbuff_name = buff[..size_name];
				#endregion //size_name
				#region name
				await _fs.ReadAsync(tbuff_name); //name
				dec.Convert(tbuff_name.Span, charBuff.Span, true, out _, out int cUsed, out bool completed);
				if (!completed)
				{
					throw new InvalidDataException($"{_fs.Name} corrupted.");
				}
				_keys[i] = charBuff[..cUsed].ToString();
				#endregion //name
				#region pos_data
				await _fs.ReadAsync(tbuff_int); //pos_data
				_dataPoses[i] = BitConverter.ToInt32(tbuff_int.Span);
				#endregion //pos_data
				#region size_data
				await _fs.ReadAsync(tbuff_int); //size_data
				_dataSizes[i] = BitConverter.ToInt32(tbuff_int.Span);
				#endregion //size_data
			}
		}

		public void ReadAllData()
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _count; i++)
			{
#if ENABLE_CACHE
				if (_values[i] == null) //not read before
				{
#endif
					_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
					_values[i] = MemoryPool<byte>.Shared.Rent(_dataSizes[i]);
					_fs.Read(_values[i].Memory.Span[.._dataSizes[i]]); //data
#if ENABLE_CACHE
				}
#endif
			}
		}

		public async ValueTask ReadAllDataAsync()
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _count; i++)
			{
#if ENABLE_CACHE
				if (_values[i] == null) //not read before
				{
#endif
					_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
					_values[i] = MemoryPool<byte>.Shared.Rent(_dataSizes[i]);
					await _fs.ReadAsync(_values[i].Memory[.._dataSizes[i]]); //data
#if ENABLE_CACHE
				}
#endif
			}
		}

		/// <summary>
		/// Read or get cached data by key.
		/// </summary>
		/// <param name="key">Key of the resource.</param>
		/// <returns>
		/// Data span, that can be used only before <see cref="ResourceUnpacker"/> disposed.
		/// </returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		public ReadOnlySpan<byte> Read(string key)
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _count; i++)
			{
				if (_keys[i] == key)
				{
#if ENABLE_CACHE
					if (_values[i] == null) //not inited
					{
#endif
						_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
						_values[i] = MemoryPool<byte>.Shared.Rent(_dataSizes[i]);
						var real_value = _values[i].Memory.Span[.._dataSizes[i]];
						_fs.Read(real_value); //data
						return real_value;
#if ENABLE_CACHE
					}
#endif
					return _values[i].Memory.Span[.._dataSizes[i]]; //slice to get real data (can be empty space from MemoryPool)
				}
			}
			throw new KeyNotFoundException("Wrong key.");
		}

		/// <summary>
		/// Asynchronously read or get cached data by key.
		/// </summary>
		/// <param name="key">Key of the resource.</param>
		/// <returns>
		/// Data memory, that can be used only before <see cref="ResourceUnpacker"/> disposed.
		/// </returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		public async ValueTask<ReadOnlyMemory<byte>> ReadAsync(string key)
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _count; i++)
			{
				if (_keys[i] == key)
				{
#if ENABLE_CACHE
					if (_values[i] == null) //not inited
					{
#endif
						_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
						_values[i] = MemoryPool<byte>.Shared.Rent(_dataSizes[i]);
						var real_value = _values[i].Memory[.._dataSizes[i]];
						await _fs.ReadAsync(real_value); //data
						return real_value;
#if ENABLE_CACHE
					}
#endif
					return _values[i].Memory[.._dataSizes[i]]; //slice to get real data (can be empty space from MemoryPool)
				}
			}
			throw new KeyNotFoundException("Wrong key.");
		}

		#region Dispose 2
		public void Dispose()
		{
			if (_values != null)
			{
				for (int i = 0; i < _values.Length; i++)
				{
					_values[i]?.Dispose();
				}
			}
			_fs.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			if (_values != null)
			{
				for (int i = 0; i < _values.Length; i++)
				{
					_values[i]?.Dispose();
				}
			}
			return _fs.DisposeAsync();
		}
		#endregion //Dispose
	}
}
