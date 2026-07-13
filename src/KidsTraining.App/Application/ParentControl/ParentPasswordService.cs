using KidsTraining.App.Domain.ParentControl;

namespace KidsTraining.App.Application.ParentControl;

internal sealed class ParentPasswordService : IParentPinProvider
{
    private readonly IParentPinStore store;

    public ParentPasswordService(IParentPinStore store)
    {
        this.store = store;
    }

    public ParentPin GetCurrentPin() => store.Read();

    public PasswordChangeResult Change(string? currentPassword, string? newPassword)
    {
        if (!ParentPin.TryCreate(currentPassword, out var current) || current != store.Read())
        {
            return PasswordChangeResult.Failed("いまのパスワードが違います。");
        }

        if (!ParentPin.TryCreate(newPassword, out var next))
        {
            return PasswordChangeResult.Failed("新しいパスワードは4桁の数字にしてください。");
        }

        try
        {
            store.Write(next);
            return PasswordChangeResult.Ok("パスワードを変更しました。");
        }
        catch
        {
            return PasswordChangeResult.Failed("パスワードを保存できませんでした。");
        }
    }
}
