using System.Buffers.Binary;
using System.Text;
using DotNut.NBitcoin.Bech32;

namespace DotNut;

public class PaymentRequestBech32Encoder
{
    private static readonly string PREFIX = "creqb1";
    
    private enum TlvTag: byte {
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
        var tlvBytes = EncodeTLV(paymentRequest);
        var words = ConvertBits(tlvBytes, 8, 5, true);
        var encoder = new Bech32Encoder("creqb")
        {
            StrictLength = false
        };
        return encoder.EncodeRaw(words, Bech32EncodingType.BECH32M).ToUpperInvariant();
    }
    
    public static PaymentRequest Decode(string creqb)
    {
        if (!creqb.StartsWith("creqb1", StringComparison.CurrentCultureIgnoreCase))
        {
            throw new ArgumentException("Invalid payment request type!");
        }
        var encoder = new Bech32Encoder("creqb")
        {
            StrictLength = false
        };
        var words = encoder.DecodeDataRaw(creqb, out _);
        var tlv = ConvertBits(words, 5, 8, false);
        return DecodeTLV(tlv);
    }

    private static byte[] EncodeTLV(PaymentRequest paymentRequest)
    {
        using var memStream = new MemoryStream();

        if (paymentRequest.PaymentId is { } pmid)
        {
            var pmidBytes = Encoding.UTF8.GetBytes(pmid);
            WriteTlv(memStream, TlvTag.PaymentId, pmidBytes);
        }

        if (paymentRequest.Amount is { } amount)
        {
            var amountBytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(amountBytes, amount);
            WriteTlv(memStream, TlvTag.Amount, amountBytes);
        }

        if (paymentRequest.Unit is { } unit)
        {
            byte[] data = [0x00];
            if (!unit.Equals("sat", StringComparison.CurrentCultureIgnoreCase))
            {
                data = Encoding.UTF8.GetBytes(unit.ToLower());
            }
            WriteTlv(memStream, TlvTag.Unit, data);
        }

        if (paymentRequest.OneTimeUse is { } s)
        {
            byte[] data = s ? [0x01] : [0x00];
            WriteTlv(memStream, TlvTag.SingleUse, data);
        }

        if (paymentRequest.Mints is { } mints)
        {
            foreach (var mint in mints)
            {
                var mintBytes = Encoding.UTF8.GetBytes(mint.ToLower());
                WriteTlv(memStream, TlvTag.Mint, mintBytes);
            }
        }

        if (paymentRequest.Memo is { } memo)
        {
            var memoBytes = Encoding.UTF8.GetBytes(memo);
            WriteTlv(memStream, TlvTag.Description, memoBytes);
        }

        if (paymentRequest.Transports is { } transports)
        {
            // 0x01 	kind 	u8 	Transport type: 0=nostr, 1=http_post
            // 0x02 	target 	bytes 	Transport target (interpretation depends on kind)
            // 0x03 	tag_tuple 	sub-sub-TLV 	Generic tag tuple (repeatable)
            
            foreach (var transport in transports)
            {
                using var subMemStream = new MemoryStream();
                switch (transport.Type.ToLower())
                {
                    case "post":
                        WriteTlv(subMemStream, 0x01, [0x01]);
                        byte[] target = Encoding.UTF8.GetBytes(transport.Target);
                        WriteTlv(subMemStream, 0x02, target);
                        break;
                    case "nostr":
                        WriteTlv(subMemStream, 0x01, [0x00]);
                        
                        (byte[] pubkey, string[] relays) = DecodeNostr(transport.Target);
                        WriteTlv(subMemStream, 0x02, pubkey);
                        foreach (var relay in relays)
                        {
                            var tuple = EncodeTagTuple(["r", relay]);
                            WriteTlv(subMemStream, 0x03, tuple);
                        }

                        if (transport.Tags is null)
                        {
                            throw new ArgumentNullException(nameof(transport.Tags), "Tags cannot be null with nostr transport!");
                        }
                        
                        foreach (var tag in transport.Tags)
                        {
                            var tuple = EncodeTagTuple(tag.ToArray());
                            WriteTlv(subMemStream, 0x03, tuple);
                        }
                        
                        break;
                    default:
                        throw new ArgumentException("Unknown transport type!");
                }
                WriteTlv(memStream, TlvTag.Transport, subMemStream.ToArray());
            }
        }

        if (paymentRequest.Nut10 is { } nut10)
        {
            using var subMemStream = new MemoryStream();
            var kind = nut10.Kind.ToUpper() switch
            {
                "P2PK" => new byte[] { 0x00 },
                "HTLC" => new byte[] { 0x01 },
                _ => throw new ArgumentException("Unknown nut10 kind!")
            };
            WriteTlv(subMemStream, (TlvTag)0x01, kind);
            var dataBytes = Encoding.UTF8.GetBytes(nut10.Data);
            WriteTlv(subMemStream, (TlvTag)0x02, dataBytes);
            
            foreach (var tag in nut10.Tags ?? [])
            {
                var tuple = EncodeTagTuple(tag.ToArray());
                WriteTlv(subMemStream, 0x03, tuple);
            }
            
            WriteTlv(memStream, TlvTag.Nut10, subMemStream.ToArray());
        }
        
        return memStream.ToArray();
    }
    private static void WriteTlv(MemoryStream memStream, TlvTag tag, byte[] data)
    { 
        WriteTlv(memStream, (byte)tag, data);
    }
    private static void WriteTlv(MemoryStream memStream, byte tag, byte[] data)
    {
        if (data.Length > ushort.MaxValue)
        {
            throw new ArgumentException("Data too long for 2-byte TLV length");
        }
        memStream.WriteByte(tag);
        memStream.WriteByte((byte)(data.Length >> 8)); // MSB
        memStream.WriteByte((byte)(data.Length & 0xFF)); // LSB
        memStream.Write(data, 0, data.Length);
    }
    
