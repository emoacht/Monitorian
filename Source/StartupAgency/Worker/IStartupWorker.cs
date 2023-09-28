
namespace StartupAgency.Worker;

internal interface IStartupWorker
{
	/// <summary>
	/// Determines whether caller instance is presumed to have started on sign in.
	/// </summary>
	bool IsStartedOnSignIn();

	/// <summary>
	/// Determines whether caller instance can be registered in startup.
	/// </summary>
	bool CanRegister();

	/// <summary>
	/// Determines whether caller instance is registered in startup.
	/// </summary>
	bool IsRegistered();

	/// <summary>
	/// Registers caller instance to startup.
	/// </summary>
	bool Register();

	/// <summary>
	/// Unregisters caller instance from startup.
	/// </summary>
	void Unregister();
}