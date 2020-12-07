using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Cqunity
{
	public class KController : MonoBehaviour
	{
		private float3 direction;
		private float intensity;

		public GameObject displayer;

		private GameObject sphere;

		private void Start()
		{
			sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.localScale = Vector3.one * 0.1f;
			Destroy(sphere.GetComponent<Collider>());
			
			StartCoroutine(Trajectory());
		}

		private IEnumerator Trajectory()
		{
			while (true)
			{
				Vector3 pos = transform.position + (Vector3) (direction * intensity);
				Instantiate(sphere, pos, Quaternion.identity);
				
				yield return new WaitForSeconds(0.1f);
			}
		}

		private void Update()
		{
			Utility.GetInputMove(ref direction, ref intensity);
			displayer.transform.localPosition = direction * intensity;
		}

		private void FixedUpdate()
		{
			
		}
	}
}