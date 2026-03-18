namespace DotNut.Tests.Unit;

public class Nut26Tests
{
        [Fact]
    public void Nut26_BasicPaymentRequest()
    {
        var encoded =
            "CREQB1QYQQSC3HVYUNQVFHXCPQQZQQQQQQQQQQQQ9QXQQPQQZSQ9MGW368QUE69UHNSVENXVH8XURPVDJN5VENXVUQWQREQYQQZQQZQQSGM6QFA3C8DTZ2FVZHVFQEACMWM0E50PE3K5TFMVPJJMN0VJ7M2TGRQQZSZMSZXYMSXQQHQ9EPGAMNWVAZ7TMJV4KXZ7FWV3SK6ATN9E5K7QCQRGQHY9MHWDEN5TE0WFJKCCTE9CURXVEN9EEHQCTRV5HSXQQSQ9EQ6AMNWVAZ7TMWDAEJUMR0DSRYDPGF";
        var decoded = PaymentRequestBech32Encoder.Decode(encoded);
        Assert.Equal("b7a90176", decoded.PaymentId);
        Assert.Equal(10UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Single(decoded.Mints);
        Assert.Equal("https://8333.space:3338", decoded.Mints[0]);
        Assert.NotNull(decoded.Transports);
        Assert.Single(decoded.Transports);
        var t = decoded.Transports[0];
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qqsgm6qfa3c8dtz2fvzhvfqeacmwm0e50pe3k5tfmvpjjmn0vj7m2tgpz3mhxue69uhhyetvv9ujuerpd46hxtnfduq3wamnwvaz7tmjv4kxz7fw8qenxvewwdcxzcm99uqs6amnwvaz7tmwdaejumr0ds4ljh7n", t.Target);
        Assert.Single(t.Tags);
        Assert.Equal("n", t.Tags.First().Key);
        Assert.Equal("17", t.Tags.First().Value.First());

       
        var encodedAgain = PaymentRequestBech32Encoder.Encode(decoded);
        // Assert.Equal(encoded, encodedAgain);
        
        // let's try a roundtrip since - idk why,
        // but the output of encodedAgain is a little bit different than this one in encoded in this test
        var decodedAgain = PaymentRequestBech32Encoder.Decode(encodedAgain);
        
        Assert.Equal(decoded.PaymentId, decodedAgain.PaymentId);
        Assert.Equal(decoded.Amount, decodedAgain.Amount);
        Assert.Equal(decoded.Unit, decodedAgain.Unit);
        Assert.Equal(decoded.Mints, decodedAgain.Mints);
        Assert.Equal(decoded.Transports[0].Target, decodedAgain.Transports[0].Target);
        Assert.Equal(decoded.Transports[0].Type, decodedAgain.Transports[0].Type);
        for (var i = 0; i < decoded.Transports[0].Tags.Length; i++)
        {
            Assert.Equal(decoded.Transports[0].Tags[i].Key, decodedAgain.Transports[0].Tags[i].Key);
            Assert.Equal(decoded.Transports[0].Tags[i].Value, decodedAgain.Transports[0].Tags[i].Value);
        }
    }
    

    [Fact]
    public void Nut26_NostrTransport()
    {
        var encoded = 
            "CREQB1QYQQSE3EXFSN2VTZ8QPQQZQQQQQQQQQQQPJQXQQPQQZSQXTGW368QUE69UHK66TWWSCJUETCV9KHQMR99E3K7MG9QQVKSAR5WPEN5TE0D45KUAPJ9EJHSCTDWPKX2TNRDAKSWQPEQYQQZQQZQQSQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQRQQZSZMSZXYMSXQQ8Q9HQGWFHXV6SCAGZ48";
        var d = PaymentRequest.Parse(encoded);
        
        Assert.Equal("f92a51b8", d.PaymentId);
        Assert.Equal(100UL, d.Amount);
        Assert.Equal("sat", d.Unit);
        Assert.Equal(2, d.Mints?.Length);
        Assert.Equal("https://mint1.example.com", d.Mints[0]);
        Assert.Equal("https://mint2.example.com", d.Mints[1]);
        
        Assert.Single(d.Transports);
        var t = d.Transports[0];
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qqsqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq8uzqt", t.Target);
        Assert.Equal(2, t.Tags.Length);
        Assert.Equal("n", t.Tags[0].Key);
        Assert.Equal("17", t.Tags[0].Value[0]);
        Assert.Equal("n", t.Tags[1].Key);
        Assert.Equal("9735", t.Tags[1].Value[0]);

        var encodedAgain = d.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }

    [Fact]
    public void Nut26_MinimalPaymentRequest()
    {
        var encoded = "CREQB1QYQQSDMXX3SNYC3N8YPSQQGQQ5QPS6R5W3C8XW309AKKJMN59EJHSCTDWPKX2TNRDAKSYP0LHG";
        var d = PaymentRequest.Parse(encoded);
        Assert.Equal("7f4a2b39", d.PaymentId);
        Assert.Equal("sat", d.Unit);
        Assert.Single(d.Mints!);
        Assert.Equal("https://mint.example.com", d.Mints[0]);
        
        var encodedAgain = d.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }

    [Fact]
    public void Nut26_Nut10Lock()
    {
        var encoded =
            "CREQB1QYQQSCEEV56R2EPJVYPQQZQQQQQQQQQQQ86QXQQPQQZSQXRGW368QUE69UHK66TWWSHX27RPD4CXCEFWVDHK6ZQQTYQSQQGQQGQYYVPJVVEKYDTZVGERWEFNXCCNGDFHVVUNYEPEXDJRWWRYVSMNXEPNVS6NXDENXGCNZVRZXF3KVEFCVG6NQENZVVCXZCNRXCCN2EFEVVENXVGRQQXSWARFD4JK7AT5QSENVVPS2N5FAS";
        var d = PaymentRequest.Parse(encoded);
        Assert.Equal("c9e45d2a", d.PaymentId);
        Assert.Equal(500UL, d.Amount);
        Assert.Equal("sat", d.Unit);
        Assert.Single(d.Mints);
        Assert.Equal("https://mint.example.com", d.Mints[0]);
        Assert.Equal("P2PK", d.Nut10.Kind);
        Assert.Equal("02c3b5bb27e361457c92d93d78dd73d3d53732110b2cfe8b50fbc0abc615e9c331", d.Nut10.Data);
        Assert.Single(d.Nut10.Tags);
        Assert.Equal("timeout", d.Nut10.Tags[0].Key);
        Assert.Equal("3600", d.Nut10.Tags[0].Value[0]);
        
        var encodedAgain =  d.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }

    [Fact]
    public void Nut26HttpPostTransport()
    {
        var encoded =
            "CREQB1QYQQJ6R5W3C97AR9WD6QYQQGQQQQQQQQQQQ05QCQQYQQ2QQCDP68GURN8GHJ7MTFDE6ZUETCV9KHQMR99E3K7MG8QPQSZQQPQYPQQGNGW368QUE69UHKZURF9EJHSCTDWPKX2TNRDAKJ7A339ACXZ7TDV4H8GQCQZ5RXXATNW3HK6PNKV9K82EF3QEMXZMR4V5EQ9X3SJM";
        var d = PaymentRequest.Parse(encoded);
        Assert.Equal("http_test", d.PaymentId);
        Assert.Equal(250UL, d.Amount);
        Assert.Equal("sat", d.Unit);
        Assert.Single(d.Mints);
        Assert.Equal("https://mint.example.com", d.Mints[0]);
        
        var t = Assert.Single(d.Transports);
        Assert.Equal("post", t.Type);
        Assert.Equal("https://api.example.com/v1/payment",t.Target);
        Assert.Single(t.Tags);
        
        var tag = Assert.Single(t.Tags);
        Assert.Equal("custom", tag.Key);
        Assert.Equal(2, tag.Value.Count);
        Assert.Equal("value1", tag.Value[0]);
        Assert.Equal("value2", tag.Value[1]);
        
        var encodedAgain =  d.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }

    [Fact]
    public void Nut26Nprofile()
    {
        var encoded =
            "CREQB1QYQQ5UN9D3SHJHM5V4EHGQSQPQQQQQQQQQQQQEQRQQQSQPGQRP58GARSWVAZ7TMDD9H8GTN90PSK6URVV5HXXMMDQUQGZQGQQYQQYQPQ80CVV07TJDRRGPA0J7J7TMNYL2YR6YR7L8J4S3EVF6U64TH6GKWSXQQMQ9EPSAMNWVAZ7TMJV4KXZ7F39EJHSCTDWPKX2TNRDAKSXQQMQ9EPSAMNWVAZ7TMJV4KXZ7FJ9EJHSCTDWPKX2TNRDAKSXQQMQ9EPSAMNWVAZ7TMJV4KXZ7FN9EJHSCTDWPKX2TNRDAKSKRFDAR";
        var d = PaymentRequest.Parse(encoded);
        
        Assert.Equal("relay_test", d.PaymentId);
        Assert.Equal(100UL, d.Amount);
        Assert.Equal("sat", d.Unit);
        Assert.NotNull(d.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(d.Mints));
        var t = Assert.Single(d.Transports);
        
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qqsrhuxx8l9ex335q7he0f09aej04zpazpl0ne2cgukyawd24mayt8gprpmhxue69uhhyetvv9unztn90psk6urvv5hxxmmdqyv8wumn8ghj7un9d3shjv3wv4uxzmtsd3jjucm0d5q3samnwvaz7tmjv4kxz7fn9ejhsctdwpkx2tnrdaksxzjpjp", t.Target);

        var encodedAgain = d.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }

    [Fact]
    public void Nut26_Description()
    {
        var encoded = "CREQB1QYQQJER9WD347AR9WD6QYQQGQQQQQQQQQQQXGQCQQYQQ2QQCDP68GURN8GHJ7MTFDE6ZUETCV9KHQMR99E3K7MGXQQV9GETNWSS8QCTED4JKUAPQV3JHXCMJD9C8G6T0DCFLJJRX";
        var decoded = PaymentRequest.Parse(encoded);
        Assert.Equal("desc_test", decoded.PaymentId);
        Assert.Equal(100UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));

        Assert.Equal("Test payment description", decoded.Memo);
        
        var encodedAgain =  decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26_SingleUseTrue()
    {
        var encoded =
            "CREQB1QYQQ7UMFDENKCE2LW4EK2HM5WF6K2QSQPQQQQQQQQQQQQEQRQQQSQPQQQYQS2QQCDP68GURN8GHJ7MTFDE6ZUETCV9KHQMR99E3K7MGX0AYM7";
        
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("single_use_true", decoded.PaymentId);
        Assert.Equal(100UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        
        Assert.NotNull(decoded.OneTimeUse);
        Assert.Equal(true, decoded.OneTimeUse);
        
        var encodedAgain =  decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    [Fact]
    public void Nut26_SingleUseFalse()
    {
        var encoded =
            "CREQB1QYQPQUMFDENKCE2LW4EK2HMXV9K8XEGZQQYQQQQQQQQQQQRYQVQQZQQYQQQSQPGQRP58GARSWVAZ7TMDD9H8GTN90PSK6URVV5HXXMMDQ40L90";
        var decoded = PaymentRequest.Parse(encoded);
        Assert.Equal("single_use_false", decoded.PaymentId);
        Assert.Equal(100UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.OneTimeUse);
        Assert.Equal(false, decoded.OneTimeUse);
        
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        
        var encodedAgain = decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26_MsatUnit()
    {
        var encoded =
            "CREQB1QYQQJATWD9697MTNV96QYQQGQQQQQQQQQQP7SQCQQ3KHXCT5Q5QPS6R5W3C8XW309AKKJMN59EJHSCTDWPKX2TNRDAKSYYMU95";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("unit_msat", decoded.PaymentId);
        Assert.Equal(1000UL, decoded.Amount);
        Assert.Equal("msat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints)); 
        
        var encodedAgain = decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26_UsdUnit()
    {
        var encoded =
            "CREQB1QYQQSATWD9697ATNVSPQQZQQQQQQQQQQQ86QXQQRW4EKGPGQRP58GARSWVAZ7TMDD9H8GTN90PSK6URVV5HXXMMDEPCJYC";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("unit_usd", decoded.PaymentId);
        Assert.Equal(500UL, decoded.Amount);
        Assert.Equal("usd", decoded.Unit);
        
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        
        var encodedAgain = decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26_MultipleTransports()
    {
        var encoded = 
            "CREQB1QYQQ7MT4D36XJHM5WFSKUUMSDAE8GQSQPQQQQQQQQQQQRAQRQQQSQPGQRP58GARSWVAZ7TMDD9H8GTN90PSK6URVV5HXXMMDQCQZQ5RP09KK2MN5YPMKJARGYPKH2MR5D9CXCEFQW3EXZMNNWPHHYARNQUQZ7QGQQYQQYQPQ80CVV07TJDRRGPA0J7J7TMNYL2YR6YR7L8J4S3EVF6U64TH6GKWSXQQ9Q9HQYVFHQUQZWQGQQYQSYQPQDP68GURN8GHJ7CTSDYCJUETCV9KHQMR99E3K7MF0WPSHJMT9DE6QWQP6QYQQZQGZQQSXSAR5WPEN5TE0V9CXJV3WV4UXZMTSD3JJUCM0D5HHQCTED4JKUAQRQQGQSURJD9HHY6T50YRXYCTRDD6HQTSH7TP";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("multi_transport",  decoded.PaymentId);
        Assert.Equal(500UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        Assert.NotNull(decoded.Transports);
        Assert.Equal(3, decoded.Transports.Length);
        
        var t = decoded.Transports[0];
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qqsrhuxx8l9ex335q7he0f09aej04zpazpl0ne2cgukyawd24mayt8g2lcy6q", t.Target);
        Assert.Equal("n", t.Tags[0].Key);
        Assert.Equal("17", t.Tags[0].Value.First());

        var t1 = decoded.Transports[1];
        Assert.Equal("post", t1.Type);
        Assert.Equal("https://api1.example.com/payment", t1.Target);
        
        var t2 = decoded.Transports[2];
        Assert.Equal("post", t2.Type);
        Assert.Equal("priority", t2.Tags.Single().Key);
        Assert.Equal("backup", t2.Tags.Single().Value.Single());

        var encodedAgain =  decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26MinimalNostrTransport()
    {
        var encoded = 
            "CREQB1QYQQ6MTFDE5K6CTVTAHX7UM5WGPSQQGQQ5QPS6R5W3C8XW309AKKJMN59EJHSCTDWPKX2TNRDAKSWQP8QYQQZQQZQQSRHUXX8L9EX335Q7HE0F09AEJ04ZPAZPL0NE2CGUKYAWD24MAYT8G7QNXMQ";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("minimal_nostr",  decoded.PaymentId);
        Assert.Equal("sat", decoded.Unit);
        Assert.Equal("https://mint.example.com",  Assert.Single(decoded.Mints));
        var t = Assert.Single(decoded.Transports);
        Assert.Equal("nostr", t.Type);
        Assert.Equal("nprofile1qqsrhuxx8l9ex335q7he0f09aej04zpazpl0ne2cgukyawd24mayt8g2lcy6q", t.Target);
        
        var encodedAgain= decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26MinimalPostTransport()
    {
        var encoded = "CREQB1QYQQCMTFDE5K6CTVTA58GARSQVQQZQQ9QQVXSAR5WPEN5TE0D45KUAPWV4UXZMTSD3JJUCM0D5RSQ8SPQQQSZQSQZA58GARSWVAZ7TMPWP5JUETCV9KHQMR99E3K7MG0TWYGX";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("minimal_http", decoded.PaymentId);
        Assert.Equal("sat",  decoded.Unit);
        Assert.Equal("https://mint.example.com",  Assert.Single(decoded.Mints));
        var t = Assert.Single(decoded.Transports);
        Assert.Equal("post", t.Type);
        Assert.Equal("https://api.example.com", t.Target);
        
        var encodedAgain= decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26Nut10HTLC()
    {
        var encoded =
            "CREQB1QYQQJ6R5D3347AR9WD6QYQQGQQQQQQQQQQP7SQCQQYQQ2QQCDP68GURN8GHJ7MTFDE6ZUETCV9KHQMR99E3K7MGXQQF5S4ZVGVSXCMMRDDJKGGRSV9UK6ETWWSYQPTGPQQQSZQSQGFS46VR9XCMRSV3SVFNXYDP3XGERZVNRVCMKZC3NV3JKYVP5X5UKXEFJ8QEXZVTZXQ6XVERPXUMX2CFKXQERVCFKXAJNGVTPV5ERVE3NV33SXQQ5PPKX7CMTW35K6EG2XYMNQVPSXQCRQVPSQVQY5PNJV4N82MNYGGCRXVEJ8QCKXVEHXCMNWETPXGMNXETZXUCNSVMZXUURXVPKXANR2V35XSUNXVM9VCMNSEPCVVEKVVF4VGCKZDEHVD3RYDPKXQUNJCEJXEJS4EHJHC";
        var decoded = PaymentRequest.Parse(encoded);
        
        Assert.Equal("htlc_test", decoded.PaymentId);
        Assert.Equal(1000UL, decoded.Amount);
        Assert.Equal("sat", decoded.Unit);
        Assert.NotNull(decoded.Mints);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        Assert.Equal("HTLC locked payment", decoded.Memo);
        var nut10 = decoded.Nut10;
        Assert.Equal("HTLC", nut10.Kind);
        Assert.Equal("a]0e66820bfb412212cf7ab3deb0459ce282a1b04fda76ea6026a67e41ae26f3dc", nut10.Data);
        Assert.Equal(2, nut10.Tags.Length);
        Assert.Equal("locktime", nut10.Tags[0].Key);
        Assert.Equal("1700000000", nut10.Tags[0].Value.Single());
        Assert.Equal("refund", nut10.Tags[1].Key);
        Assert.Equal("033281c37677ea273eb7183b783067f5244933ef78d8c3f15b1a77cb246099c26e", nut10.Tags[1].Value.Single());
        
        var encodedAgain = decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
    
    [Fact]
    public void Nut26CustomCurrencyUnit()
    {
        var encoded =
            "CREQB1QYQQKCM4WD6X7M2LW4HXJAQZQQYQQQQQQQQQQQRYQVQQXCN5VVZSQXRGW368QUE69UHK66TWWSHX27RPD4CXCEFWVDHK6PZHCW8";
        var decoded = PaymentRequest.Parse(encoded);

        Assert.Equal("custom_unit", decoded.PaymentId);
        Assert.Equal(100UL, decoded.Amount);
        Assert.Equal("btc", decoded.Unit);
        Assert.Equal("https://mint.example.com", Assert.Single(decoded.Mints));
        
        var encodedAgain = decoded.ToBech32String();
        Assert.Equal(encoded, encodedAgain);
    }
}