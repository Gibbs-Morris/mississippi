using System;
using System.Diagnostics.CodeAnalysis;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Represents the result of an aggregate operation that may succeed or fail.
/// </summary>
/// <remarks>
///     This is a discriminated union type that carries either a success indicator
///     or an error code with message. Use <see cref="OperationResult.Ok" /> and
///     <see cref="OperationResult.Fail" /> factory methods to create instances.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Aggregates.OperationResult")]
public readonly record struct OperationResult
{
    [Id(0)]
    private readonly bool success;

    [Id(1)]
    private readonly string? errorCode;

    [Id(2)]
    private readonly string? errorMessage;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OperationResult" /> struct.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    /// <param name="errorMessage">The error message if the operation failed.</param>
    private OperationResult(
        bool success,
        string? errorCode,
        string? errorMessage
    )
    {
        this.success = success;
        this.errorCode = errorCode;
        this.errorMessage = errorMessage;
    }

    /// <summary>
    ///     Gets a value indicating whether this instance was default-initialized.
    /// </summary>
    private bool IsDefault => !success && (errorCode is null) && (errorMessage is null);

    /// <summary>
    ///     Gets the error code when the operation failed.
    /// </summary>
    public string? ErrorCode => errorCode;

    /// <summary>
    ///     Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage => errorMessage;

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(false, nameof(ErrorCode))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public bool Success => success || IsDefault;

    /// <summary>
    ///     Creates a failed operation result with the specified error details.
    /// </summary>
    /// <param name="errorCode">The error code identifying the failure type.</param>
    /// <param name="errorMessage">A human-readable description of the failure.</param>
    /// <returns>A failed <see cref="OperationResult" />.</returns>
    public static OperationResult Fail(
        string errorCode,
        string errorMessage
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new(false, errorCode, errorMessage);
    }

    /// <summary>
    ///     Creates a failed operation result with the specified error details.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorCode">The error code identifying the failure type.</param>
    /// <param name="errorMessage">A human-readable description of the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}" />.</returns>
    public static OperationResult<T> Fail<T>(
        string errorCode,
        string errorMessage
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return OperationResult<T>.CreateFailed(errorCode, errorMessage);
    }

    /// <summary>
    ///     Creates a successful operation result.
    /// </summary>
    /// <returns>A successful <see cref="OperationResult" />.</returns>
    public static OperationResult Ok() => new(true, null, null);

    /// <summary>
    ///     Creates a successful operation result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="OperationResult{T}" />.</returns>
    public static OperationResult<T> Ok<T>(
        T value
    ) =>
        OperationResult<T>.CreateSuccess(value);
}

/// <summary>
///     Represents the result of an aggregate operation that may succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
///     This is a discriminated union type that carries either a success value
///     or an error code with message. Use the static <c>Ok</c> and <c>Fail</c>
///     factory methods to create instances.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Aggregates.OperationResult`1")]
public readonly record struct OperationResult<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OperationResult{T}" /> struct.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="value">The success value.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    /// <param name="errorMessage">The error message if the operation failed.</param>
    private OperationResult(
        bool success,
        T? value,
        string? errorCode,
        string? errorMessage
    )
    {
        Success = success;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Gets a value indicating whether this instance was default-initialized.
    /// </summary>
    private bool IsDefault => !Success && (ErrorCode is null) && (ErrorMessage is null);

    /// <summary>
    ///     Gets the error code when the operation failed.
    /// </summary>
    [Id(2)]
    public string? ErrorCode { get; }

    /// <summary>
    ///     Gets the error message when the operation failed.
    /// </summary>
    [Id(3)]
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    [Id(0)]
    [MemberNotNullWhen(false, nameof(ErrorCode))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    [MemberNotNullWhen(true, nameof(Value))]
    public bool Success { get; }


    /// <summary>
    ///     Gets the success value when the operation succeeded.
    /// </summary>
    [Id(1)]
    public T? Value { get; }

    /// <summary>
    ///     Creates a failed operation result with the specified error details.
    /// </summary>
    /// <param name="errorCode">The error code identifying the failure type.</param>
    /// <param name="errorMessage">A human-readable description of the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}" />.</returns>
    internal static OperationResult<T> CreateFailed(
        string errorCode,
        string errorMessage
    ) =>
        new(false, default, errorCode, errorMessage);

    /// <summary>
    ///     Creates a successful operation result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="OperationResult{T}" />.</returns>
    internal static OperationResult<T> CreateSuccess(
        T value
    ) =>
        new(true, value, null, null);

    /// <summary>
    ///     Converts this result to a non-generic <see cref="OperationResult" />.
    /// </summary>
    /// <returns>An <see cref="OperationResult" /> with the same success/failure state.</returns>
    public OperationResult ToResult() => (IsDefault || Success) ? OperationResult.Ok() : OperationResult.Fail(ErrorCode, ErrorMessage);
}