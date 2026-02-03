using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls.Users;
using Octokit;

namespace Flowery.Integrations.Uno.GitHub;

/// <summary>
/// User provider for GitHub identity using Octokit.
/// </summary>
public class GitHubUserProvider : IUserProvider, ITokenSaveProvider, ITokenStateProvider, ITokenValidationProvider
{
    private readonly GitHubClient _client;
    private readonly ISecureStorage? _secureStorage;
    private bool _hasToken;
    private const string GitHubResource = "Flowery.GitHub";
    private const string TokenKey = "AccessToken";

    public string ProviderKey => "github";
    public string DisplayName => "GitHub";
    public string ImplementationVersion => "1.0.0";
    public bool SupportsAvatars => true;
    public bool SupportsPresence => false;
    public bool SupportsRealtime => false;
    public bool HasToken => _hasToken;

    public event Action? UsersChanged;

    /// <summary>
    /// Initializes a new instance with a personal access token and optional storage.
    /// </summary>
    public GitHubUserProvider(
        string? accessToken,
        ISecureStorage? storage = null,
        string productName = "FlowKanban",
        bool loadTokenFromStorage = true)
    {
        _secureStorage = storage;
        _client = new GitHubClient(new ProductHeaderValue(productName));

        var token = accessToken;
        if (string.IsNullOrEmpty(token) && loadTokenFromStorage && _secureStorage != null)
        {
            try
            {
                token = _secureStorage.GetValue(GitHubResource, TokenKey);
            }
            catch
            {
                token = null;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            _client.Credentials = new Credentials(token);
            _hasToken = true;
        }
        else
        {
            _hasToken = false;
        }
    }

    /// <summary>
    /// Initializes a new instance using secure storage to retrieve the token.
    /// </summary>
    public GitHubUserProvider(
        ISecureStorage storage,
        string productName = "FlowKanban",
        bool loadTokenFromStorage = true)
        : this(null, storage, productName, loadTokenFromStorage)
    {
    }

    /// <summary>
    /// Initializes a new instance with a pre-configured GitHubClient.
    /// </summary>
    public GitHubUserProvider(GitHubClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<IEnumerable<IFlowUser>> GetAllUsersAsync(CancellationToken cancellation = default)
    {
        if (!_hasToken)
        {
            return Enumerable.Empty<IFlowUser>();
        }

        try
        {
            var me = await GetCurrentUserAsync(cancellation);
            return me != null ? new[] { me } : Enumerable.Empty<IFlowUser>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GitHubUserProvider.GetAllUsersAsync failed: {ex.GetType().Name} - {ex.Message}");
            return Enumerable.Empty<IFlowUser>();
        }
    }

    public async Task<IFlowUser?> GetUserByIdAsync(string rawId, CancellationToken cancellation = default)
    {
        if (string.IsNullOrEmpty(rawId)) return null;
        if (!_hasToken) return null;

        try
        {
            var user = await _client.User.Get(rawId);
            return MapUser(user);
        }
        catch (NotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GitHubUserProvider.GetUserByIdAsync failed: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<IFlowUser>> SearchUsersAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<IFlowUser>();
        if (!_hasToken) return Enumerable.Empty<IFlowUser>();

        try
        {
            var request = new SearchUsersRequest(query);
            var result = await _client.Search.SearchUsers(request);
            return result.Items.Take(maxResults).Select(MapUser);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GitHubUserProvider.SearchUsersAsync failed: {ex.GetType().Name} - {ex.Message}");
            return Enumerable.Empty<IFlowUser>();
        }
    }

    public async Task<IFlowUser?> GetCurrentUserAsync(CancellationToken cancellation = default)
    {
        if (!_hasToken) return null;

        try
        {
            var user = await _client.User.Current();
            return MapUser(user);
        }
        catch
        {
            return null;
        }
    }

    public Task RefreshAsync(CancellationToken cancellation = default)
    {
        UsersChanged?.Invoke();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists the access token to secure storage and updates the client credentials.
    /// </summary>
    public void SaveToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                _secureStorage?.RemoveValue(GitHubResource, TokenKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GitHubUserProvider.SaveToken remove failed: {ex.GetType().Name} - {ex.Message}");
            }
            _client.Credentials = Credentials.Anonymous!;
            _hasToken = false;
        }
        else
        {
            try
            {
                _secureStorage?.SetValue(GitHubResource, TokenKey, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GitHubUserProvider.SaveToken store failed: {ex.GetType().Name} - {ex.Message}");
            }
            _client.Credentials = new Credentials(token);
            _hasToken = true;
        }

        UsersChanged?.Invoke();
    }

    /// <summary>
    /// Validates that the current token can access organization membership endpoints.
    /// </summary>
    public async Task<ProviderTokenValidationResult> ValidateAccessAsync(CancellationToken cancellation = default)
    {
        if (!_hasToken)
        {
            return ProviderTokenValidationResult.NoToken();
        }

        try
        {
            _ = await _client.User.Current();
            return ProviderTokenValidationResult.Success();
        }
        catch (ForbiddenException ex)
        {
            return ProviderTokenValidationResult.MissingScope(ex.Message);
        }
        catch (AuthorizationException ex)
        {
            return ProviderTokenValidationResult.Invalid(ex.Message);
        }
        catch (Exception ex)
        {
            return ProviderTokenValidationResult.Error(ex.Message);
        }
    }

    private IFlowUser MapUser(User user)
    {
        // Login is the unique raw ID
        return new FlowUser(user.Login, user.Name ?? user.Login, ProviderKey)
        {
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Department = user.Company,
            Title = null, // GitHub doesn't clearly separate title from company
            CustomData = new Dictionary<string, object>
            {
                ["bio"] = user.Bio ?? string.Empty,
                ["htmlUrl"] = user.HtmlUrl ?? string.Empty,
                ["type"] = user.Type?.ToString() ?? string.Empty,
                ["blog"] = user.Blog ?? string.Empty
            }
        };
    }

}
