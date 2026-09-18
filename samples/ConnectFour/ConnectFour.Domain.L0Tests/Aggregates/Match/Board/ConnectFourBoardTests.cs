using MississippiSamples.ConnectFour.Domain.Aggregates.Match;
using MississippiSamples.ConnectFour.Domain.Aggregates.Match.Board;


namespace MississippiSamples.ConnectFour.Domain.L0Tests.Aggregates.Match.Board;

/// <summary>
///     Verifies the pure Connect Four board geometry and gravity rules.
/// </summary>
public sealed class ConnectFourBoardTests
{
    private static (ImmutableArray<DiscColor> Board, ImmutableArray<int> WinningCells) Play(
        int[] columns
    )
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        ImmutableArray<int> winningCells = [];
        for (int move = 0; move < columns.Length; move++)
        {
            DiscColor color = (move % 2) == 0 ? DiscColor.Red : DiscColor.Yellow;
            Assert.True(ConnectFourBoard.TryDropDisc(board, columns[move], color, out board, out int row));
            winningCells = ConnectFourBoard.FindWinningCells(board, columns[move], row, color);
        }

        return (board, winningCells);
    }

    /// <summary>
    ///     Applying a disc to an occupied cell is rejected instead of overwriting it.
    /// </summary>
    [Fact]
    public void ApplyingDiscToOccupiedCellThrows()
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.ApplyDisc(ConnectFourBoard.Empty, 0, 0, DiscColor.Red);
        Assert.Throws<InvalidOperationException>(() => ConnectFourBoard.ApplyDisc(board, 0, 0, DiscColor.Yellow));
    }

    /// <summary>
    ///     Applying an empty color is rejected as an invalid placement.
    /// </summary>
    [Fact]
    public void ApplyingEmptyColorThrows()
    {
        Assert.Throws<ArgumentException>(() => ConnectFourBoard.ApplyDisc(
            ConnectFourBoard.Empty,
            0,
            0,
            DiscColor.Empty));
    }

    /// <summary>
    ///     Default and empty immutable arrays normalize to a fresh empty board.
    /// </summary>
    [Fact]
    public void DefaultAndEmptyBoardsNormalizeToEmptyBoard()
    {
        ImmutableArray<DiscColor> defaultBoard = default;
        Assert.False(ConnectFourBoard.IsFull(defaultBoard));
        Assert.False(ConnectFourBoard.IsFull(ImmutableArray<DiscColor>.Empty));
        Assert.True(
            ConnectFourBoard.TryDropDisc(
                defaultBoard,
                0,
                DiscColor.Red,
                out ImmutableArray<DiscColor> updatedBoard,
                out int row));
        Assert.Equal(0, row);
        Assert.Equal(ConnectFourBoard.CellCount, updatedBoard.Length);
    }

    /// <summary>
    ///     Both diagonal fixtures find a four-cell red line without wrapping at an edge.
    /// </summary>
    /// <param name="fixture">The diagonal fixture index.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DiagonalFixturesFindWinningLines(
        int fixture
    )
    {
        int[] columns = fixture == 0 ? [0, 1, 1, 2, 3, 2, 2, 3, 4, 3, 3] : [6, 5, 5, 4, 3, 4, 4, 3, 2, 3, 3];
        (ImmutableArray<DiscColor> _, ImmutableArray<int> winningCells) = Play(columns);
        Assert.Equal(4, winningCells.Length);
    }

    /// <summary>
    ///     A disc falls from row zero upward and does not mutate the source board.
    /// </summary>
    [Fact]
    public void DiscFallsToLowestAvailableRowWithoutMutatingSource()
    {
        ImmutableArray<DiscColor> initial = ConnectFourBoard.Empty;
        Assert.True(
            ConnectFourBoard.TryDropDisc(
                initial,
                2,
                DiscColor.Red,
                out ImmutableArray<DiscColor> first,
                out int firstRow));
        Assert.True(
            ConnectFourBoard.TryDropDisc(
                first,
                2,
                DiscColor.Yellow,
                out ImmutableArray<DiscColor> second,
                out int secondRow));
        Assert.Equal(0, firstRow);
        Assert.Equal(1, secondRow);
        Assert.All(initial, cell => Assert.Equal(DiscColor.Empty, cell));
        Assert.Equal(DiscColor.Red, first[ConnectFourBoard.ToIndex(2, 0)]);
        Assert.Equal(DiscColor.Yellow, second[ConnectFourBoard.ToIndex(2, 1)]);
    }

    /// <summary>
    ///     The supplied 42-move fixture fills the board without an earlier win.
    /// </summary>
    [Fact]
    public void DrawFixtureFillsBoardWithoutWinningLine()
    {
        int[] columns =
        [
            1, 1, 0, 6, 6, 2, 4, 6, 4, 6, 2, 3, 3, 3, 5, 0, 3, 5, 1, 4, 5,
            0, 4, 1, 2, 5, 6, 1, 1, 5, 4, 3, 3, 5, 2, 0, 4, 2, 0, 6, 0, 2,
        ];
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        for (int move = 0; move < columns.Length; move++)
        {
            DiscColor color = (move % 2) == 0 ? DiscColor.Red : DiscColor.Yellow;
            Assert.True(ConnectFourBoard.TryDropDisc(board, columns[move], color, out board, out int row));
            Assert.Empty(ConnectFourBoard.FindWinningCells(board, columns[move], row, color));
        }

        Assert.True(ConnectFourBoard.IsFull(board));
    }

    /// <summary>
    ///     An empty color cannot produce a winning line.
    /// </summary>
    [Fact]
    public void EmptyColorWinningQueryReturnsNoLine()
    {
        Assert.Empty(ConnectFourBoard.FindWinningCells(ConnectFourBoard.Empty, 0, 0, DiscColor.Empty));
    }

    /// <summary>
    ///     An empty disc color is rejected without changing the board.
    /// </summary>
    [Fact]
    public void EmptyDiscColorRejectsDropWithoutChangingBoard()
    {
        ImmutableArray<DiscColor> initial = ConnectFourBoard.Empty;
        Assert.False(
            ConnectFourBoard.TryDropDisc(
                initial,
                3,
                DiscColor.Empty,
                out ImmutableArray<DiscColor> unchanged,
                out int row));
        Assert.Equal(-1, row);
        Assert.Equal(initial, unchanged);
    }

    /// <summary>
    ///     A full column rejects a further drop and does not change the board.
    /// </summary>
    [Fact]
    public void FullColumnRejectsFurtherDropWithoutChangingBoard()
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        for (int move = 0; move < ConnectFourBoard.RowCount; move++)
        {
            Assert.True(
                ConnectFourBoard.TryDropDisc(
                    board,
                    0,
                    (move % 2) == 0 ? DiscColor.Red : DiscColor.Yellow,
                    out board,
                    out int _));
        }

        ImmutableArray<DiscColor> fullBoard = board;
        Assert.False(
            ConnectFourBoard.TryDropDisc(
                fullBoard,
                0,
                DiscColor.Red,
                out ImmutableArray<DiscColor> unchanged,
                out int row));
        Assert.Equal(-1, row);
        Assert.Equal(fullBoard, unchanged);
    }

    /// <summary>
    ///     The horizontal fixture wins for red on its seventh accepted move.
    /// </summary>
    [Fact]
    public void HorizontalFixtureFindsWinningLineOnMoveSeven()
    {
        (ImmutableArray<DiscColor> Board, ImmutableArray<int> WinningCells) result = Play([0, 0, 1, 1, 2, 2, 3]);
        Assert.Equal(DiscColor.Red, result.Board[ConnectFourBoard.ToIndex(3, 0)]);
        Assert.Equal(
            [
                ConnectFourBoard.ToIndex(3, 0),
                ConnectFourBoard.ToIndex(2, 0),
                ConnectFourBoard.ToIndex(1, 0),
                ConnectFourBoard.ToIndex(0, 0),
            ],
            result.WinningCells);
    }

    /// <summary>
    ///     Invalid columns and rows are rejected by direct coordinate conversion.
    /// </summary>
    /// <param name="column">The zero-based column.</param>
    /// <param name="row">The zero-based row.</param>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(7, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 6)]
    public void InvalidCoordinatesThrowArgumentOutOfRangeException(
        int column,
        int row
    )
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ConnectFourBoard.ToIndex(column, row));
    }

    /// <summary>
    ///     Empty and partially populated boards are not full.
    /// </summary>
    [Fact]
    public void IsFullReturnsFalseForEmptyAndPartialBoards()
    {
        Assert.False(ConnectFourBoard.IsFull(ConnectFourBoard.Empty));
        ImmutableArray<DiscColor> partialBoard = ConnectFourBoard.ApplyDisc(
            ConnectFourBoard.Empty,
            0,
            0,
            DiscColor.Red);
        Assert.False(ConnectFourBoard.IsFull(partialBoard));
    }

    /// <summary>
    ///     Malformed board lengths are rejected before indexing.
    /// </summary>
    /// <param name="length">The malformed board length.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(43)]
    public void MalformedBoardLengthThrowsArgumentException(
        int length
    )
    {
        ImmutableArray<DiscColor> malformedBoard = ImmutableArray.CreateRange(new DiscColor[length]);
        Assert.Throws<ArgumentException>(() => ConnectFourBoard.IsFull(malformedBoard));
    }

    /// <summary>
    ///     A winning query whose origin cell has another color returns no line.
    /// </summary>
    [Fact]
    public void MismatchedOriginColorWinningQueryReturnsNoLine()
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.ApplyDisc(ConnectFourBoard.Empty, 1, 0, DiscColor.Yellow);
        Assert.Empty(ConnectFourBoard.FindWinningCells(board, 1, 0, DiscColor.Red));
    }

    /// <summary>
    ///     An out-of-range column is rejected without changing the board.
    /// </summary>
    /// <param name="column">The invalid zero-based column.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void OutOfRangeColumnRejectsDropWithoutChangingBoard(
        int column
    )
    {
        ImmutableArray<DiscColor> initial = ConnectFourBoard.Empty;
        Assert.False(
            ConnectFourBoard.TryDropDisc(
                initial,
                column,
                DiscColor.Red,
                out ImmutableArray<DiscColor> unchanged,
                out int row));
        Assert.Equal(-1, row);
        Assert.Equal(initial, unchanged);
    }

    /// <summary>
    ///     The vertical fixture wins for red on its seventh accepted move.
    /// </summary>
    [Fact]
    public void VerticalFixtureFindsWinningLineOnMoveSeven()
    {
        (ImmutableArray<DiscColor> Board, ImmutableArray<int> WinningCells) result = Play([0, 1, 0, 1, 0, 1, 0]);
        Assert.Equal(DiscColor.Red, result.Board[ConnectFourBoard.ToIndex(0, 3)]);
        Assert.Equal(4, result.WinningCells.Length);
        Assert.Contains(ConnectFourBoard.ToIndex(0, 0), result.WinningCells);
        Assert.Contains(ConnectFourBoard.ToIndex(0, 3), result.WinningCells);
    }

    /// <summary>
    ///     A board edge does not connect column six to column zero.
    /// </summary>
    [Fact]
    public void WinningDetectionDoesNotWrapAcrossBoardEdges()
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        foreach (int column in new[] { 5, 6, 0, 1 })
        {
            board = ConnectFourBoard.ApplyDisc(board, column, 0, DiscColor.Red);
        }

        Assert.Empty(ConnectFourBoard.FindWinningCells(board, 1, 0, DiscColor.Red));
    }
}