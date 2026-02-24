namespace backend.Infrastructure.Common
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }

        public bool IsFailure => !IsSuccess;
        public ServiceError? ErrorCode { get; }
        private readonly T? _value;
        public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access the value, the result is failure!");

        private ServiceResult(T? value)
        {
            IsSuccess = true;
            _value = value;
        }

        private ServiceResult(ServiceError error)
        {
            IsSuccess = false;
            ErrorCode = error;
        }

        public static ServiceResult<T> Success(T value) => new(value);

        public static ServiceResult<T> Failure(ServiceError error) => new(error);
    }
}