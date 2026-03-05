namespace ChessBrowser;

public class PngParser
{
    public static List<ChessGame> ParseFileGames(string fileContent)
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
            
            // get tag value: whatever's between the first and last double quotes
            int firstQuoteIdx = tagName.IndexOf('"');
            int lastQuoteIdx = tagName.LastIndexOf('"');
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
                    game.Site = tagValue;
                    break;
                case "Date":
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
                        _ => '?'
                    };
                    break;
                case "WhiteElo":
                    // todo: might have to handle if tries to parse a non-int value
                    game.WhiteElo = int.Parse(tagValue);
                    break;
                case "BlackElo":
                    // todo: might have to handle if tries to parse a non-int value
                    game.BlackElo = int.Parse(tagValue);
                    break;
            }
        }
        
        // add the moves, and separate each element (line) with a new line
        game.Moves = string.Join("\n", movesLines).Trim();
        
        return game;
    }
}