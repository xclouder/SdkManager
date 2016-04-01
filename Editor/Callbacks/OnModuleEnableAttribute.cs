using UnityEngine;
using System.Collections;
using System;

namespace xClouder.SdkManager.Callbacks {
	public class OnModuleEnableAttribute : Attribute {

		public OnModuleEnableAttribute(int order) {
			ExecuteOrder = order;
		}

		public int ExecuteOrder {get;set;}

	}

}