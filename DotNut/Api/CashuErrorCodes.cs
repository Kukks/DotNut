namespace DotNut.Api;

/// <summary>
/// Codes a mint returns in <see cref="CashuProtocolError.Code"/>, as listed in the spec's
/// error_codes.md. The descriptions are the spec wording.
/// </summary>
public static class CashuErrorCodes
{
    /// <summary>Proof verification failed. NUT-03, NUT-05.</summary>
    public const int ProofVerificationFailed = 10001;

    /// <summary>Proofs already spent. NUT-03, NUT-05.</summary>
    public const int ProofsAlreadySpent = 11001;

    /// <summary>Proofs are pending. NUT-03, NUT-05.</summary>
    public const int ProofsPending = 11002;

    /// <summary>Outputs already signed. NUT-03, NUT-04, NUT-05.</summary>
    public const int OutputsAlreadySigned = 11003;

    /// <summary>Outputs are pending. NUT-03, NUT-04, NUT-05.</summary>
    public const int OutputsPending = 11004;

    /// <summary>Transaction is not balanced (inputs != outputs). NUT-02, NUT-03, NUT-05.</summary>
    public const int TransactionNotBalanced = 11005;

    /// <summary>Amount outside of limit range. NUT-04, NUT-05.</summary>
    public const int AmountOutsideLimitRange = 11006;

    /// <summary>Duplicate inputs provided. NUT-03, NUT-04, NUT-05.</summary>
    public const int DuplicateInputs = 11007;

    /// <summary>Duplicate outputs provided. NUT-03, NUT-04, NUT-05.</summary>
    public const int DuplicateOutputs = 11008;

    /// <summary>Inputs/Outputs of multiple units. NUT-03, NUT-04, NUT-05.</summary>
    public const int MultipleUnits = 11009;

    /// <summary>Inputs and outputs not of same unit. NUT-03, NUT-04, NUT-05.</summary>
    public const int InputsAndOutputsNotSameUnit = 11010;

    /// <summary>Amountless invoice is not supported. NUT-05.</summary>
    public const int AmountlessInvoiceNotSupported = 11011;

    /// <summary>Amount in request does not equal invoice. NUT-05.</summary>
    public const int AmountDoesNotEqualInvoice = 11012;

    /// <summary>Unit in request is not supported. NUT-04, NUT-05.</summary>
    public const int UnitNotSupported = 11013;

    /// <summary>Max inputs exceeded. NUT-03, NUT-05.</summary>
    public const int MaxInputsExceeded = 11014;

    /// <summary>Max outputs exceeded. NUT-03, NUT-04, NUT-05.</summary>
    public const int MaxOutputsExceeded = 11015;

    /// <summary>Duplicate quote IDs provided. NUT-29.</summary>
    public const int DuplicateQuoteIds = 11016;

    /// <summary>Max batch size exceeded. NUT-29.</summary>
    public const int MaxBatchSizeExceeded = 11017;

    /// <summary>Keyset is not known. NUT-02, NUT-04.</summary>
    public const int KeysetNotKnown = 12001;

    /// <summary>Keyset is inactive, cannot sign messages. NUT-02, NUT-03, NUT-04.</summary>
    public const int KeysetInactive = 12002;

    /// <summary>Keyset has expired. NUT-02, NUT-03, NUT-04, NUT-05.</summary>
    public const int KeysetExpired = 12003;

    /// <summary>Quote request is not paid. NUT-04.</summary>
    public const int QuoteNotPaid = 20001;

    /// <summary>Quote has already been issued. NUT-04.</summary>
    public const int QuoteAlreadyIssued = 20002;

    /// <summary>Minting is disabled. NUT-04.</summary>
    public const int MintingDisabled = 20003;

    /// <summary>Lightning payment failed. NUT-05.</summary>
    public const int LightningPaymentFailed = 20004;

    /// <summary>Quote is pending. NUT-04, NUT-05, NUT-29.</summary>
    public const int QuotePending = 20005;

    /// <summary>Invoice already paid. NUT-05.</summary>
    public const int InvoiceAlreadyPaid = 20006;

    /// <summary>Quote is expired. NUT-04, NUT-05.</summary>
    public const int QuoteExpired = 20007;

    /// <summary>Signature for mint request invalid. NUT-20.</summary>
    public const int MintRequestSignatureInvalid = 20008;

    /// <summary>Pubkey required for mint quote. NUT-20.</summary>
    public const int PubkeyRequiredForMintQuote = 20009;

    /// <summary>Endpoint requires clear auth. NUT-21.</summary>
    public const int ClearAuthRequired = 30001;

    /// <summary>Clear authentication failed. NUT-21.</summary>
    public const int ClearAuthFailed = 30002;

    /// <summary>Endpoint requires blind auth. NUT-22.</summary>
    public const int BlindAuthRequired = 31001;

    /// <summary>Blind authentication failed. NUT-22.</summary>
    public const int BlindAuthFailed = 31002;

    /// <summary>Maximum BAT mint amount exceeded. NUT-22.</summary>
    public const int MaxBatMintAmountExceeded = 31003;

    /// <summary>BAT mint rate limit exceeded. NUT-22.</summary>
    public const int BatMintRateLimitExceeded = 31004;
}
