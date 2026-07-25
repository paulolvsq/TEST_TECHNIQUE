namespace Distributor.Matrices;

public interface IMatrixSpanMultiplier
{
    Task MultiplyAsync<T>(
        MatrixSpan<T> left,
        MatrixSpan<T> right,
        MatrixSpan<T> result,
        MatrixMultiplicationState state
    )
        where T : struct, IEquatable<T>, IFormattable;
}

public sealed class MatrixSpanMultiplier : IMatrixSpanMultiplier
{
    public async Task MultiplyAsync<T>(
        MatrixSpan<T> left,
        MatrixSpan<T> right,
        MatrixSpan<T> result,
        MatrixMultiplicationState state
    )
        where T : struct, IEquatable<T>, IFormattable
    {
        await Task.Yield();

        var product = left.ToMatrix() * right.ToMatrix();

	// correction : suppression du bloc lock pour permettre l'écriture sur des segments distincts de la matrice
	for (var row = 0; row < result.RowCount; row++)
	{
	    for (var column = 0; column < result.ColumnCount; column++)
	    {
		result[row, column] = product[row, column];
	    }
	}

        state.IncrementCompletedCount();
    }
}
