using System.Net;
using System.Net.Sockets;

namespace TimerVisual;

/// <summary>Cliente NTP simples (mesma lógica do repositório original, com timeout e async).</summary>
public static class Ntp
{
    public static readonly (string Nome, string Host)[] Servidores =
    [
        ("a.ntp.br", "a.ntp.br"),
        ("d.st1.ntp.br", "d.st1.ntp.br"),
        ("pool.ntp.org", "pool.ntp.org"),
        ("200.20.186.94", "200.20.186.94"),
        ("time.windows.com", "time.windows.com"),
        ("time.google.com", "time.google.com"),
        ("time.aws.com", "time.aws.com"),
    ];

    /// <summary>Retorna a diferença (NTP - relógio local).</summary>
    public static async Task<TimeSpan> ObterDiferencaAsync(string servidor, int timeoutMs = 3000)
    {
        var dados = new byte[48];
        dados[0] = 0x1B; // LI = 0, VN = 3, Mode = 3 (cliente)

        var enderecos = await Dns.GetHostAddressesAsync(servidor);
        var endereco = enderecos.First(a => a.AddressFamily == AddressFamily.InterNetwork);

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        using var cts = new CancellationTokenSource(timeoutMs);

        var enviado = DateTimeOffset.UtcNow;
        await socket.ConnectAsync(new IPEndPoint(endereco, 123), cts.Token);
        await socket.SendAsync(dados, SocketFlags.None, cts.Token);
        await socket.ReceiveAsync(dados, SocketFlags.None, cts.Token);
        var recebido = DateTimeOffset.UtcNow;

        ulong intPart = (ulong)dados[40] << 24 | (ulong)dados[41] << 16 | (ulong)dados[42] << 8 | dados[43];
        ulong fracPart = (ulong)dados[44] << 24 | (ulong)dados[45] << 16 | (ulong)dados[46] << 8 | dados[47];
        var ms = intPart * 1000 + fracPart * 1000 / 0x100000000UL;

        var horaNtp = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
        var meio = enviado + (recebido - enviado) / 2; // compensa a latência
        return horaNtp - meio;
    }
}
