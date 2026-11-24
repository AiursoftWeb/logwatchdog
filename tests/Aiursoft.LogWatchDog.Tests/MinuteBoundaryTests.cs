namespace Aiursoft.LogWatchDog.Tests;

[TestClass]
public class MinuteBoundaryTests
{
    [TestMethod]
    [DataRow(0L)]
    [DataRow(590_000_000L)]
    [DataRow(599_990_000L)]
    [DataRow(599_999_999L)]
    public void TruncatedTimerDelayCrossesNextMinute(long ticks)
    {
        var boundary = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
        var now = boundary.AddTicks(ticks);
        var delay = Program.NextScanDelay(now);
        var timerWakeUp = now.AddMilliseconds((long)delay.TotalMilliseconds);

        Assert.IsTrue(timerWakeUp >= boundary.AddMinutes(1));
        Assert.IsTrue(timerWakeUp < boundary.AddMinutes(1).AddSeconds(1));
    }
}
