using System.Security.Cryptography;
using backend.Application.Common.Interfaces;
using backend.Infrastructure.Common;

namespace backend.Infrastructure.Security
{
    public class SecureTokenGenerator : ITokenGenerator
    {
        public string GenerateToken(int byteLength = 16)
        {
            byte[] idBytes = new byte[byteLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(idBytes); // fills array with cryptographically secure random bytes
            }
            // encode as hexadecimal string, 32 chars for 128 bits (16 bytes)
                
            //id = BitConverter.ToString(idBytes).Replace("-", "").ToLowerInvariant();
            string id = Convert.ToHexStringLower(idBytes); // alternative method, also produces lowercase hex string
            return id;
        }
    }
}