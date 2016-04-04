using UnityEngine;
using System.Collections;

namespace xClouder.SdkManager.Callbacks
{
	public class OnModuleDisableAttribute : System.Attribute {

		public OnModuleDisableAttribute(int order = 0) {
			ExecuteOrder = order;
		}

		public int ExecuteOrder {get;set;}

	}
}