namespace ScreenNap.Logging;

// Which directory log files go to and the notice about that choice (specification LOG-1,
// LOG-2, LOG-9, LOG-11); Directory is null when no directory can be used
internal readonly record struct LogDirectoryChoice(string? Directory, string? NoticeLevel, string? Notice)
{
    internal const string DirectoryVariable = "SCREENNAP_LOG_DIR";

    internal static LogDirectoryChoice Choose(string? configuredDirectory, string defaultDirectory, Func<string, bool> tryCreate)
    {
        ArgumentNullException.ThrowIfNull(tryCreate);
        if (string.IsNullOrEmpty(configuredDirectory))
        {
            return tryCreate(defaultDirectory)
                ? new LogDirectoryChoice(defaultDirectory, Logger.InfoLevel, $"{DirectoryVariable} is not set; logging to {defaultDirectory}")
                : default;
        }

        if (Path.IsPathFullyQualified(configuredDirectory) && tryCreate(configuredDirectory))
            return new LogDirectoryChoice(configuredDirectory, null, null);

        return tryCreate(defaultDirectory)
            ? new LogDirectoryChoice(defaultDirectory, Logger.WarnLevel, $"{DirectoryVariable} \"{configuredDirectory}\" is not an absolute directory path that can be created; logging to {defaultDirectory}")
            : default;
    }
}
