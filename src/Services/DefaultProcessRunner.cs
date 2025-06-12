using BlueZNet.Interfaces;
using BlueZNet.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Services
{
    /// <summary>
    /// Default implementation of process runner for executing external commands.
    /// </summary>
    public class DefaultProcessRunner : IProcessRunner
    {
        private readonly ILogger _logger;
        private readonly BlueZNetConfiguration _configuration;
        private readonly ConcurrentDictionary<string, bool> _commandExistsCache = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance with default configuration.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public DefaultProcessRunner(ILogger logger = null)
            : this(logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom configuration.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public DefaultProcessRunner(ILogger logger, BlueZNetConfiguration configuration)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _configuration = configuration ?? BlueZNetConfiguration.Default;
        }

        /// <inheritdoc />
        public async Task<ProcessResult> RunAsync(string command, string arguments, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            Process process = null;
            try
            {
                if (_configuration.EnableVerboseLogging)
                {
                    _logger.LogTrace("Executing command: {Command} {Arguments}", command, arguments);
                }

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments ?? "",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                var timeout = Task.Delay(_configuration.ProcessTimeoutMs, cancellationToken);
                var processTask = Task.Run(() => process.WaitForExit(), cancellationToken);

                var completedTask = await Task.WhenAny(processTask, timeout);

                if (completedTask == timeout)
                {
                    _logger.LogWarning("Command {Command} timed out after {Timeout}ms", command, _configuration.ProcessTimeoutMs);

                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to kill timed out process {Command}", command);
                    }

                    return new ProcessResult
                    {
                        ExitCode = -1,
                        StandardError = "Process timed out"
                    };
                }

                var output = await outputTask;
                var error = await errorTask;

                var result = new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = output ?? "",
                    StandardError = error ?? ""
                };

                if (_configuration.EnableVerboseLogging)
                {
                    _logger.LogTrace("Command {Command} completed with exit code {ExitCode}", command, result.ExitCode);
                }

                if (!result.Success)
                {
                    _logger.LogDebug("Command {Command} failed: {Error}", command, result.StandardError);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Command {Command} was cancelled", command);

                try
                {
                    if (process != null && !process.HasExited)
                        process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill cancelled process {Command}", command);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute command: {Command} {Arguments}", command, arguments);

                return new ProcessResult
                {
                    ExitCode = -1,
                    StandardError = ex.Message
                };
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <inheritdoc />
        public async Task<bool> CommandExistsAsync(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            if (_commandExistsCache.TryGetValue(command, out var cachedResult))
                return cachedResult;

            try
            {
                var result = await RunAsync("which", command);
                var exists = result.Success;

                _commandExistsCache[command] = exists;

                if (_configuration.EnableVerboseLogging)
                {
                    _logger.LogTrace("Command existence check for {Command}: {Exists}", command, exists);
                }

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check if command {Command} exists", command);
                _commandExistsCache[command] = false;
                return false;
            }
        }
    }
}