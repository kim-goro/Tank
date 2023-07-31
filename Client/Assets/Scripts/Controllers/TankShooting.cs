using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
	/// <summary>
	/// 클라이언트 세션의 Tank에게만 붙어서 키보드로 조작하게함
	/// </summary>
	public class TankShooting : MonoBehaviour
    {
		[SerializeField] Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
		[SerializeField] AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
		[SerializeField] AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
		[SerializeField] AudioClip m_FireClip;                // Audio that plays when each shot is fired.
		float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
		float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
		float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.
		float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
		float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.
		bool m_Fired;                       // Whether or not the shell has been launched with this button press.


		void OnEnable()
		{
			// When the tank is turned on, reset the launch force and the UI
			m_CurrentLaunchForce = m_MinLaunchForce;
			m_AimSlider.value = m_MinLaunchForce;
		}


		void Start()
		{
			// The rate that the launch force charges up is the range of possible forces by the max charge time.
			m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
		}


		void Update()
		{
			// The slider should have a default value of the minimum launch force.
			m_AimSlider.value = m_MinLaunchForce;

			// If the max force has been exceeded and the shell hasn't yet been launched...
			if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
			{
				// ... use the max force and launch the shell.
				m_CurrentLaunchForce = m_MaxLaunchForce;
				Fire();
			}
			// Otherwise, if the fire button has just started being pressed...
			else if (Input.GetButtonDown("Fire1"))
			{
				if (_coSkill != null) { return; }
				// ... reset the fired flag and reset the launch force.
				m_Fired = false;
				m_CurrentLaunchForce = m_MinLaunchForce;

				// Change the clip to the charging clip and start it playing.
				m_ShootingAudio.clip = m_ChargingClip;
				m_ShootingAudio.Play();
			}
			// Otherwise, if the fire button is being held and the shell hasn't been launched yet...
			else if (Input.GetButton("Fire1") && !m_Fired)
			{
				if (_coSkill != null) { return; }
				// Increment the launch force and update the slider.
				m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

				m_AimSlider.value = m_CurrentLaunchForce;
			}
			// Otherwise, if the fire button is released and the shell hasn't been launched yet...
			else if (Input.GetButtonUp("Fire1") && !m_Fired)
			{
				if (_coSkill != null) { return; }
				// ... launch the shell.
				Fire();
			}
		}

		protected Coroutine _coSkill;
		void Fire()
		{
			if(_coSkill != null) { return; }
			// Set the fired flag so only Fire is only called once.
			m_Fired = true;

			C_Shoot movePacket = new C_Shoot();
			movePacket.objId = GetComponent<TankMovement>().ObjId;
			movePacket.power = m_CurrentLaunchForce / 100;
			Managers.Network.Send(movePacket);

			// Change the clip to the firing clip and play it.
			m_ShootingAudio.clip = m_FireClip;
			m_ShootingAudio.Play();

			// Reset the launch force.  This is a precaution in case of missing button events.
			m_CurrentLaunchForce = m_MinLaunchForce;
			_coSkill = StartCoroutine("CoStartShootCooltime");
		}

		IEnumerator CoStartShootCooltime()
		{
			// 대기 시간
			yield return new WaitForSeconds(0.3f);
			_coSkill = null;
		}
	}
}