using System.Text.Json;

using Comments.Domain.Events;
using Comments.Infrastructure.Search;

namespace Comments.Tests;

public class SearchIndexingTests
{
    [Fact]
    public void PlainText_removes_tags_and_decodes_entities()
    {
        var html = "Привет <strong>мир</strong> &amp; <a href=\"https://example.com\" title=\"t\">ссылка</a> <code>a &lt; b</code>";

        Assert.Equal("Привет мир & ссылка a < b", PlainText.FromHtml(html));
    }

    [Fact]
    public void PlainText_keeps_a_word_split_by_tags_together()
    {
        Assert.Equal("foobar", PlainText.FromHtml("foo<i>bar</i>"));
    }

    [Fact]
    public void PlainText_does_not_turn_encoded_markup_into_tags()
    {
        // &lt;script&gt; остаётся обычным текстом, а не вырезается как тег
        Assert.Equal("<script>x</script>", PlainText.FromHtml("&lt;script&gt;x&lt;/script&gt;"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlainText_returns_empty_for_blank_input(string? html)
    {
        Assert.Equal(string.Empty, PlainText.FromHtml(html));
    }

    [Fact]
    public void Builds_a_search_document_from_the_event()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var e = new CommentCreatedEvent(
            id, parentId, "Anna123", "anna@example.com", "<i>hello</i>   world", createdAt);

        var document = SearchDocuments.FromEvent(e);

        Assert.Equal(id, document.Id);
        Assert.Equal(parentId, document.ParentId);
        Assert.Equal("Anna123", document.UserName);
        Assert.Equal("hello world", document.Text);
        Assert.Equal(createdAt, document.CreatedAt);
    }

    [Fact]
    public void Indexed_json_has_explicit_field_names_and_no_email()
    {
        var e = new CommentCreatedEvent(
            Guid.NewGuid(), null, "Anna123", "anna@example.com", "text", DateTime.UtcNow);

        var json = JsonSerializer.Serialize(CommentIndexDocument.From(SearchDocuments.FromEvent(e)));

        using var parsed = JsonDocument.Parse(json);
        var names = parsed.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();

        Assert.Equal(["createdAt", "id", "parentId", "text", "userName"], names);
        Assert.DoesNotContain("anna@example.com", json);
    }
}