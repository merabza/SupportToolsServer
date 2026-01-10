using Carcass.Persistence;
using GrammarGeDb;

namespace AppGrammarGeRepositories;

public class GrammarGeUnitOfWork : CarcassUnitOfWork
{
    public GrammarGeUnitOfWork(GrammarGeDbContext dbContext) : base(dbContext)
    {
    }
}