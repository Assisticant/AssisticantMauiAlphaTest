namespace AssisticantMauiAlphaTest;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		// Model: no constructor - initial state set with an object initializer.
		var person = new Person { Name = "Ada", Age = 30 };

		// ViewModel: constructor-injected with the Model, same as DataContext
		// would be handed a ViewModel on WPF via ForView.Wrap.
		BindingContext = new PersonViewModel(person);
	}
}
