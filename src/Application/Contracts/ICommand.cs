namespace Sigma.Application.Contracts;

public interface ICommand
{
}

public interface ICommand<TResponse> : ICommand
{
}