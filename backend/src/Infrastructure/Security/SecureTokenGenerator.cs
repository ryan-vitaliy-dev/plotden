using System.Security.Cryptography;

using Application.Common.Interfaces;
// using Infrastructure.Common;

namespace Infrastructure.Security
{
    public class SecureTokenGenerator : ITokenGenerator
    {
        public string GenerateToken(int byteLength = 32)
        {
            byte[] idBytes = new byte[byteLength];

            // old from dotnet 8:
            // using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            // {
            //     rng.GetBytes(idBytes); // fills array with cryptographically secure random bytes
            // }

            byte[] bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToHexStringLower(bytes);

            // encode as hexadecimal string, 32 chars for 128 bits (16 bytes)
        }
    }
}