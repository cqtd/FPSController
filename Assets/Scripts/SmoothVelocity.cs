using UnityEngine;

namespace Cqunity
{
	public class SmoothVelocity
	{
		private float m_current = default;
		private float m_currentVelocity = default;

		public void Update(float target, float smoothTime, out float result)
		{
			m_current = Mathf.SmoothDamp(m_current, target, ref m_currentVelocity, smoothTime);
			result = m_current;
		}

		public void SetCurrent(float value)
		{
			m_current = value;
		}
	}
}