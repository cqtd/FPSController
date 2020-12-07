using System;
using System.Linq;
using UnityEngine;

namespace Cqunity
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(Collider))]
	public class ControllerBase : MonoBehaviour
	{
		[Serializable]
		public class MovementConfiguration : IConfiguration<MovementConfiguration>
		{
			public float walkingSpeed = default;
			public float runningSpeed = default;
			public float movementSmoothness = default;
			public float jumpForce = default;

			public MovementConfiguration GetDefault()
			{
				walkingSpeed = 5f;
				runningSpeed = 9f;
				movementSmoothness = 0.125f;
				jumpForce = 35f;

				return this;
			}
		}

		[Serializable]
		public class LockConfiguration : IConfiguration<LockConfiguration>
		{
			public float mouseSensitivity = default;
			public float rotationSmoothness = default;
			public float minVerticalAngle = default;
			public float maxVerticalAngle = default;

			public LockConfiguration GetDefault()
			{
				this.mouseSensitivity = 7f;
				this.rotationSmoothness = 0.05f;
				this.minVerticalAngle = -90f;
				this.maxVerticalAngle = 90f;

				return this;
			}
		}
		
		[SerializeField] private Rigidbody m_rigidbody = default;
		[SerializeField] private Collider m_collider = default;
		
		[SerializeField] private Transform m_arms = default;
		[SerializeField] private MovementConfiguration m_movementConfig = default;
		[SerializeField] private LockConfiguration m_lockConfig = default;

		protected SmoothRotation m_rotationX = default;
		protected SmoothRotation m_rotationY = default;

		protected SmoothVelocity m_velocityX = default;
		protected SmoothVelocity m_velocityZ = default;

		protected bool bIsGrounded = default;

		private RaycastHit[] m_groundCastResultArray;
		private RaycastHit[] m_wallCastResultArray;

		private const int RAYCAST_CAPACITY = 8;

		private void Awake()
		{
			Cursor.lockState = CursorLockMode.Locked;
			
			m_groundCastResultArray = new RaycastHit[RAYCAST_CAPACITY];
			m_wallCastResultArray = new RaycastHit[RAYCAST_CAPACITY];
			
			InitializeInstanceFields();
		}

		private void Start()
		{
			ValidateRotationRestriction();
		}
		
		private void Reset()
		{
			m_collider = GetComponent<Collider>();
			m_rigidbody = GetComponent<Rigidbody>();
			
			m_movementConfig = new MovementConfiguration().GetDefault();
			m_lockConfig = new LockConfiguration().GetDefault();
		}

		private void FixedUpdate()
		{
			RotateCameraAndPawn();
			MovePawn();
			ValidateGroundness();
		}

		private void Update()
		{
			ValidateArms();
			Jump();
		}

		private float GetMouseX()
		{
			return Input.GetAxisRaw("Mouse X") * m_lockConfig.mouseSensitivity;
		}

		private float GetMouseY()
		{
			return Input.GetAxisRaw("Mouse Y") * m_lockConfig.mouseSensitivity;
		}

		private float GetMove()
		{
			return Input.GetAxisRaw("Horizontal");
		}

		private float GetStrafe()
		{
			return Input.GetAxisRaw("Vertical");
		}

		protected virtual void InitializeInstanceFields()
		{
			m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			
			m_arms.SetPositionAndRotation(transform.position, transform.rotation);
			
			m_rotationX = new SmoothRotation(GetMouseX());
			m_rotationY = new SmoothRotation(GetMouseY());
			
			m_velocityX = new SmoothVelocity();
			m_velocityZ = new SmoothVelocity();
		}

		protected virtual void ValidateRotationRestriction()
		{
			ClampRotation(ref m_lockConfig.minVerticalAngle, -90, 90);
			ClampRotation(ref m_lockConfig.maxVerticalAngle, -90, 90);

			if (m_lockConfig.maxVerticalAngle >= m_lockConfig.minVerticalAngle)
			{
				return;
			}

			float tempMinimum = m_lockConfig.minVerticalAngle;
			
			m_lockConfig.minVerticalAngle = m_lockConfig.maxVerticalAngle;
			m_lockConfig.maxVerticalAngle = tempMinimum;
		}

		private static void ClampRotation(ref float rotationRestriction, float min, float max)
		{
			if (rotationRestriction >= min)
			{
				return;
			}

			if (rotationRestriction <= max)
			{
				return;
			}

			rotationRestriction = Mathf.Clamp(rotationRestriction, min, max);
		}

		private void OnCollisionStay()
		{
			var bounds = m_collider.bounds;
			var extents = bounds.extents;
			var radius = extents.x - 0.01f;
			Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
				m_groundCastResultArray, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
			if (!m_groundCastResultArray.Any(hit => hit.collider != null && hit.collider != m_collider)) return;
			for (var i = 0; i < m_groundCastResultArray.Length; i++)
			{
				m_groundCastResultArray[i] = new RaycastHit();
			}

			bIsGrounded = true;
		}
		
		private void MovePawn()
		{
			// Direction 가져오기
			Vector3 direction = new Vector3(GetMove(), 0f, GetStrafe());
			Vector3 worldDirection = transform.TransformDirection(direction);
			Vector3 velocity = worldDirection * (Input.GetKey(KeyCode.LeftShift)
				? m_movementConfig.runningSpeed
				: m_movementConfig.walkingSpeed);

			bool intersectCollision = DetectWall(velocity);
			if (intersectCollision)
			{
				m_velocityX.SetCurrent(0f);
				return;
			}
			
			m_velocityX.Update(velocity.x, m_movementConfig.movementSmoothness, out float smoothX);
			m_velocityZ.Update(velocity.z, m_movementConfig.movementSmoothness, out float smoothZ);

			Vector3 rigidbodyVelocity = m_rigidbody.velocity;
			Vector3 enforcemenet = new Vector3(smoothX - rigidbodyVelocity.x, 0f, smoothZ - rigidbodyVelocity.z);
			
			m_rigidbody.AddForce(enforcemenet, ForceMode.VelocityChange);
		}

		private bool DetectWall(Vector3 velocity)
		{
			if (bIsGrounded) return false;

			if (!(m_collider is CapsuleCollider capsule))
			{
				return false;
			}
			
			Bounds bounds = m_collider.bounds;
			
			float radius = capsule.radius;
			float halfHeight = capsule.height * 0.5f - radius * 1.0f;

			var point1 = bounds.center + Vector3.up * halfHeight;
			var point2 = bounds.center - Vector3.up * halfHeight;

			Physics.CapsuleCastNonAlloc(point1, point2, radius, velocity.normalized, m_wallCastResultArray,
				radius * 0.04f, ~0, QueryTriggerInteraction.Ignore);

			var collides = m_wallCastResultArray.Any(e => e.collider != null && e.collider != m_collider);
			if (!collides)
			{
				return false;
			}

			for (int i = 0; i < RAYCAST_CAPACITY; i++)
			{
				m_wallCastResultArray[i] = new RaycastHit();
			}

			return true;

		}

		private void RotateCameraAndPawn()
		{
			m_rotationX.Update(GetMouseX(), m_lockConfig.rotationSmoothness, out float rotX);
			m_rotationY.Update(GetMouseY(), m_lockConfig.rotationSmoothness, out float rotY);
			
			RestrictVerticalRot(ref rotY);

			Vector3 worldUp = m_arms.InverseTransformDirection(Vector3.up);
			Quaternion rotation = m_arms.rotation
			                      * Quaternion.AngleAxis(rotX, worldUp)
			                      * Quaternion.AngleAxis(rotY, Vector3.left);
			
			transform.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
			m_arms.rotation = rotation;
		}

		private void RestrictVerticalRot(ref float mouseY)
		{
			var currentAngle = NormalizeAngle(m_arms.eulerAngles.x);
			var minY = m_lockConfig.minVerticalAngle + currentAngle;
			var maxY = m_lockConfig.maxVerticalAngle + currentAngle;
			mouseY = Mathf.Clamp(mouseY, minY + 0.01f, maxY - 0.01f);
		}
		
		private static float NormalizeAngle(float angleDegrees)
		{
			while (angleDegrees > 180f)
			{
				angleDegrees -= 360f;
			}

			while (angleDegrees <= -180f)
			{
				angleDegrees += 360f;
			}

			return angleDegrees;
		}

		private void ValidateGroundness()
		{
			
		}

		private void ValidateArms()
		{
			
		}

		private void Jump()
		{
			
		}
	}

	public class SmoothRotation
	{
		private float m_current = default;
		private float m_currentVelocity = default;

		public SmoothRotation(float initialValue)
		{
			m_current = initialValue;
		}

		public void Update(float target, float smoothTime, out float result)
		{
			m_current = Mathf.SmoothDampAngle(m_current, target, ref m_currentVelocity, smoothTime);
			result = m_current;
		}

		public void SetCurrent(float value)
		{
			m_current = value;
		}
	}

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