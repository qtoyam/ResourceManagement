using System;

using static FastBytes.FastReadNoEndianness;

namespace RMReader
{
#nullable disable
	public sealed class ResourceUnpackerSlim : IDisposable, IAsyncDisposable
	{
		private readonly struct ResourceInfo
		{
			public readonly int Size;
			public readonly long Position;
			public ResourceInfo(int size, long position)
			{
				Size = size;
				Position = position;
			}
		}
		private FileStream _f;
		private ResourceInfo[] _resources;

		public int Count => _resources.Length;
		public int MaxFileLength { get; private set; } = -1;

		//file structure
		//	header:
		//		count - int32
		private const int COUNT_L = sizeof(int);
		//		data_size[] - int32[count-1] //except last
		private const int DATA_SIZE_L = sizeof(int);
		//	data[] - (byte[])[count]
		public async Task InitAsync(string path)
		{
			if (_f != null) throw new InvalidOperationException("Re-init is invalid.");
			_f = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.SequentialScan);
			Memory<byte> tbuff = new byte[COUNT_L];
			if (await _f.ReadAsync(tbuff).ConfigureAwait(false) != tbuff.Length)
			{
				throw new FileCorruptedException(path);
			}
			int count = ReadInt32(tbuff.Span);
			int data_sizes_l = (count - 1) * DATA_SIZE_L;
			tbuff = new byte[data_sizes_l];
			if (await _f.ReadAsync(tbuff).ConfigureAwait(false) != tbuff.Length)
			{
				throw new FileCorruptedException(path);
			}
			_resources = new ResourceInfo[count];
			long pos = data_sizes_l + COUNT_L; //header
			int i;
			for (i = 0; i < _resources.Length - 1; i++)
			{
				int size = ReadInt32(tbuff.Span);
				if (size > MaxFileLength) MaxFileLength = size;
				tbuff = tbuff[DATA_SIZE_L..];
				_resources[i] = new(size, pos);
				pos += size;
			}
			//init last resource
			long sizeLastBig = _f.Length - pos;
			if (sizeLastBig < 0 || sizeLastBig > int.MaxValue) throw new FileCorruptedException(path);
			int sizeLast = (int)sizeLastBig;
			if (sizeLastBig > MaxFileLength) MaxFileLength = sizeLast;
			_resources[i] = new(sizeLast, pos);
		}

		public async Task<Memory<byte>> ReadAsync(int index)
		{
			var r = _resources[index];
			Memory<byte> res = new byte[r.Size];
			_f.Seek(r.Position, SeekOrigin.Begin);
			await _f.ReadAsync(res).ConfigureAwait(false);
			return res;
		}

		public ValueTask<int> ReadAsync(int index, Memory<byte> buffer)
		{
			var r = _resources[index];
			_f.Seek(r.Position, SeekOrigin.Begin);
			return _f.ReadAsync(buffer[..r.Size]); //configureawait ??
		}

		public ValueTask DisposeAsync() => _f?.DisposeAsync() ?? ValueTask.CompletedTask;
		public void Dispose() => _f?.Dispose();
	}
}
