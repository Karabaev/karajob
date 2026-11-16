using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace Karajob.Web.Components.MasterProfiles;

public static class MarkdownHtml
{
  private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseEmphasisExtras()
    .UseAutoLinks()
    .UseTaskLists()
    .DisableHtml()
    .Build();

  private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

  // Raw HTML is disabled and unsafe URL schemes are stripped, so the output is safe to emit as MarkupString.
  public static MarkupString Render(string markdown)
  {
    var document = Markdown.Parse(markdown, Pipeline);

    foreach (var link in document.Descendants<LinkInline>())
    {
      if (link.Url is not null && IsUnsafe(link.Url))
      {
        link.Url = string.Empty;
      }
    }

    foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
    {
      if (IsUnsafe(autolink.Url))
      {
        autolink.Remove();
      }
    }

    return new MarkupString(document.ToHtml(Pipeline));
  }

  private static bool IsUnsafe(string url)
  {
    var cleaned = new string(url.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).ToArray());
    var end = cleaned.IndexOfAny(['/', '?', '#']);
    var head = end < 0 ? cleaned : cleaned[..end];
    var colon = head.IndexOf(':');
    if (colon < 0)
    {
      return false;
    }

    var scheme = head[..colon];
    return !AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
  }
}
