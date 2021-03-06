/*************************************************************************
 *  FileName: BaseSdkImporter.cs
 *  Author: xClouder
 *  Create Time: 04/14/2016
 *  Description:
 *
 *************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace xClouder.SdkManager{
	public class BaseSdkImporter
	{

		#region Public Method

		//base sdk importer only implement copy rules, no convention
		public void EnableSDK(SDKInfo sdk)
		{
			Debug.Log("Enable SDK:" + sdk.Name);
			var copyRules = sdk.CopyRules;
			if (copyRules == null || copyRules.Count == 0)
				return;

			foreach (var pair in copyRules)
			{
				var toPath = pair.Key;
				var fromPath = pair.Value;

				RelativeCopy(sdk, fromPath, toPath);
			}

			EnableUserCode(sdk);
		}

		public void DisableSDK(SDKInfo sdk)
		{
			Debug.Log("Disable SDK:" + sdk.Name);

			var copyRules = sdk.CopyRules;
			if (copyRules == null || copyRules.Count == 0)
				return;

			foreach (var pair in copyRules)
			{
				var toPath = pair.Key;
				var path = GetToPath(toPath);

				if (Directory.Exists(path) || File.Exists(path))
				{
					FileUtil.DeleteFileOrDirectory(path);
				}
				else
				{
					Debug.LogWarning("Path:" + path + " NOT EXIST");
				}
			}

		}

		#endregion

		protected string GetFromPath(SDKInfo sdk, string relativeFrom)
		{
			var fromPath = Path.Combine(SdkManager.Instance.GetSdkStoreDir(), sdk.Dir);
			fromPath = Path.Combine(fromPath, relativeFrom);
			return fromPath;
		}

		protected string GetToPath(string relativeTo)
		{
			return Path.Combine(Application.dataPath, relativeTo);
		}

		/**
		 * fromPath relative to sdk folder
		 * toPath relative to /Assets
		 */
		protected void RelativeCopy(SDKInfo sdk, string fromPath, string toPath)
		{
			var from = GetFromPath(sdk, fromPath);
			var to = GetToPath(toPath);
			Debug.Log("fromPath:" + from);
			Debug.Log("toPath:" + to);

			var attr = File.GetAttributes(from);
			if ((attr & FileAttributes.Directory) == 0){
				var fileInfo = new FileInfo(to);
				var parentDir = fileInfo.Directory.FullName;
				if (!Directory.Exists(parentDir))
				{
					Debug.Log("directory:" + parentDir + " not exist, create it.");
					Directory.CreateDirectory(parentDir);
				}
			}

			FileUtil.CopyFileOrDirectory(from, to);

		}

		protected virtual void EnableUserCode(SDKInfo sdk)
		{

		}

		protected virtual void DisableUserCode(SDKInfo sdk)
		{

		}
	}


}