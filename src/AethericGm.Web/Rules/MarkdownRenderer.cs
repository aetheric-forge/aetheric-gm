using Markdig;

namespace AethericGm.Web.Rules;

public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

    public static string ToHtml(string? source) => Markdown.ToHtml(source ?? string.Empty, Pipeline);
}
