using MediatR;
using OmniFlow.Application.Common.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Assets.Commands;

public class DeleteAssetCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand>
{
    private readonly IOmniFlowDbContext _context;

    public DeleteAssetCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (asset == null) throw new NotFoundException("Asset", request.Id);

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
