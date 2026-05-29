namespace Domain.Common
{
    public enum ServiceError
    {
        /*
        NOTE: This will eventually need to be refactored. I'm still learning so I'm not sure how best to do it.
            Some possible options are:
            1. Make ServiceResult have a generic error code type parameter, so that each service can define its own error codes. 
            - This would allow for more service-specific error handling, but the controllers would have duplicate logic for generic error codes. 
            - Maybe make a new base controller or something?
        */

        // GENERAL
        InvalidInput, // for cases where the input is invalid

        //Conflict, // for cases where the operation cannot be completed due to a conflict with the current state of the resource, e.g. trying to create a user with an email that already exists.
        DbError,
        OperationCancelled,
        GenerationFailedError,
        UnknownError,

        // ACCOUNT
        InvalidCredentials,
        NoAccountFound,
        UsernameTaken,
        PasswordAlreadySet,
        PasswordNotSet,
        AccountNotVerified,
        
        // TOKEN
        BadTokenType,
        NoTokenFound,
        TokenExpired,
        TokenInvalid,

        // SESSION
        SessionCreationFailed,
        InvalidSession,
    }
}