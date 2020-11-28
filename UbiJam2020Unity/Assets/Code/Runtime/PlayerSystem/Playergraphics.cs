using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class Playergraphics : MonoBehaviour
{
	#region Serialize Fields

	[SerializeField,] private float minDistance = 1;

	#endregion

	#region Private Fields

	private List<Transform> bodyParts;
	private Vector2 lastFramePosition;

	#endregion

	#region Unity methods

	void Start()
	{
		lastFramePosition = transform.position;
		bodyParts = GetComponentInChildren<SpriteSkin>().rootBone.GetComponentsInChildren<Transform>().ToList();
		for (int i = 1; i < bodyParts.Count; i++)
		{
			bodyParts[i].SetParent(null, true);
		}
	}

	void FixedUpdate()
	{
		float curspeed = ((Vector2) transform.position - lastFramePosition).magnitude;

		for (int i = 1; i < bodyParts.Count; i++)
		{
			var curBodyPart = bodyParts[i];
			var PrevBodyPart = bodyParts[i - 1];

			var dis = Vector3.Distance(PrevBodyPart.position, curBodyPart.position);

			Vector3 newpos = PrevBodyPart.position;

			float T = ((Time.deltaTime * dis) / minDistance) * curspeed;

			if (T > 0.5f)
			{
				T = 0.5f;
			}

			Vector3 currentPosition = Vector3.Slerp(curBodyPart.position, newpos, T);
			currentPosition.z = bodyParts[0].position.z;
			curBodyPart.position = currentPosition;
			curBodyPart.rotation = Quaternion.Slerp(curBodyPart.rotation, PrevBodyPart.rotation, T);
		}

		lastFramePosition = transform.position;
	}

	#endregion
}