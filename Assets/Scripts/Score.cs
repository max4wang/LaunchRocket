using UnityEngine;
using System.Collections;

/// <summary>
/// Display some numbers.
/// </summary>
public class Score : MonoBehaviour 
{
	public UILabel moveLbl;
	public UILabel launchLbl;
	public UILabel scoreLbl;

	private int moveNum;
	private int launchNum;
	private int scoreNum;

	public void init(FuseManager manager)
	{
		manager.moveNotify = () => 
		{
			moveNum++;
			moveLbl.text = "Move: " + moveNum;
			updateScore();
		};

		manager.launchNotify = () => 
		{
			launchNum++;
			launchLbl.text = "Launch: " + launchNum;
			updateScore();
		};
	}

	private void updateScore()
	{
		scoreNum = launchNum * 1000 / (moveNum + 1);
		scoreLbl.text = "Score: " + scoreNum;
	}
}
