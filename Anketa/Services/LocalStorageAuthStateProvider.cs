using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Anketa.Services
{
    public class LocalStorageAuthStateProvider : AuthenticationStateProvider, ICustomAuthProvider
    {
        private readonly LocalStorageAuthService _localStorage;
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageAuthStateProvider(LocalStorageAuthService localStorage, IJSRuntime jsRuntime)
        {
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var userSession = await _localStorage.GetUserSessionAsync();

                if (userSession == null)
                {
                    Console.WriteLine("Сессия не найдена");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                if (userSession.ExpiryTime < DateTime.UtcNow)
                {
                    Console.WriteLine("Сессия истекла");
                    await _localStorage.RemoveUserSessionAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                if ((userSession.ExpiryTime - DateTime.UtcNow).TotalDays < 1)
                {
                    userSession.ExpiryTime = DateTime.UtcNow.AddDays(7);
                    await _localStorage.SaveUserSessionAsync(userSession);
                }

                Console.WriteLine($"Пользователь авторизован: {userSession.UserName}");
                return CreateAuthenticationState(userSession);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения состояния: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task LoginUser(string userName, string email, string role, string userId)
        {
            try
            {
                Console.WriteLine($"Выполняется вход пользователя: {userName}");

                var userSession = new UserSession
                {
                    UserName = userName,
                    Email = email,
                    Role = role,
                    UserId = userId,
                    LoginTime = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddDays(7)
                };

                await _localStorage.SaveUserSessionAsync(userSession);
                var authState = CreateAuthenticationState(userSession);

                Console.WriteLine("Уведомление об изменении состояния аутентификации");
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                throw new Exception($"Ошибка входа: {ex.Message}");
            }
        }

        public async Task LogoutUser()
        {
            try
            {
                Console.WriteLine("Выполняется выход пользователя");

                await _localStorage.RemoveUserSessionAsync();
                var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

                Console.WriteLine("Уведомление об изменении состояния аутентификации");
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выхода: {ex.Message}");
                throw new Exception($"Ошибка выхода: {ex.Message}");
            }
        }

        public async Task<bool> IsUserAuthenticated()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                var isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
                Console.WriteLine($"Проверка аутентификации: {isAuthenticated}");
                return isAuthenticated;
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
                var authState = await GetAuthenticationStateAsync();
                var userName = authState.User.Identity?.Name;
                Console.WriteLine($"Текущий пользователь: {userName}");
                return userName;
            }
            catch
            {
                return null;
            }
        }

        private AuthenticationState CreateAuthenticationState(UserSession userSession)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role),
                new Claim("UserId", userSession.UserId),
                new Claim("LoginTime", userSession.LoginTime.ToString("O"))
            };

            var identity = new ClaimsIdentity(claims, "LocalStorageAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}
