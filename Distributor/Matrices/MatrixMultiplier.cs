using MathNet.Numerics.LinearAlgebra;

namespace Distributor.Matrices;

public interface IMatrixMultiplier
{
    Task<Matrix<T>> MultiplyAsync<T>(Matrix<T> left, Matrix<T> right, int spanSize, CancellationToken token = default)
        where T : struct, IEquatable<T>, IFormattable;
}

public sealed class MatrixMultiplier : IMatrixMultiplier
{
    private readonly IMatrixSplitter _splitter;
    private readonly IMatrixSpanMultiplier _spanMultiplier;

    public MatrixMultiplier(IMatrixSplitter splitter, IMatrixSpanMultiplier spanMultiplier)
    {
        _splitter = splitter;
        _spanMultiplier = spanMultiplier;
    }

    // décalaration async de la méthode
    public async Task<Matrix<T>> MultiplyAsync<T>(
        Matrix<T> left,
        Matrix<T> right,
        int spanSize,
        CancellationToken token = default
    )
        where T : struct, IEquatable<T>, IFormattable
    {
        var result = Matrix<T>.Build.Dense(left.RowCount, right.ColumnCount);
        var spans = _splitter.Split(result, spanSize);
        var state = new MatrixMultiplicationState();

	// première correction : création de la liste de tâches
	var tasks = new List<Task>();

        foreach (var resultSpan in spans)
        {
            var leftSpan = new MatrixSpan<T>(left, resultSpan.RowIndex, resultSpan.RowCount, 0, left.ColumnCount);
            var rightSpan = new MatrixSpan<T>(right, 0, right.RowCount, resultSpan.ColumnIndex, resultSpan.ColumnCount);

	    // deuxième correction : on ajoute la tâche à la liste créée en la lançant et on supprime le bloc lock
	    tasks.Add(Task.Run(() => _spanMultiplier.MultiplyAsync(leftSpan, rightSpan, resultSpan, state), token));

        }

	// troiisème correction : on attend que toutes les tâches aient terminé de s'exécuter
	await Task.WhenAll(tasks);
	
        return result;
    }
}
