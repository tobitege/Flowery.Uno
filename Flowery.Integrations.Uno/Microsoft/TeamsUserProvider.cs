using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls.Users;

namespace Flowery.Integrations.Uno.Microsoft;

/// <summary>
/// User provider for Microsoft Teams (via Microsoft Graph).
/// </summary>
public class TeamsUserProvider : IUserProvider
{
    public string ProviderKey => "teams";
    public string DisplayName => "Microsoft Teams";
    public string ImplementationVersion => "1.0.0";
    public bool SupportsAvatars => true;
    public bool SupportsPresence => true;
    public bool SupportsRealtime => true;

    public event Action? UsersChanged;

    private readonly string _tenantId;
    private readonly string _clientId;

    public TeamsUserProvider(string tenantId, string clientId)
    {
        _tenantId = tenantId;
        _clientId = clientId;
    }

    public Task<IEnumerable<IFlowUser>> GetAllUsersAsync(CancellationToken cancellation = default)
    {
        // TODO: Implement using Microsoft.Graph (GET /users)
        throw new NotImplementedException();
    }

    public Task<IFlowUser?> GetUserByIdAsync(string rawId, CancellationToken cancellation = default)
    {
        // TODO: Implement using Microsoft.Graph (GET /users/{id})
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IFlowUser>> SearchUsersAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellation = default)
    {
        // TODO: Implement using Microsoft.Graph ($search filter)
        throw new NotImplementedException();
    }

    public Task<IFlowUser?> GetCurrentUserAsync(CancellationToken cancellation = default)
    {
        // TODO: Implement using Microsoft.Graph (GET /me)
        throw new NotImplementedException();
    }

    public Task RefreshAsync(CancellationToken cancellation = default)
    {
        UsersChanged?.Invoke();
        return Task.CompletedTask;
    }
}
