using Distributor.Matrices;

namespace Distributor.Tests.Matrices;

public sealed class MatrixMultiplicationStateTests
{
    [Fact]
    public async Task IncrementCompletedCount_CalledConcurrently_ProducesAccurateCount()
    {
        const int iterations = 20;
        var processors = Environment.ProcessorCount;
        using var barrier = new Barrier(processors);
        var state = new MatrixMultiplicationState();

        var tasks = Enumerable
            .Range(0, processors)
            .Select(_ =>
                Task.Run(() =>
                {
                    for (var index = 0; index < iterations; index++)
                    {
                        barrier.SignalAndWait();
                        state.IncrementCompletedCount();
                    }
                })
            )
            .ToArray();

        await Task.WhenAll(tasks);

        var completedCount = processors * iterations;
        state.GetCompletedCount().ShouldBe(completedCount);
    }
}
