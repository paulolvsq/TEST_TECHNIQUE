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

    public Task<Matrix<T>> MultiplyAsync<T>(
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

        foreach (var resultSpan in spans)
        {
            var leftSpan = new MatrixSpan<T>(left, resultSpan.RowIndex, resultSpan.RowCount, 0, left.ColumnCount);
            var rightSpan = new MatrixSpan<T>(right, 0, right.RowCount, resultSpan.ColumnIndex, resultSpan.ColumnCount);

            lock (state.Lock)
            {
                var task = Task.Run(() => _spanMultiplier.MultiplyAsync(leftSpan, rightSpan, resultSpan, state), token);

                task.Wait(token);
            }
        }

        return Task.FromResult(result);
    }
}
