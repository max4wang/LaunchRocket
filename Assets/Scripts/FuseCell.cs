using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fuse cell script
/// </summary>
public class FuseCell : MonoBehaviour 
{
	private float rotateTweenTime = 0.1f;
	private float fireTweenTime = 0.1f;

	public bool isFireStart;
	public bool isRocketEnd;
	public FuseCell preCell = null;
	public List<FuseCell> sufCell = new List<FuseCell>(4);

	public UISprite sprite;
	public FuseType fuseType;
	public byte fuseCode;
	public float curAngle;
	public int colNum;
	public int tempRowNum;
	public bool readyToNew;

	public int travelGen;
	private FuseManager manager;

	/// <summary>
	/// init. random angle.
	/// </summary>
	public void init(FuseType type, int col, byte code, string spriteName, FuseManager manager)
	{
		reset(false);
		this.manager = manager;
		fuseType = type;
		fuseCode = code;
		colNum = col;
		sprite.spriteName = spriteName;
		curAngle = 0;
		transform.localRotation = Quaternion.identity;
		int result = Random.Range(0, 4);
		for(int i=0; i<result; i++)
		{
			rotateCell(false);
		}
	}

	public void changeColor(bool isFire)
	{
		if(isFire)
		{
			sprite.color = Color.red;
		}
		else
		{
			sprite.color = Color.yellow;
		}
	}

	public void reset(bool onlyColor)
	{
		sprite.color = Color.white;
		if(!onlyColor)
		{
			readyToNew = false;
			tempRowNum = 0;
			isFireStart = false;
		  	isRocketEnd = false;
		  	preCell = null;
		 	sufCell.Clear();
		}
	}

	private void rotateCell(bool useAnim)
	{
		if((fuseCode & 0x1) == 0)
		{
			fuseCode >>= 1;
		}
		else
		{
			fuseCode >>= 1;
			fuseCode += 0x8;
		}

		float newAngle = curAngle + 90f;
		if(newAngle >= 360f) newAngle = 0;

		if(useAnim)
		{
			TweenRotation tween = TweenRotation.Begin(gameObject, rotateTweenTime, Quaternion.identity);
			tween.from = Vector3.forward *(curAngle >= 270f ? -90 : curAngle);
			tween.to = Vector3.forward * newAngle;
			tween.SetOnFinished(rotateCellDone);
		}
		else
		{
			transform.Rotate(Vector3.forward * 90f);
		}
		curAngle = newAngle;
	}

	private void rotateCellDone()
	{
		manager.switchInput(true);
		if(manager != null) manager.onCellMove(fuseType == FuseType.normal);
	}

	public void fireAnim(GameObject effectObj)
	{
		TweenPosition.Begin(effectObj, fireTweenTime, transform.localPosition + Vector3.back * 10f);
		StartCoroutine(triggerNext(effectObj));
	}

	/// <summary>
	/// Check if the rocket can launch here.
	/// </summary>
	private IEnumerator triggerNext(GameObject effectObj)
	{
		yield return new WaitForSeconds(fireTweenTime);
		for(int i = 0; i < sufCell.Count; i++)
		{
			sufCell[i].fireAnim((i > 0) ? manager.getFireEffect(sufCell[i].transform.localPosition) : effectObj);
		}
		if(sufCell.Count == 0)
		{
			Destroy(effectObj);
		}
		// just hide the cell
		transform.localScale = Vector3.zero;
		readyToNew = true;
		if(isRocketEnd)
		{
			manager.rocketLaunched(tempRowNum);
		}
	}

	/// <summary>
	/// Raises the click event.
	/// </summary>
	void OnClick ()
	{
		manager.switchInput(false);
		rotateCell(true);
	}
}
