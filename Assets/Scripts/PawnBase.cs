using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Cqunity
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(Collider))]
	public class PawnBase : MonoBehaviour
	{
		
		#region Data Types

		[Serializable]
		public class MovementConfiguration : IConfiguration<MovementConfiguration>
		{
			public float walkingSpeed = default;
			public float runningSpeed = default;
			public float movementSmoothness = default;
			public float jumpForce = default;

			public bool allowChangeDirectionDuringJump = default;

			public MovementConfiguration GetDefault()
			{
				walkingSpeed = 5f;
				runningSpeed = 9f;
				movementSmoothness = 0.125f;
				jumpForce = 35f;
				allowChangeDirectionDuringJump = false;

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

		[Serializable]
		public struct PossessConfiguration
		{
			public bool allowPossess;
			public bool allowPossessOverride;
			public bool allowUnpossessByNonOwner;
			
			public PossessConfiguration GetDefault()
			{
				this.allowPossess = true;
				this.allowPossessOverride = false;
				this.allowUnpossessByNonOwner = false;
				
				return this;
			}
		}

		#endregion

		#region Edittable Fields
		
		[SerializeField] protected Rigidbody m_rigidbody = default;
		[SerializeField] protected Collider m_collider = default;
		
		[SerializeField] protected Transform m_arms = default;
		[SerializeField] protected Vector3 m_armPositionOffset = default;
		
		[Header("Configuration")]
		[SerializeField] protected MovementConfiguration m_movementConfig = default;
		[SerializeField] protected LockConfiguration m_lockConfig = default;
		[SerializeField] protected PossessConfiguration m_possessConfig = default;

		#endregion

		#region Internal Members
		
		protected SmoothRotation m_rotationX = default;
		protected SmoothRotation m_rotationY = default;

		protected SmoothVelocity m_velocityX = default;
		protected SmoothVelocity m_velocityZ = default;

		protected bool bIsGrounded = default;

		protected RaycastHit[] m_groundCastResultArray;
		protected RaycastHit[] m_wallCastResultArray;

		protected PlayerControllerBase m_controller = default;

		protected const int RAYCAST_CAPACITY = 8;
		
		#endregion

		#region Unity Events

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
			m_possessConfig = new PossessConfiguration().GetDefault();
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

		private void OnCollisionStay()
		{
			OnStayCollisionEvent();
		}
		
		#endregion

		#region Possess

		public PossessConfiguration PossesingConfig {
			get
			{
				return m_possessConfig;
			}
		}

		public void Possess(PlayerControllerBase playerController)
		{
			if (!m_possessConfig.allowPossess)
			{
				Assert.IsTrue(m_possessConfig.allowPossess);
				Debug.LogError("빙의가 허용되지 않는 폰입니다.");
				return;
			}

			if (!m_possessConfig.allowPossessOverride)
			{
				if (m_controller != null)
				{
					Assert.IsTrue(m_possessConfig.allowPossessOverride);
					Debug.LogError("빙의 오버라이드가 허용되지 않는 폰입니다.");
					return;
				}
			
				// 이미 연결된 컨트롤러가 있는 경우 디스커넥트
				m_controller.pawnDisconnectPossessing.Invoke(this);
			}
			
			this.m_controller = playerController;
			m_controller.onPawnPossessed?.Invoke(this);
		}

		public bool Unpossess(PlayerControllerBase playerController)
		{
			if (!ReferenceEquals(m_controller, playerController))
			{
				if (!m_possessConfig.allowUnpossessByNonOwner)
				{
					Assert.IsTrue(m_possessConfig.allowUnpossessByNonOwner);
					Debug.LogError("빙의 주체가 아닌 다른 컨트롤러부터 해제가 허용되지 않는 폰입니다.");
					
					return false;
				}
			}
			
			m_controller = null;
			playerController.onPawnUnpossessed?.Invoke(this);
			
			return true;
		}

		/// <summary>
		/// 자신의 폰인지 확인할 때
		/// </summary>
		/// <param name="playerController"></param>
		/// <returns></returns>
		public bool CanControl(PlayerControllerBase playerController)
		{
			if (ReferenceEquals(this.m_controller, playerController)) return true;
			return false;
		}

		/// <summary>
		/// 외부 컨트롤러에 의해 통제되고 있음
		/// </summary>
		/// <returns></returns>
		public bool HasControlled()
		{
			return m_controller != null;
		}

		#endregion

		#region Bridge

		/// <summary>
		/// 커스텀 감도가 적용된 마우스 평행 입력 수치
		/// </summary>
		/// <returns></returns>
		private float GetMouseX()
		{
			if (!HasControlled()) return 0;
			return m_controller.GetMouseX() * m_lockConfig.mouseSensitivity;
		}

		/// <summary>
		/// 커스텀 감도가 적용된 마우스 수직 입력 수치
		/// </summary>
		/// <returns></returns>
		private float GetMouseY()
		{
			if (!HasControlled()) return 0;
			return m_controller.GetMouseY() * m_lockConfig.mouseSensitivity;
		}

		/// <summary>
		/// 좌우 이동
		/// </summary>
		/// <returns></returns>
		private float GetMove()
		{
			if (!HasControlled()) return 0;
			return m_controller.GetMove();
		}

		/// <summary>
		/// 전후 이동
		/// </summary>
		/// <returns></returns>
		private float GetStrafe()
		{
			if (!HasControlled()) return 0;
			return m_controller.GetStrafe();
		}
		
		#endregion

		#region Basic Movements

		/// <summary>
		/// 인스턴스 필드 초기화
		/// </summary>
		protected virtual void InitializeInstanceFields()
		{
			m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			
			m_arms.SetPositionAndRotation(transform.position, transform.rotation);
			
			m_rotationX = new SmoothRotation(GetMouseX());
			m_rotationY = new SmoothRotation(GetMouseY());
			
			m_velocityX = new SmoothVelocity();
			m_velocityZ = new SmoothVelocity();
		}

		/// <summary>
		/// 회전 제한 검증 로직
		/// </summary>
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

		/// <summary>
		/// 로테이션 클램프 유틸리티
		/// </summary>
		/// <param name="rotationRestriction"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
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
		
		/// <summary>
		/// 컬리젼 체크
		/// 로직 확인 후 간소화 필요
		/// @TODO : 로직 확인
		/// </summary>
		private void OnStayCollisionEvent()
		{
			// 그라운드 체크
			Bounds bounds = m_collider.bounds;
			
			Vector3 extents = bounds.extents;
			float radius = extents.x - 0.01f;
			
			Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
				m_groundCastResultArray, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

			if (!m_groundCastResultArray.Any(hit => hit.collider != null && hit.collider != m_collider))
			{
				return;
			}
			
			for (int i = 0; i < RAYCAST_CAPACITY; i++)
			{
				m_groundCastResultArray[i] = new RaycastHit();
			}

			bIsGrounded = true;
		}

		/// <summary>
		/// 폰 움직이기
		/// @TODO : K-Controller 방식으로 변경하기
		/// </summary>
		private void MovePawn()
		{
			// 점프 중 방향 바꾸기를 허용하지 않는 경우
			if (!m_movementConfig.allowChangeDirectionDuringJump)
			{
				if (!bIsGrounded)
				{
					return;
				}
			}
			
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

		/// <summary>
		/// 움직이기 전 이동가능한지 물리로 체크
		/// 디테일하게 수정할 필요성 있음
		/// </summary>
		/// <param name="velocity"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 카메라, 폰 회전 수행
		/// </summary>
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

		/// <summary>
		/// 수직 각도 제한
		/// </summary>
		/// <param name="mouseY"></param>
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
			bIsGrounded = false;
		}

		private void ValidateArms()
		{
			m_arms.position = transform.position + transform.TransformVector(m_armPositionOffset);
		}

		/// <summary>
		/// 점프 로직
		/// </summary>
		private void Jump()
		{
			// 아직 바닥에 닿지 않음
			if (!bIsGrounded)
			{
				return;
			}

			// 점프 수행
			if (Input.GetKeyDown(KeyCode.Space))
			{
				m_rigidbody.AddForce(Vector3.up * m_movementConfig.jumpForce, ForceMode.Impulse);
			}
		}

		#endregion

	}
}