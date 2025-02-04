//Created by TestModelClassCreator at 2/4/2025 7:31:10 PM

namespace SupportToolsServerDb.Models;

//ეს არის სატესტო მოდელი, რომელიც არის უბრალოდ ნიმუშისათვის და შესაძლებელია წაიშალოს საჭირების შემთხვევაში

public sealed class TestModel
{
    public int TestId { get; set; }
    public string TestName { get; set; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public TestModel(string testName)
    {
        TestName = testName;
    }
}