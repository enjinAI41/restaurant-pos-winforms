using Rivada.Restaurant.Models;

namespace Rivada.Restaurant.Services.Interfaces
{
    public interface IAuthService
    {
        User? Authenticate(string username, string password);
    }
}
