using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Handles execution of external processes like pactl and bluetoothctl.
    /// Implement this interface to customize process execution behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts external process execution, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize process timeout and execution parameters</description></item>
    /// <item><description>Implement process execution logging and monitoring</description></item>
    /// <item><description>Add retry logic for failed process executions</description></item>
    /// <item><description>Mock process execution for testing scenarios</description></item>
    /// <item><description>Implement alternative execution strategies</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom implementation with retry logic
    /// public class RetryingProcessRunner : IProcessRunner
    /// {
    ///     public async Task&lt;ProcessResult&gt; RunAsync(string command, string arguments, CancellationToken cancellationToken = default)
    ///     {
    ///         for (int attempt = 0; attempt &lt; 3; attempt++)
    ///         {
    ///             var result = await ExecuteProcessAsync(command, arguments, cancellationToken);
    ///             if (result.Success || attempt == 2) return result;
    ///             await Task.Delay(1000, cancellationToken);
    ///         }
    ///         return new ProcessResult { ExitCode = -1, StandardError = "Max retries exceeded" };
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IProcessRunner
    {
        /// <summary>
        /// Executes a process with the specified command and arguments.
        /// </summary>
        /// <param name="command">The command to execute (e.g., "pactl", "bluetoothctl").</param>
        /// <param name="arguments">The arguments to pass to the command.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The result of the process execution including exit code and output.</returns>
        Task<ProcessResult> RunAsync(string command, string arguments, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a command exists in the system PATH.
        /// </summary>
        /// <param name="command">The command name to check (e.g., "pactl").</param>
        /// <returns>True if the command exists and can be executed; otherwise, false.</returns>
        Task<bool> CommandExistsAsync(string command);
    }

    /// <summary>
    /// Represents the result of a process execution.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Gets or sets the exit code of the process.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output from the process.
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the standard error from the process.
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the process executed successfully (exit code 0).
        /// </summary>
        public bool Success => ExitCode == 0;
    }
}