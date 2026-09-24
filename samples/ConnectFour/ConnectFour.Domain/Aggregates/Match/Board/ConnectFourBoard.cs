using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace MississippiSamples.ConnectFour.Domain.Aggregates.Match.Board;

/// <summary>
///     Provides pure board operations for the Connect Four aggregate.
/// </summary>
internal static class ConnectFourBoard
{
    /// <summary>
    ///     The total number of cells on a Connect Four board.
    /// </summary>
    public const int CellCount = ColumnCount * RowCount;

    /// <summary>
    ///     The number of columns on a Connect Four board.
    /// </summary>
    public const int ColumnCount = 7;

    /// <summary>
    ///     The number of rows on a Connect Four board.
    /// </summary>
    public const int RowCount = 6;

    /// <summary>
    ///     Gets a fresh immutable empty board value.
    /// </summary>
    public static ImmutableArray<DiscColor> Empty { get; } = ImmutableArray.Create(new DiscColor[CellCount]);

    /// <summary>
    ///     Applies an accepted disc placement to a board.
    /// </summary>
    /// <param name="board">The board to copy.</param>
    /// <param name="column">The zero-based column.</param>
    /// <param name="row">The zero-based row.</param>
    /// <param name="color">The disc color.</param>
    /// <returns>A new board containing the disc.</returns>
    public static ImmutableArray<DiscColor> ApplyDisc(
        ImmutableArray<DiscColor> board,
        int column,
        int row,
        DiscColor color
    )
    {
        ImmutableArray<DiscColor> normalizedBoard = Normalize(board);
        ValidatePosition(column, row);
        if (color is DiscColor.Empty)
        {
            throw new ArgumentException("A placed disc must have a player color.", nameof(color));
        }

        int index = ToIndex(column, row);
        if (normalizedBoard[index] is not DiscColor.Empty)
        {
            throw new InvalidOperationException("A disc cannot be placed in an occupied cell.");
        }

        ImmutableArray<DiscColor>.Builder cells = normalizedBoard.ToBuilder();
        cells[index] = color;
        return cells.ToImmutable();
    }

    /// <summary>
    ///     Finds the contiguous winning line containing the newly placed disc.
    /// </summary>
    /// <param name="board">The board after the disc was placed.</param>
    /// <param name="column">The zero-based column of the new disc.</param>
    /// <param name="row">The zero-based row of the new disc.</param>
    /// <param name="color">The color of the new disc.</param>
    /// <returns>The winning cell indexes, or an empty array when there is no win.</returns>
    public static ImmutableArray<int> FindWinningCells(
        ImmutableArray<DiscColor> board,
        int column,
        int row,
        DiscColor color
    )
    {
        ImmutableArray<DiscColor> normalizedBoard = Normalize(board);
        ValidatePosition(column, row);
        if (color is DiscColor.Empty || (normalizedBoard[ToIndex(column, row)] != color))
        {
            return [];
        }

        (int ColumnDelta, int RowDelta)[] directions =
        [
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1),
        ];
        foreach ((int columnDelta, int rowDelta) in directions)
        {
            List<int> line = [ToIndex(column, row)];
            AddDirection(line, normalizedBoard, column, row, color, columnDelta, rowDelta);
            AddDirection(line, normalizedBoard, column, row, color, -columnDelta, -rowDelta);
            if (line.Count >= 4)
            {
                return line.ToImmutableArray();
            }
        }

        return [];
    }

    /// <summary>
    ///     Determines whether every board cell is occupied.
    /// </summary>
    /// <param name="board">The board to inspect.</param>
    /// <returns><c>true</c> when no empty cells remain; otherwise, <c>false</c>.</returns>
    public static bool IsFull(
        ImmutableArray<DiscColor> board
    ) =>
        Normalize(board).All(cell => cell is not DiscColor.Empty);

    /// <summary>
    ///     Converts a zero-based board coordinate to its stable row-major index.
    /// </summary>
    /// <param name="column">The zero-based column.</param>
    /// <param name="row">The zero-based row.</param>
    /// <returns>The row-major cell index.</returns>
    public static int ToIndex(
        int column,
        int row
    )
    {
        ValidatePosition(column, row);
        return (row * ColumnCount) + column;
    }

    /// <summary>
    ///     Attempts to drop a disc into the lowest empty cell in a column.
    /// </summary>
    /// <param name="board">The board to copy.</param>
    /// <param name="column">The zero-based column.</param>
    /// <param name="color">The disc color.</param>
    /// <param name="updatedBoard">The updated board when the drop succeeds.</param>
    /// <param name="row">The zero-based row selected by gravity.</param>
    /// <returns><c>true</c> when a cell was available; otherwise, <c>false</c>.</returns>
    public static bool TryDropDisc(
        ImmutableArray<DiscColor> board,
        int column,
        DiscColor color,
        out ImmutableArray<DiscColor> updatedBoard,
        out int row
    )
    {
        ImmutableArray<DiscColor> normalizedBoard = Normalize(board);
        updatedBoard = normalizedBoard;
        row = -1;
        if (((uint)column >= ColumnCount) || color is DiscColor.Empty)
        {
            return false;
        }

        for (int candidateRow = 0; candidateRow < RowCount; candidateRow++)
        {
            if (normalizedBoard[ToIndex(column, candidateRow)] is DiscColor.Empty)
            {
                row = candidateRow;
                updatedBoard = ApplyDisc(normalizedBoard, column, candidateRow, color);
                return true;
            }
        }

        return false;
    }

    private static void AddDirection(
        List<int> line,
        ImmutableArray<DiscColor> board,
        int column,
        int row,
        DiscColor color,
        int columnDelta,
        int rowDelta
    )
    {
        int candidateColumn = column + columnDelta;
        int candidateRow = row + rowDelta;
        while (((uint)candidateColumn < ColumnCount) &&
               ((uint)candidateRow < RowCount) &&
               (board[ToIndex(candidateColumn, candidateRow)] == color))
        {
            line.Add(ToIndex(candidateColumn, candidateRow));
            candidateColumn += columnDelta;
            candidateRow += rowDelta;
        }
    }

    private static ImmutableArray<DiscColor> Normalize(
        ImmutableArray<DiscColor> board
    )
    {
        if (board.IsDefaultOrEmpty)
        {
            return Empty;
        }

        if (board.Length != CellCount)
        {
            throw new ArgumentException($"A Connect Four board must contain exactly {CellCount} cells.", nameof(board));
        }

        return board;
    }

    private static void ValidatePosition(
        int column,
        int row
    )
    {
        if ((uint)column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "The column must be between 0 and 6.");
        }

        if ((uint)row >= RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "The row must be between 0 and 5.");
        }
    }
}