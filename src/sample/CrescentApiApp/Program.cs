namespace Mississippi.CrescentApiApp;

/// <summary>
///     The main program class for the user's application.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     The application entry point.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        await app.RunAsync();
    }
}