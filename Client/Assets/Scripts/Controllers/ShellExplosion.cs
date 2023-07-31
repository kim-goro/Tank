using UnityEngine;

namespace Complete
{
    public class ShellExplosion : BasicObject
    {
		public Transform particleGroup;
		public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
		public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
		bool ignoreFirstFrameMove = false;

		public virtual void Init(S_BroadcastSpawnMissle inputPacket)
		{
			Init(inputPacket.objId);
			beforePos = transform.position;
			ignoreFirstFrameMove = false;

			transform.position = new Vector3(inputPacket.posX, inputPacket.posY, inputPacket.posZ);
			transform.rotation = Quaternion.Euler(new Vector3(inputPacket.AngX, inputPacket.AngY, inputPacket.ANgZ));
			beforePos = transform.position;
		}

		Vector3 beforePos;
		public override void MoveTo(S_BroadcastMove inputPacket)
		{
			base.MoveTo(inputPacket);
			if (ignoreFirstFrameMove)
			{
				transform.LookAt(beforePos);
				transform.Rotate(new Vector3(0, 180, 0));
			}
			beforePos = transform.position;
			ignoreFirstFrameMove = true;
		}

		public override void DestroySelf()
		{
			base.DestroySelf();

			// Unparent the particles from the shell.
			particleGroup.transform.parent = null;

			// Play the particle system.
			m_ExplosionParticles.Play();

			// Play the explosion sound effect.
			m_ExplosionAudio.Play();

			// Once the particles have finished, destroy the gameobject they are on.
			ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
			Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
		}
	}
}