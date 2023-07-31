using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace Complete
{
	public class TankMovement : BasicObject
	{
		public System.Action<float, float> hpChanged;
		float mHp = 100f;
		float mMaxHp = 100f;
		public float hp { get { return mHp; } private set { mHp = value; hpChanged?.Invoke(mHp, mMaxHp); } }
		public float maxHp { get { return mMaxHp; } private set { mMaxHp = value; hpChanged?.Invoke(mHp, mMaxHp); } }
		Vector3 beforePos;
		Vector3 beforeRot;
		[SerializeField] Text UI_nameTag;
		[SerializeField] Text UI_rankTag;

		public override void Init(int ObjId)
		{
			base.Init(ObjId);
			m_Dead = false;
			if (m_MovementAudio == null) { m_MovementAudio = GetComponent<AudioSource>(); }
			if (m_MovementAudio != null) { m_OriginalPitch = m_MovementAudio.pitch; }

		}
		public virtual void Init(S_BroadcastEnter inputPacket)
		{
			Init(inputPacket.objId);
			SetSessionIdUI(inputPacket.sessionId);
			hp = inputPacket.hp;
			maxHp = inputPacket.hp;
			transform.position = new Vector3(inputPacket.posX, inputPacket.posY, inputPacket.posZ);
			transform.rotation = Quaternion.Euler(new Vector3(inputPacket.angX, inputPacket.angY, inputPacket.angZ));
			beforePos = transform.position;
			beforeRot = transform.rotation.eulerAngles;
#if UNITY_EDITOR
			MapVoxelManager.AddObjet(this);
#endif
		}

		public void SetSessionIdUI(int SessionId)
		{
			if (UI_nameTag == null) { return; }
			UI_nameTag.text = "[Session ID : " + SessionId.ToString() + "]";
		}

		public void SetKillCountUI(int killCount)
		{
			if (UI_rankTag == null) { return; }
			UI_rankTag.text = "killScore(" + killCount.ToString() + ")";
		}

		public override void MoveTo(S_BroadcastMove inputPacket)
		{
			base.MoveTo(inputPacket);

			enginValue = Vector3.Distance(beforePos, transform.position) * 5 + Vector3.Distance(beforeRot, transform.rotation.eulerAngles);
			EngineAudio();

			beforePos = transform.position;
			beforeRot = transform.rotation.eulerAngles;
		}

		public override void DestroySelf()
		{
			base.DestroySelf();

			GameObject effect = Managers.Resource.Instantiate("CompleteTankExplosion");
			Debug.Log(effect);
			effect.GetComponent<ParticleSystem>().Play();
			effect.transform.parent = null;
			effect.transform.position = transform.position;
			GameObject.Destroy(effect, 0.5f);
		}

		public virtual void OnDamaged(float damage)
		{
			hp -= damage;
		}

		[Space(10)]
		[SerializeField] AudioSource m_MovementAudio;
		[SerializeField] AudioClip m_EngineIdling;
		[SerializeField] AudioClip m_EngineDriving;
		float m_PitchRange = 0.2f;
		float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
		float enginValue = 0;

		void LateUpdate()
		{
			//base.FixedUpdate();

			if (UI_nameTag != null)
			{
				UI_nameTag.transform.LookAt(Camera.main.transform);
				UI_nameTag.transform.Rotate(new Vector3(0, 180, 0));
			}
		}

		void EngineAudio()
		{
			if (Mathf.Abs(enginValue) < 0.1f)
			{
				m_MovementAudio.clip = m_EngineIdling;
				m_MovementAudio.pitch = enginValue;
				m_MovementAudio.Play();
				// ... and if the audio source is currently playing the driving clip...
				if (m_MovementAudio.clip == m_EngineDriving)
				{
					// ... change the clip to idling and play it.
					m_MovementAudio.clip = m_EngineIdling;
					m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
					m_MovementAudio.Play();
				}
			}
			else
			{
				// Otherwise if the tank is moving and if the idling clip is currently playing...
				m_MovementAudio.clip = m_EngineDriving;
				m_MovementAudio.pitch = enginValue;
				m_MovementAudio.Play();
			}
		}
	}
}