using System.Text.Json;

using Api.Entities;
using Api.Exceptions;

namespace Api.GameLogic;

public enum DiscColor
{
    Empty = 0,
    Red = 1,
    Yellow = 2
}

public struct ConnectFourMove
{
    required public Guid PlayerId { get; set; }
    public int Column { get; set; }
}

public class ConnectFourGame : IGame
{
    public string GameType => "ConnectFour";

    public List<User> Players { get; set; }
    public Dictionary<Guid, DiscColor> PlayerColors { get; set; } = new();
    public int[][] Board { get; set; } = new int[6][];
    public Guid? PlayerTurn { get; set; }
    public string? Winner { get; set; } = null;

    public ConnectFourGame(List<User> players)
    {
        Players = players;
        PlayerTurn = players[0].Id;

        PlayerColors[Players[0].Id] = DiscColor.Red;
        PlayerColors[Players[1].Id] = DiscColor.Yellow;

        for (var i = 0; i < 6; i++)
        {
            Board[i] = new int[7];
        }
    }

    public object GetState()
    {
        var currentPlayer = Players.FirstOrDefault(p => p.Id == PlayerTurn);
        return new
        {
            Board,
            PlayerTurn = currentPlayer?.Name,
            Winner,
            WinCounts = Players.Select(p => p.Wins).ToList()
        };
    }

    public bool MakeMove(JsonElement moveData)
    {
        if (!moveData.TryDeserialize(out ConnectFourMove move))
            return false;

        return ApplyMove(move.PlayerId, move.Column);
    }

    private bool ApplyMove(Guid playerId, int column)
    {
        if (Winner is not null)
            return false;

        if (playerId != PlayerTurn)
            return false;

        if (column < 0 || column >= 7)
            throw new InvalidMoveException($"Column {column} is out of bounds (valid: 0-6)", playerId);

        var row = -1;
        for (var r = 5; r >= 0; r--)
        {
            if (Board[r][column] == (int)DiscColor.Empty)
            {
                row = r;
                break;
            }
        }

        if (row == -1)
            throw new InvalidMoveException($"Column {column} is full", playerId);

        Board[row][column] = (int)PlayerColors[playerId];

        CheckWinner(row, column);

        if (Winner == "Red")
        {
            Winner = Players[0].Name;
            Players[0].Wins++;
        }
        else if (Winner == "Yellow")
        {
            Winner = Players[1].Name;
            Players[1].Wins++;
        }

        PlayerTurn = PlayerColors.FirstOtherKey(playerId);

        return true;
    }

    private void CheckWinner(int lastRow, int lastCol)
    {
        var color = Board[lastRow][lastCol];

        if (CheckDirection(lastRow, lastCol, 0, 1, color) ||
            CheckDirection(lastRow, lastCol, 0, -1, color))
        {
            Winner = ((DiscColor)color).ToString();
            return;
        }

        if (CheckDirection(lastRow, lastCol, 1, 0, color) ||
            CheckDirection(lastRow, lastCol, -1, 0, color))
        {
            Winner = ((DiscColor)color).ToString();
            return;
        }

        if (CheckDirection(lastRow, lastCol, 1, 1, color) ||
            CheckDirection(lastRow, lastCol, -1, -1, color))
        {
            Winner = ((DiscColor)color).ToString();
            return;
        }

        if (CheckDirection(lastRow, lastCol, 1, -1, color) ||
            CheckDirection(lastRow, lastCol, -1, 1, color))
        {
            Winner = ((DiscColor)color).ToString();
            return;
        }

        if (Board.IsBoardFull())
        {
            Winner = "Draw";
        }
    }

    private bool CheckDirection(int row, int col, int dRow, int dCol, int color)
    {
        var count = 1;

        var r = row + dRow;
        var c = col + dCol;
        while (r >= 0 && r < 6 && c >= 0 && c < 7 && Board[r][c] == color)
        {
            count++;
            r += dRow;
            c += dCol;
        }

        r = row - dRow;
        c = col - dCol;
        while (r >= 0 && r < 6 && c >= 0 && c < 7 && Board[r][c] == color)
        {
            count++;
            r -= dRow;
            c -= dCol;
        }

        return count >= 4;
    }
}