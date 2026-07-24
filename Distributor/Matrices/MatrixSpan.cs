using MathNet.Numerics.LinearAlgebra;

namespace Distributor.Matrices;

public sealed record MatrixSpan<T>
    where T : struct, IEquatable<T>, IFormattable
{
    private readonly Matrix<T> _matrix;
    private readonly int _rowIndex;
    private readonly int _columnIndex;

    public MatrixSpan(Matrix<T> matrix, int rowIndex, int rowCount, int columnIndex, int columnCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rowIndex, matrix.RowCount);

        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCount, matrix.RowCount - rowIndex);

        ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columnIndex, matrix.ColumnCount);

        ArgumentOutOfRangeException.ThrowIfNegative(columnCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columnCount, matrix.ColumnCount - columnIndex);

        _matrix = matrix;
        _rowIndex = rowIndex;
        _columnIndex = columnIndex;
        RowCount = rowCount;
        ColumnCount = columnCount;
    }

    public T this[int row, int column]
    {
        get
        {
            ValidateIndices(row, column);

            return _matrix[_rowIndex + row, _columnIndex + column];
        }
        set
        {
            ValidateIndices(row, column);

            _matrix[_rowIndex + row, _columnIndex + column] = value;
        }
    }

    public int ColumnCount { get; }
    public int ColumnIndex => _columnIndex;
    public int RowCount { get; }
    public int RowIndex => _rowIndex;

    public Matrix<T> ToMatrix()
    {
        return _matrix.SubMatrix(_rowIndex, RowCount, _columnIndex, ColumnCount);
    }

    private void ValidateIndices(int row, int column)
    {
        if ((uint)row >= (uint)RowCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(row),
                $"Matrix span row {row} is out of range [0, {RowCount - 1}]."
            );
        }

        if ((uint)column >= (uint)ColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                $"Matrix span column {column} is out of range [0, {ColumnCount - 1}]."
            );
        }
    }
}
