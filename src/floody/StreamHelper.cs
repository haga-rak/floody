using System.Buffers;

namespace floody;

public static class StreamHelper
{
    private static readonly byte[] SharedBuffer = new byte[32 * 1024];

    public static async ValueTask<long> DrainAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var total = 0L;
        int read;

        while ((read = await stream.ReadAsync(SharedBuffer, cancellationToken)) > 0)
        {
            total += read;
        }

        return total;
    }
}