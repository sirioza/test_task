using System.Numerics;

namespace StringsSorter.Extensions
{
    //it could moved to Common project if needed in other projects
    internal static class CheckNullOrZeroExtensions
    {
        public static T NotNull<T>(this T? value, string name) => value ?? throw new ArgumentException(name);

        public static T NotZero<T>(this T value, string name) where T : INumber<T>
        {
            if (value == T.Zero) throw new ArgumentException(name);

            return value;
        }
    }
}
