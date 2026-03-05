namespace ChessBrowser;

/// <summary>
/// Represents a single parsed chess game from PGN data.
/// </summary>
public class ChessGame
{
  public string EventName { get; set; } = "";
  public string Site { get; set; } = "";
  public string Round { get; set; } = "";
  public string EventDate { get; set; } = "";

  public string WhiteName { get; set; } = "";
  public string BlackName { get; set; } = "";
  public int? WhiteElo { get; set; }
  public int? BlackElo { get; set; }

  /// <summary>Result: 'W' = white wins, 'B' = black wins, 'D' = draw.</summary>
  public char Result { get; set; }

  /// <summary>Moves text verbatim.</summary>
  public string Moves { get; set; } = "";
}
