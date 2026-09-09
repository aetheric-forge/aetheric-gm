#pragma warning disable BL0006 // Exercise Blazor event dispatch and rendered controls without a browser.
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Campus;
using AethericGm.Core.Campaigns;
using AethericGm.Core.Entities;
using AethericGm.Core.Sessions;
using AethericGm.Infrastructure.Composition;
using AethericGm.Institutions.Gm;
using AethericGm.Web.Components.Sessions;
using AethericGm.Web.People;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace AethericGm.Tests;

public sealed class NotebookSaveInteractionTests
{
    [Fact]
    public async Task Link_is_immediately_saveable_and_browser_recovery_failure_does_not_block_persistence()
    {
        using var storage = new GmTestStorage();
        await using var data = storage.CreateServices();
        await data.InitializeLocalGmStorageAsync();
        var campus = new Campus(new CampusContext(InstitutionTemplateBuilder.Create().WithDescriptor("Test campus", new Version(1, 0), "Notebook test").Build(), data));
        var gm = campus.RegisterAethericGm(data);
        var campaign = Campaign.Create("Campaign", DateTimeOffset.UtcNow);
        await gm.Campaigns.SaveAsync(campaign);
        var notebook = await gm.Sessions.SaveAsync(new(Guid.NewGuid(), campaign.Id, "Notebook", "", null, SessionStatus.Draft, 0, DateTimeOffset.UtcNow, "operator"));
        var reference = new CampaignEntitySummary(new EntityReference(EntityKind.Character, Guid.NewGuid()), "Valkyr", [], null, null);
        var browser = new TestBrowser();
        await using var services = new ServiceCollection().AddLogging().AddSingleton(gm)
            .AddSingleton<IJSRuntime>(browser).AddSingleton<NavigationManager>(new TestNavigation()).BuildServiceProvider();
        await using var renderer = new TestRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(() => renderer.StartAsync(new Dictionary<string, object?>
        {
            [nameof(NotebookEditor.Initial)] = notebook, [nameof(NotebookEditor.Author)] = "operator",
            [nameof(NotebookEditor.References)] = new[] { reference }
        }));
        Task insertion = Task.CompletedTask;
        await renderer.Dispatcher.InvokeAsync(() => { insertion = renderer.ClickAsync("Valkyr"); });
        await browser.RememberStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            Assert.False(renderer.ButtonDisabled("Save now"));
            Assert.Contains("Unsaved changes", renderer.Text());
        });
        await renderer.Dispatcher.InvokeAsync(() => renderer.ClickAsync("Save now"));
        var saved = (await gm.Sessions.GetAsync(campaign.Id, notebook.Id))!;
        Assert.Contains($"/characters/{reference.Reference.Id}", saved.Markdown);
        browser.RememberCompletion.SetException(new JSException("Browser storage unavailable"));
        await insertion;
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            Assert.False(renderer.ButtonDisabled("Save now"));
            Assert.Contains("All changes saved", renderer.Text());
            Assert.Contains("Browser draft recovery is unavailable", renderer.Text());
        });
        var before = (await gm.Sessions.ListRevisionsAsync(campaign.Id, notebook.Id)).Count;
        await renderer.Dispatcher.InvokeAsync(() => renderer.ClickAsync("Save now"));
        Assert.Equal(before, (await gm.Sessions.ListRevisionsAsync(campaign.Id, notebook.Id)).Count);
    }

    private sealed class TestRenderer(IServiceProvider services, ILoggerFactory logger) : Renderer(services, logger)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        private int root;
        public Task StartAsync(Dictionary<string, object?> parameters)
        {
            root = AssignRootComponentId(InstantiateComponent(typeof(NotebookEditor)));
            return RenderRootComponentAsync(root, ParameterView.FromDictionary(parameters));
        }
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => throw new InvalidOperationException("Component render failed", exception);
        private RenderTreeFrame[] Frames() { var frames = GetCurrentRenderTreeFrames(root); return frames.Array.Take(frames.Count).ToArray(); }
        public string Text() => string.Concat(Frames().Where(f => f.FrameType is RenderTreeFrameType.Text or RenderTreeFrameType.Markup).Select(f => f.FrameType == RenderTreeFrameType.Text ? f.TextContent : f.MarkupContent));
        private RenderTreeFrame[] Button(string label)
        {
            var frames = Frames();
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i].FrameType != RenderTreeFrameType.Element || frames[i].ElementName != "button") continue;
                var subtree = frames.Skip(i + 1).Take(frames[i].ElementSubtreeLength - 1).ToArray();
                if (string.Concat(subtree.Where(f => f.FrameType == RenderTreeFrameType.Text).Select(f => f.TextContent)).Contains(label)) return subtree;
            }
            throw new InvalidOperationException($"Button not found: {label}");
        }
        public bool ButtonDisabled(string label) => Button(label).Any(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "disabled" && f.AttributeValue is true);
        public Task ClickAsync(string label) => DispatchEventAsync(Button(label).Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "onclick").AttributeEventHandlerId, null, new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
    }
    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("https://localhost/", "https://localhost/");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
        protected override void SetNavigationLockState(bool value) { }
    }
    private sealed class TestBrowser : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource RememberStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RememberCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import") return (TValue)(object)this;
            if (identifier == "attach") return (TValue)(object)new NotebookEditor.DraftState(true, null);
            if (identifier == "acknowledge") return (TValue)(object)true;
            if (identifier == "remember") { RememberStarted.TrySetResult(); await RememberCompletion.Task; }
            return default!;
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
