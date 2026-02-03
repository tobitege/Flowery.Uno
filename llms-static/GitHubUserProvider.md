# GitHubUserProvider

An enterprise-grade implementation of `IUserProvider` for integrating GitHub identity and user discovery into the FlowKanban system.

## Overview

`GitHubUserProvider` uses the **Octokit** SDK to communicate with GitHub. It supports:

- **Personal Access Tokens (PAT)** for authentication.
- **Secure Persistence** of tokens via `ISecureStorage`.
- **Organization Membership** discovery (All Users).
- **Global User Search** with rich profile mapping.
- **Avatar Support** with automatic URL resolution.

## Configuration

The provider can be initialized with a pre-existing token or by injecting a secure storage service for automatic lifecycle management.

```csharp
using Flowery.Integrations.Uno.GitHub;
using Flowery.Services;

// Option 1: Manual Initialization
var provider = new GitHubUserProvider("ghp_your_token_here");

// Option 2: Secure/Persistent Initialization (Recommended)
var secureStorage = new PasswordVaultSecureStorage();
var provider = new GitHubUserProvider(secureStorage);

// Save a new token safely
provider.SaveToken("ghp_new_token");
```

## API Reference

### Properties

| Property | Value | Description |
| -------- | ----- | ----------- |
| `ProviderKey` | `"github"` | The identifier used in composite IDs (e.g., `github:username`) |
| `DisplayName` | `"GitHub"` | Human-readable name for UI |
| `ImplementationVersion` | `"1.0.0"` | Current adapter version |
| `SupportsAvatars` | `true` | Indicates profile picture support |
| `SupportsPresence` | `false` | Presence is not currently supported via REST API |

### Methods

| Method | Signature | Description |
| -------- | --------- | ----------- |
| `GetAllUsersAsync` | `Task<IEnumerable<IFlowUser>>` | Fetches the current user and members of all organizations they belong to. |
| `GetUserByIdAsync` | `Task<IFlowUser?>` | Resolves a GitHub user by their **Login** name. |
| `SearchUsersAsync` | `Task<IEnumerable<IFlowUser>>` | Performs a global GitHub user search matching the query. |
| `GetCurrentUserAsync` | `Task<IFlowUser?>` | Fetches the authenticated user profile. |
| `SaveToken` | `void SaveToken(string token)` | Persists the PAT to secure storage and updates the active client. |

## User Data Mapping

The provider maps the following GitHub data to the `IFlowUser` contract:

- **ID**: GitHub Login (case-insensitive)
- **DisplayName**: Full Name (fallback to Login)
- **Email**: Public Email
- **AvatarUrl**: Profile Image URL
- **Department**: Company
- **CustomData**: Includes `bio`, `htmlUrl`, `type`, `blog`, and `twitter`.

## Error Handling

- **Rate Limiting**: Retries or returns partial results on API rate limits.
- **Network**: Handles connectivity issues gracefully during async lookups.
- **Authentication**: Returns `null` or empty lists if the token is invalid or expired.

## See Also

- [ISecureStorage](ISecureStorage.md)
- [IUserProvider](../Flowery.Uno/Controls/Kanban/Users/Interfaces/IUserProvider.cs)
- [CompositeUserProvider](../Flowery.Uno/Controls/Kanban/Users/Providers/CompositeUserProvider.cs)
