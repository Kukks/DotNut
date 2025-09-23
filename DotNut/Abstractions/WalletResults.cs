namespace DotNut;

/// <summary>
/// Result of a send operation
/// </summary>
public class SendResult
{
    public CashuToken Token { get; set; } = null!;
    public string TokenString { get; set; } = string.Empty;
    public ulong AmountSent { get; set; }
    public List<Proof> RemainingProofs { get; set; } = new();
    public ulong FeesPaid { get; set; }
}

/// <summary>
/// Result of a receive operation
/// </summary>
public class ReceiveResult
{
    public List<Proof> ReceivedProofs { get; set; } = new();
    public ulong AmountReceived { get; set; }
    public CashuToken Token { get; set; } = null!;
    public bool SignatureVerified { get; set; }
}

/// <summary>
/// Result of a swap operation
/// </summary>
public class SwapResult
{
    public List<Proof> SwappedProofs { get; set; } = new();
    public ulong TotalAmount { get; set; }
    public KeysetId TargetKeysetId { get; set; } = new("");
    public ulong FeesPaid { get; set; }
}

/// <summary>
/// Result of a melt operation (paying invoice)
/// </summary>
public class MeltResult
{
    public bool Paid { get; set; }
    public string? PaymentPreimage { get; set; }
    public List<Proof> ChangeProofs { get; set; } = new();
    public ulong AmountPaid { get; set; }
    public ulong FeesPaid { get; set; }
    public string QuoteId { get; set; } = string.Empty;
}

/// <summary>
/// Result of a mint operation (receiving from invoice)
/// </summary>
public class MintResult
{
    public List<Proof> MintedProofs { get; set; } = new();
    public ulong AmountMinted { get; set; }
    public string QuoteId { get; set; } = string.Empty;
    public bool QuotePaid { get; set; }
}

/// <summary>
/// Result of checking proof states
/// </summary>
public class StateResult
{
    public Dictionary<string, ProofState> States { get; set; } = new();
}

/// <summary>
/// Proof state information
/// </summary>
public class ProofState
{
    public bool Spent { get; set; }
    public bool Pending { get; set; }
    public string? Witness { get; set; }
}

/// <summary>
/// Result of a restore operation
/// </summary>
public class RestoreResult
{
    public List<Proof> RestoredProofs { get; set; } = new();
    public Dictionary<string, ProofState> States { get; set; } = new();
    public ulong TotalAmountRestored { get; set; }
}
