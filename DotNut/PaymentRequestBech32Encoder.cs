using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using DotNut.NBitcoin.Bech32;

namespace DotNut;

public class PaymentRequestBech32Encoder
{
    private static readonly Bech32Encoder Encoder = new("creqb") { StrictLength = false };
    private static readonly byte[] NpubPrefixBytes = "npub"u8.ToArray();
    private static readonly byte[] NprofilePrefixBytes = "nprofile"u8.ToArray();

    private enum TlvTag : byte
    {
        PaymentId = 0x01,
        Amount = 0x02,
        Unit = 0x03,
        SingleUse = 0x04,
        Mint = 0x05,
        Description = 0x06,
        Transport = 0x07,
        Nut10 = 0x08
    }

    public static string Encode(PaymentRequest paymentRequest)
    {
        var writer = new ArrayBufferWriter<byte>(256);
        EncodeTLV(writer, paymentRequest);

        var tlvBytes = writer.WrittenSpan;
        Span<byte> words = tlvBytes.Length * 2 > 1024
            ? new byte[tlvBytes.Length * 2]
            : stackalloc byte[tlvBytes.Length * 2];

        var wordsLen = ConvertBits(tlvBytes, words, 8, 5, true);
        return Encoder.EncodeRaw(words[..wordsLen].ToArray(), Bech32EncodingType.BECH32M).ToUpperInvariant();
    }

    public static PaymentRequest Decode(string creqb)
    {
        if (!creqb.StartsWith("creqb1", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid payment request type!");
        }

        var words = Encoder.DecodeDataRaw(creqb, out _);
        var tlv = ConvertBits(words, 5, 8, false);
        return DecodeTLV(tlv);
    }

    private static void EncodeTLV(IBufferWriter<byte> writer, PaymentRequest paymentRequest)
    {
        if (paymentRequest.PaymentId is { } pmid)
        {
            WriteTlvUtf8(writer, TlvTag.PaymentId, pmid);
        }

        if (paymentRequest.Amount is { } amount)
        {
            Span<byte> amountBytes = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(amountBytes, amount);
            WriteTlv(writer, TlvTag.Amount, amountBytes);
        }

        if (paymentRequest.Unit is { } unit)
        {
            if (unit.Equals("sat", StringComparison.OrdinalIgnoreCase))
            {
                WriteTlv(writer, TlvTag.Unit, [0x00]);
            }
            else
            {
                WriteTlvUtf8(writer, TlvTag.Unit, unit.ToLowerInvariant());
            }
        }

        if (paymentRequest.OneTimeUse is { } s)
        {
            WriteTlv(writer, TlvTag.SingleUse, s ? [0x01] : [0x00]);
        }

        if (paymentRequest.Mints is { } mints)
        {
            foreach (var mint in mints)
            {
                WriteTlvUtf8(writer, TlvTag.Mint, mint.ToLowerInvariant());
            }
        }

        if (paymentRequest.Memo is { } memo)
        {
            WriteTlvUtf8(writer, TlvTag.Description, memo);
        }

        if (paymentRequest.Transports is { } transports)
        {
            foreach (var transport in transports)
            {
                var subWriter = new ArrayBufferWriter<byte>(128);
                EncodeTransport(subWriter, transport);
                WriteTlv(writer, TlvTag.Transport, subWriter.WrittenSpan);
            }
        }

        if (paymentRequest.Nut10 is { } nut10)
        {
            var subWriter = new ArrayBufferWriter<byte>(128);
            EncodeNut10(subWriter, nut10);
            WriteTlv(writer, TlvTag.Nut10, subWriter.WrittenSpan);
        }
    }

    private static void EncodeNut10(IBufferWriter<byte> writer, Nut10LockingCondition nut10)
    {
        var kindByte = nut10.Kind.ToUpperInvariant() switch
        {
            "P2PK" => (byte)0x00,
            "HTLC" => (byte)0x01,
            _ => throw new ArgumentException("Unknown nut10 kind!")
        };
        WriteTlv(writer, 0x01, [kindByte]);
        WriteTlvUtf8(writer, 0x02, nut10.Data);

        foreach (var tag in nut10.Tags ?? [])
        {
            WriteTagTuple(writer, 0x03, tag.ToArray());
        }
    }

    private static void EncodeTransport(IBufferWriter<byte> writer, PaymentRequestTransport transport)
    {
        switch (transport.Type.ToLowerInvariant())
        {
            case "post":
                WriteTlv(writer, 0x01, [0x01]);
                WriteTlvUtf8(writer, 0x02, transport.Target);
                foreach (var tag in transport.Tags ?? [])
                {
                    WriteTagTuple(writer, 0x03, tag.ToArray());
                }
                break;

            case "nostr":
                WriteTlv(writer, 0x01, [0x00]);

                var (pubkey, relays) = DecodeNostr(transport.Target);
                WriteTlv(writer, 0x02, pubkey);

                foreach (var relay in relays)
                {
                    WriteTagTuple(writer, 0x03, ["r", relay]);
                }


                foreach (var tag in transport.Tags ?? [])
                {
                    WriteTagTuple(writer, 0x03, tag.ToArray());
                }
                break;

            default:
                throw new ArgumentException("Unknown transport type!");
        }
    }

    private static void WriteTagTuple(IBufferWriter<byte> writer, byte tag, ReadOnlySpan<string> tuple)
    {
        // Calculate total size for the tuple data
        var totalLen = 0;
        foreach (var s in tuple)
        {
            var byteLen = Encoding.UTF8.GetByteCount(s);
            if (byteLen > 255)
                throw new ArgumentException($"Tag tuple string too long (max 255 bytes): {s}");
            totalLen += 1 + byteLen;
        }

        if (totalLen > ushort.MaxValue)
            throw new ArgumentException("Tag tuple too long!");

        // Write TLV header + tuple data directly
        var span = writer.GetSpan(3 + totalLen);
        span[0] = tag;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1, 2), (ushort)totalLen);

