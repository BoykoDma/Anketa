using Anketa.Models;
using Anketa.Models.ConnectionDB;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Anketa.Services
{
    public class LogAndRegServices
    {
        private readonly Context _context;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private const int WorkFactor = 12;

        public LogAndRegServices(Context context, AuthenticationStateProvider authenticationStateProvider)
        {
            _context = context;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<bool> RegisterUser(string name, string email, string password)
        {
            try
            {
                if (_context.Users.Any(x => x.Name == name))
                    throw new Exception("Пользователь с таким именем уже существует");

                if (_context.Users.Any(x => x.Email == email))
                    throw new Exception("Пользователь с таким email уже существует");

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    throw new Exception("Пароль должен содержать не менее 6 символов");

                var user = new User
                {
                    Name = name,
                    Email = email,
                    Password = HashPassword(password),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await PerformLogin(user);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации: {ex.Message}");
            }
        }

        public async Task<bool> LoginUser(string name, string password)
        {
            try
            {
                Console.WriteLine($"=== LOGIN ATTEMPT ===");
                Console.WriteLine($"Username: {name}");

                var user = _context.Users.FirstOrDefault(x => x.Name == name);
                if (user == null)
                {
                    Console.WriteLine("User not found");
                    throw new Exception("Пользователь не найден");
                }

                if (!VerifyPassword(password, user.Password))
                {
                    Console.WriteLine("Invalid password");
                    throw new Exception("Неверный пароль");
                }

                Console.WriteLine($"User found: {user.Name}, ID: {user.Id}");

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await PerformLogin(user);

                var isAuth = await IsUserAuthenticated();
                Console.WriteLine($"After login - IsAuthenticated: {isAuth}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                throw new Exception($"Ошибка входа: {ex.Message}");
            }
        }

        public async Task LogoutUser()
        {
            try
            {
                await PerformLogout();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выхода: {ex.Message}");
            }
        }

        public async Task<bool> IsUserAuthenticated()
        {
            try
            {
                if (_authenticationStateProvider is ICustomAuthProvider customAuth)
                {
                    return await customAuth.IsUserAuthenticated();
                }

                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                return authState.User.Identity?.IsAuthenticated ?? false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetCurrentUserName()
        {
            try
            {
                if (_authenticationStateProvider is ICustomAuthProvider customAuth)
                {
                    return await customAuth.GetCurrentUserName();
                }

                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                return authState.User.Identity?.Name;
            }
            catch
            {
                return null;
            }
        }

        private async Task PerformLogin(User user)
        {
            Console.WriteLine($"Performing login for user: {user.Name}");

            if (_authenticationStateProvider is ICustomAuthProvider customAuth)
            {
                Console.WriteLine("Using ICustomAuthProvider");
                await customAuth.LoginUser(user.Name, user.Email, user.Role, user.Id.ToString());

                var isAuth = await customAuth.IsUserAuthenticated();
                Console.WriteLine($"Immediate check - IsAuthenticated: {isAuth}");
            }
            else
            {
                Console.WriteLine("Using standard AuthenticationStateProvider");
            }

            await Task.Delay(500);

            var finalAuth = await IsUserAuthenticated();
            Console.WriteLine($"Final check - IsAuthenticated: {finalAuth}");
        }

        private async Task PerformLogout()
        {
            if (_authenticationStateProvider is ICustomAuthProvider customAuth)
            {
                await customAuth.LogoutUser();
            }
            else
            {
                Console.WriteLine("Пользователь вышел из системы, но используется стандартный провайдер");
            }

            await Task.Delay(100);
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым");

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
