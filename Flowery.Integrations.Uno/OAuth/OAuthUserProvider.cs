using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls.Users;
using Flowery.Services;
using IdentityModel.OidcClient;

namespace Flowery.Integrations.Uno.OAuth;

/// <summary>
/// Generic OAuth2/OpenID Connect user provider.
/// </summary>
public class OAuthUserProvider : IUserProvider, ITokenSaveProvider, ITokenStateProvider, IInteractiveAuthProvider
{
    private static readonly HttpClient HttpClient = new();

    public string ProviderKey { get; }
    public string DisplayName { get; }
    public string ImplementationVersion => "1.0.0";
    public bool SupportsAvatars { get; set; } = true;
    public bool SupportsPresence { get; set; } = false;
    public bool SupportsRealtime { get; set; } = false;

    public event Action? UsersChanged;

    private readonly string _authority;
    private readonly string _clientId;
    private readonly ISecureStorage? _secureStorage;
    private readonly string _resourceKey;
    private const string HasTokenKey = "HasToken";
    private readonly OidcHybridClient _authClient;
    private readonly SemaphoreSlim _userInfoLock = new(1, 1);
    private string? _userInfoEndpoint;
    private FlowUser? _currentUser;
    private string? _accessToken;
    private string? _refreshToken;

    public bool HasToken => !string.IsNullOrWhiteSpace(_accessToken);

    public OAuthUserProvider(
        string providerKey,
        string displayName,
        string authority,
        string clientId,
        string? clientSecret = null,
        string scope = "openid profile email",
        ISecureStorage? secureStorage = null,
        bool loadTokenFromStorage = true,
        string? redirectUri = null,
        string? postLogoutRedirectUri = null)
    {
        ProviderKey = providerKey;
        DisplayName = displayName;
        _authority = authority;
        _clientId = clientId;
        _secureStorage = secureStorage;
        _resourceKey = $"Flowery.OAuth.{providerKey}";

        _authClient = new OidcHybridClient(new OidcClientOptions
        {
            Authority = _authority,
            ClientId = _clientId,
            ClientSecret = clientSecret,
            Scope = scope,
            RedirectUri = redirectUri,
            PostLogoutRedirectUri = postLogoutRedirectUri
        });

        if (loadTokenFromStorage && _secureStorage != null)
        {
            try
            {
                var hasTokenSaved = _secureStorage.GetValue(_resourceKey, HasTokenKey);
                if (string.Equals(hasTokenSaved, "1", StringComparison.Ordinal))
                {
                    _accessToken = _secureStorage.GetValue(_resourceKey, "AccessToken");
                    _refreshToken = _secureStorage.GetValue(_resourceKey, "RefreshToken");
                    _currentUser = LoadUserSnapshot();
                }
                else
                {
                    _accessToken = null;
                    _refreshToken = null;
                    _currentUser = null;
                }
            }
            catch
            {
                _accessToken = null;
                _refreshToken = null;
                _currentUser = null;
            }
        }

        _ = _authClient.PrepareAsync();
    }

    public Task<IEnumerable<IFlowUser>> GetAllUsersAsync(CancellationToken cancellation = default)
    {
        return GetUsersFromUserInfoAsync(cancellation);
    }

    public Task<IFlowUser?> GetUserByIdAsync(string rawId, CancellationToken cancellation = default)
    {
        return GetUserByIdFromUserInfoAsync(rawId, cancellation);
    }

