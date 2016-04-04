# SdkManager
这是一个Unity3d中用来方便管理第三方SDK导入的插件，可以可视化管理哪些SDK当前需要引入到工程。
主要解决应用发布到不同平台时，需要切换不同SDK的烦恼。例如，应用的国内版和国国际版使用同一套代码，但分别需要接入微信、新浪微博，或者Facebook、twitter，SdkManager将这一切变得轻松。

### 原理
SdkManager将所有将用到的SDK文件拷贝到Assets/.SdkStore目录中，在Unity3d中视为不可见的。启用一个SDK时，在Assets/ManagedSDKs目录下建立软链接到.SdkStore相应SDK目录，并触发相应回调。

###如何使用
####1.导入插件  
在Unity3d工程的Assets目录中运行
```
git clone https://github.com/xclouder/SdkManager.git
```
####2.导入SDK
通过菜单`[Window/SdkManager]`打开SDK管理面板，点击右下角的"Install"按钮，选择SDK所在的文件夹。
在已安装的SDK前面勾上/取消，即可启用/禁用此SDK。禁用的SDK在项目打包时会被忽略，启用的SDK目录在Assets/ManagedSDKs下可以看到。

####3.事件处理(可选)
启用/禁用一个SDK时，项目配置可能要相应做修改，如sdk相关appId需要设置等。SdkManager在Enable/Disable一个SDK时，会触发如下回调：
```
	[OnModuleEnable]
	public static void OnModuleEnable(SDKInfo sdk)
	{
		Debug.Log("enable sdk:" + sdk.Name);
	}

	[OnModuleDisable]
	public static void OnModuleDisable(SDKInfo sdk)
	{
		Debug.Log("enable sdk:" + sdk.Name);
	}
```
开发者可在相应回调中修改配置信息

###CI集成
除了可视化修改SDK引用，还可以在CI构建脚本中直接调用
```
SdkManager.DisableAllSDKs(false);
SdkManager.EnableSDK(sdkId1, false);
SdkManager.EnableSDK(sdkId2, false);
SdkManager.EnableSDK(sdkId3, false);
...

AssetDatabase.Refresh();

```
开发者可以通过配置描述当前Build需要引用的sdkIds，脚本中读取配置并将其Enable，就能实现CI构建时SDK自动化切换。

###后续计划
1.支持SDK的Update  
2.添加SDK描述文件以及目录规范，以确定安装SDK时，Plugins目录等文件拷贝规则  
3.支持SDK套餐及其管理，几个SDK引用为一个套餐，切换一个目标平台时，只需切换一个套餐（请原谅我不知怎么想到了套餐这个词）  
4.组件仓库，依赖管理  

