namespace Api.Structs;

public struct TicTacToeMove
{
   required public Guid PlayerId { get; set; }
   public int X { get; set; }
   public int Y { get; set; }
}