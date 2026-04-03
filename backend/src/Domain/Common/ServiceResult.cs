namespace Domain.Common
{
    /// <summary>
    /// Represents the result of a service operation, encapsulating either a successful value of type <typeparamref name="T"/>
    /// or a failure with an associated <see cref="ServiceError"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value returned in case of success.</typeparam>
    public class ServiceResult<T>
    {

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets a value indicating whether the operation failed.
        /// This is the inverse of <see cref="IsSuccess"/>.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Gets the error associated with a failed operation.
        /// Returns <c>null</c> if the operation was successful.
        /// </summary>
        public ServiceError? ErrorCode { get; }

        private readonly T? _value;

        /// <summary>
        /// Gets the value of the successful operation.
        /// Throws <see cref="InvalidOperationException"/> if accessed when the result represents a failure.
        /// </summary>
        public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access the value, the result is failure!");

        /// <summary>
        /// Initializes a new successful <see cref="ServiceResult{T}"/> with the specified value.
        /// </summary>
        /// <param name="value">The value of the successful operation.</param>
        private ServiceResult(T? value)
        {
            IsSuccess = true;
            _value = value;
        }

        /// <summary>
        /// Initializes a new failed <see cref="ServiceResult{T}"/> with the specified error.
        /// </summary>
        /// <param name="error">The error associated with the failure.</param>
        private ServiceResult(ServiceError error)
        {
            IsSuccess = false;
            ErrorCode = error;
        }
        
        /// <summary>
        /// Creates a new successful <see cref="ServiceResult{T}"/> with the provided value.
        /// </summary>
        /// <param name="value">The value of the successful operation.</param>
        /// <returns>A successful <see cref="ServiceResult{T}"/> containing the specified value.</returns>
        public static ServiceResult<T> Success(T value) => new(value);

        /// <summary>
        /// Creates a new failed <see cref="ServiceResult{T}"/> with the specified error.
        /// </summary>
        /// <param name="error">The error describing the failure.</param>
        /// <returns>A failed <see cref="ServiceResult{T}"/> containing the specified error.</returns>
        public static ServiceResult<T> Failure(ServiceError error) => new(error);
    }
}