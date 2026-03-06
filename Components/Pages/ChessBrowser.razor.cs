using Microsoft.AspNetCore.Components.Forms;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using ChessBrowser;

namespace ChessBrowser.Components.Pages
{
  public partial class ChessBrowser
  {
    /// <summary>
    /// Bound to the Unsername form input
    /// </summary>
    private string Username = "";

    /// <summary>
    /// Bound to the Password form input
    /// </summary>
    private string Password = "";

    /// <summary>
    /// Bound to the Database form input
    /// </summary>
    private string Database = "";

    /// <summary>
    /// Represents the progress percentage of the current
    /// upload operation. Update this value to update 
    /// the progress bar.
    /// </summary>
    private int    Progress = 0;

    /// <summary>
    /// This method runs when a PGN file is selected for upload.
    /// Given a list of lines from the selected file, parses the 
    /// PGN data, and uploads each chess game to the user's database.
    /// </summary>
    /// <param name="PGNFileLines">The lines from the selected file</param>
    private async Task InsertGameData(string[] PGNFileLines)
    {
      // This will build a connection string to your user's database on atr,
      // assuimg you've filled in the credentials in the GUI
      string connection = GetConnectionString();

      // Parse the provided PGN data
      string fileContent = string.Join("\n", PGNFileLines);
      List<ChessGame> games = PgnParser.ParseGamesFile(fileContent);
      if (games.Count == 0)
        return;

      using (MySqlConnection conn = new MySqlConnection(connection))
      {
        try
        {
          // Open a connection
          conn.Open();

          // Iterate through your data and generate appropriate insert commands
          int total = games.Count;
          int processed = 0;
          foreach (ChessGame g in games)
          {
            object eventDateParam;
            string ed = (g.EventDate ?? "").Trim();
            if (string.IsNullOrEmpty(ed)) eventDateParam = DBNull.Value;
            else
            {
              string[] parts = ed.Split('.');
              if (parts.Length != 3) eventDateParam = DBNull.Value;
              else if (int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2], out int d) && y >= 1 && y <= 9999 && m >= 1 && m <= 12 && d >= 1 && d <= 31)
                eventDateParam = new DateTime(y, m, d);
              else eventDateParam = DBNull.Value;
            }

            int eID;
            using (var cmdEvent = new MySqlCommand("SELECT eID FROM Events WHERE Name = @name AND Site = @site AND `Date` <=> @eventDate LIMIT 1", conn))
            {
              cmdEvent.Parameters.AddWithValue("@name", g.EventName);
              cmdEvent.Parameters.AddWithValue("@site", g.Site);
              cmdEvent.Parameters.AddWithValue("@eventDate", eventDateParam);
              object? eidObj = cmdEvent.ExecuteScalar();
              if (eidObj != null && eidObj != DBNull.Value)
                eID = Convert.ToInt32(eidObj);
              else
              {
                using (var cmdIns = new MySqlCommand("INSERT INTO Events (Name, Site, `Date`) VALUES (@name, @site, @eventDate)", conn))
                {
                  cmdIns.Parameters.AddWithValue("@name", g.EventName);
                  cmdIns.Parameters.AddWithValue("@site", g.Site);
                  cmdIns.Parameters.AddWithValue("@eventDate", eventDateParam);
                  cmdIns.ExecuteNonQuery();
                }
                eID = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID()", conn).ExecuteScalar());
              }
            }

            int whitePID;
            using (var cmdP = new MySqlCommand("SELECT pID, Elo FROM Players WHERE Name = @name LIMIT 1", conn))
            {
              cmdP.Parameters.AddWithValue("@name", g.WhiteName);
              using (var r = cmdP.ExecuteReader())
              {
                if (r.Read())
                {
                  whitePID = r.GetInt32("pID");
                  int? curElo = r.IsDBNull(r.GetOrdinal("Elo")) ? null : r.GetInt32("Elo");
                  r.Close();
                  if (g.WhiteElo.HasValue && (!curElo.HasValue || g.WhiteElo.Value > curElo.Value))
                  {
                    using (var cmdU = new MySqlCommand("UPDATE Players SET Elo = @elo WHERE pID = @pID", conn))
                    {
                      cmdU.Parameters.AddWithValue("@elo", g.WhiteElo.Value);
                      cmdU.Parameters.AddWithValue("@pID", whitePID);
                      cmdU.ExecuteNonQuery();
                    }
                  }
                }
                else
                {
                  r.Close();
                  using (var cmdI = new MySqlCommand("INSERT INTO Players (Name, Elo) VALUES (@name, @elo)", conn))
                  {
                    cmdI.Parameters.AddWithValue("@name", g.WhiteName);
                    cmdI.Parameters.AddWithValue("@elo", (object?)g.WhiteElo ?? DBNull.Value);
                    cmdI.ExecuteNonQuery();
                  }
                  whitePID = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID()", conn).ExecuteScalar());
                }
              }
            }

            int blackPID;
            using (var cmdP = new MySqlCommand("SELECT pID, Elo FROM Players WHERE Name = @name LIMIT 1", conn))
            {
              cmdP.Parameters.AddWithValue("@name", g.BlackName);
              using (var r = cmdP.ExecuteReader())
              {
                if (r.Read())
                {
                  blackPID = r.GetInt32("pID");
                  int? curElo = r.IsDBNull(r.GetOrdinal("Elo")) ? null : r.GetInt32("Elo");
                  r.Close();
                  if (g.BlackElo.HasValue && (!curElo.HasValue || g.BlackElo.Value > curElo.Value))
                  {
                    using (var cmdU = new MySqlCommand("UPDATE Players SET Elo = @elo WHERE pID = @pID", conn))
                    {
                      cmdU.Parameters.AddWithValue("@elo", g.BlackElo.Value);
                      cmdU.Parameters.AddWithValue("@pID", blackPID);
                      cmdU.ExecuteNonQuery();
                    }
                  }
                }
                else
                {
                  r.Close();
                  using (var cmdI = new MySqlCommand("INSERT INTO Players (Name, Elo) VALUES (@name, @elo)", conn))
                  {
                    cmdI.Parameters.AddWithValue("@name", g.BlackName);
                    cmdI.Parameters.AddWithValue("@elo", (object?)g.BlackElo ?? DBNull.Value);
                    cmdI.ExecuteNonQuery();
                  }
                  blackPID = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID()", conn).ExecuteScalar());
                }
              }
            }

            using (var cmdGame = new MySqlCommand("INSERT IGNORE INTO Games (eID, WhitePlayer, BlackPlayer, Round, Result, Moves) VALUES (@eID, @whitePlayer, @blackPlayer, @round, @result, @moves)", conn))
            {
              cmdGame.Parameters.AddWithValue("@eID", eID);
              cmdGame.Parameters.AddWithValue("@whitePlayer", whitePID);
              cmdGame.Parameters.AddWithValue("@blackPlayer", blackPID);
              cmdGame.Parameters.AddWithValue("@round", g.Round);
              cmdGame.Parameters.AddWithValue("@result", g.Result.ToString());
              string moves = g.Moves ?? "";
              cmdGame.Parameters.AddWithValue("@moves", moves.Length > 2000 ? moves.Substring(0, 2000) : moves);
              cmdGame.ExecuteNonQuery();
            }

            // Update the Progress member variable every time progress has been made
            // (e.g. one iteration of your upload loop)
            // This will update the progress bar in the GUI
            // Its value should be an integer representing a percentage of completion
            processed++;
            Progress = (int)(100.0 * processed / total);
            // This tells the GUI to redraw after you update Progress (this should go inside your loop)
            await InvokeAsync(StateHasChanged);
          }
        }
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine(e.Message);
          await InvokeAsync(() => { Results = "Upload error: " + e.Message; StateHasChanged(); });
        }
      }

    }


    /// <summary>
    /// Queries the database for games that match all the given filters.
    /// The filters are taken from the various controls in the GUI.
    /// </summary>
    /// <param name="white">The white player, or "" if none</param>
    /// <param name="black">The black player, or "" if none</param>
    /// <param name="opening">The first move, e.g. "1.e4", or "" if none</param>
    /// <param name="winner">The winner as "W", "B", "D", or "" if none</param>
    /// <param name="useDate">true if the filter includes a date range, false otherwise</param>
    /// <param name="start">The start of the date range</param>
    /// <param name="end">The end of the date range</param>
    /// <param name="showMoves">true if the returned data should include the PGN moves</param>
    /// <returns>A string separated by newlines containing the filtered games</returns>
    private string PerformQuery(string white, string black, string opening,
      string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
    {
      // This will build a connection string to your user's database on atr,
      // assuimg you've typed a user and password in the GUI
      string connection = GetConnectionString();

      // Build up this string containing the results from your query
      string parsedResult = "";

      // Use this to count the number of rows returned by your query
      // (see below return statement)
      int numRows = 0;

      using (MySqlConnection conn = new MySqlConnection(connection))
      {
        try
        {
          // Open a connection
          conn.Open();

          // Generate and execute an SQL command,
          // then parse the results into an appropriate string and return it.
          string selectList = "Events.Name AS eName, Events.Site AS eSite, Events.`Date` AS eDate, WhiteP.Name AS wName, WhiteP.Elo AS wElo, BlackP.Name AS bName, BlackP.Elo AS bElo, Games.Result";
          if (showMoves)
            selectList += ", Games.Moves AS Moves";
          string sql = "SELECT " + selectList + " FROM Games JOIN Events ON Games.eID = Events.eID JOIN Players AS WhiteP ON Games.WhitePlayer = WhiteP.pID JOIN Players AS BlackP ON Games.BlackPlayer = BlackP.pID WHERE 1=1";
          if (!string.IsNullOrEmpty(white)) sql += " AND WhiteP.Name = @white";
          if (!string.IsNullOrEmpty(black)) sql += " AND BlackP.Name = @black";
          if (!string.IsNullOrEmpty(winner)) sql += " AND Games.Result = @winner";
          if (!string.IsNullOrEmpty(opening)) sql += " AND Games.Moves LIKE CONCAT(@opening, '%')";
          if (useDate) sql += " AND Events.`Date` >= @startDate AND Events.`Date` <= @endDate";

          using (var cmd = new MySqlCommand(sql, conn))
          {
            if (!string.IsNullOrEmpty(white)) cmd.Parameters.AddWithValue("@white", white);
            if (!string.IsNullOrEmpty(black)) cmd.Parameters.AddWithValue("@black", black);
            if (!string.IsNullOrEmpty(winner)) cmd.Parameters.AddWithValue("@winner", winner);
            if (!string.IsNullOrEmpty(opening)) cmd.Parameters.AddWithValue("@opening", opening);
            if (useDate) { cmd.Parameters.AddWithValue("@startDate", start); cmd.Parameters.AddWithValue("@endDate", end); }

            using (var r = cmd.ExecuteReader())
            {
              int eNameOrd = r.GetOrdinal("eName");
              int eSiteOrd = r.GetOrdinal("eSite");
              int eDateOrd = r.GetOrdinal("eDate");
              int wNameOrd = r.GetOrdinal("wName");
              int wEloOrd = r.GetOrdinal("wElo");
              int bNameOrd = r.GetOrdinal("bName");
              int bEloOrd = r.GetOrdinal("bElo");
              int resultOrd = r.GetOrdinal("Result");
              int movesOrd = showMoves ? r.GetOrdinal("Moves") : -1;

              var blocks = new List<string>();
              while (r.Read())
              {
                string dateStr;
                try { dateStr = r.IsDBNull(eDateOrd) ? "?" : r.GetDateTime(eDateOrd).ToString("MM/dd/yyyy"); }
                catch { dateStr = "?"; }
                string wEloStr = r.IsDBNull(wEloOrd) ? "" : " (" + r.GetInt32(wEloOrd) + ")";
                string bEloStr = r.IsDBNull(bEloOrd) ? "" : " (" + r.GetInt32(bEloOrd) + ")";
                string block = "Event: " + r.GetString(eNameOrd) + "\nSite: " + r.GetString(eSiteOrd) + "\nDate: " + dateStr + "\nWhite: " + r.GetString(wNameOrd) + wEloStr + "\nBlack: " + r.GetString(bNameOrd) + bEloStr + "\nResult: " + r.GetString(resultOrd);
                if (showMoves && movesOrd >= 0 && !r.IsDBNull(movesOrd))
                  block += "\n" + r.GetString(movesOrd);
                blocks.Add(block);
              }
              numRows = blocks.Count;
              parsedResult = string.Join("\n\n", blocks);
            }
          }
        }
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine(e.Message);
        }
      }

      return numRows + " results\n\n" + parsedResult;
    }


    private string GetConnectionString()
    {
      return "server=atr.eng.utah.edu;database=" + Database + ";uid=" + Username + ";password=" + Password;
    }


    /// <summary>
    /// This method will run when the file chooser is used.
    /// It loads the files contents as an array of strings,
    /// then invokes the InsertGameData method.
    /// </summary>
    /// <param name="args">The event arguments, which contains the selected file name</param>
    private async void HandleFileChooser(EventArgs args)
    {
      try
      {
        string fileContent = string.Empty;

        InputFileChangeEventArgs eventArgs = args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
        if (eventArgs.FileCount == 1)
        {
          var file = eventArgs.File;
          if (file is null)
          {
            return;
          }

          // load the chosen file and split it into an array of strings, one per line
          using var stream = file.OpenReadStream(52428800); // max 50MB
          using var reader = new StreamReader(stream);                   
          fileContent = await reader.ReadToEndAsync();
          string[] fileLines = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

          // insert the games, and don't wait for it to finish
          // _ = throws away the task result, since we aren't waiting for it
          _ = InsertGameData(fileLines);
        }
      }
      catch (Exception e)
      {
        Debug.WriteLine("an error occurred while loading the file..." + e);
      }
    }

  }

}
