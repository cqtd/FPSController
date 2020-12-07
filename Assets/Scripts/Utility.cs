using Unity.Mathematics;
using UnityEngine;

namespace Cqunity
{
	public class Utility
	{
		public static void GetInputMove(ref float3 movementDirection, ref float intensity)
		{
			var horizontal = InputUtility.GetMoveHorizontalInput();
			var vertical = InputUtility.GetMoveVerticalInput();

			float3 analogInput = GetAnalogInput(horizontal, vertical);

			intensity = math.length(analogInput);

			if (intensity <= 0.1f)
			{
				intensity = 0.0f;
			}
			else
			{
				movementDirection = GetDesiredForwardDirection(analogInput, movementDirection);
			}
		}
		
		public static float3 GetAnalogInput(float x, float y)
		{
			var analogInput = new float3(x, 0.0f, y);

			if (math.length(analogInput) > 1.0f)
			{
				analogInput =
					math.normalize(analogInput);
			}

			return analogInput;
		}
		
		public static float3 GetRelativeLinearVelocity(float3 absoluteLinearVelocity, float3 normalizedViewDirection)
		{
			float3 forward2d = math.normalize(new float3(normalizedViewDirection.x, 0.0f, normalizedViewDirection.z));

			quaternion cameraRotation = Missing.forRotation(Missing.forward, forward2d);

			return Missing.rotateVector(cameraRotation, absoluteLinearVelocity);
		}
		
		public static float3 GetDesiredForwardDirection(float3 absoluteLinearVelocity, float3 forwardDirection)
		{
			var relativeDesiredVelocity = GetRelativeLinearVelocity(absoluteLinearVelocity, Camera.main.transform.forward);

			return math.normalizesafe(relativeDesiredVelocity, forwardDirection);
		}
	}
}