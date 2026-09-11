namespace prjFriendlyFoodWebAPI.Services.Common;

public enum ServiceResultStatus
{
    Success,
    Created,
    ValidationError,
    NotFound,
    Conflict,
    UnexpectedError
}

public sealed class ServiceResult<T>
{
    private ServiceResult(
        ServiceResultStatus status,
        string message,
        T? data,
        IReadOnlyCollection<string>? errors)
    {
        Status = status;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public ServiceResultStatus Status { get; }

    public string Message { get; }

    public T? Data { get; }

    public IReadOnlyCollection<string>? Errors { get; }

    public bool IsSuccess => Status is ServiceResultStatus.Success or ServiceResultStatus.Created;

    public static ServiceResult<T> Success(T data, string message = "操作成功") =>
        new(ServiceResultStatus.Success, message, data, null);

    public static ServiceResult<T> Created(T data, string message = "建立成功") =>
        new(ServiceResultStatus.Created, message, data, null);

    public static ServiceResult<T> Validation(
        string message,
        params string[] errors) =>
        new(ServiceResultStatus.ValidationError, message, default, errors);

    public static ServiceResult<T> NotFound(string message) =>
        new(ServiceResultStatus.NotFound, message, default, [message]);

    public static ServiceResult<T> Conflict(string message) =>
        new(ServiceResultStatus.Conflict, message, default, [message]);

    public static ServiceResult<T> Unexpected(string message) =>
        new(ServiceResultStatus.UnexpectedError, message, default, [message]);
}
