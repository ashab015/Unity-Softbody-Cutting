using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BLINDED_AM_ME{

	public class SaveData{
		
		public string secretPassword  = "no_such_luck";
		public string fileDestination = "Application.persistentDataPath";
		public string fileName        = "Saved_Data.txt";

		private Dictionary<string, object> allData;

		// constructors
		public SaveData ()
		{
			this.Init();
		}
		public SaveData (string password)
		{
			this.secretPassword = password;
			this.Init();
		}
		public SaveData (string password, string destination)
		{
			this.secretPassword = password;
			this.fileDestination = destination;
			this.Init();
		}
		public SaveData (string password, string destination, string fileName)
		{
			this.secretPassword = password;
			this.fileDestination = destination;
			this.fileName        = fileName;
			this.Init();
		}

		private void Init(){
			
				Debug.Log("new SavedData");

			allData = new Dictionary<string, object>();
			allData.Add("emptyObj", "0");

			if(fileDestination.Equals("Application.persistentDataPath"))
				fileDestination = Application.persistentDataPath;

			Load_Data();
		}

		/// <summary>
		/// Call this at the beginning of the App's opening
		/// </summary>
		public void Load_Data(){

			byte[] theKeyBytes  = Encoding.UTF8.GetBytes(secretPassword);

			string finalOutcome = "";
			string line = "";

			if(System.IO.File.Exists(fileDestination+"/"+fileName)){

				//Pass the file path and file name to the StreamReader constructor
				using (StreamReader sr = new StreamReader(fileDestination+"/"+fileName)){

					//Read the first line of text
					line = sr.ReadLine();

					//Continue to read until you reach end of file
					while (line != null) 
					{
						finalOutcome += line;
						//Read the next line
						line = sr.ReadLine();
					}

					//close the file
					sr.Close();

					byte[] theDataBytes = Convert.FromBase64String(finalOutcome);

					// decipher
					int tempInt = 0;
					for(int i=0; i<theDataBytes.Length; i++){

						tempInt = (int) theDataBytes[i];
						tempInt -= (int) theKeyBytes[i % theKeyBytes.Length];
						if(tempInt < 0)// aka negative
							tempInt += 256;
						theDataBytes[i] = (byte) tempInt;
					}

					string theDecodedString = Encoding.UTF8.GetString(theDataBytes);
					Debug.Log(theDecodedString);
					allData = MiniJSON.Json.Deserialize(theDecodedString) as Dictionary<string,object>;

				}

			}else{

				allData = new Dictionary<string, object>();
				allData.Add("emptyObj", "0");
			}

		}


		/// <summary>
		/// Saves the data to destination.
		/// </summary>
		/// <param name="destination">Destination.</param>
		/// <param name="fileName">File name.</param>
		public void Save_Data(){

			byte[] theKeyBytes  = Encoding.UTF8.GetBytes(secretPassword);
			byte[] theDataBytes = Encoding.UTF8.GetBytes(MiniJSON.Json.Serialize(allData));

			// apply the keybytes
			int tempInt = 0;  // byte can equal 0 - 255
			for(int i=0; i<theDataBytes.Length; i++){
							
					tempInt = (int) theDataBytes[i];
					tempInt += (int) theKeyBytes[i % theKeyBytes.Length];
					tempInt = tempInt  % 256;
					theDataBytes[i] = (byte) tempInt;
			}
			
			string theEncodedString = Convert.ToBase64String(theDataBytes, Base64FormattingOptions.InsertLineBreaks);

			// check if destination exists
			if(!System.IO.Directory.Exists(fileDestination))
				System.IO.Directory.CreateDirectory(fileDestination);


			//Pass the filepath and filename to the StreamWriter Constructor
			using(StreamWriter sw = new StreamWriter(fileDestination + "/" + fileName)){
				sw.Write(theEncodedString);
				//Close the file
				sw.Close();
			}	
		
		}

		public void Set_Value(string key, string value){

			if(allData.ContainsKey(key)){
				allData[key] = value;
			}else{
				allData.Add(key, value);
			}
		}
					
		public string Get_Value(string key, string defualtValue){

			if(allData.ContainsKey(key)){
					return allData[key].ToString();
			}else{
					return defualtValue;
			}
		}

	}
}