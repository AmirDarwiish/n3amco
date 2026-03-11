namespace DairySystem.Api.Users.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipActivityLogAttribute : Attribute
    {
    }

}
