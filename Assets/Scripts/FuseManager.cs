using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FuseType
{
	normal,
	spaciel
}

/// <summary>
/// Fuse manager. all of main logic.
/// </summary>
public class FuseManager : MonoBehaviour 
{
	private const float cellSize = 70f;
	private const int col = 7;
	private const int row = 10;
	private float firePosOffset = 70f;
	private float rocketPosOffset = 10f;
	private float posTweenTime = 0.3f;
	private float scaleTweenTime = 0.1f;

	private string fusePrefab = "Prefabs/FuseCell";
	private string fireEffectPrefab = "Prefabs/FireEffect";
	private string firePrefab = "Prefabs/Fire";
	private string reocketPrefab = "Prefabs/Rocket";

	public event System.Action<bool> resetNotify;
	public System.Action moveNotify;
	public System.Action launchNotify;
	public UICamera cam;
	public Transform fuseGrid;
	public Transform fireParent;
	public Transform rocketParent;
	public Score scoreScript;

	private Object fireEffectObj;
	private List<GameObject> rocketList;

	private List<List<FuseCell>> allCellList;
	private List<FuseCell> startFireList;
	private List<FuseCell> uselessEndList;
	private int curTravelGen;
	private int canLaunchRocketNum;

	/// <summary>
	/// left-bottom point
	/// </summary>
	private float originalPosX;
	private float originalPosY;

	private Vector3 getFuseCellPos(int j, int i)
	{
		return new Vector3(originalPosX + cellSize * j, originalPosY + cellSize * i, 0);
	}

	void Start()
	{
		init();
		scoreScript.init(this);
	}
	
	void OnDestroy()
	{
		resetNotify = null;
		moveNotify = null;
		launchNotify = null;
		Debug.Log("FuseManager OnDestroy");
	}

	public void switchInput(bool enable)
	{
		cam.enabled = enable;
	}

	#region init

	public void init()
	{
		allCellList = new List<List<FuseCell>>(col);

		Object cellObj = Resources.Load(fusePrefab);
		originalPosX = -(cellSize * (col - 1) / 2);
		originalPosY = -(cellSize * (row - 1) / 2);
		for(int j=0; j<col; j++)
		{
			List<FuseCell> colList = new List<FuseCell>(row);
			allCellList.Add(colList);
			for(int i=0; i<row; i++)
			{
				GameObject cell = Instantiate(cellObj) as GameObject;
				cell.transform.parent = fuseGrid;
				cell.transform.localScale = Vector3.one;
				cell.transform.localPosition = getFuseCellPos(j, i);
				FuseCell cellScript = cell.GetComponent<FuseCell>();
				if(cellScript)
				{
					randomType(cellScript, j);
					resetNotify += cellScript.reset;
					colList.Add(cellScript);
				}
			}
		}
		initFireAndRocket();
		checkConnectivity();
	}

	/// <summary>
	/// Randoms the type. init Fusecell Script.
	/// </summary>
	private bool randomType(FuseCell fuseCell, int col)
	{
		FuseType type = FuseType.normal;
		string fuseSpriteName = "0";
		byte fuseCode = 0;
		int result = Random.Range(0, 4);
		switch(result)
		{
		case 0:
			fuseSpriteName = "1";
			fuseCode = 0x7;
			break;
		case 1:
			fuseSpriteName = "2";
			fuseCode = 0x9;
			break;
		case 2:
			fuseSpriteName = "3";
			fuseCode = 0xA;
			break;
		case 3:
			fuseSpriteName = "4";
			fuseCode = 0xF;
			type = FuseType.spaciel;
			break;
		default:
			Debug.LogError("No Such Type");
			return false;
		}
		fuseCell.init(type, col, fuseCode, fuseSpriteName, this);
		return true;
	}

	public void reset()
	{
		// just change all to white color
		if(resetNotify != null) resetNotify(false);
		checkConnectivity();
	}

	private void initFireAndRocket()
	{
		fireEffectObj = Resources.Load(fireEffectPrefab);
		Object fireObj = Resources.Load(firePrefab);
		Object rocketObj = Resources.Load(reocketPrefab);
		rocketList = new List<GameObject>(row);
		for(int i = 0; i < row; i++)
		{
			GameObject fire = getFire(fireObj);
			fire.transform.localPosition = new Vector3(originalPosX - firePosOffset, originalPosY + cellSize * i, 0);
			GameObject rocket = getRocket(rocketObj);
			rocketList.Add(rocket);
			rocket.transform.localPosition = new Vector3(originalPosX + cellSize * col + rocketPosOffset, originalPosY + cellSize * i, 0);
		}
	}

	#endregion

	#region check connectivity

	private void checkConnectivity()
	{
		curTravelGen++;
		startFireList = new List<FuseCell>();
		uselessEndList = new List<FuseCell>();
		canLaunchRocketNum = 0;

		checkCol(0, true);
		checkCol(col - 1, false);

		// remove useless path
		foreach(FuseCell end in uselessEndList)
		{
			removeFromPre(end);
		}

		// has rocket to launch.
		if(startFireList.Count > 0)
		{
			Debug.Log("start fire count=" + startFireList.Count);
			Debug.Log("launch rocket count=" + canLaunchRocketNum);

			switchInput(false);
			if(resetNotify != null) resetNotify(true);
			// change color immediately. show fire anim one by one.
			foreach(FuseCell start in startFireList)
			{
				prepareFire(start);
				start.fireAnim(getFireEffect(start.transform.localPosition));
			}
		}
	}

