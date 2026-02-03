using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Kanban.Controls.Users;

namespace Flowery.Integrations.Uno.Slack;

/// <summary>
/// User provider for Slack identity.
/// </summary>
public class SlackUserProvider : IUserProvider
{
    public string ProviderKey => "slack";
    public string DisplayName => "Slack";
    public string ImplementationVersion => "1.0.0";
    public bool SupportsAvatars => true;
    public bool SupportsPresence => true;
    public bool SupportsRealtime => true;

    public event Action? UsersChanged;

    private readonly string _botToken;

    public SlackUserProvider(string botToken)
    {
        _botToken = botToken;
    }

    public Task<IEnumerable<IFlowUser>> GetAllUsersAsync(CancellationToken cancellation = default)
    {
        // TODO: Implement using Slack API (users.list)
        throw new NotImplementedException();
    }

    public Task<IFlowUser?> GetUserByIdAsync(string rawId, CancellationToken cancellation = default)
    {
        // TODO: Implement using Slack API (users.info)
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IFlowUser>> SearchUsersAsync(
        string query,
        int maxResults = 20,
        CancellationToken cancellation = default)
    {
        // TODO: Implement using Slack Web API search methods
        throw new NotImplementedException();
    }

    public Task<IFlowUser?> GetCurrentUserAsync(CancellationToken cancellation = default)
    {
        // TODO: Implement using auth.test
        throw new NotImplementedException();
    }

    public Task RefreshAsync(CancellationToken cancellation = default)
    {
        UsersChanged?.Invoke();
        return Task.CompletedTask;
    }
}
