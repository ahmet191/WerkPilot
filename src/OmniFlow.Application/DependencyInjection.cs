using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Common.Behaviors;

namespace OmniFlow.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application layer: MediatR handlers, FluentValidation
    /// validators, and the cross-cutting pipeline behaviors
    /// (logging → validation, in execution order).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Approval engine: materializes ApprovalDefinition templates into
        // concrete requests inside the caller's unit of work.
        services.AddScoped<Approvals.ApprovalRequestBuilder>();

        return services;
    }
}
