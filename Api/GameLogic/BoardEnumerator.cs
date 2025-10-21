using System.Collections;
namespace Api.GameLogic;

public class BoardEnumerator : IEnumerable<(int row, int col, State value)>
{
    private readonly int[][] _board;

    public BoardEnumerator(int[][] board)
    {
        _board = board;
    }

    public IEnumerator<(int row, int col, State value)> GetEnumerator()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                yield return (i, j, (State)_board[i][j]);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}