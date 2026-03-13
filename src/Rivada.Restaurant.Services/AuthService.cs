using System;
using Rivada.Restaurant.Data.Repositories;
using Rivada.Restaurant.Models;
using Rivada.Restaurant.Services.Interfaces;

namespace Rivada.Restaurant.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public User? Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return _userRepository.GetUserByUsernameAndPassword(username.Trim(), password);
        }
    }
}
