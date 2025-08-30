using MediatR;
using LanguageExt;
using LanguageExt.Common;

namespace Darklands.Core.Application.Common
{
    /// <summary>
    /// Represents a command that modifies system state and returns no data.
    /// Its handler MUST return Fin<Unit>.
    /// </summary>
    public interface ICommand : IRequest<LanguageExt.Fin<LanguageExt.Unit>> { }

    /// <summary>
    /// Represents a command that modifies system state and returns a "Fast-Path" result.
    /// Its handler MUST return Fin<TResult>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface ICommand<TResult> : IRequest<LanguageExt.Fin<TResult>> { }

    /// <summary>
    /// Represents a query that retrieves data and does not modify state.
    /// Its handler MUST return Fin<TResult>.
    /// </summary>
    /// <typeparam name="TResult">The type of the data being queried.</typeparam>
    public interface IQuery<TResult> : IRequest<LanguageExt.Fin<TResult>> { }
}
