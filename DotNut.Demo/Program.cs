using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotNut;
using DotNut.Api;
using DotNut.ApiModels;
using DotNut.NUT13;
using NBitcoin;
using NBitcoin.Secp256k1;
using static DotNut.ApiModels.StateResponseItem;

namespace CashuDemo
{
    class Program
    {
        private static readonly CashuHttpClient _cashuClient;

        static Program()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("https://nofees.testnut.cashu.space") }; // Beispiel-Mint-URL
            _cashuClient = new CashuHttpClient(httpClient);
        }

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Cashu Token Demo...");
                Console.WriteLine("Please enter your Cashu token (e.g., cashuAey...):");

                // Step 1: Read token from user
                string? inputToken = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(inputToken))
                {
                    throw new Exception("No token provided.");
                }

                var token = CashuTokenHelper.Decode(inputToken, out string version);
                Console.WriteLine("Decoded Input Token:");
                Console.WriteLine(JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true }));

                // Step 2: Check token (CheckState)
                bool isSpendable = await CheckTokenState(token);
                Console.WriteLine($"Token Spendable: {isSpendable}");

                if (!isSpendable)
                {
                    Console.WriteLine("Token is not spendable. Exiting.");
                    return;
                }

                // Step 3: Claim token (swap)
                var newToken = await SwapToken(token);
                Console.WriteLine("New Token after Swap:");
                Console.WriteLine(newToken);

                // Step 4: Decode and output new token
                var decodedNewToken = CashuTokenHelper.Decode(newToken, out string _);
                Console.WriteLine("Decoded New Token:");
                Console.WriteLine(JsonSerializer.Serialize(decodedNewToken, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static async Task<bool> CheckTokenState(CashuToken token)
        {
            var proofs = token.Tokens.SelectMany(t => t.Proofs).ToArray();
            var postCheckStateRequest = new PostCheckStateRequest
            {
                Ys = proofs.Select(p => Cashu.MessageToCurve(p.Secret.ToString()).ToHex()).ToArray()
            };

            var response = await _cashuClient.CheckState(postCheckStateRequest);
            return response.States.All(s => s.State == TokenState.UNSPENT);
        }

        static async Task<string> SwapToken(CashuToken oldToken)
        {
            var keysResponse = await _cashuClient.GetKeys();

            var oldProofs = oldToken.Tokens.SelectMany(t => t.Proofs).ToArray();

            var outputs = new List<BlindedMessage>();
            var blindingDataList = new Dictionary<StringSecret, ECPrivKey>();
            for (int i = 0; i < oldProofs.Length; i++)
            {
                var mnemonic = new Mnemonic(Wordlist.English);
                var mnemonicWords = mnemonic.Words;

                var oldProof = oldProofs[i];
                var amount = oldProof.Amount;

                var A = keysResponse.Keysets.Where(x => x.Id == oldProof.Id).First().Keys[(ulong)amount];

                var id = oldProof.Id;
                var secret = mnemonic.DeriveSecret(id, 0);
                var y = secret.ToCurve();
                var r = ECPrivKey.Create(mnemonic.DeriveBlindingFactor(id, 0));
                var b_ = Cashu.ComputeB_(y, r);

                outputs.Add(new BlindedMessage
                {
                    Id = id,
                    Amount = amount,
                    B_ = b_,
                });

                blindingDataList.Add(secret, r);
            }

            var swapRequest = new PostSwapRequest
            {
                Inputs = oldProofs,
                Outputs = outputs.ToArray()
            };

            var swapResponse = await _cashuClient.Swap(swapRequest);

            var newTokens = new List<Proof>();
            for (int i = 0; i < swapResponse.Signatures.Length; i++)
            {
                var signature = swapResponse.Signatures[i];

                var id = signature.Id;
                var amount = signature.Amount;
                var A = keysResponse.Keysets.Where(x => x.Id == signature.Id).First().Keys[(ulong)amount];

                var blindingData = blindingDataList.ElementAt(i);

                var secret = blindingData.Key;
                var y = secret.ToCurve();
                var r = blindingData.Value;
                var b_ = Cashu.ComputeB_(y, r);
                var c_ = signature.C_;
                var e = signature.DLEQ.E;
                var s = signature.DLEQ.S;
                var c = Cashu.UnblindC(c_, r, A);

                if (!signature.Verify(A, b_))
                    throw new Exception();

                var newToken = new Proof
                {
                    Id = id,
                    Amount = amount,
                    Secret = secret,
                    C = c,
                    DLEQ = new DotNut.DLEQProof
                    {
                        E = e,
                        R = r,
                        S = s
                    }
                };

                newTokens.Add(newToken);
            }

            var newCashuToken = new CashuToken()
            {
                Unit = oldToken.Unit,
                Tokens = new List<CashuToken.Token> { new CashuToken.Token() { Mint = oldToken.Tokens.First().Mint, Proofs = newTokens } }
            };

            return newCashuToken.Encode("B", false);
        }
    }
}