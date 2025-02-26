//Created by TestQueryClassCreator at 2/4/2025 7:31:10 PM

namespace SupportToolsServerDb.QueryModels;

public sealed class TestQuery
{
    public TestQuery(string testName)
    {
        TestName = testName;
    }

    public int TestId { get; set; }
    public string TestName { get; set; }
}