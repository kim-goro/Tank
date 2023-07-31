using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary> 키보드를 통해 콘텐츠를 조작할 수 있는 스크립트 </summary>
public class ContentDebugInputTool : MonoBehaviour
{

	bool mShowDebugMode = false;
	public bool showDebugMode
	{
		get { return mShowDebugMode; }
		set
		{
			mShowDebugMode = value;
			//Debug.Log("<color=grey>[" + nameof(ContentDebugInputTool) + "] '" + nameof(showDebugMode) + "=" + value + "'</color>");
		}
	}

	void Awake()
	{
		Application.logMessageReceived += HandleLog;
	}

	void LateUpdate()
	{
		if (Input.GetKey(KeyCode.LeftControl))
		{
			// *디버그 모드로 전환하는 '`'버튼
			if (Input.GetKeyDown(toggleKey)) { showDebugMode = !showDebugMode; }
		}
	}

	#region 콘솔창
	struct ConsoleMessage
	{
		public readonly string message;
		public readonly string stackTrace;
		public readonly LogType type;
		public ConsoleMessage(string message, string stackTrace, LogType type)
		{
			this.message = message;
			this.stackTrace = stackTrace;
			this.type = type;
		}
	}

	KeyCode toggleKey = KeyCode.BackQuote;
	List<ConsoleMessage> entries = new List<ConsoleMessage>();
	Vector2 scrollPos;
	bool collapse;

	// Visual elements:
	const int margin = 20;
	Rect windowRect = new Rect(margin, margin, Screen.width - (2 * margin), Screen.height - (2 * margin));
	GUIContent clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
	GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

	void OnGUI()
	{
		if (!showDebugMode) { return; }
		windowRect = GUILayout.Window(123456, windowRect, ConsoleWindow, "Console");
	}

	/// <summary> A window displaying the logged messages. </summary>
	/// <param name="windowID">The window's ID.</param>
	void ConsoleWindow(int windowID)
	{
		scrollPos = GUILayout.BeginScrollView(scrollPos);

		// Go through each logged entry
		for (int i = 0; i < entries.Count; i++)
		{
			ConsoleMessage entry = entries[i];

			// If this message is the same as the last one and the collapse feature is chosen, skip it
			if (collapse && i > 0 && entry.message == entries[i - 1].message) { continue; }

			// Change the text colour according to the log type
			switch (entry.type)
			{
				case LogType.Error:
				case LogType.Exception:
					GUI.contentColor = Color.red;
					break;
				case LogType.Warning:
					GUI.contentColor = Color.yellow;
					break;
				default:
					GUI.contentColor = Color.white;
					break;
			}
			GUILayout.Label(entry.message);
		}
		GUI.contentColor = Color.white;
		GUILayout.EndScrollView();
		GUILayout.BeginHorizontal();

		// Clear button
		if (GUILayout.Button(clearLabel)) { entries.Clear(); }

		// Collapse toggle
		collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();

		// Set the window to be draggable by the top title bar
		GUI.DragWindow(new Rect(0, 0, 5000, 20));
	}

	/// <summary> Logged messages are sent through this callback function. </summary>
	/// <param name="message">The message itself.</param>
	/// <param name="stackTrace">A trace of where the message came from.</param>
	/// <param name="type">The type of message: error/exception, warning, or assert.</param>
	void HandleLog(string message, string stackTrace, LogType type)
	{
		ConsoleMessage entry = new ConsoleMessage(message, stackTrace, type);
		entries.Add(entry);
	}
	#endregion
}
