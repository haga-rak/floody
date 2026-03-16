using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace floody.tests;

public class Socks5ProxyTests
{
    [Fact]
    public async Task Socks5_Proxy_Should_Return_200_On_Https()
    {
        var proxyHost = "192.168.1.201";
        var proxyPort = 44344;
        var targetUri = new Uri("https://sandbox.fluxzy.io:5001/ip");

        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(proxyHost, proxyPort, cancellationToken);

                var stream = new NetworkStream(socket, ownsSocket: true);
                await PerformSocks5Handshake(stream, context.DnsEndPoint, cancellationToken);

                return stream;
            },
            SslOptions =
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };

        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, targetUri)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task PerformSocks5Handshake(NetworkStream stream, DnsEndPoint target, CancellationToken cancellationToken)
    {
        // Greeting: VER=5, NMETHODS=1, METHOD=0 (no auth)
        await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, cancellationToken);

        var response = new byte[2];
        await stream.ReadExactlyAsync(response, cancellationToken);

        if (response[0] != 0x05 || response[1] != 0x00)
            throw new IOException("SOCKS5 handshake failed: server rejected no-auth method");

        // CONNECT request: VER=5, CMD=1 (connect), RSV=0, ATYP=3 (domain)
        var hostBytes = Encoding.ASCII.GetBytes(target.Host);
        var request = new byte[4 + 1 + hostBytes.Length + 2];
        request[0] = 0x05; // VER
        request[1] = 0x01; // CMD: CONNECT
        request[2] = 0x00; // RSV
        request[3] = 0x03; // ATYP: domain name
        request[4] = (byte)hostBytes.Length;
        hostBytes.CopyTo(request, 5);
        request[^2] = (byte)(target.Port >> 8);
        request[^1] = (byte)(target.Port & 0xFF);

        await stream.WriteAsync(request, cancellationToken);

        // Read reply header (4 bytes minimum)
        var reply = new byte[4];
        await stream.ReadExactlyAsync(reply, cancellationToken);

        if (reply[0] != 0x05 || reply[1] != 0x00)
            throw new IOException($"SOCKS5 CONNECT failed with status: {reply[1]}");

        // Drain the bound address based on ATYP
        var skipBytes = reply[3] switch
        {
            0x01 => 4 + 2, // IPv4 + port
            0x04 => 16 + 2, // IPv6 + port
            0x03 => 1, // domain length byte (then domain + port)
            _ => throw new IOException($"SOCKS5 unexpected ATYP: {reply[3]}")
        };

        var skip = new byte[skipBytes];
        await stream.ReadExactlyAsync(skip, cancellationToken);

        // If domain, still need to read the domain bytes + 2 port bytes
        if (reply[3] == 0x03)
        {
            var domainAndPort = new byte[skip[0] + 2];
            await stream.ReadExactlyAsync(domainAndPort, cancellationToken);
        }
    }
}
