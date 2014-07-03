using UnityEngine;
using System.Collections;

/// <summary>
/// Just startgame button
/// </summary>
public class StartScene : MonoBehaviour 
{
	private float btnH = 50f;
	private float btnW = 100f;
	void OnGUI()
	{
		if (GUI.Button(new Rect((Screen.width- btnW) / 2 , (Screen.height- btnH) / 2, btnW, btnH), "Start Game"))
		{
			Application.LoadLevel("GameScene");
		}
	}
}
