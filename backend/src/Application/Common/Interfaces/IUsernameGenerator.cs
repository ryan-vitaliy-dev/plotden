namespace Application.Common.Interfaces
{
    public interface IUsernameGenerator
    {
        Task<string> GenerateUsername();
    }
}