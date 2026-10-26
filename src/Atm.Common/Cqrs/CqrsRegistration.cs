using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atm.Common.Cqrs;

public static class CqrsRegistration
{
    public static IServiceCollection AddCqrs(this IServiceCollection s, Assembly a)
    {
        s.TryAddScoped<IDispatcher, Dispatcher>();

        foreach (var type in a.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var i in type.GetInterfaces().Where(IsHandlerOrValidator))
                s.AddScoped(i, type);
        }
        return s;
    }

    private static bool IsHandlerOrValidator(Type i) =>
        i.IsGenericType &&
        (i.GetGenericTypeDefinition() == typeof(IHandler<,>) || i.GetGenericTypeDefinition() == typeof(IValidator<>));
}
