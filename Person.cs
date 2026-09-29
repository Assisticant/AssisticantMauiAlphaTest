using Assisticant.Fields;

namespace AssisticantMauiAlphaTest;

// MODEL: independent state only. No constructor - initial values are set
// with an object initializer at the call site (see MainPage.xaml.cs).
public partial class Person
{
    [Observable] public partial string Name { get; set; }
    [Observable] public partial int Age { get; set; }
}
