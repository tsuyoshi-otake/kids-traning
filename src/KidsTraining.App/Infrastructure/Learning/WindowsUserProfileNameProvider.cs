using KidsTraining.App.Application.Learning;

namespace KidsTraining.App.Infrastructure.Learning;

internal sealed class WindowsUserProfileNameProvider : IUserProfileNameProvider
{
    public string GetProfileName()
    {
        var userName = Environment.UserName;
        return string.IsNullOrWhiteSpace(userName) ? "User" : userName.Trim();
    }
}
