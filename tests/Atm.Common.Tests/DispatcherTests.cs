using Atm.Common.Cqrs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Atm.Common.Tests;

public class DispatcherTests
{
    public record Ping(string Name) : ICommand<string>;

    private class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private class BusinessValidator : AbstractValidator<Ping>
    {
        public BusinessValidator() =>
            RuleFor(x => x.Name).Must(_ => false).WithMessage("rule broken").WithErrorCode("business");
    }

    private static (IDispatcher dispatcher, IHandler<Ping, string> handler) Build(IValidator<Ping>? validator)
    {
        var handler = Substitute.For<IHandler<Ping, string>>();
        handler.HandleAsync(Arg.Any<Ping>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("pong"));
        var services = new ServiceCollection();
        services.AddCqrs(typeof(Result<>).Assembly);
        services.AddSingleton(handler);
        if (validator is not null) services.AddSingleton(validator);
        return (services.BuildServiceProvider().GetRequiredService<IDispatcher>(), handler);
    }

    [Fact]
    public async Task Send_runs_handler_when_valid()
    {
        var (dispatcher, handler) = Build(new PingValidator());

        var result = await dispatcher.SendAsync(new Ping("a"));

        Assert.True(result.IsSuccess);
        Assert.Equal("pong", result.Value);
        await handler.Received(1).HandleAsync(Arg.Any<Ping>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_returns_validation_errors_and_skips_handler_when_validator_fails()
    {
        var (dispatcher, handler) = Build(new PingValidator());

        var result = await dispatcher.SendAsync(new Ping(""));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Errors[0].Kind);
        Assert.Equal(nameof(Ping.Name), result.Errors[0].Field);
        await handler.DidNotReceive().HandleAsync(Arg.Any<Ping>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_maps_validator_error_code_business_to_ErrorKind_Business()
    {
        var (dispatcher, _) = Build(new BusinessValidator());

        var result = await dispatcher.SendAsync(new Ping("a"));

        Assert.Equal(ErrorKind.Business, result.Errors[0].Kind);
        Assert.Equal("rule broken", result.Errors[0].Message);
    }
}
