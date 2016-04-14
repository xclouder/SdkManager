using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using xClouder.Utils;
using xClouder.SdkManager.Callbacks;
using System.Linq;
using System.Reflection;

namespace xClouder.SdkManager{
	public class SdkManager {

		private static SdkManager _ins;
		public static SdkManager Instance
		{
			get {
				if (_ins == null)
				{
					_ins = new SdkManager();
					_ins.Init();
				}

				return _ins;
			}
		}

		//store the origin sdk assets
		private static string DIR_SDK_ASSET_STORE = ".SdkStore";

		private static string DIR_USER_CODE = ".USER_CODE";

		private static string DIR_MGR_DATA = ".DATA";
		private static string FILE_ENABLED_SDKS = "enabledSDKs.txt";

		//store the soft link of sdk assets
		private static string DIR_MANAGED_SDKS = "ManagedSDKs";

		private static string[] EXCEPT_DIRS = new string[]{ DIR_MGR_DATA, DIR_USER_CODE };

		private string managedSDKsDir;
		private string sdkStoreDir;

		//temp for use
		private BaseSdkImporter sdkImporter = new BaseSdkImporter();

		public string GetSdkStoreDir() 
		{
			return sdkStoreDir;
		}

		private void Init()
		{
			//init dir related
			sdkStoreDir = Path.Combine(Application.dataPath, DIR_SDK_ASSET_STORE);
			managedSDKsDir = Path.Combine(Application.dataPath, DIR_MANAGED_SDKS);

			//scan the local installed SDKs
			LoadSdkInfos(false);

		}

		private bool IsInExceptDir(string dir)
		{
			var dirInfo = new DirectoryInfo(dir);
			foreach (var d in EXCEPT_DIRS)
			{
				if (d.Equals(dirInfo.Name))
				{
					return true;
				}
			}

			return false;
				
		}

		private bool hasLoadedSdkInfos = false;
		public void LoadSdkInfos(bool force)
		{
			if (!force && hasLoadedSdkInfos)
			{
				return;
			}

			if (!Directory.Exists(sdkStoreDir))
			{
				Directory.CreateDirectory(sdkStoreDir);
			}

			if (!Directory.Exists(managedSDKsDir))
			{
				Directory.CreateDirectory(managedSDKsDir);
			}

			string[] directories = Directory.GetDirectories(sdkStoreDir);
			foreach (var dir in directories)
			{
				if (IsInExceptDir(dir))
					continue;

				try {
					var sdk = ParseSdkInfoFromDirectory(dir);
					AddSdk(sdk);
				}
				catch (System.Exception e)
				{
					Debug.LogError("error:" + e.ToString());
					continue;
				}
			}

			directories = Directory.GetDirectories(managedSDKsDir);
			foreach (var dir in directories)
			{
				try {
					var sdk = ParseSdkInfoFromDirectory(dir);
					if (IsSdkExists(sdk))
					{
						var realSdkInfo = GetSDKInfo(sdk.Id);
						realSdkInfo.Enabled = true;
					}
				}
				catch (System.Exception e)
				{
					Debug.LogWarning("error:" + e.ToString());
					continue;
				}
			}

			hasLoadedSdkInfos = true;
		}

		public void CleanSDKInfos()
		{
			if (sdkInfos != null)
				sdkInfos.Clear();

			if (sdkInfoDict != null)
				sdkInfoDict.Clear();
		}

		public SDKInfo GetSDKInfo(string infoId)
		{
			if (sdkInfoDict == null)
				return null;

			if (sdkInfoDict.ContainsKey(infoId))
			{
				return sdkInfoDict[infoId];
			}

			return null;
		}

		public SDKInfo InstallSDK(string sdkDirectory)
		{
			if (!IsValidSdkDir(sdkDirectory))
			{
				throw new System.InvalidOperationException("not a valid sdk directory");
			}

			var sdkInfo = ParseSdkInfoFromDirectory(sdkDirectory);

			//add module into list,duplicate will throw exception
			AddSdk(sdkInfo);

			//copy from sourceDir to sdkStore
			string destDir = Path.Combine(sdkStoreDir, sdkInfo.Name);
			FileUtil.CopyFileOrDirectory(sdkDirectory, destDir);

			return sdkInfo;

		}

