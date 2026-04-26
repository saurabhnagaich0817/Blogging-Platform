namespace InkWell.Shared.Responses;

// Base response without data (e.g., for simple success confirmations like Deletes)
public class BaseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public BaseResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

// Generic response with data (e.g., for GET requests or creation responses)
public class BaseResponse<T> : BaseResponse
{
    public T? Data { get; set; }

    public BaseResponse(bool success, string message, T? data) : base(success, message)
    {
        Data = data;
    }
}
