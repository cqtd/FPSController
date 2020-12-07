namespace Cqunity
{
	internal interface IConfiguration<out T> where T : class, new()
	{
		T GetDefault();
	}
}