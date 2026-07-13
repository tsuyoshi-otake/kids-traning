using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal interface IParentPinProvider
{
    ParentPin GetCurrentPin();
}
