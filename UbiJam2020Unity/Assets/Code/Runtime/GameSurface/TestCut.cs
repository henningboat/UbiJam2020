using System;
using UnityEngine;

namespace Runtime.GameSurface
{
	public class TestCut : MonoBehaviour
	{
		[SerializeField] private Transform _target;

		private void Update()
		{
			GameSurface.Instance.Cut(transform.position,_target.position);
		}
	}
}