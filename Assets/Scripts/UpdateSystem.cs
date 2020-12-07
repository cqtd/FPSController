using UnityEngine.LowLevel;

namespace Cqunity
{
	public struct UpdateSystem
	{
		public static void Listen<T>(PlayerLoopSystem.UpdateFunction updateFunction)
		{
			PlayerLoopSystem updateSystems = PlayerLoop.GetCurrentPlayerLoop();
			Listen<T>(ref updateSystems, updateFunction);
			PlayerLoop.SetPlayerLoop(updateSystems);
		}

		private static bool Listen<T>(ref PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction updateFunction)
		{
			if (system.type == typeof(T))
			{
				system.updateDelegate += updateFunction;

				return true;
			}
			else
			{
				if (system.subSystemList != null)
				{
					int count = system.subSystemList.Length;
					for (int i = 0; i < count; i++)
					{
						if (Listen<T>(ref system.subSystemList[i], updateFunction))
						{
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
