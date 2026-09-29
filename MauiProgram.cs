using AssisticantMauiAlphaTest.Friends;
using Microsoft.Extensions.Logging;

namespace AssisticantMauiAlphaTest;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		// Without this, UpdateScheduler queues every notification forever and
		// PropertyChanged/CanExecuteChanged never fire. The dispatcher must
		// always queue, never run inline, so use DispatchDelayed(Zero) rather
		// than MainThread.BeginInvokeOnMainThread.
		Assisticant.UpdateScheduler.Initialize(action =>
			Application.Current!.Dispatcher.DispatchDelayed(TimeSpan.Zero, action));

		builder.Services.AddSingleton<FriendService>();
		builder.Services.AddSingleton<FriendsModel>();
		builder.Services.AddTransient<FriendListViewModel>();
		builder.Services.AddTransient<FriendsPage>();

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
