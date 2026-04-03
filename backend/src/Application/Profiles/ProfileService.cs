using Application.Common.Interfaces;

namespace Application.Profiles
{
    public class ProfileService(IAppDbContext context)
    {
        private readonly IAppDbContext _context = context;

        /*

        + CreateProfileAsync
        + GetProfileByIdAsync
        + UpdateProfileAsync
        + DeleteProfileAsync
        */

        // public async Task<ServiceResult<bool>> CheckProfileExistsAsync(Guid profileId, CancellationToken clt)
        // {
            
        // }

            
            

            // if (await _context.Accounts.AnyAsync(a => a.Email == email, clt))
            // {
            //     return ServiceResult<bool>.Failure(ServiceError.EmailAlreadyInUse);
            // }

            // var account = new Account
            // {
            //     Email = email,
            //     PasswordHash = _passwordHasher.HashPassword(null!, password),
            //     Username = email.Split('@')[0],
            //     CreatedAt = DateTime.UtcNow
            // };

            // _context.Accounts.Add(account);
            // await _context.SaveChangesAsync(clt);

            // // Send verification email (not implemented here)

            // return ServiceResult<bool>.Success(true);
        //}




        // public async Task<ServiceResult<bool>> TestAsync(CancellationToken clt)
        // {
        //     Console.WriteLine("Received test GET.");
        //     bool sent = await _emailSender.SendAsync(new EmailMessage("bob@mail.com", "TestEmail", "Hey there!"), clt);
        //     if(sent)
        //     {
        //         return ServiceResult<bool>.Success(true);
        //     }
        //     else
        //     {
        //         return ServiceResult<bool>.Failure(ServiceError.UnknownError);
        //     }
        // }
    }
}