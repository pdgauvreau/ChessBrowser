namespace ChessBrowser;

public class PgnParser
{
    /**
     * A method that takes the values from a pgn file, and creates Chess Games containing
     * the values of each game from the file.
     *
     * fileContent (string): a string containing the contents of the pgn file
     */
    public static List<ChessGame> ParseGamesFile(string fileContent)
    {
        var games = new List<ChessGame>();
        var lines = fileContent.Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            // skip blank line that separates each game
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            
            if (i >= lines.Length)
                break;
            
            // collect the tagged data -- []
            var tagLines = new List<string>();
            while (i < lines.Length && lines[i].TrimStart().StartsWith("["))
            {
                tagLines.Add(lines[i]);
                i++;
            }

            if (tagLines.Count == 0)
                break;
            
            // skip the blank line that separates the tagged data and the moves
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
                i++;
            
            // collect the lines of moves data -- go until reach the next blank line
            var movesLines = new List<string>();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]) && !lines[i].TrimStart().StartsWith("["))
            {
                movesLines.Add(lines[i]);
                i++;
            }

            var game = ParseChessGame(tagLines, movesLines);            
            games.Add(game);
        }
        
        return games;
    }

    /**
     * A private helper method that creates a ChessGame object containing the values from
     * a chess game's string of tag lines and moves lines.
     */
    private static ChessGame ParseChessGame(List<string> tagLines, List<string> movesLines)
    {
        // create a chess game containing the data from the file
        var game = new ChessGame();

        foreach (var line in tagLines)
        {
            if (!line.StartsWith("["))
                continue;   
            
            // get tag name: whatever's between '[' and the first ' ' (space)
            int spaceIdx = line.IndexOf(' ');
            if (spaceIdx < 0)
                continue;
            string tagName = line.Substring(1, spaceIdx - 1);
            
            // get tag value: whatever's between the first and last double quotes (in the line)
            int firstQuoteIdx = line.IndexOf('"');
            int lastQuoteIdx = line.LastIndexOf('"');
            if (firstQuoteIdx < 0 || firstQuoteIdx == lastQuoteIdx)
                continue;
            string tagValue = line.Substring(firstQuoteIdx + 1, lastQuoteIdx - firstQuoteIdx - 1);
            
            // set the values for the chess game
            switch (tagName)
            {
                case "Event":
                    game.EventName = tagValue;
                    break;
                case "Site":
                    game.Site = string.IsNullOrWhiteSpace(tagValue) ? "?" : tagValue;
                    break;
                case "Date":
                    game.GameDate = tagValue;
                    break;
                case "EventDate":
                    game.EventDate = tagValue;
                    break;
                case "Round":
                    game.Round = tagValue;
                    break;
                case "White":
                    game.WhiteName = tagValue;
                    break;
                case "Black":
                    game.BlackName = tagValue;
                    break;
                case "Result":
                    game.Result = tagValue switch
                    {
                        "1-0" => 'W',
                        "0-1" => 'B',
                        "1/2-1/2" => 'D',
                        _ => 'D'
                    };
                    break;
                case "WhiteElo":
                    game.WhiteElo = int.TryParse(tagValue, out int we) && we >= 0 ? we : null;
                    break;
                case "BlackElo":
                    game.BlackElo = int.TryParse(tagValue, out int be) && be >= 0 ? be : null;
                    break;
            }
        }
        
        // add the moves, and separate each element (line) with a new line
        game.Moves = string.Join("\n", movesLines).Trim();
        
        return game;
    }
}