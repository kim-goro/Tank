using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Complete
{
	public class TankRenderer : MonoBehaviour
	{
		MeshRenderer[] renderers;
		Color originColor = Color.black;
		Color glowColor = Color.white;
		float lerpValue = 0;

		void Awake()
		{
			renderers = GetComponentsInChildren<MeshRenderer>();
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material.EnableKeyword("_EMISSION");
				renderers[i].material.SetColor("_EmissionColor", originColor);
			}
			TankMovement tm = GetComponent<TankMovement>();
			if (tm != null)
			{
				tm.hpChanged += (a, b) => { BlinkEffect(); };
			}
		}

		public void BlinkEffect()
		{
			lerpValue = 1f;
		}

		public void SetColor(Color color)
		{
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material.SetColor("_Color", color);
			}
		}

		void FixedUpdate()
		{
			if(renderers.Length <=0) { return; }

			lerpValue = Mathf.Clamp(lerpValue - Time.deltaTime * 2f, 0, 1);
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].material.SetColor("_EmissionColor", Color.Lerp(originColor, glowColor, lerpValue));
			}
		}
	}
}