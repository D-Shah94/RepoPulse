namespace RepoPulse.API.Options
{
    /// <summary>
    /// Strongly-typed representation of the "GitHubApi" section in appsettings.json
    /// Using the Options pattern ensures configuration is validated at startup
    /// and prevents magic strings from polluting the application logic
    /// </summary>
    public class GitHubApiOptions
    {
        /// <summary>
        /// The base URL for the GitHub API (e.g., https://api.github.com).
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Time-To-Live in minutes for cached API responses
        /// Defaulting to 60 as a sensible baseline
        /// </summary>
        public int CacheTtlMinutes { get; set; } = 60;

        /// <summary>
        /// The required User-Agent string to send with every GitHub request
        /// GitHub rejects requests without a valid User-Agent
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;
    }
}
