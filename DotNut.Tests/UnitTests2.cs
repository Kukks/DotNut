using DotNut.Abstractions;

namespace DotNut.Tests;
/// <summary>
/// Tests of higher-level abstractions
/// </summary>
public class UnitTests2
{
    [Fact]
    public async Task InMemoryCounter()
    {
        var ctr = new InMemoryCounter();
        Assert.NotNull(ctr);
        var testId1 = new KeysetId("00qwertyuiopasdf");
        var ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal(0, ctrNum);
        
        await ctr.IncrementCounter(testId1);
        Assert.Equal(0, ctrNum);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal(1, ctrNum);
        
        await ctr.IncrementCounter(testId1, 5);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal(6, ctrNum);
        
        await ctr.SetCounter(testId1, 1337);
        ctrNum = await ctr.GetCounterForId(testId1);
        Assert.Equal(1337, ctrNum);
    }
    
    
}