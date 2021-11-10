	public sealed class ResourceUnpacker : IDisposable, IAsyncDisposable
	{
		public const int MaxBytePerName = byte.MaxValue;

		private string[]? _keys;
		private byte[][] _values;
		private int[] _dataPoses;
		private readonly FileStream _fs; //dispose

		//1

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
		* keys:
		*	size_name byte
		*	name size_name bytes
		*	pos_data byte
		* data:
		*	size_data int32
		*	data size_data bytes
		*/
		public void ReadNames()
		{
			//already at '221' after ctor
			Span<byte> buff = stackalloc byte[MaxBytePerName];
			var tbuff_int = buff.Slice(0, sizeof(int));
			#region count
			_fs.Read(tbuff_int); //count
			_keys = new string[BitConverter.ToInt32(tbuff_int)];
			_values = new byte[_keys.Length][];
			_dataPoses = new int[_keys.Length];
			#endregion //count
			#region codepage
			_fs.Read(tbuff_int); //codepage
			var dec = Encoding.GetEncoding(BitConverter.ToInt32(tbuff_int)).GetDecoder();
			#endregion //codepage
			Span<char> charBuff = stackalloc char[MaxBytePerName];
			int size_name;
			for (int i = 0; i < _keys.Length; i++)
			{
				#region size_name
				size_name = _fs.ReadByte(); //size_name
				var tbuff_name = buff.Slice(0, size_name);
				#endregion //size_name
				#region name
				_fs.Read(tbuff_name);//name
				dec.Convert(tbuff_name, charBuff, true, out _, out int cUsed, out bool completed);
				if (!completed)
				{
					throw new InvalidDataException($"{_fs.Name} corrupted.");
				}
				_keys[i] = charBuff.Slice(0, cUsed).ToString();
				#endregion //name
				#region pos_data
				_fs.Read(tbuff_int); //pos_data
				_dataPoses[i] = BitConverter.ToInt32(tbuff_int);
				#endregion //pos_data
			}
		}

		public async Task ReadNamesAsync()
		{
			if (_keys != null) throw new InvalidOperationException("Keys already inited.");
			//already at '221' after ctor
			Memory<byte> buff = new byte[MaxBytePerName];
			var tbuff_int = buff.Slice(0, sizeof(int));
			#region count
			await _fs.ReadAsync(tbuff_int); //count
			_keys = new string[BitConverter.ToInt32(tbuff_int.Span)];
			_values = new byte[_keys.Length][];
			_dataPoses = new int[_keys.Length];
			#endregion //count
			#region codepage
			await _fs.ReadAsync(tbuff_int); //codepage
			var dec = Encoding.GetEncoding(BitConverter.ToInt32(tbuff_int.Span)).GetDecoder();
			#endregion //codepage
			Memory<char> charBuff = new char[MaxBytePerName];
			Memory<byte> tbuff_name;
			int size_name;
			for (int i = 0; i < _keys.Length; i++)
			{
				#region size_name
				size_name = _fs.ReadByte(); //size_name
				tbuff_name = buff.Slice(0, size_name);
				#endregion //size_name
				#region name
				await _fs.ReadAsync(tbuff_name); //name
				dec.Convert(tbuff_name.Span, charBuff.Span, true, out _, out int cUsed, out bool completed);
				if (!completed)
				{
					throw new InvalidDataException($"{_fs.Name} corrupted.");
				}
				_keys[i] = charBuff.Slice(0, cUsed).ToString();
				#endregion //name
				#region pos_data
				await _fs.ReadAsync(tbuff_int); //pos_data
				_dataPoses[i] = BitConverter.ToInt32(tbuff_int.Span);
				#endregion //pos_data
			}
		}

		public void ReadAllData()
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			Span<byte> tbuff_int = stackalloc byte[sizeof(int)]; //buffer for size_data
			for (int i = 0; i < _keys.Length; i++)
			{
#if !DISABLE_CACHE
				if (_values[i] == null) //not read before
				{
#endif
					_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
					_fs.Read(tbuff_int); //size_data
					_fs.Read(_values[i] = new byte[BitConverter.ToInt32(tbuff_int)]); //data
#if !DISABLE_CACHE
				}
#endif
			}
		}

		public async ValueTask ReadAllDataAsync()
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			Memory<byte> tbuff_int = new byte[sizeof(int)]; //buffer for size_data
			for (int i = 0; i < _keys.Length; i++)
			{
#if !DISABLE_CACHE
				if (_values[i] == null) //not read before
				{
#endif
					_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
					await _fs.ReadAsync(tbuff_int); //size_data
					await _fs.ReadAsync(_values[i] = new byte[BitConverter.ToInt32(tbuff_int.Span)]); //data
#if !DISABLE_CACHE
				}
#endif
			}
		}

		public byte[] Read(string key)
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _keys.Length; i++)
			{
				if (_keys[i] == key)
				{
#if !ENABLE_CACHE
					if (_values[i] == null) //not inited
					{
#endif
						_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
						Span<byte> tbuff_int = stackalloc byte[sizeof(int)]; //buffer for size_data
						_fs.Read(tbuff_int); //size_data
						_fs.Read(_values[i] = new byte[BitConverter.ToInt32(tbuff_int)]); //data
#if !ENABLE_CACHE
					}
#endif
					return _values[i];
				}
			}
			throw new KeyNotFoundException("Wrong key.");
		}

		public async ValueTask<byte[]> ReadAsync(string key)
		{
			if (_keys == null) throw new InvalidOperationException("Keys not inited.");
			for (int i = 0; i < _keys.Length; i++)
			{
				if (_keys[i] == key)
				{
#if !ENABLE_CACHE
					if (_values[i] == null) //not read before
					{
#endif
						_fs.Seek(_dataPoses[i], SeekOrigin.Begin); //jump to data
						Memory<byte> tbuff_int = new byte[sizeof(int)]; //buffer for size_data
						await _fs.ReadAsync(tbuff_int); //size_data
						await _fs.ReadAsync(_values[i] = new byte[BitConverter.ToInt32(tbuff_int.Span)]); //data
#if !ENABLE_CACHE
					}
#endif
					return _values[i];
				}
			}
			throw new ArgumentException("Wrong key.", nameof(key));
		}

		#region Dispose 1
		public void Dispose()
		{
			_keys = null;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			_values = null;
			_dataPoses = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			_fs.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			_keys = null;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			_values = null;
			_dataPoses = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			return _fs.DisposeAsync();
		}
		#endregion //Dispose
	}