namespace AppserverMCP.Interfaces;

public interface IPlatformService
{
    Task<string> GetAccessTokenAsync();
}
