using System;
using UnityEngine;

namespace Cqunity
{
	public delegate void PawnEvent(PawnBase pawn);

	public class PlayerControllerBase : MonoBehaviour
	{
		public PawnBase m_pawn = default;
		public bool m_possessOnAwake = default;

		public PawnEvent pawnDisconnectPossessing = default;
		
		public PawnEvent onPawnPossessed = default;
		public PawnEvent onPawnUnpossessed = default;

		private void Awake()
		{
			pawnDisconnectPossessing += OnPosseingDisconnected;
			
			onPawnPossessed += OnPossessed;
			onPawnUnpossessed += OnUnpossessed;
		}
		
		private void Start()
		{
			if (m_possessOnAwake && m_pawn != null)
			{
				m_pawn.Possess(this);
			}
		}

		private void Reset()
		{
			m_possessOnAwake = true;
			if (m_pawn == null)
			{
				m_pawn = FindObjectOfType<PawnBase>();
			}
		}

		public bool HasPawn {
			get
			{
				return m_pawn.CanControl(this);
			}
		}

		/// <summary>
		/// 마우스 평행 입력 수치
		/// </summary>
		/// <returns></returns>
		public float GetMouseX()
		{
			return Input.GetAxisRaw("Mouse X");
		}

		/// <summary>
		/// 마우스 수직 입력 수치
		/// </summary>
		/// <returns></returns>
		public float GetMouseY()
		{
			return Input.GetAxisRaw("Mouse Y");
		}

		/// <summary>
		/// 좌우 이동
		/// </summary>
		/// <returns></returns>
		public float GetMove()
		{
			return Input.GetAxisRaw("Horizontal");
		}

		/// <summary>
		/// 전후 이동
		/// </summary>
		/// <returns></returns>
		public float GetStrafe()
		{
			return Input.GetAxisRaw("Vertical");
		}

		private void OnPossessed(PawnBase pawn)
		{
			
		}
		
		private void OnUnpossessed(PawnBase pawn)
		{
			
		}
		
		private void OnPosseingDisconnected(PawnBase pawn)
		{
			
		}
		
	}
}