using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
	public class TankHealth : MonoBehaviour
	{
		[SerializeField] Slider m_Slider;
		[SerializeField] Image m_FillImage;  
		[SerializeField] Color m_FullHealthColor = Color.green; 
		[SerializeField] Color m_ZeroHealthColor = Color.red;

		void Awake()
		{
			TankMovement tm = GetComponent<TankMovement>();
			if(tm != null)
			{
				tm.hpChanged += SetHealthUI;
			}
		}

		void SetHealthUI(float amountRemainHp, float maxHp)
		{
			if (m_Slider == null || m_FillImage == null) { return; }
			m_Slider.value = amountRemainHp;
			m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, amountRemainHp / maxHp);
		}
	}
}