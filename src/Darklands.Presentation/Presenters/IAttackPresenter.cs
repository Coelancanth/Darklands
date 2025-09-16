using System.Threading.Tasks;
using Darklands.Presentation.Views;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Interface for the attack presenter in the MVP pattern.
    /// Defines the contract for attack/combat presentation logic.
    /// </summary>
    public interface IAttackPresenter
    {
        /// <summary>
        /// Attaches a view to this presenter.
        /// </summary>
        void AttachView(IAttackView view);

        /// <summary>
        /// Initializes the presenter.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Disposes of presenter resources.
        /// </summary>
        void Dispose();
    }
}
