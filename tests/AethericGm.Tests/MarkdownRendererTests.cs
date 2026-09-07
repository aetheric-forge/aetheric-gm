using AethericGm.Web.Rules;

namespace AethericGm.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Renders_markdown_and_disables_embedded_html()
    {
        var html = MarkdownRenderer.ToHtml("## Rules\n\nUse **two actions**.\n\n<script>alert('no')</script>");

        Assert.Contains("<h2>Rules</h2>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>two actions</strong>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
    }
}
