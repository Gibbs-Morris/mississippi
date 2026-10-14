using MississippiSamples.ConnectFour.Domain.Aggregates.Match;
using MississippiSamples.ConnectFour.Domain.Aggregates.Match.Board;


namespace MississippiSamples.ConnectFour.Domain.L0Tests.Aggregates.Match.Board;

/// <summary>
///     Verifies that only player colors can be placed or identified as a winning line.
/// </summary>
public sealed class InvalidDiscColorTests
{
    /// <summary>
    ///     Applying an undefined color rejects the placement and preserves the source board.
    /// </summary>
    /// <param name="value">The undefined serialized color value.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ApplyingUndefinedColorThrowsWithoutChangingBoard(
        int value
    )
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ConnectFourBoard.ApplyDisc(
            board,
            0,
            0,
            (DiscColor)value));
        Assert.Equal("color", exception.ParamName);
        Assert.All(board, cell => Assert.Equal(DiscColor.Empty, cell));
    }

    /// <summary>
    ///     Dropping an undefined color rejects the move without consuming a cell.
    /// </summary>
    /// <param name="value">The undefined serialized color value.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void DroppingUndefinedColorReturnsFalseWithoutChangingBoard(
        int value
    )
    {
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        Assert.False(
            ConnectFourBoard.TryDropDisc(
                board,
                0,
                (DiscColor)value,
                out ImmutableArray<DiscColor> unchanged,
                out int row));
        Assert.Equal(-1, row);
        Assert.Equal(board, unchanged);
        Assert.All(board, cell => Assert.Equal(DiscColor.Empty, cell));
    }

    /// <summary>
    ///     A full board must contain only valid player colors.
    /// </summary>
    /// <param name="value">The undefined serialized color value.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void IsFullRejectsUndefinedCellOccupants(
        int value
    )
    {
        DiscColor[] cells = new DiscColor[ConnectFourBoard.CellCount];
        Array.Fill(cells, DiscColor.Red);
        cells[ConnectFourBoard.CellCount - 1] = (DiscColor)value;
        Assert.False(ConnectFourBoard.IsFull(ImmutableArray.Create(cells)));
    }

    /// <summary>
    ///     Either valid player color can fill every cell.
    /// </summary>
    /// <param name="value">The valid player color value.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void IsFullStillAcceptsPlayerColors(
        int value
    )
    {
        DiscColor color = (DiscColor)value;
        DiscColor[] cells = new DiscColor[ConnectFourBoard.CellCount];
        Array.Fill(cells, color);
        Assert.True(ConnectFourBoard.IsFull(ImmutableArray.Create(cells)));
    }

    /// <summary>
    ///     Either player color can still create a horizontal winning line.
    /// </summary>
    /// <param name="value">The valid player color value.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PlayerColorsStillProduceWinningLines(
        int value
    )
    {
        DiscColor color = (DiscColor)value;
        ImmutableArray<DiscColor> board = ConnectFourBoard.Empty;
        for (int column = 0; column < 4; column++)
        {
            Assert.True(ConnectFourBoard.TryDropDisc(board, column, color, out board, out int row));
            Assert.Equal(0, row);
        }

        Assert.Equal([3, 2, 1, 0], ConnectFourBoard.FindWinningCells(board, 3, 0, color));
    }

    /// <summary>
    ///     Undefined cell occupants cannot be announced as a player winning line.
    /// </summary>
    /// <param name="value">The undefined serialized color value.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void UndefinedColorWinningQueryReturnsNoLine(
        int value
    )
    {
        DiscColor[] cells = new DiscColor[ConnectFourBoard.CellCount];
        cells[0] = (DiscColor)value;
        cells[1] = (DiscColor)value;
        cells[2] = (DiscColor)value;
        cells[3] = (DiscColor)value;
        Assert.Empty(ConnectFourBoard.FindWinningCells(ImmutableArray.Create(cells), 3, 0, (DiscColor)value));
    }
}