namespace ActualLab.Fusion.Operations.Internal;

/// <summary>
/// This handler captures invocations of nested commands inside
/// operations and logs them into context.Operation.NestedOperations
/// so that invalidation for them could be auto-replayed too.
/// </summary>
public class NestedOperationLogger(IServiceProvider services) : ICommandHandler<ICommand>
{
    private ComputeServiceCommandCompletionInvalidator? _postCompletionInvalidator;
    private ILogger? _log;

    protected IServiceProvider Services { get; } = services;

    protected ComputeServiceCommandCompletionInvalidator ComputeServiceCommandCompletionInvalidator
        => _postCompletionInvalidator ??= Services.GetRequiredService<ComputeServiceCommandCompletionInvalidator>();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    [CommandFilter(Priority = FusionOperationsCommandHandlerPriority.NestedCommandLogger)]
    public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var mustBeUsed =
            context.OuterContext != null // Should be a nested context
            && ComputeServiceCommandCompletionInvalidator.IsRequired(command, out _)
            && !Invalidation.IsActive;
        if (!mustBeUsed) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }

        var operation = context.TryGetOperation();
        var operationItemsBackup = PropertyBag.Empty;
        if (operation != null) {
            operationItemsBackup = operation.Items.Snapshot;
            operation.Items.Snapshot = PropertyBag.Empty;
        }

        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        finally {
            if (operation != null) {
                // There was an operation already
                var operationItems = operation.Items;
                operation.NestedOperations = operation.NestedOperations.Add(new(command, operationItems.Snapshot));
                operationItems.Snapshot = operationItemsBackup;
            }
            else {
                // There was no operation, but it could be requested inside one of nested commands
                operation = context.TryGetOperation();
                if (operation != null) {
                    // The operation is requested inside the nested command, so we have to make a couple fixes:
                    // - Add nested operation that corresponds to the current command
                    // - Replace its Items with an empty bag
                    var operationItems = operation.Items;
                    operation.NestedOperations = operation.NestedOperations.Add(new(command, operationItems.Snapshot));
                    operationItems.Snapshot = PropertyBag.Empty;
                }
            }
        }
    }
}
