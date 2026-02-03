using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.RuntimeTests
{
    public static class RuntimeTestRunner
    {
        public static async Task<int> RunAsync(Window window, string resultPath)
        {
            var dispatcher = window.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
            var results = new List<RuntimeTestCaseResult>();

            await RunOnDispatcherAsync(dispatcher, async () =>
            {
                var host = PrepareHost(window);
                RuntimeTestContext.Initialize(window, host);
                await EnsureLoadedAsync(host);
            }).ConfigureAwait(false);

            var testCases = DiscoverTests();
            foreach (var testCase in testCases)
            {
                var result = await ExecuteTestCaseAsync(dispatcher, testCase).ConfigureAwait(false);
                results.Add(result);
            }

            WriteResults(resultPath, results);
            return results.Any(result => !result.Passed) ? 1 : 0;
        }

        private static Grid PrepareHost(Window window)
        {
            var host = new Grid();
            window.Content = host;
            return host;
        }

        private static IEnumerable<RuntimeTestCase> DiscoverTests()
        {
            var assembly = typeof(RuntimeTestRunner).Assembly;
            var testClasses = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<TestClassAttribute>() != null);

            foreach (var testClass in testClasses)
            {
                var methods = testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => method.GetCustomAttribute<TestMethodAttribute>() != null)
                    .Where(method => method.GetParameters().Length == 0);

                foreach (var method in methods)
                {
                    yield return new RuntimeTestCase(testClass, method);
                }
            }
        }

        private static async Task<RuntimeTestCaseResult> ExecuteTestCaseAsync(DispatcherQueue dispatcher, RuntimeTestCase testCase)
        {
            try
            {
                await RunOnDispatcherAsync(dispatcher, async () =>
                {
                    var instance = Activator.CreateInstance(testCase.TestClassType)
                                   ?? throw new InvalidOperationException($"Failed to create {testCase.TestClassType.FullName}.");

                    var result = testCase.Method.Invoke(instance, null);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(true);
                    }
                }).ConfigureAwait(false);

                return RuntimeTestCaseResult.Success(testCase.Name);
            }
            catch (Exception ex)
            {
                return RuntimeTestCaseResult.Failed(testCase.Name, ex);
            }
        }

        private static async Task RunOnDispatcherAsync(DispatcherQueue dispatcher, Func<Task> action)
        {
            if (dispatcher.HasThreadAccess)
            {
                await action().ConfigureAwait(true);
                return;
            }

            var tcs = new TaskCompletionSource<object?>();
            dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    await action().ConfigureAwait(true);
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        private static Task EnsureLoadedAsync(FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>();
            RoutedEventHandler? handler = null;
            handler = (_, _) =>
            {
                element.Loaded -= handler;
                tcs.TrySetResult(null);
            };
            element.Loaded += handler;
            return tcs.Task;
        }

        private static void WriteResults(string resultPath, IReadOnlyCollection<RuntimeTestCaseResult> results)
        {
            var document = new XDocument(
                new XElement("tests",
                    new XAttribute("total", results.Count),
                    new XAttribute("passed", results.Count(result => result.Passed)),
                    new XAttribute("failed", results.Count(result => !result.Passed)),
                    results.Select(result =>
                        new XElement("test",
                            new XAttribute("name", result.Name),
                            new XAttribute("outcome", result.Passed ? "Passed" : "Failed"),
                            result.Message is null ? null : new XElement("message", result.Message)
                        ))
                )
            );

            document.Save(resultPath);
        }

        private sealed class RuntimeTestCase
        {
            public RuntimeTestCase(Type testClassType, MethodInfo method)
            {
                TestClassType = testClassType;
                Method = method;
            }

            public Type TestClassType { get; }
            public MethodInfo Method { get; }

            public string Name => $"{TestClassType.FullName}.{Method.Name}";
        }

        private sealed class RuntimeTestCaseResult
        {
            private RuntimeTestCaseResult(string name, bool passed, string? message)
            {
                Name = name;
                Passed = passed;
                Message = message;
            }

            public string Name { get; }
            public bool Passed { get; }
            public string? Message { get; }

            public static RuntimeTestCaseResult Success(string name) => new RuntimeTestCaseResult(name, true, null);

            public static RuntimeTestCaseResult Failed(string name, Exception exception)
            {
                return new RuntimeTestCaseResult(name, false, exception.ToString());
            }
        }
    }
}
