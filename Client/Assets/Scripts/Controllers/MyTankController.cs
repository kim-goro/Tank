using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Complete;
using static Define;

/// <summary>
/// 클라이언트 세션의 Tank에게만 붙어서 키보드로 조작하게함
/// </summary>
[RequireComponent(typeof(TankMovement))]
public class MyTankController : MonoBehaviour
{
	TankMovement tm;
	float m_MovementInputValue;         // The current value of the movement input.
	float m_TurnInputValue;             // The current value of the turn input.
	Vector2Int inputMovement = new Vector2Int(0,0);
	bool isInputOccur = false;
	bool updateForward = false;

	private void Awake()
	{
		tm = GetComponent<TankMovement>();
	}

	void Update()
	{
		isInputOccur = false;
		if (Input.GetKeyDown(KeyCode.A))
		{
			updateForward = true;
			isInputOccur = true;
			inputMovement.y = -1;
		} 
		if (Input.GetKeyDown(KeyCode.D))
		{
			updateForward = true;
			isInputOccur = true;
			inputMovement.y = 1;
		}
		if (Input.GetKeyDown(KeyCode.W))
		{
			isInputOccur = true;
			inputMovement.x = 1;
		}
		if (Input.GetKeyDown(KeyCode.S))
		{
			isInputOccur = true;
			inputMovement.x = -1;
		}

		if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
		{
			updateForward = false;
			isInputOccur = true;
			inputMovement.y = 0;
		}
		if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S))
		{
			isInputOccur = true;
			inputMovement.x = 0;
		}
		//m_MovementInputValue = Input.GetAxis("Vertical1");
		//m_TurnInputValue = Input.GetAxis("Horizontal1");

		if (isInputOccur || updateForward)
		{
			C_Move movePacket = new C_Move();
			movePacket.objId = tm.ObjId;
			movePacket.inputForward = (int)inputMovement.x;
			movePacket.rotateRight = (int)inputMovement.y;
			movePacket.forwardX = transform.forward.x;
			movePacket.forwardY = transform.forward.y;
			movePacket.forwardZ = transform.forward.z;
			Managers.Network.Send(movePacket);
		}
	}
}
