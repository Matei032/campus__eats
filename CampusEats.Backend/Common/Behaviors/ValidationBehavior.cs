using CampusEats.Backend.Common;
using FluentValidation;
using MediatR;

namespace CampusEats.Backend.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators, continue
        if (!_validators.Any())
        {
            return await next();
        }

        // Run all validators
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // If validation failed, return Result with errors
        if (failures.Any())
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            
            // Handle Result<T> response
            if (typeof(TResponse).IsGenericType && 
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod(nameof(Result<object>.Failure), new[] { typeof(List<string>) });
                
                return (TResponse)failureMethod!.Invoke(null, new object[] { errors })!;
            }
            
            // Handle Result (non-generic) response
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(errors);
            }

            // If not Result type, throw validation exception (fallback)
            throw new ValidationException(failures);
        }

        // Continue pipeline
        return await next();
    }
}