	private void checkCol(int j, bool isFire)
	{
		for(int i=0; i<row; i++)
		{
			FuseCell cell = allCellList[j][i];
			
			if(cell.travelGen != curTravelGen 
			   && (isFire ? (cell.fuseCode & 0x1) == 0x1 : (cell.fuseCode & 0x4) == 0x4))
			{
				cell.changeColor(isFire);
				cell.travelGen = curTravelGen;
				if(isFire)
				{
					cell.isFireStart = true;
					cell.tempRowNum = i;
					startFireList.Add(cell);
				}
			}
			else
			{
				continue;
			}
			
			checkAround(cell, j, i, isFire);
		}
	}
	
	private void checkAround(FuseCell cell, int j, int i, bool isFire)
	{
		bool isFindNew = false;
		byte cursor = 0x1;
		for(int c = 0; c < 4; c++)
		{
			if((cell.fuseCode & cursor) == cursor)
			{
				// simplify
				switch(c)
				{
				case 0:
					// left
					if(j > 0)
					{
						isFindNew = checkBorder(cell, j - 1, i, 0x4, isFire, isFindNew);
					}
					break;
				case 1:
					// top
					if(i < row - 1)
					{
						isFindNew = checkBorder(cell, j, i + 1, 0x8, isFire, isFindNew);
					}
					break;
				case 2:
					// right
					if(j < col - 1)
					{
						isFindNew = checkBorder(cell, j + 1, i, 0x1, isFire, isFindNew);
					}
					else if(isFire && j == col - 1)
					{
						cell.isRocketEnd = true;
						cell.tempRowNum = i;
						canLaunchRocketNum++;
					}
					break;
				case 3:
					// btm
					if(i > 0)
					{
						isFindNew = checkBorder(cell, j, i - 1, 0x2, isFire, isFindNew);
					}
					break;
				}
			}
			cursor <<= 1;
		}
		if(isFire && !isFindNew && !cell.isRocketEnd)
		{
			uselessEndList.Add(cell);
		}
	}
	
	private bool checkBorder(FuseCell cell, int j, int i, byte flag, bool isFire, bool isFindNew)
	{
		FuseCell cell_border = allCellList[j][i];
		if((cell_border.fuseCode & flag) == flag)
		{
			if(cell_border.travelGen != curTravelGen)
			{
				if(isFire)
				{
					isFindNew = true;
					cell_border.preCell = cell;
					cell.sufCell.Add(cell_border);
				}
				cell_border.changeColor(isFire);
				cell_border.travelGen = curTravelGen;
				checkAround(cell_border, j, i, isFire);
			}
			else
			{
				//isFindOld = true;
			}
		}
		return isFindNew;
	}

	#endregion

	#region en...

	public void onCellMove(bool hasChange)
	{
		if(moveNotify != null) moveNotify();
		if(hasChange)
		{
			reset();
		}
	}

	private void removeFromPre(FuseCell cell)
	{
		if(cell.isRocketEnd) return;
		if(cell.preCell != null)
		{
			List<FuseCell> pre_suf = cell.preCell.sufCell;
			pre_suf.Remove(cell);
			if(pre_suf.Count == 0)
			{
				removeFromPre(cell.preCell);
			}
		}
		else
		{
			if(cell.isFireStart && cell.sufCell.Count == 0)
			{
				cell.isFireStart = false;
				startFireList.Remove(cell);
			}
		}
	}

	private void prepareFire(FuseCell cell)
	{
		cell.sprite.color = Color.green;
		allCellList[cell.colNum].Remove(cell);
		allCellList[cell.colNum].Add(cell);
		foreach(FuseCell sufCell in cell.sufCell)
		{
			prepareFire(sufCell);
		}
	}

	public void rocketLaunched(int rowNum)
	{
		if(rowNum < row && rowNum >= 0)
		{
			Animator animator = rocketList[rowNum].GetComponentInChildren<Animator>();
			animator.Play("Launch");
		}
		else
		{
			Debug.LogError("Rocket Out Of Index, rowNum=" + rowNum);
		}
		if(launchNotify != null) launchNotify();
		canLaunchRocketNum--;
		if(canLaunchRocketNum == 0)
		{
			reposition();
		}
	}

	private void reposition()
	{
		FuseCell cell = null;
		for(int j=0; j<col; j++)
		{
			for(int i=0; i<row; i++)
			{
				cell = allCellList[j][i];
				TweenPosition.Begin(cell.gameObject, posTweenTime, getFuseCellPos(j, i)).method = UITweener.Method.EaseOut;
				if(cell.readyToNew)
				{
					randomType(cell, cell.colNum);
					UITweener tweener = TweenScale.Begin(cell.gameObject, scaleTweenTime, Vector3.one);
					tweener.delay = posTweenTime;
					if(j == col - 1 && i == row - 1)
					{
						tweener.SetOnFinished(repositionDone);
					}
				}
			}
		}
	}

	private void repositionDone()
	{
		switchInput(true);
		reset();
	}

	#endregion

	#region GameObject spawn

	/// <summary>
	/// maybe pool
	/// </summary>
	public GameObject getFireEffect(Vector3 pos)
	{
		GameObject gObj = Instantiate(fireEffectObj) as GameObject;
		gObj.transform.parent = fuseGrid;
		gObj.transform.localScale = Vector3.one;
		gObj.transform.localPosition = pos;
		return gObj;
	}

	public GameObject getRocket(Object obj)
	{
		GameObject gObj = Instantiate(obj) as GameObject;
		gObj.transform.parent = rocketParent;
		gObj.transform.localScale = Vector3.one;
		return gObj;
	}

	public GameObject getFire(Object obj)
	{
		GameObject gObj = Instantiate(obj) as GameObject;
		gObj.transform.parent = fireParent;
		gObj.transform.localScale = Vector3.one;
		return gObj;
	}

	#endregion
}
