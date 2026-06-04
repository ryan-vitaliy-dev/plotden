using System.Net;

namespace Application.Common
{
    public record ClientInfo(IPAddress? IpAddress, string? UserAgent);
}