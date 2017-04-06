using UnityEngine;
using System.Collections;
using BLINDED_AM_ME;

public class ExampleUseof_SavedData : MonoBehaviour {

	public string password = "no_suck_luck";
	public string fileName = "saveData.txt";
	public SaveData data;

	// Use this for initialization
	void Start () {
	
		data = new SaveData(password, Application.persistentDataPath, fileName);

		int numBombs = int.Parse(data.Get_Value("numBombs", "0"));
		Debug.Log(numBombs.ToString());

		numBombs = 5;
		data.Set_Value("numBombs", numBombs.ToString());

		data.Save_Data();

	}


	public void OnApplicationPause(bool pauseStatus) {
		if(pauseStatus)
			data.Save_Data();
	}
	public void OnApplicationQuit(){
		data.Save_Data();
	}
}
