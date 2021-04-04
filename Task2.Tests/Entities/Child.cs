namespace Task2.Tests.Entities
{
    internal class Child : Parent
    {
        public int ChildProperty { get; } = 3;

        public readonly string ChildField = "321";
    }
}
