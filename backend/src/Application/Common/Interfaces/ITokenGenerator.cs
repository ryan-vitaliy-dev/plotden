namespace backend.Application.Common.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateToken(int byteLength = 16);
    }
}