        var offset = 3;
        foreach (var s in tuple)
        {
            var byteLen = Encoding.UTF8.GetByteCount(s);
            span[offset++] = (byte)byteLen;
            Encoding.UTF8.GetBytes(s, span.Slice(offset, byteLen));
            offset += byteLen;
        }

        writer.Advance(3 + totalLen);
    }

    private static void WriteTlv(IBufferWriter<byte> writer, TlvTag tag, ReadOnlySpan<byte> data)
        => WriteTlv(writer, (byte)tag, data);

    private static void WriteTlv(IBufferWriter<byte> writer, byte tag, ReadOnlySpan<byte> data)
    {
        if (data.Length > ushort.MaxValue)
            throw new ArgumentException("TLV data too long!");

        var span = writer.GetSpan(3 + data.Length);
        span[0] = tag;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1, 2), (ushort)data.Length);
        data.CopyTo(span.Slice(3));
        writer.Advance(3 + data.Length);
    }

    private static void WriteTlvUtf8(IBufferWriter<byte> writer, TlvTag tag, string value)
        => WriteTlvUtf8(writer, (byte)tag, value);

    private static void WriteTlvUtf8(IBufferWriter<byte> writer, byte tag, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ushort.MaxValue)
            throw new ArgumentException("TLV string too long!");

        var span = writer.GetSpan(3 + byteCount);
        span[0] = tag;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1, 2), (ushort)byteCount);
        Encoding.UTF8.GetBytes(value, span.Slice(3, byteCount));
        writer.Advance(3 + byteCount);
    }

    private static PaymentRequest DecodeTLV(ReadOnlySpan<byte> data)
    {
        var pr = new PaymentRequest();
        var offset = 0;
        var mints = new List<string>();
        var transports = new List<PaymentRequestTransport>();

        while (offset < data.Length)
        {
            var tag = data[offset];
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 1, 2));
            offset += 3;
            var value = data.Slice(offset, length);
            offset += length;

            switch (tag)
            {
                case 0x01:
                    pr.PaymentId = Encoding.UTF8.GetString(value);
                    break;
                case 0x02:
                    pr.Amount = BinaryPrimitives.ReadUInt64BigEndian(value);
                    break;
                case 0x03:
                    pr.Unit = value.Length == 1 && value[0] == 0x00 ? "sat" : Encoding.UTF8.GetString(value);
                    break;
                case 0x04:
                    pr.OneTimeUse = value.Length == 1 && value[0] == 0x01;
                    break;
                case 0x05:
                    mints.Add(Encoding.UTF8.GetString(value));
                    break;
                case 0x06:
                    pr.Memo = Encoding.UTF8.GetString(value);
                    break;
                case 0x07:
                    transports.Add(DecodeTransport(value));
                    break;
                case 0x08:
                    pr.Nut10 = DecodeNut10(value);
                    break;
            }
        }

        if (mints.Count > 0)
            pr.Mints = mints.ToArray();

        if (transports.Count > 0)
            pr.Transports = transports.ToArray();

        return pr;
    }

    private static PaymentRequestTransport DecodeTransport(ReadOnlySpan<byte> data)
    {
        var transport = new PaymentRequestTransport();
        var offset = 0;
        byte[]? targetBytes = null;
        var allTuples = new List<string[]>();

        while (offset < data.Length)
        {
            var tag = data[offset];
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 1, 2));
            offset += 3;
            var value = data.Slice(offset, length);
            offset += length;

            switch (tag)
            {
                case 0x01:
                    transport.Type = value[0] switch
                    {
                        0x00 => "nostr",
                        0x01 => "post",
                        _ => throw new FormatException("Unknown transport kind")
                    };
                    break;
                case 0x02:
                    targetBytes = value.ToArray();
                    break;
                case 0x03:
                    allTuples.Add(DecodeTagTuple(value));
                    break;
            }
        }

        if (transport.Type == "nostr" && targetBytes != null)
        {
            var relays = new List<string>();
            var tags = new List<string[]>();

            foreach (var tuple in allTuples)
            {
                if (tuple is ["r", _, ..])
                    relays.Add(tuple[1]);
                else
                    tags.Add(tuple);
            }

            transport.Target = EncodeNprofile(targetBytes, relays.ToArray());

            if (tags.Count > 0)
                transport.Tags = tags.Select(t => new Tag(t)).ToArray();
        }
        else if (transport.Type == "post" && targetBytes != null)
        {
            transport.Target = Encoding.UTF8.GetString(targetBytes);

            if (allTuples.Count > 0)
                transport.Tags = allTuples.Select(t => new Tag(t)).ToArray();
        }

        return transport;
    }

    private static Nut10LockingCondition DecodeNut10(ReadOnlySpan<byte> data)
    {
        var nut10 = new Nut10LockingCondition();
        var offset = 0;
        var tags = new List<string[]>();

        while (offset < data.Length)
        {
            var tag = data[offset];
            var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 1, 2));
            offset += 3;
            var value = data.Slice(offset, length);
            offset += length;

            switch (tag)
            {
                case 0x01:
                    nut10.Kind = value[0] switch
                    {
                        0x00 => "P2PK",
                        0x01 => "HTLC",
                        _ => throw new FormatException("Unknown nut10 kind")
                    };
                    break;
                case 0x02:
                    nut10.Data = Encoding.UTF8.GetString(value);
                    break;
                case 0x03:
                    tags.Add(DecodeTagTuple(value));
                    break;
            }
        }

        if (tags.Count > 0)
            nut10.Tags = tags.Select(t => new Tag(t)).ToArray();

        return nut10;
    }

    private static string[] DecodeTagTuple(ReadOnlySpan<byte> data)
    {
        var result = new List<string>();
        var offset = 0;

        while (offset < data.Length)
        {
            var length = data[offset++];
            if (offset + length > data.Length)
                throw new FormatException("Invalid tag tuple: data too short");

            result.Add(Encoding.UTF8.GetString(data.Slice(offset, length)));
            offset += length;
        }

        return result.ToArray();
    }

    #region Nostr Helpers

    public static (byte[] Pubkey, string[] Relays) DecodeNostr(string n)
    {
        if (n.StartsWith("nprofile", StringComparison.Ordinal))
            return DecodeNprofile(n);

        return (DecodeNpub(n), []);
    }

    private static byte[] DecodeNpub(string npub)
    {
        var encoder = new Bech32Encoder(NpubPrefixBytes) { StrictLength = false };
        var data = encoder.DecodeDataRaw(npub, out var encodingType);

        if (encodingType != Bech32EncodingType.BECH32)
            throw new FormatException("Invalid npub: expected BECH32 encoding");

        var pubkey = ConvertBits(data, 5, 8, false);
        if (pubkey.Length != 32)
            throw new FormatException($"Invalid npub: expected 32 bytes, got {pubkey.Length}");

        return pubkey;
    }

    private static (byte[] Pubkey, string[] Relays) DecodeNprofile(string nprofile)
    {
        var encoder = new Bech32Encoder(NprofilePrefixBytes) { StrictLength = false };
        var data = encoder.DecodeDataRaw(nprofile, out var encodingType);

        if (encodingType != Bech32EncodingType.BECH32)
            throw new FormatException("Invalid nprofile: expected BECH32 encoding");

        var tlvData = ConvertBits(data, 5, 8, false);
        byte[]? pubkey = null;
        var relays = new List<string>();
        var offset = 0;

        while (offset < tlvData.Length)
        {
            if (offset + 2 > tlvData.Length)
                throw new FormatException("Nprofile TLV data too short");

            var tag = tlvData[offset];
            var length = tlvData[offset + 1];
            offset += 2;

            if (offset + length > tlvData.Length)
                throw new FormatException($"Nprofile TLV value too short: expected {length} bytes");

            switch (tag)
            {
                case 0x00:
                    if (length != 32)
                        throw new FormatException($"Invalid pubkey length: expected 32 bytes, got {length}");
                    pubkey = tlvData.AsSpan(offset, 32).ToArray();
                    break;
                case 0x01:
                    relays.Add(Encoding.UTF8.GetString(tlvData.AsSpan(offset, length)));
                    break;
            }

            offset += length;
        }

        if (pubkey == null)
            throw new FormatException("Nprofile missing required pubkey");

        return (pubkey, relays.ToArray());
    }

    private static string EncodeNprofile(byte[] pubkey, string[] relays)
    {
        if (pubkey.Length != 32)
            throw new ArgumentException($"Invalid pubkey: expected 32 bytes, got {pubkey.Length}");

        // Calculate total size: pubkey (1 + 1 + 32) + relays (1 + 1 + len each)
        var totalSize = 34;
        foreach (var relay in relays)
        {
            var len = Encoding.UTF8.GetByteCount(relay);
            if (len > 255)
                throw new ArgumentException($"Relay URL too long: {relay} (max 255 bytes)");
            totalSize += 2 + len;
        }

        Span<byte> result = totalSize <= 512 ? stackalloc byte[totalSize] : new byte[totalSize];
        var offset = 0;

        // Write pubkey: T=0x00, L=32, V=<32 bytes>
        result[offset++] = 0x00;
        result[offset++] = 32;
        pubkey.CopyTo(result.Slice(offset, 32));
        offset += 32;

        // Write each relay: T=0x01, L=<len>, V=<UTF-8 string>
        foreach (var relay in relays)
        {
            var len = Encoding.UTF8.GetByteCount(relay);
            result[offset++] = 0x01;
            result[offset++] = (byte)len;
            Encoding.UTF8.GetBytes(relay, result.Slice(offset, len));
            offset += len;
        }

        var words = ConvertBits(result.ToArray(), 8, 5, true);
        var encoder = new Bech32Encoder(NprofilePrefixBytes) { StrictLength = false };
        return encoder.EncodeRaw(words, Bech32EncodingType.BECH32);
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        => ConvertBits(data.AsSpan(), fromBits, toBits, pad);

    private static byte[] ConvertBits(ReadOnlySpan<byte> data, int fromBits, int toBits, bool pad)
    {
        // estimate max output size
        var maxLen = (data.Length * fromBits + toBits - 1) / toBits + 1;
        Span<byte> output = maxLen <= 512 ? stackalloc byte[maxLen] : new byte[maxLen];
        var written = ConvertBits(data, output, fromBits, toBits, pad);
        return output[..written].ToArray();
    }

    private static int ConvertBits(ReadOnlySpan<byte> data, Span<byte> output, int fromBits, int toBits, bool pad)
    {
        var acc = 0;
        var bits = 0;
        var maxv = (1 << toBits) - 1;
        var idx = 0;

        foreach (var value in data)
        {
            if ((value >> fromBits) > 0)
                throw new FormatException("Invalid data");

            acc = (acc << fromBits) | value;
            bits += fromBits;

            while (bits >= toBits)
            {
                bits -= toBits;
                output[idx++] = (byte)((acc >> bits) & maxv);
            }
        }

        if (pad)
        {
            if (bits > 0)
                output[idx++] = (byte)((acc << (toBits - bits)) & maxv);
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new FormatException("Invalid padding");
        }

        return idx;
    }

    #endregion
}
