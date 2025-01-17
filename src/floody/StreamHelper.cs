using System.Buffers;

namespace floody;

public static class StreamHelper
{
    public static async ValueTask<long> DrainAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);

        try
        {
            var total = 0L;
            int read;

            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}