namespace SupportToolsServer.Domain.Sync;

public interface ICrudEntity
{
    bool IsSameById(ICrudEntity other);
}