    public Task<IEnumerable<IFlowUser>> SearchUsersAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellation = default)
    {
        return SearchUsersFromUserInfoAsync(query, maxResults, cancellation);
    }

    public Task<IFlowUser?> GetCurrentUserAsync(CancellationToken cancellation = default)
    {
        return GetCurrentUserFromUserInfoAsync(cancellation);
    }

    public Task RefreshAsync(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            UsersChanged?.Invoke();
            return Task.CompletedTask;
        }

        UsersChanged?.Invoke();
        return Task.CompletedTask;
    }

    public async Task<bool> AuthenticateAsync(CancellationToken cancellation = default)
    {
        var result = await _authClient.LoginAsync(cancellation);
        if (result.IsError)
        {
            return false;
        }

        _accessToken = result.AccessToken;
        _refreshToken = result.RefreshToken;
        _currentUser = BuildUserFromClaims(result.User?.Claims);
        PersistUserSnapshot(_currentUser);

        if (_secureStorage != null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_accessToken))
                {
                    _secureStorage.SetValue(_resourceKey, HasTokenKey, "1");
                    _secureStorage.SetValue(_resourceKey, "AccessToken", _accessToken);
                }
                if (!string.IsNullOrWhiteSpace(_refreshToken))
                {
                    _secureStorage.SetValue(_resourceKey, "RefreshToken", _refreshToken);
                }
                if (string.IsNullOrWhiteSpace(_accessToken))
                {
                    _secureStorage.RemoveValue(_resourceKey, HasTokenKey);
                }
            }
            catch
            {
                // Ignore storage failures.
            }
        }

        UsersChanged?.Invoke();
        return true;
    }

    public void SaveToken(string token)
    {
        var isEmpty = string.IsNullOrWhiteSpace(token);
        _accessToken = isEmpty ? null : token;
        _refreshToken = isEmpty ? null : _refreshToken;
        _currentUser = null;
        if (_secureStorage != null)
        {
            try
            {
                if (isEmpty)
                {
                    ClearUserSnapshot();
                    _secureStorage.RemoveValue(_resourceKey, HasTokenKey);
                    _secureStorage.RemoveValue(_resourceKey, "AccessToken");
                    _secureStorage.RemoveValue(_resourceKey, "RefreshToken");
                }
                else
                {
                    _secureStorage.SetValue(_resourceKey, HasTokenKey, "1");
                    _secureStorage.SetValue(_resourceKey, "AccessToken", token);
                }
            }
            catch
            {
                // Ignore storage failures.
            }
        }

        UsersChanged?.Invoke();
    }

    private async Task<IEnumerable<IFlowUser>> GetUsersFromUserInfoAsync(CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            return Array.Empty<IFlowUser>();
        }

        var user = await GetCurrentUserFromUserInfoAsync(cancellation);
        if (user == null)
        {
            return Array.Empty<IFlowUser>();
        }

        return new[] { user };
    }

    private async Task<IFlowUser?> GetUserByIdFromUserInfoAsync(string rawId, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(rawId) || string.IsNullOrWhiteSpace(_accessToken))
        {
            return null;
        }

        var user = await GetCurrentUserFromUserInfoAsync(cancellation);
        if (user == null)
        {
            return null;
        }

        if (string.Equals(user.RawId, rawId, StringComparison.Ordinal))
        {
            return user;
        }

        if (string.Equals(user.Id, rawId, StringComparison.Ordinal))
        {
            return user;
        }

        return null;
    }

    private async Task<IEnumerable<IFlowUser>> SearchUsersFromUserInfoAsync(
        string query,
        int maxResults,
        CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            return Array.Empty<IFlowUser>();
        }

        var user = await GetCurrentUserFromUserInfoAsync(cancellation);
        if (user == null)
        {
            return Array.Empty<IFlowUser>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new[] { user };
        }

        var matches =
            (!string.IsNullOrWhiteSpace(user.DisplayName) &&
             user.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(user.Email) &&
             user.Email.Contains(query, StringComparison.OrdinalIgnoreCase));

        return matches ? new[] { user } : Array.Empty<IFlowUser>();
    }

    private async Task<IFlowUser?> GetCurrentUserFromUserInfoAsync(CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            return null;
        }

        if (_currentUser != null)
        {
            return _currentUser;
        }

        var snapshot = LoadUserSnapshot();
        if (snapshot != null)
        {
            _currentUser = snapshot;
            return _currentUser;
        }

        if (cancellation.IsCancellationRequested)
        {
            return null;
        }

        await _userInfoLock.WaitAsync(CancellationToken.None);
        try
        {
            if (_currentUser != null)
            {
                return _currentUser;
            }

            var endpoint = await ResolveUserInfoEndpointAsync(cancellation);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            using var response = await HttpClient.SendAsync(request, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
            var user = BuildUserFromUserInfo(json.RootElement);
            _currentUser = user;
            PersistUserSnapshot(user);
            return user;
        }
        catch
        {
            return null;
        }
        finally
        {
            _userInfoLock.Release();
        }
    }

    private async Task<string?> ResolveUserInfoEndpointAsync(CancellationToken cancellation)
    {
        if (!string.IsNullOrWhiteSpace(_userInfoEndpoint))
        {
            return _userInfoEndpoint;
        }

        if (cancellation.IsCancellationRequested)
        {
            return null;
        }

        var authority = _authority.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
        {
            return null;
        }

        var discoveryUrl = $"{authority}/.well-known/openid-configuration";

        try
        {
            using var response = await HttpClient.GetAsync(discoveryUrl, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);

            if (json.RootElement.TryGetProperty("userinfo_endpoint", out var endpointElement) &&
                endpointElement.ValueKind == JsonValueKind.String)
            {
                _userInfoEndpoint = endpointElement.GetString();
            }
        }
        catch
        {
            _userInfoEndpoint = null;
        }

        return _userInfoEndpoint;
    }

    private FlowUser? LoadUserSnapshot()
    {
        if (_secureStorage == null)
        {
            return null;
        }

        var rawId = _secureStorage.GetValue(_resourceKey, "UserRawId");
        var displayName = _secureStorage.GetValue(_resourceKey, "UserDisplayName");
        if (string.IsNullOrWhiteSpace(rawId) || string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        return new FlowUser
        {
            RawId = rawId,
            ProviderKey = ProviderKey,
            Id = FlowUserIdHelper.Compose(ProviderKey, rawId),
            DisplayName = displayName,
            Email = _secureStorage.GetValue(_resourceKey, "UserEmail"),
            AvatarUrl = _secureStorage.GetValue(_resourceKey, "UserAvatarUrl")
        };
    }

    private void PersistUserSnapshot(FlowUser? user)
    {
        if (_secureStorage == null)
        {
            return;
        }

        if (user == null)
        {
            ClearUserSnapshot();
            return;
        }

        try
        {
            _secureStorage.SetValue(_resourceKey, "UserRawId", user.RawId);
            _secureStorage.SetValue(_resourceKey, "UserDisplayName", user.DisplayName);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                _secureStorage.SetValue(_resourceKey, "UserEmail", user.Email);
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                _secureStorage.SetValue(_resourceKey, "UserAvatarUrl", user.AvatarUrl);
            }
        }
        catch
        {
            // Ignore persistence failures.
        }
    }

    private void ClearUserSnapshot()
    {
        if (_secureStorage == null)
        {
            return;
        }

        try
        {
            _secureStorage.RemoveValue(_resourceKey, "UserRawId");
            _secureStorage.RemoveValue(_resourceKey, "UserDisplayName");
            _secureStorage.RemoveValue(_resourceKey, "UserEmail");
            _secureStorage.RemoveValue(_resourceKey, "UserAvatarUrl");
        }
        catch
        {
            // Ignore persistence failures.
        }
    }

    private FlowUser? BuildUserFromUserInfo(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var rawId = GetString(root, "sub")
                    ?? GetString(root, "id")
                    ?? GetString(root, "email")
                    ?? GetString(root, "preferred_username");

        if (string.IsNullOrWhiteSpace(rawId))
        {
            return null;
        }

        var displayName =
            GetString(root, "name")
            ?? GetString(root, "preferred_username")
            ?? GetString(root, "email")
            ?? rawId;

        return new FlowUser
        {
            RawId = rawId,
            ProviderKey = ProviderKey,
            Id = FlowUserIdHelper.Compose(ProviderKey, rawId),
            DisplayName = displayName,
            Email = GetString(root, "email"),
            AvatarUrl = GetString(root, "picture"),
            Title = GetString(root, "title"),
            Department = GetString(root, "department")
        };
    }

    private FlowUser? BuildUserFromClaims(IEnumerable<Claim>? claims)
    {
        if (claims == null)
        {
            return null;
        }

        var claimList = claims.ToList();
        var rawId = GetClaimValue(claimList, "sub")
                    ?? GetClaimValue(claimList, "id")
                    ?? GetClaimValue(claimList, "email")
                    ?? GetClaimValue(claimList, "preferred_username");

        if (string.IsNullOrWhiteSpace(rawId))
        {
            return null;
        }

        var displayName = GetClaimValue(claimList, "name")
                          ?? GetClaimValue(claimList, "preferred_username")
                          ?? GetClaimValue(claimList, "email")
                          ?? rawId;

        return new FlowUser
        {
            RawId = rawId,
            ProviderKey = ProviderKey,
            Id = FlowUserIdHelper.Compose(ProviderKey, rawId),
            DisplayName = displayName,
            Email = GetClaimValue(claimList, "email"),
            AvatarUrl = GetClaimValue(claimList, "picture")
        };
    }

    private static string? GetClaimValue(IEnumerable<Claim> claims, string type)
    {
        return claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.Ordinal))?.Value;
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
