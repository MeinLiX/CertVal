using FluentValidation;
using CertVal.Application.Common.Models;
using MediatR;

namespace CertVal.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                // For Result<T> responses, return a failure result instead of throwing
                if (typeof(TResponse).IsGenericType && 
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var errors = failures.Select(f => f.ErrorMessage).ToList();
                    var resultType = typeof(TResponse);
                    var method = typeof(Result<>).MakeGenericType(resultType.GetGenericArguments()[0])
                        .GetMethod(nameof(Result.Failure), new[] { typeof(string) });
                    
                    if (method != null)
                    {
                        var result = method.Invoke(null, new object[] { string.Join("; ", errors) });
                        return (TResponse)result!;
                    }
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    var errors = failures.Select(f => f.ErrorMessage).ToList();
                    return (TResponse)(object)Result.Failure(string.Join("; ", errors));
                }

                throw new Exceptions.ValidationException(failures);
            }
        }

        return await next();
    }
}