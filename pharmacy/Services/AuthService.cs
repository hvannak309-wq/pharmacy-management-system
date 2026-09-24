using pharmacy.DTOs;
using pharmacy.Helpers;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Services
{
    public class AuthService : IAuthService
    {
        private readonly Interfaces.IUserRepository _users;
        private readonly IUnitOfWork _uow;

        public AuthService(Interfaces.IUserRepository users, IUnitOfWork uow)
        {
            _users = users;
            _uow = uow;
        }

        public async Task<Result<User>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<User>.Failure("Username and password required.");

            var user = await _users.GetByUsernameAsync(username.Trim());
            if (user is null || !user.IsActive)
                return Result<User>.Failure("Invalid username or password.");
            if (!PasswordHasher.Verify(password, user.PasswordHash))
                return Result<User>.Failure("Invalid username or password.");

            return Result<User>.Success(user);
        }

        public async Task<Result<int>> CreateUserAsync(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                return Result<int>.Failure("Username required.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return Result<int>.Failure("Password must be at least 6 characters.");
            if (await _users.FindAsync(u => u.Username == user.Username) is { Count: > 0 })
                return Result<int>.Failure("Username already taken.");

            user.PasswordHash = PasswordHasher.Hash(password);
            await _users.AddAsync(user);
            await _uow.SaveChangesAsync();
            return Result<int>.Success(user.Id);
        }

        public async Task<Result<int>> UpdateUserAsync(User user, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                return Result<int>.Failure("Username required.");
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 6)
                    return Result<int>.Failure("Password must be at least 6 characters.");
                user.PasswordHash = PasswordHasher.Hash(newPassword);
            }

            _users.Update(user);
            await _uow.SaveChangesAsync();
            return Result<int>.Success(user.Id);
        }
    }
}
