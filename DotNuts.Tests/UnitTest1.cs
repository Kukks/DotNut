namespace DotNuts.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // {
        //     "token": [
        //     {
        //         "mint": "https://8333.space:3338",
        //         "proofs": [
        //         {
        //             "amount": 2,
        //             "id": "009a1f293253e41e",
        //             "secret": "407915bc212be61a77e3e6d2aeb4c727980bda51cd06a6afc29e2861768a7837",
        //             "C": "02bc9097997d81afb2cc7346b5e4345a9346bd2a506eb7958598a72f0cf85163ea"
        //         },
        //         {
        //             "amount": 8,
        //             "id": "009a1f293253e41e",
        //             "secret": "fe15109314e61d7756b0f8ee0f23a624acaa3f4e042f61433c728c7057b931be",
        //             "C": "029e8e5050b890a7d6c0968db16bc1d5d5fa040ea1de284f6ec69d61299f671059"
        //         }
        //         ]
        //     }
        //     ],
        //     "unit": "sat",
        //     "memo": "Thank you."
        // }

        //should become cashuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9


        var result = CashuTokenHelper.Decode(
            "cashuAeyJ0b2tlbiI6W3sibWludCI6Imh0dHBzOi8vODMzMy5zcGFjZTozMzM4IiwicHJvb2ZzIjpbeyJhbW91bnQiOjIsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6IjQwNzkxNWJjMjEyYmU2MWE3N2UzZTZkMmFlYjRjNzI3OTgwYmRhNTFjZDA2YTZhZmMyOWUyODYxNzY4YTc4MzciLCJDIjoiMDJiYzkwOTc5OTdkODFhZmIyY2M3MzQ2YjVlNDM0NWE5MzQ2YmQyYTUwNmViNzk1ODU5OGE3MmYwY2Y4NTE2M2VhIn0seyJhbW91bnQiOjgsImlkIjoiMDA5YTFmMjkzMjUzZTQxZSIsInNlY3JldCI6ImZlMTUxMDkzMTRlNjFkNzc1NmIwZjhlZTBmMjNhNjI0YWNhYTNmNGUwNDJmNjE0MzNjNzI4YzcwNTdiOTMxYmUiLCJDIjoiMDI5ZThlNTA1MGI4OTBhN2Q2YzA5NjhkYjE2YmMxZDVkNWZhMDQwZWExZGUyODRmNmVjNjlkNjEyOTlmNjcxMDU5In1dfV0sInVuaXQiOiJzYXQiLCJtZW1vIjoiVGhhbmsgeW91LiJ9",
            out var v);
        Assert.Equal("A", v);
        Assert.Equal("Thank you.", result.Memo);
        Assert.Equal("sat", result.Unit);
        var token = Assert.Single(result.Tokens);
        Assert.Equal("https://8333.space:3338", token.Mint);
        Assert.Equal(2, token.Proofs.Count);
        Assert.Collection(token.Proofs, proof =>
            {
                Assert.Equal(2, proof.Amount);
                Assert.Equal("009a1f293253e41e", proof.Id);
                Assert.Equal("407915bc212be61a77e3e6d2aeb4c727980bda51cd06a6afc29e2861768a7837", proof.Secret);
                Assert.Equal("02bc9097997d81afb2cc7346b5e4345a9346bd2a506eb7958598a72f0cf85163ea", proof.C);
            }, proof =>
            {
                Assert.Equal(8, proof.Amount);
                Assert.Equal("009a1f293253e41e", proof.Id);
                Assert.Equal("fe15109314e61d7756b0f8ee0f23a624acaa3f4e042f61433c728c7057b931be", proof.Secret);
                Assert.Equal("029e8e5050b890a7d6c0968db16bc1d5d5fa040ea1de284f6ec69d61299f671059", proof.C);
            }
        );
    }
}