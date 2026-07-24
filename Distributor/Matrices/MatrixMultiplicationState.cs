namespace Distributor.Matrices;

public sealed class MatrixMultiplicationState
{
    private int _completedCount;

    public Lock Lock { get; } = new();

    public int IncrementCompletedCount()
    {
        return _completedCount++;
    }

    public int GetCompletedCount()
    {
        return Volatile.Read(ref _completedCount);
    }
}
