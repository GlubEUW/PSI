namespace Api.GameLogic;

public enum State
{
    Empty,
    X,
    O
}

public class TicTacToeGame : IGame
{
    public string GameType => "TicTacToe";
    
    public int[][] Board { get; set; } = new int[3][]
    {
        [0,0,0],
        [0,0,0],
        [0,0,0]
    };
    
    public bool IsXTurn { get; set; } = true;
    public string? Winner { get; set; } = null;

    public object GetState()
    {
        return new { Board, IsXTurn, Winner };
    }

    public bool MakeMove(string playerID, object moveData)
    {
        // moveData will be { x, y, isPlayerX }
        var move = moveData as TicTacToeMove;
        if (move == null) return false;

        return ApplyMove(move.X, move.Y, move.IsPlayerX);
    }

    private bool ApplyMove(int x, int y, bool isPlayerX)
    {
        if (Winner != null) return false;
        if (Board[x][y] != (int)State.Empty) return false;
        if ((IsXTurn && !isPlayerX) || (!IsXTurn && isPlayerX)) return false;

        Board[x][y] = isPlayerX ? (int)State.X : (int)State.O;
        CheckWinner();
        IsXTurn = !IsXTurn;
        return true;
    }

    private void CheckWinner()
    {
        int[][] b = Board;
        
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            int sum = b[i][0] + b[i][1] + b[i][2];
            if (sum == 3) { Winner = "X"; return; }
            if (sum == 6) { Winner = "O"; return; }
        }
        
        // Check columns
        for (int j = 0; j < 3; j++)
        {
            int sum = b[0][j] + b[1][j] + b[2][j];
            if (sum == 3) { Winner = "X"; return; }
            if (sum == 6) { Winner = "O"; return; }
        }
        
        // Check diagonals
        int diag1 = b[0][0] + b[1][1] + b[2][2];
        if (diag1 == 3) { Winner = "X"; return; }
        if (diag1 == 6) { Winner = "O"; return; }
        
        int diag2 = b[0][2] + b[1][1] + b[2][0];
        if (diag2 == 3) { Winner = "X"; return; }
        if (diag2 == 6) { Winner = "O"; return; }
    }

    public string? GetWinner() => Winner;
}

// Helper class for move data
public class TicTacToeMove
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsPlayerX { get; set; }
}