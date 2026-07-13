using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal interface IParentPinStore
{
    ParentPin Read();

    void Write(ParentPin pin);
}