		public void UninstallSDK(SDKInfo sdk)
		{
			//if enabled, disable it first
			if (sdk.Enabled)
			{
				DisableSDK(sdk);
			}

			var path = Path.Combine(sdkStoreDir, sdk.Dir);
			bool succ = FileUtil.DeleteFileOrDirectory(path);
			if (!succ)
			{
				throw new IOException("delete path failed:" + path);
			}

			sdkInfos.Remove(sdk);
			sdkInfoDict.Remove(sdk.Id);
		}

		public void EnableSDK(SDKInfo sdk, bool refreshAssetsImmediately = true)
		{
			if (!Directory.Exists(managedSDKsDir))
			{
				Directory.CreateDirectory(managedSDKsDir);
			}

			if (!IsSdkExists(sdk)){
				throw new System.InvalidOperationException("sdk not exist:" + sdk.Name);
			}

			if (sdk.Enabled)
			{
				Debug.Log("sdk: " + sdk.Name + "is enabled");
				return;
			}

			sdkImporter.EnableSDK(sdk);

			/*
			string srcPath = Path.Combine(sdkStoreDir, sdk.Dir);
			string link = Path.Combine(managedSDKsDir, sdk.Dir);

			if ((Directory.Exists(srcPath) || File.Exists(srcPath))
				&&
				(!File.Exists(link) && !Directory.Exists(link))
				&&
				FileLinkUtil.CreateSymbol(srcPath, link))
			{

				Debug.Log("link:" + srcPath + " -> " + link);
			}
			else{
				Debug.LogWarning("link fail:" + srcPath + " -> " + link);
			}
			*/

			sdk.Enabled = true;

			if (refreshAssetsImmediately)
				RefreshAssets();

			NotifyEnabledSDK(sdk);
		}

		private void RefreshAssets()
		{
			AssetDatabase.Refresh();

			SaveData();
		}
		private void SaveData()
		{
			
			//save enabled sdks to .txt file
			if (sdkInfos == null)
				return;
			
			List<string> ls = new List<string>();
			foreach (SDKInfo i in sdkInfos)
			{
				if (i.Enabled)
					ls.Add(i.Id);
			}

			if (ls.Count == 0)
				return;
			
			var data = string.Join("|", ls.ToArray());
			Debug.Log("save data:" + data);

			var path = Path.Combine(GetSdkStoreDir(), DIR_MGR_DATA);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			path = Path.Combine(path, FILE_ENABLED_SDKS);
			File.WriteAllText(path, data);
		}

		private void NotifyEnabledSDK(SDKInfo sdk)
		{

			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

			var methods = assemblies.SelectMany(s => s.GetTypes())
				.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
				.Where(m => System.Attribute.IsDefined(m, typeof(OnModuleEnableAttribute)));

			//todo, sort by attribute's order
			foreach (var m in methods)
			{
				m.Invoke(null, new object[]{sdk});
			}

		}

		public void DisableSDK(SDKInfo sdk, bool refreshAssetsImmediately = true)
		{
			if (!IsSdkExists(sdk)){
				throw new System.InvalidOperationException("sdk not exist:" + sdk.Name);
			}

			if (!sdk.Enabled)
			{
				Debug.Log("sdk: " + sdk.Name + "has already disabled");
				return;
			}

			sdkImporter.DisableSDK(sdk);

			/*
			string link = Path.Combine(managedSDKsDir, sdk.Dir);
			if (File.Exists(link) || Directory.Exists(link)){
				FileLinkUtil.RemoveSymbol(link);
				Debug.Log("RemoveSymbol:" + link);
			}
			else{
				Debug.Log("no link file:" + link);
			}
			*/

			sdk.Enabled = false;

			if (refreshAssetsImmediately)
				RefreshAssets();

			NotifyDisabledSDK(sdk);
		}

		private void NotifyDisabledSDK(SDKInfo sdk)
		{
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

			var methods = assemblies.SelectMany(s => s.GetTypes())
				.SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
				.Where(m => System.Attribute.IsDefined(m, typeof(OnModuleDisableAttribute)));

			//todo, sort by attribute's order
			foreach (var m in methods)
			{
				m.Invoke(null, new object[]{sdk});
			}
		}

		//get a copy of the list
		public List<SDKInfo> GetInstalledSDKs()
		{
			if (sdkInfos == null)
				return null;

			var list = new List<SDKInfo>();
			list.AddRange(sdkInfos);
			return list;
		}

