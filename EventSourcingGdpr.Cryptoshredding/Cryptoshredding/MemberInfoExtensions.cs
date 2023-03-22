using System.Reflection;
using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Attributes;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

internal static class MemberInfoExtensions
{
    public static bool ShouldEncrypt(this MemberInfo memberInfo) => memberInfo.GetCustomAttribute<PersonalDataAttribute>(inherit: true) != null;
}
