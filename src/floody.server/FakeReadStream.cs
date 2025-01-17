namespace fluxzy.bench.kestrel;

/// <summary>
/// A read-only stream with a given length that pretends to read data but doesn't actually do anything.
/// </summary>
public class FakeReadStream : Stream
{
    private readonly long _length;
    private readonly bool _infiniteStream;
    private long _position;

    public FakeReadStream(long length)
    {
        if (length < 0)
        {
            _infiniteStream = true;
        }

        _length = length;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_infiniteStream)
        {
            Array.Fill(buffer, (byte)70, offset, count);

            return count;
        }

        var remaining = _length - _position;

        if (remaining <= 0)
        {
            return 0;
        }

        var read = (int)Math.Min(remaining, count);
        _position += read;

        Array.Fill(buffer, (byte)70, offset, read);

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("This stream does not support seeking.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("This stream does not support setting the length.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("This stream does not support writing.");
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException("This stream does not support seeking.");

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException("This stream does not support setting the position.");
    }
}