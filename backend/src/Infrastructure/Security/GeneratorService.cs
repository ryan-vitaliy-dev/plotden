using backend.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

using backend.Infrastructure.Persistence;

namespace backend.Infrastructure.Security
{
    public class GeneratorService()
    {

        // public async Task<ServiceResult<string>> GenerateUniqueId(int byteLength = 16)
        // {

        //     byte[] idBytes = new byte[byteLength];
        //     using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        //     {
        //         rng.GetBytes(idBytes); // fills array with cryptographically secure random bytes
        //     }
        //     // encode as hexadecimal string, 32 chars for 128 bits (16 bytes)
                
        //     //id = BitConverter.ToString(idBytes).Replace("-", "").ToLowerInvariant();
        //     string id = Convert.ToHexStringLower(idBytes); // alternative method, also produces lowercase hex string
        //     return ServiceResult<string>.Success(id);
        // }

        // public async Task<ServiceResult<string>> GenerateUniqueSessionId(int byteLength = 16, int maxAttempts = 3, CancellationToken clt = default)
        // {
        //     string id;
        //     //bool alreadyExists;

        //     for (int attempt = 0; attempt < maxAttempts; attempt++)
        //     {
        //         ServiceResult<string> result = await GenerateUniqueId(byteLength);
        //         if (result.IsSuccess)
        //         {
        //             id = result.Value; // safe to access because we check Success, and if Success is true, Value is guaranteed to be non-null by the contract of ServiceResult<T>
        //             try
        //             {
        //                 // sanity check, collisions very very unlikely.
        //                 // alreadyExists = await _context.Sessions.AnyAsync(
        //                 //     s => s.SessionId == id,
        //                 //     clt
        //                 // ); 
        //                 // if (!alreadyExists)
        //                 // {
        //                 //     return ServiceResult<string>.Success(id);
        //                 // }
        //                 return ServiceResult<string>.Success(id);
        //             }
        //             catch (OperationCanceledException)
        //             {
        //                 return ServiceResult<string>.Failure(ServiceError.OperationCancelled);
        //             }
        //         }
        //     }
        //     // will likely never happen. Odds are astronomically low. Accounted for it anyways.
        //     return ServiceResult<string>.Failure(ServiceError.GenerationFailedError);
        // }
    }
}