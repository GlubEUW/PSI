using Api.Enums;

namespace Api.Structs;

public struct RpsMove
{
   public required Guid PlayerId { get; set; }
   public RpsChoice Choice { get; set; }
}