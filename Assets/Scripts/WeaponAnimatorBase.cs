using System;
using UnityEngine;

namespace Cqunity
{
	[RequireComponent(typeof(Animator))]
	public class WeaponAnimatorBase : MonoBehaviour
	{
		[SerializeField] protected Animator m_animator = default;
		[SerializeField] protected WeaponGunBase m_weaponGun = default;
		
		private static readonly int aim = Animator.StringToHash("Aim");
		
		private static readonly int meleeAttack1 = Animator.StringToHash("Knife Attack 1");
		private static readonly int meleeAttack2 = Animator.StringToHash("Knife Attack 2");
		
		private static readonly int throwGrenade = Animator.StringToHash("GrenadeThrow");
		
		private static readonly int fireGun = Animator.StringToHash("Fire");
		private static readonly int aimedFireGun = Animator.StringToHash("Aim Fire");
		
		private static readonly int inspectWeapon = Animator.StringToHash("Inspect");
		private static readonly int holster = Animator.StringToHash("Holster");
		
		private static readonly int walk = Animator.StringToHash("Walk");
		private static readonly int run = Animator.StringToHash("Run");
		
		private static readonly int changeEmptyMagazine = Animator.StringToHash("Reload Out Of Ammo");
		private static readonly int changeMagazine = Animator.StringToHash("Reload Ammo Left");
		

		protected virtual void Reset()
		{
			m_animator = GetComponent<Animator>();
		}

		protected virtual void Awake()
		{
			
		}
	}
}