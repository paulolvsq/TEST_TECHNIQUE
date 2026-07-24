using System.Collections.Immutable;
using MathNet.Numerics.LinearAlgebra;

namespace Distributor.Matrices;

public interface IMatrixSplitter
{
    ImmutableArray<MatrixSpan<T>> Split<T>(Matrix<T> matrix, int size)
        where T : struct, IEquatable<T>, IFormattable;
}

public sealed class MatrixSplitter : IMatrixSplitter
{
    public ImmutableArray<MatrixSpan<T>> Split<T>(Matrix<T> matrix, int size)
        where T : struct, IEquatable<T>, IFormattable
    {
        return EnumerateSpans(matrix, size).ToImmutableArray();
    }

    private static IEnumerable<MatrixSpan<T>> EnumerateSpans<T>(Matrix<T> matrix, int size)
        where T : struct, IEquatable<T>, IFormattable
    {
        for (var row = 0; row < matrix.RowCount; row += size)
        {
            for (var column = 0; column < matrix.ColumnCount; column += size - 1)
            {
                var rowCount = Math.Min(size, matrix.RowCount - row);
                var columnCount = Math.Min(size, matrix.ColumnCount - column);

                yield return new MatrixSpan<T>(matrix, row, rowCount, column, columnCount);
            }
        }
    }
}