    private static byte[] EncodeTagTuple(IEnumerable<string> tuple)
    {
        var utf8 = Encoding.UTF8;

        int totalLength = 0;
        foreach (var s in tuple)
        {
            int byteLen = utf8.GetByteCount(s);
            if (byteLen > 255)
                throw new ArgumentException($"Tag tuple string too long (max 255 bytes): {s}");

            totalLength += 1 + byteLen;
        }

        var result = new byte[totalLength];
        var span = result.AsSpan();
        int offset = 0;

        foreach (var s in tuple)
        {
            int byteLen = utf8.GetByteCount(s);
            span[offset++] = (byte)byteLen;
            utf8.GetBytes(s, span.Slice(offset, byteLen));
            offset += byteLen;
        }

        return result;
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
            var length = (data[offset + 1] << 8) + data[offset + 2];
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
                    var t = DecodeTransport(value);
                    transports.Add(t);
                    break;
                case 0x08:
                    pr.Nut10 = DecodeNut10(value);
                    break;
            }
        }

        if (mints.Count > 0)
        {
            pr.Mints = mints.ToArray();
        }

        if (transports.Count > 0)
        {
            pr.Transports = transports.ToArray();
        }

        return pr;
    }
    
    private static PaymentRequestTransport DecodeTransport(ReadOnlySpan<byte> data)
    {
        var transport = new PaymentRequestTransport();
        var offset = 0;
        byte[]? targetBytes = null;
        var allTuples = new List<string[]>();

        // First pass: collect all raw data
        while (offset < data.Length)
        {
            var tag = data[offset];
            var length = (data[offset + 1] << 8) + data[offset + 2];
            offset += 3;
            var value = data.Slice(offset, length);
            offset += length;

            switch (tag)
            {
                case 0x01: // kind
                    transport.Type = value[0] switch
                    {
                        0x00 => "nostr",
                        0x01 => "post",
                        _ => throw new FormatException("Unknown transport kind")
                    };
                    break;
                case 0x02: // target (raw bytes, interpretation depends on kind)
                    targetBytes = value.ToArray();
                    break;
                case 0x03: // tag_tuple
                    allTuples.Add(DecodeTagTuple(value));
                    break;
            }
        }

        // Second pass: process collected data based on type
        if (transport.Type == "nostr" && targetBytes != null)
        {
            var relays = new List<string>();
            var tags = new List<string[]>();

            foreach (var tuple in allTuples)
            {
                if (tuple.Length >= 2 && tuple[0] == "r")
                {
                    relays.Add(tuple[1]);
                }
                else
                {
                    tags.Add(tuple);
                }
            }
            
            transport.Target = EncodeNprofile(targetBytes, relays.ToArray());

            if (tags.Count > 0) //FIXME: this is temporary. we should be able to have key-only or multiple values tags.
            {
                transport.Tags = tags.Select(t => new Tag(t)).ToArray();
            }
        }
        else if (transport.Type == "post" && targetBytes != null)
        {
            transport.Target = Encoding.UTF8.GetString(targetBytes);

            if (allTuples.Count > 0) 
            {
                transport.Tags = allTuples.Select(t => new Tag(t)).ToArray();
            }
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
            var length = (data[offset + 1] << 8) + data[offset + 2];
            offset += 3;
            var value = data.Slice(offset, length);
            offset += length;

            switch (tag)
            {
                case 0x01: // kind
                    nut10.Kind = value[0] switch
                    {
                        0x00 => "P2PK",
                        0x01 => "HTLC",
                        _ => throw new FormatException("Unknown nut10 kind")
                    };
                    break;
                case 0x02: // data
                    nut10.Data = Encoding.UTF8.GetString(value);
                    break;
                case 0x03: // tag_tuple
                    tags.Add(DecodeTagTuple(value));
                    break;
            }
        }

        if (tags.Count > 0)
        {
            nut10.Tags = tags.Select(t => new Tag(t)).ToArray();
        }

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
            {
                throw new FormatException("Invalid tag tuple: data too short");
            }
        
            var value = data.Slice(offset, length);
            result.Add(Encoding.UTF8.GetString(value));
            offset += length;
        }
    
        return result.ToArray();
    }


    #region Nostr Helpers

    private const string NpubPrefix = "npub";
    private const string NprofilePrefix = "nprofile";

    // Made public for debugging - TODO: make private again
    public static (byte[] Pubkey, string[] Relays) DecodeNostr(string n)
    {
        if (n.StartsWith(NprofilePrefix))
        {
            return DecodeNprofile(n);
        }
        var pubkey = DecodeNpub(n);
        return (pubkey, []);
    }

    private static byte[] DecodeNpub(string npub)
    {
        var encoder = new Bech32Encoder(Encoding.ASCII.GetBytes(NpubPrefix))
        {
            StrictLength = false
        };

        var data = encoder.DecodeDataRaw(npub, out var encodingType);
        if (encodingType != Bech32EncodingType.BECH32)
        {
            throw new FormatException("Invalid npub: expected BECH32 encoding");
        }

        var pubkey = ConvertBits(data, 5, 8, false);
        if (pubkey.Length != 32)
        {
            throw new FormatException($"Invalid npub: expected 32 bytes, got {pubkey.Length}");
        }

        return pubkey;
    }

    private static string EncodeNpub(byte[] pubkey)
    {
        if (pubkey.Length != 32)
        {
            throw new ArgumentException($"Invalid pubkey: expected 32 bytes, got {pubkey.Length}");
        }

        var words = ConvertBits(pubkey, 8, 5, true);

        var encoder = new Bech32Encoder(Encoding.ASCII.GetBytes(NpubPrefix))
        {
            StrictLength = false
        };

        return encoder.EncodeRaw(words, Bech32EncodingType.BECH32);
    }

    private static (byte[] Pubkey, string[] Relays) DecodeNprofile(string nprofile)
    {
        var encoder = new Bech32Encoder(Encoding.ASCII.GetBytes(NprofilePrefix))
        {
            StrictLength = false
        };

        var data = encoder.DecodeDataRaw(nprofile, out var encodingType);
        if (encodingType != Bech32EncodingType.BECH32)
        {
            throw new FormatException("Invalid nprofile: expected BECH32 encoding");
        }

        // Convert from 5-bit to 8-bit
        var tlvData = ConvertBits(data, 5, 8, false);

        // Parse TLV structure (1-byte T, 1-byte L format)
        byte[]? pubkey = null;
        var relays = new List<string>();
        var offset = 0;

        while (offset < tlvData.Length)
        {
            if (offset + 2 > tlvData.Length)
            {
                throw new FormatException("Nprofile TLV data too short");
            }

            var tag = tlvData[offset];
            var length = tlvData[offset + 1];
            offset += 2;

            if (offset + length > tlvData.Length)
            {
                throw new FormatException($"Nprofile TLV value too short: expected {length} bytes");
            }

            var value = new byte[length];
            Array.Copy(tlvData, offset, value, 0, length);
            offset += length;

            switch (tag)
            {
                case 0x00: // Pubkey
                    if (value.Length != 32)
                    {
                        throw new FormatException($"Invalid pubkey length: expected 32 bytes, got {value.Length}");
                    }
                    pubkey = value;
                    break;
                case 0x01: // Relay URL
                    relays.Add(Encoding.UTF8.GetString(value));
                    break;
                // Ignore unknown tags
            }
        }

        if (pubkey == null)
        {
            throw new FormatException("Nprofile missing required pubkey");
        }

        return (pubkey, relays.ToArray());
    }

    private static string EncodeNprofile(byte[] pubkey, string[] relays)
    {
        var tlv = EncodePubkeyRelaysTlv(pubkey, relays);

        // Convert from 8-bit to 5-bit
        var words = ConvertBits(tlv, 8, 5, true);

        var encoder = new Bech32Encoder(Encoding.ASCII.GetBytes(NprofilePrefix))
        {
            StrictLength = false
        };

        return encoder.EncodeRaw(words, Bech32EncodingType.BECH32);
    }

    private static byte[] EncodePubkeyRelaysTlv(byte[] pubkey, string[] relays)
    {
        if (pubkey.Length != 32)
        {
            throw new ArgumentException($"Invalid pubkey: expected 32 bytes, got {pubkey.Length}");
        }

        var encodedRelays = relays.Select(Encoding.UTF8.GetBytes).ToArray();

        // Validate relay lengths fit in 1 byte
        for (var i = 0; i < encodedRelays.Length; i++)
        {
            if (encodedRelays[i].Length > 255)
            {
                throw new ArgumentException($"Relay URL too long: {relays[i]} (max 255 bytes)");
            }
        }

        // Calculate total size: pubkey (1 + 1 + 32) + relays (1 + 1 + len each)
        var totalSize = 2 + 32 + encodedRelays.Sum(r => 2 + r.Length);
        var result = new byte[totalSize];

        var offset = 0;

        // Write pubkey: T=0x00, L=32, V=<32 bytes>
        result[offset++] = 0x00;
        result[offset++] = 32;
        Array.Copy(pubkey, 0, result, offset, 32);
        offset += 32;

        // Write each relay: T=0x01, L=<len>, V=<UTF-8 string>
        foreach (var relay in encodedRelays)
        {
            result[offset++] = 0x01;
            result[offset++] = (byte)relay.Length;
            Array.Copy(relay, 0, result, offset, relay.Length);
            offset += relay.Length;
        }

        return result;
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        var acc = 0;
        var bits = 0;
        var maxv = (1 << toBits) - 1;
        var ret = new List<byte>();

        foreach (var value in data)
        {
            if ((value >> fromBits) > 0)
            {
                throw new FormatException("Invalid data");
            }
            acc = (acc << fromBits) | value;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                ret.Add((byte)((acc >> bits) & maxv));
            }
        }

        if (pad)
        {
            if (bits > 0)
            {
                ret.Add((byte)((acc << (toBits - bits)) & maxv));
            }
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new FormatException("Invalid padding");
        }

        return ret.ToArray();
    }

    #endregion
}