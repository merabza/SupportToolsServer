using System.Threading;
using Moq;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Handlers that answer every call with the given result
internal static class HandlerMocks
{
    public static Mock<ICommandHandler<TCommand>> Command<TCommand>(Result result) where TCommand : ICommand
    {
        var handler = new Mock<ICommandHandler<TCommand>>();
        handler.Setup(h => h.Handle(It.IsAny<TCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return handler;
    }

    public static Mock<IQueryHandler<TQuery, TResponse>> Query<TQuery, TResponse>(Result<TResponse> result)
        where TQuery : IQuery<TResponse>
    {
        var handler = new Mock<IQueryHandler<TQuery, TResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<TQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return handler;
    }
}
