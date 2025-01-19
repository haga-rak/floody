namespace floody
{
    /// <summary>
    ///   A stream that counts the number of bytes written and read 
    /// </summary>
    public class ByteCounterStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Action<int> _readCountCallBack;
        private readonly Action<int> _writeCountCallBack;
        
        public ByteCounterStream(Stream innerStream, Action<int> readCountCallBack, Action<int> writeCountCallBack)
        {
            _innerStream = innerStream;
            _readCountCallBack = readCountCallBack;
            _writeCountCallBack = writeCountCallBack;
        }
        
        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);
            _readCountCallBack(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await base.ReadAsync(buffer, offset, count, cancellationToken);
            _readCountCallBack(read);
            return read;
        }
        
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            _writeCountCallBack(count);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var read = await _innerStream.ReadAsync(buffer, cancellationToken);
            _readCountCallBack(read);
            return read;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            _writeCountCallBack(buffer.Length);
            return _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            _writeCountCallBack(count);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }
    }
}