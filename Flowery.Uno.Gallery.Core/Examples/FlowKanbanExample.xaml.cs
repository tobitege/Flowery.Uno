using Flowery.Controls;
using Flowery.Enums;
using Flowery.Integrations.Uno.GitHub;
using Flowery.Integrations.Uno.OAuth;
using Flowery.Services;
using Flowery.Uno.Kanban.Controls;
using Flowery.Uno.Kanban.Controls.Users;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class FlowKanbanExample : ScrollableExamplePage
    {
        private FlowKanbanManager? _kanbanManager;

        public FlowKanbanExample()
        {
            InitializeComponent();
            InitializeKanbanDemo();
            Unloaded += OnFlowKanbanUnloaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnFlowKanbanUnloaded(object sender, RoutedEventArgs e)
        {
            _kanbanManager?.Shutdown();
        }

        private void InitializeKanbanDemo()
        {
#pragma warning disable CA1416 // net*-desktop TFMs aren't OS-specific; Kanban providers are Uno-platform supported.
            if (DemoKanban == null)
                return;

            if (FlowKanban.DefaultUserProvider != null)
            {
                DemoKanban.UserProvider = FlowKanban.DefaultUserProvider;
            }
            else
            {
                var demoProvider = new CompositeUserProvider();
                demoProvider.RegisterProvider(new LocalUserProvider(includeDemoUsers: true));
                try
                {
                    demoProvider.RegisterProvider(new GitHubUserProvider(new PasswordVaultSecureStorage(), loadTokenFromStorage: true));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FlowKanbanExample GitHub provider init failed: {ex.GetType().Name} - {ex.Message}");
                }
                try
                {
                    demoProvider.RegisterProvider(new OAuthUserProvider(
                        providerKey: "oidc-demo",
                        displayName: "OIDC Demo",
                        authority: "https://demo.duendesoftware.com/",
                        clientId: "interactive.confidential",
                        clientSecret: "secret",
                        scope: "openid profile api offline_access",
                        secureStorage: new PasswordVaultSecureStorage(),
                        loadTokenFromStorage: true));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FlowKanbanExample OIDC provider init failed: {ex.GetType().Name} - {ex.Message}");
                }
                DemoKanban.UserProvider = demoProvider;
            }

            DemoKanban.EditCardCommand = new RelayCommand<FlowTask>(async task =>
            {
                if (task == null || XamlRoot == null)
                    return;

                var assignees = await GetAssigneeOptionsAsync(DemoKanban.UserProvider);
                await FlowTaskEditorDialog.ShowAsyncWithAssignees(
                    task,
                    XamlRoot,
                    DemoKanban.Board.Tags,
                    assignees,
                    DemoKanban.AutoExpandCardDetails);
            });

            _kanbanManager = new FlowKanbanManager(DemoKanban, autoAttach: false);
            _kanbanManager.PersistenceFailed += (_, args) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Kanban persistence {args.Operation} failed: {args.Exception.Message}");
            };

            var loaded = _kanbanManager.Initialize();
            if (!loaded)
            {
                DemoKanban.Board = CreateSampleKanbanBoard();
            }
#pragma warning restore CA1416
        }

        private static FlowKanbanData CreateSampleKanbanBoard()
        {
            return new FlowKanbanData
            {
                Columns =
                [
                    new FlowKanbanColumnData
                    {
                        Title = "Todo",
                        Tasks =
                        [
                            new FlowTask
                            {
                                Title = "Make content sections more interactive",
                                Description = "Add extra visual elements to remove big walls of text\n\nhigh priority",
                                Palette = DaisyColor.Primary,
                                Subtasks =
                                {
                                    new FlowSubtask { Title = "Add cards explaining Kanban concept", IsCompleted = true },
                                    new FlowSubtask { Title = "Add visual roadmap" },
                                    new FlowSubtask { Title = "Add images" }
                                }
                            },
                            new FlowTask
                            {
                                Title = "Some other todo",
                                Description = "Additional task details here",
                                Palette = DaisyColor.Default
                            },
                            new FlowTask
                            {
                                Title = "Another todo",
                                Description = "Important pending item",
                                Palette = DaisyColor.Secondary
                            },
                            new FlowTask
                            {
                                Title = "One more",
                                Description = "Final todo item",
                                Palette = DaisyColor.Default
                            }
                        ]
                    },
                    new FlowKanbanColumnData
                    {
                        Title = "In Progress",
                        Tasks =
                        [
                            new FlowTask
                            {
                                Title = "Make content sections more interactive",
                                Description = "Subtask: 1/3 complete",
                                Palette = DaisyColor.Error
                            }
                        ]
                    },
                    new FlowKanbanColumnData
                    {
                        Title = "Done",
                        Tasks =
                        [
                            new FlowTask
                            {
                                Title = "Buy more coffee",
                                Description = "Completed: 1/3",
                                Palette = DaisyColor.Default,
                                Subtasks =
                                [
                                    new FlowSubtask { Title = "Order beans online", IsCompleted = true },
                                    new FlowSubtask { Title = "Pick up from store", IsCompleted = false },
                                    new FlowSubtask { Title = "Try new roast", IsCompleted = false }
                                ]
                            },
                            new FlowTask
                            {
                                Title = "Published repo",
                                Description = "Successfully published",
                                Palette = DaisyColor.Success
                            }
                        ]
                    }
                ]
            };
        }

        private static async Task<IReadOnlyList<FlowTaskAssigneeOption>> GetAssigneeOptionsAsync(IUserProvider? provider)
        {
            if (provider == null)
                return Array.Empty<FlowTaskAssigneeOption>();

            var users = await provider.GetAllUsersAsync();
            if (users == null)
                return Array.Empty<FlowTaskAssigneeOption>();

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var results = new List<FlowTaskAssigneeOption>();
            foreach (var user in users)
            {
                if (user == null)
                    continue;

                var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.RawId : user.DisplayName;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var id = user.Id?.Trim();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (seen.Add(id))
                    results.Add(new FlowTaskAssigneeOption(id, name.Trim()));
            }

            results.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.DisplayName, right.DisplayName));
            return results;
        }

    }
}
