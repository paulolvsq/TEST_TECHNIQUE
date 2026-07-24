using Distributor.Matrices;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Distributor.Tests.Matrices;

public sealed class MatrixSpanMultiplierTests
{
    [Fact]
    public async Task MultiplyAsync_CalledConcurrently_ProducesAccurateCompletedCount()
    {
        var left = DenseMatrix.OfArray(
            new double[,]
            {
                { 1, 0 },
                { 0, 1 },
            }
        );
        var right = DenseMatrix.OfArray(
            new double[,]
            {
                { 1, 0 },
                { 0, 1 },
            }
        );
        var multiplier = new MatrixSpanMultiplier();
        var state = new MatrixMultiplicationState();
        var taskCount = 50;

        var tasks = Enumerable
            .Range(0, taskCount)
            .Select(_ =>
            {
                var result = Matrix<double>.Build.Dense(2, 2);
                var resultSpan = new MatrixSpan<double>(result, 0, 2, 0, 2);
                var leftSpan = new MatrixSpan<double>(left, 0, 2, 0, 2);
                var rightSpan = new MatrixSpan<double>(right, 0, 2, 0, 2);
                return Task.Run(() => multiplier.MultiplyAsync(leftSpan, rightSpan, resultSpan, state));
            })
            .ToArray();

        await Task.WhenAll(tasks);

        state.GetCompletedCount().ShouldBe(taskCount);
    }

    [Fact]
    public async Task MultiplyAsync_WithClampedTiling_ProducesCorrectResult()
    {
        var left = DenseMatrix.OfArray(
            new double[,]
            {
                { 1, 2, 3 },
                { 4, 5, 6 },
                { 7, 8, 9 },
                { 10, 11, 12 },
                { 13, 14, 15 },
            }
        );
        var right = DenseMatrix.OfArray(
            new double[,]
            {
                { 1, 2, 3, 4, 5 },
                { 6, 7, 8, 9, 10 },
                { 11, 12, 13, 14, 15 },
            }
        );
        var splitter = new MatrixSplitter();
        var multiplier = new MatrixSpanMultiplier();
        var state = new MatrixMultiplicationState();
        var tileSize = 3;

        var result = Matrix<double>.Build.Dense(left.RowCount, right.ColumnCount);
        var resultSpans = splitter.Split(result, tileSize);

        var tasks = resultSpans
            .Select(resultSpan =>
            {
                var leftSpan = new MatrixSpan<double>(
                    left,
                    resultSpan.RowIndex,
                    resultSpan.RowCount,
                    0,
                    left.ColumnCount
                );
                var rightSpan = new MatrixSpan<double>(
                    right,
                    0,
                    right.RowCount,
                    resultSpan.ColumnIndex,
                    resultSpan.ColumnCount
                );
                return Task.Run(() => multiplier.MultiplyAsync(leftSpan, rightSpan, resultSpan, state));
            })
            .ToArray();

        await Task.WhenAll(tasks);

        var expected = left * right;

        for (var row = 0; row < left.RowCount; row++)
        {
            for (var column = 0; column < right.ColumnCount; column++)
            {
                result[row, column]
                    .ShouldBe(
                        expected[row, column],
                        $"Mismatch at [{row},{column}]: expected {expected[row, column]} but got {result[row, column]}"
                    );
            }
        }
    }
}
