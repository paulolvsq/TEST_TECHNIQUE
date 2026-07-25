using Distributor.Matrices;
using MathNet.Numerics.LinearAlgebra;

namespace Distributor.Tests.Matrices;

public sealed class MatrixSplitterTests
{
    [Fact]
    public void Split_WhenMatrixSizeNotMultipleOfSpanSize_ClampsLastSpanToMatrixBounds()
    {
        var splitter = new MatrixSplitter();
        var matrix = Matrix<double>.Build.Dense(5, 5);

        var spans = splitter.Split(matrix, 3);

        spans.Length.ShouldBe(4);

        foreach (var span in spans)
        {
            (span.RowIndex + span.RowCount).ShouldBeLessThanOrEqualTo(5);
            (span.ColumnIndex + span.ColumnCount).ShouldBeLessThanOrEqualTo(5);
        }

        var lastSpan = spans[^1];
        (lastSpan.RowIndex + lastSpan.RowCount).ShouldBe(5);
        (lastSpan.ColumnIndex + lastSpan.ColumnCount).ShouldBe(5);
    }

    [Fact]
    public void Split_WhenMatrixSizeIsMultipleOfSpanSize_AllSpansExactlyFit()
    {
        var splitter = new MatrixSplitter();
        var matrix = Matrix<double>.Build.Dense(4, 4);

        var spans = splitter.Split(matrix, 2);

        spans.Length.ShouldBe(4);

        foreach (var span in spans)
        {
            span.RowCount.ShouldBe(2);
            span.ColumnCount.ShouldBe(2);
        }
    }

    [Fact]
    public void Split_GivenNonUniformDimensions_CoversEntireMatrix()
    {
        var splitter = new MatrixSplitter();
        var rows = 7;
        var columns = 5;
        var matrix = Matrix<double>.Build.Dense(rows, columns);

        var spans = splitter.Split(matrix, 3);

        var covered = new bool[rows, columns];

        foreach (var span in spans)
        {
            for (var row = span.RowIndex; row < span.RowIndex + span.RowCount; row++)
            {
                for (var column = span.ColumnIndex; column < span.ColumnIndex + span.ColumnCount; column++)
                {
                    covered[row, column] = true;
                }
            }
        }

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                covered[row, column].ShouldBeTrue($"Cell [{row},{column}] was not covered by any span.");
            }
        }
    }

    [Fact]
    public void Split_WhenSizeIsOne_TerminatesInsteadOfLoopingForever()
    {
	var splitter = new MatrixSplitter();
	var matrix = Matrix<double>.Build.Dense(3, 3);
	
	// avec size == 1, le pas column += size - 1 devient column += 0
	// cela ne fait jamais progresser la boucle et provoque des boucles infinies sur les colonnes de la matrice
	// je fais le choix de lancer le test en bornant le temps d'attente
	// pour ne pas bloquer indéfiniment le lancement de tous les tests
	var task = Task.Run(() => splitter.Split(matrix, 1));
	var completedInTime = task.Wait(TimeSpan.FromSeconds(2));

	completedInTime.ShouldBeTrue(
	    "Split(matrix, 1) did not finish within the allotted time: probable infinite loop."
	);

	// bonus : une fois qu'on sait que ça termine, on vérifie aussi que le résultat est correct — avec size == 1
	// chaque cellule doit former son propre span 1x1.
	var spans = task.Result;

	spans.Length.ShouldBe(matrix.RowCount * matrix.ColumnCount);

	foreach (var span in spans)
	{
	    span.RowCount.ShouldBe(1);
	    span.ColumnCount.ShouldBe(1);
	}
    }
    
}
