using Api.Exceptions;

namespace Api.Utils;

public static class ExceptionLogger
{
    private static readonly string _log_Directory = "logs";
    private static readonly string _log_File_Name = "errors.log";
    private static readonly Lock _file_Lock = new();

    static ExceptionLogger()
    {
        if (!Directory.Exists(_log_Directory))
        {
            Directory.CreateDirectory(_log_Directory);
        }
    }

    public static void LogException(Exception exception, string context = "")
    {
        var logPath = Path.Combine(_log_Directory, _log_File_Name);
        var logEntry = FormatLogEntry(exception, context);

        lock (_file_Lock)
        {
            try
            {
                File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.Error.WriteLine(logEntry);
            }
        }
    }


    // Format: [Timestamp] LEVEL | Context | ExceptionType: Message | AdditionalData
    private static string FormatLogEntry(Exception exception, string context)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var exceptionType = exception.GetType().Name;
        var message = exception.Message;
        var stackTrace = exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "No stack trace";

        var logBuilder = new System.Text.StringBuilder();
        logBuilder.Append($"[{timestamp}] ERROR");

        if (!string.IsNullOrEmpty(context))
        {
            logBuilder.Append($" | Context: {context}");
        }

        logBuilder.Append($" | Type: {exceptionType}");
        logBuilder.Append($" | Message: {message}");

        if (exception is GameException gameEx)
        {
            logBuilder.Append($" | GameId: {gameEx.GameId ?? "N/A"}");

            if (gameEx is InvalidMoveException invalidMove)
            {
                logBuilder.Append($" | PlayerId: {invalidMove.PlayerId}");
                logBuilder.Append($" | Reason: {invalidMove.Reason}");
            }
        }
        else if (exception is LobbyException lobbyEx)
        {
            logBuilder.Append($" | LobbyCode: {lobbyEx.LobbyCode ?? "N/A"}");

            if (lobbyEx is LobbyFullException lobbyFull)
            {
                logBuilder.Append($" | Capacity: {lobbyFull.CurrentPlayers}/{lobbyFull.MaxPlayers}");
            }
            else if (lobbyEx is PlayerNotFoundException playerNotFound)
            {
                logBuilder.Append($" | PlayerId: {playerNotFound.PlayerId}");
            }
        }

        logBuilder.Append($" | StackTrace: {stackTrace}");

        return logBuilder.ToString();
    }
}