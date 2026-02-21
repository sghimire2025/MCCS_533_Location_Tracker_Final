using Microsoft.Extensions.Logging;
using SQLite;
using System.Text.Json;

namespace LocationTrackerFinal.Utilities
{
    /// <summary>
    /// Centralized error handling utility for the application.
    /// Provides consistent error handling, logging, and user-friendly error messages.
    /// </summary>
    public class ErrorHandler
    {
        private readonly ILogger _logger;

        public ErrorHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an async operation with comprehensive error handling.
        /// Catches and logs exceptions, and provides user-friendly error messages.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">A descriptive name for the operation (for logging)</param>
        /// <param name="onError">Optional callback to invoke when an error occurs (e.g., to emit error events)</param>
        /// <returns>A Result object containing either the success value or error information</returns>
        public async Task<Result<T>> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            Action<string>? onError = null)
        {
            try
            {
                var result = await operation();
                return Result<T>.Success(result);
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = "Network connection failed. Please check your internet connection.";
                _logger.LogError(ex, "Network error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result<T>.Failure(errorMessage, ex);
            }
            catch (SQLiteException ex)
            {
                var errorMessage = "Database error occurred. Your data may not be saved.";
                _logger.LogError(ex, "Database error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result<T>.Failure(errorMessage, ex);
            }
            catch (JsonException ex)
            {
                var errorMessage = "Failed to process server response.";
                _logger.LogError(ex, "JSON parsing error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result<T>.Failure(errorMessage, ex);
            }
            catch (OperationCanceledException ex)
            {
                // This is expected when operations are cancelled (e.g., stopping tracking)
                _logger.LogInformation("Operation cancelled: {OperationName}", operationName);
                return Result<T>.Failure("Operation cancelled", ex);
            }
            catch (Exception ex)
            {
                var errorMessage = "An unexpected error occurred.";
                _logger.LogError(ex, "Unexpected error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result<T>.Failure(errorMessage, ex);
            }
        }

        /// <summary>
        /// Executes an async operation that doesn't return a value with comprehensive error handling.
        /// </summary>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="operationName">A descriptive name for the operation (for logging)</param>
        /// <param name="onError">Optional callback to invoke when an error occurs</param>
        /// <returns>A Result object indicating success or failure</returns>
        public async Task<Result> ExecuteWithErrorHandlingAsync(
            Func<Task> operation,
            string operationName,
            Action<string>? onError = null)
        {
            try
            {
                await operation();
                return Result.Success();
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = "Network connection failed. Please check your internet connection.";
                _logger.LogError(ex, "Network error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result.Failure(errorMessage, ex);
            }
            catch (SQLiteException ex)
            {
                var errorMessage = "Database error occurred. Your data may not be saved.";
                _logger.LogError(ex, "Database error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result.Failure(errorMessage, ex);
            }
            catch (JsonException ex)
            {
                var errorMessage = "Failed to process server response.";
                _logger.LogError(ex, "JSON parsing error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result.Failure(errorMessage, ex);
            }
            catch (OperationCanceledException ex)
            {
                // This is expected when operations are cancelled
                _logger.LogInformation("Operation cancelled: {OperationName}", operationName);
                return Result.Failure("Operation cancelled", ex);
            }
            catch (Exception ex)
            {
                var errorMessage = "An unexpected error occurred.";
                _logger.LogError(ex, "Unexpected error in {OperationName}", operationName);
                onError?.Invoke(errorMessage);
                return Result.Failure(errorMessage, ex);
            }
        }
    }

    /// <summary>
    /// Represents the result of an operation that returns a value.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private Result(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null, null);
        public static Result<T> Failure(string errorMessage, Exception? exception = null) => 
            new Result<T>(false, default, errorMessage, exception);
    }

    /// <summary>
    /// Represents the result of an operation that doesn't return a value.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private Result(bool isSuccess, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result Success() => new Result(true, null, null);
        public static Result Failure(string errorMessage, Exception? exception = null) => 
            new Result(false, errorMessage, exception);
    }
}
