using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TONX.Modules;

public static class ApiTokenProvider
{
    private static readonly char[] SuffixAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private const string TotpSeed = "JBSWY3DPEHPK3PXP";
    private const int Difficulty = 20;
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    public async static Task<string> BuildTokenAsync()
    {
        var totp = GenerateTotp();
        var nonce = totp + GenerateSuffix();
        var proof = await Task.Run(() => SolveProof(nonce)).ConfigureAwait(false);
        var payload = new PuzzlePayload
        {
            Nonce = nonce,
            Proof = proof,
        };
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    private static long SolveProof(string nonce)
    {
        var prefix = nonce + ":";
        for (long proof = 0; proof < long.MaxValue; proof++)
        {
            var candidate = Encoding.UTF8.GetBytes(prefix + proof);
            var hash = SHA256.HashData(candidate);
            if (IsHashValid(hash))
                return proof;
        }
        throw new InvalidOperationException("未找到合法解");
    }

    private static bool IsHashValid(byte[] hash)
    {
        const int fullBytes = Difficulty / 8;
        const int remaining = Difficulty % 8;
        for (var i = 0; i < fullBytes; i++)
        {
            if (hash[i] != 0)
                return false;
        }
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (remaining > 0)
        {
            const byte mask = unchecked((byte)(0xFF << (8 - remaining)));
            if ((hash[fullBytes] & mask) != 0)
                return false;
        }
        return true;
    }

    private static string GenerateSuffix()
    {
        Span<byte> random = stackalloc byte[8];
        _rng.GetBytes(random);
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            var index = random[i] % SuffixAlphabet.Length;
            chars[i] = SuffixAlphabet[index];
        }
        return new(chars);
    }

    private static string GenerateTotp()
    {
        // 生成 TOTP
        var key = Base32Decode(TotpSeed);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counter = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(timestamp));
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);
        var code = binary % 1_000_000;
        return code.ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = input.Trim().Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        using var stream = new MemoryStream();
        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var index in clean.Select(symbol => alphabet.IndexOf(symbol)))
        {
            if (index < 0)
            {
                throw new FormatException("Seed 非法");
            }
            bitBuffer = (bitBuffer << 5) | index;
            bitsInBuffer += 5;
            if (bitsInBuffer < 8)
                continue;

            bitsInBuffer -= 8;
            var value = (bitBuffer >> bitsInBuffer) & 0xFF;
            stream.WriteByte((byte)value);
        }
        return stream.ToArray();
    }

    private sealed class PuzzlePayload
    {
        public string Nonce { get; set; } = string.Empty;
        public long Proof { get; set; }
    }
}