using System;
using System.Data;

namespace Games
{
    public class TicTacToe : ITurnBasedGame
    {
        private readonly char[,] board = new char[3, 3];
        private int movesMade = 0;
        private int currentPlayer = 1; // 1 = X, 2 = O
        private string state = "InProgress"; // InProgress, Draw, XWins, OWins

        public string State => state;
        public int CurrentPlayer => currentPlayer;

        public TicTacToe()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    board[i, j] = ' ';
        }

        // move should be a Tuple<int, int> representing (row, col)
        public bool MakeMove(object move)
        {
            if (state != "InProgress" || move is not Tuple<int, int> pos)
                return false;

            int row = pos.Item1;
            int col = pos.Item2;

            if (row < 0 || row > 2 || col < 0 || col > 2 || board[row, col] != ' ')
                return false;

            board[row, col] = currentPlayer == 1 ? 'X' : 'O';
            movesMade++;

            if (CheckWin(row, col))
                state = currentPlayer == 1 ? "XWins" : "OWins";
            else if (movesMade == 9)
                state = "Draw";
            else
                currentPlayer = 3 - currentPlayer; // Switch player

            return true;
        }

        private bool CheckWin(int row, int col)
        {
            char symbol = board[row, col];

            // Check row
            if (board[row, 0] == symbol && board[row, 1] == symbol && board[row, 2] == symbol)
                return true;
            // Check column
            if (board[0, col] == symbol && board[1, col] == symbol && board[2, col] == symbol)
                return true;
            // Check diagonals
            if (row == col && board[0, 0] == symbol && board[1, 1] == symbol && board[2, 2] == symbol)
                return true;
            if (row + col == 2 && board[0, 2] == symbol && board[1, 1] == symbol && board[2, 0] == symbol)
                return true;

            return false;
        }

        // Optional: For debugging or display
        public override string ToString()
        {
            return
                $"{board[0, 0]}|{board[0, 1]}|{board[0, 2]}\n" +
                $"-+-+-\n" +
                $"{board[1, 0]}|{board[1, 1]}|{board[1, 2]}\n" +
                $"-+-+-\n" +
                $"{board[2, 0]}|{board[2, 1]}|{board[2, 2]}";
        }
    }
}