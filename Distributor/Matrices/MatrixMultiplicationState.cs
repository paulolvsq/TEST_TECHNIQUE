namespace Distributor.Matrices;

public sealed class MatrixMultiplicationState
{
    private int _completedCount;

    public Lock Lock { get; } = new();

    public int IncrementCompletedCount()
    {
	// correction : opération atomique pour incrémenter le compteur
        return Interlocked.Increment(ref _completedCount);
    }

    public int GetCompletedCount()
    {
        return Volatile.Read(ref _completedCount);
    }
}
