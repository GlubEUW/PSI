using Xunit;
using Games;
using System;

namespace Games.Tests
{
    public class TicTacToeTests
    {
        [Fact]
        public void NewGame_StartsInProgressAndPlayer1()
        {
            var game = new TicTacToe();
            Assert.Equal("InProgress", game.State);
            Assert.Equal(1, game.CurrentPlayer);
        }

        [Fact]
        public void MakeMove_ValidMove_SucceedsAndAlternatesPlayer()
        {
            var game = new TicTacToe();

            bool ok = game.MakeMove(Tuple.Create(0, 0)); // X
            Assert.True(ok);
            Assert.Equal(2, game.CurrentPlayer);

            ok = game.MakeMove(Tuple.Create(0, 1)); // O
            Assert.True(ok);
            Assert.Equal(1, game.CurrentPlayer);
        }

        [Fact]
        public void MakeMove_InvalidMoves_ReturnFalse()
        {
            var game = new TicTacToe();

            Assert.True(game.MakeMove(Tuple.Create(0, 0)));
            Assert.False(game.MakeMove(Tuple.Create(0, 0))); // same cell
            Assert.False(game.MakeMove(Tuple.Create(3, 3))); // out of bounds
            Assert.False(game.MakeMove("not a tuple"));       // wrong type
        }

        [Fact]
        public void XWins_WhenCompletesTopRow()
        {
            var game = new TicTacToe();

            game.MakeMove(Tuple.Create(0, 0)); // X
            game.MakeMove(Tuple.Create(1, 0)); // O
            game.MakeMove(Tuple.Create(0, 1)); // X
            game.MakeMove(Tuple.Create(1, 1)); // O
            game.MakeMove(Tuple.Create(0, 2)); // X wins

            Assert.Equal("XWins", game.State);
            Assert.False(game.MakeMove(Tuple.Create(2, 2))); // no moves after win
        }

        [Fact]
        public void GameEnds_Draw()
        {
            var game = new TicTacToe();

            game.MakeMove(Tuple.Create(0, 0)); // X
            game.MakeMove(Tuple.Create(0, 1)); // O
            game.MakeMove(Tuple.Create(0, 2)); // X
            game.MakeMove(Tuple.Create(1, 2)); // O
            game.MakeMove(Tuple.Create(1, 0)); // X
            game.MakeMove(Tuple.Create(1, 1)); // O
            game.MakeMove(Tuple.Create(2, 1)); // X
            game.MakeMove(Tuple.Create(2, 0)); // O
            game.MakeMove(Tuple.Create(2, 2)); // X

            Assert.Equal("Draw", game.State);
        }

        [Fact]
        public void GetBoardCopy_ReturnsExpectedSymbols()
        {
            var game = new TicTacToe();

            game.MakeMove(Tuple.Create(0, 0)); // X
            game.MakeMove(Tuple.Create(1, 1)); // O

            var board = game.GetBoardCopy();
            Assert.Equal('X', board[0, 0]);
            Assert.Equal('O', board[1, 1]);
            Assert.Equal(' ', board[0, 1]);
        }
    }
}