		public List<SDKInfo> GetEnabledSDKs()
		{
			if (sdkInfos == null)
				return null;

			var list = new List<SDKInfo>();
			foreach (var sdk in sdkInfos)
			{
				if (sdk.Enabled)
				{
					list.Add(sdk);
				}
			}
			return list;
		}

		public void DisableAll(bool refreshAssetsAfterAllDisabled)
		{
			var list = GetEnabledSDKs();
			if (list == null || list.Count == 0)
			{
				return;
			}

			foreach (var sdk in list)
			{
				try
				{
					DisableSDK(sdk, false);
				}
				catch(System.Exception e)
				{
					Debug.LogError("uninstall sdk error:" + e.ToString());
					continue;
				}
			}

			if (refreshAssetsAfterAllDisabled)
				AssetDatabase.Refresh();
		}

		#region static Methods
		public static void DisableAllSDKs(bool refreshAssetsAfterAllDisabled)
		{
			var mgr = SdkManager.Instance;
			mgr.DisableAll(refreshAssetsAfterAllDisabled);
		}

		public static void EnableSDK(string sdkId, bool refreshImmediately)
		{
			var mgr = SdkManager.Instance;
			var sdkInfo = mgr.GetSDKInfo(sdkId);
			if (sdkInfo == null)
			{
				Debug.LogError("no SDK found for id:" + sdkId);
				return;
			}

			mgr.EnableSDK(sdkInfo, refreshImmediately);
		}

		public static void DisableSDK(string sdkId, bool refreshImmediately)
		{
			var mgr = SdkManager.Instance;
			var sdkInfo = mgr.GetSDKInfo(sdkId);
			if (sdkInfo == null)
			{
				Debug.LogError("no SDK found for id:" + sdkId);
				return;
			}

			mgr.DisableSDK(sdkInfo, refreshImmediately);
		}

		#endregion

		#region Private 
		//SdkInfos cache
		private List<SDKInfo> sdkInfos;
		private IDictionary<string, SDKInfo> sdkInfoDict;

		private void AddSdk(SDKInfo sdk)
		{
			if (sdkInfos == null)
			{
				sdkInfos = new List<SDKInfo>();
				sdkInfoDict = new Dictionary<string, SDKInfo>();
			}

			if (IsSdkExists(sdk))
			{
				throw new System.InvalidOperationException("installing duplicate SDK, please uninstall old sdk first");
			}

			sdkInfos.Add(sdk);
			sdkInfoDict.Add(sdk.Id, sdk);
		}

		private bool IsSdkExists(SDKInfo sdk)
		{
			if (sdkInfoDict == null)
			{
				return false;
			}

			return sdkInfoDict.ContainsKey(sdk.Id);
		}
		#endregion


		#region Util
		private static string SDK_INFO_FILE_NAME = "info.json";
		private static SDKInfo ParseSdkInfoFromDirectory(string dir)
		{
			if (!IsValidSdkDir(dir))
			{
				throw new System.InvalidOperationException("dir is not a valid sdkdir");
			}

			SDKInfo info = null;

			var infoFilePath = Path.Combine(dir, SDK_INFO_FILE_NAME);
			if (!File.Exists(infoFilePath))
			{
				info = new SDKInfo();

			}
			else
			{
				var json = File.ReadAllText(infoFilePath);
				Debug.Log("info json:" + json);

				try 
				{
					//info = JsonUtility.FromJson<SDKInfo>(json);
					info = LitJson.JsonMapper.ToObject<SDKInfo>(json);

					if (info.CopyRules != null)
					{
						foreach (var pair in info.CopyRules)
						{
							pair.Key.TrimEnd(new char[]{Path.DirectorySeparatorChar});

						}
					}

				}catch (System.Exception e)
				{
					Debug.LogError("read info file error." + e.ToString());
				}
			}

			DirectoryInfo di = new DirectoryInfo(dir);
			string dirName = di.Name;

			info.Dir = info.Dir ?? dirName;
			info.Enabled = false;

			//temp
			info.Version = info.Version ?? "1.0";
			info.Id = info.Id ?? dirName;
			info.Name = info.Name ?? dirName;

			Debug.Log("info:" + info);
			return info;

		}

		private static bool IsValidSdkDir(string sdkDir)
		{
			return Directory.Exists(sdkDir);
		}

		#endregion
	}
}