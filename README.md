# SdkManager
Unity3d中用来方便管理第三方SDK导入的插件，可以可视化管理哪些SDK当前需要引入到工程。
主要解决应用发布到不同平台时，需要切换不同SDK的烦恼。

SdkManager还提供一套API来管理引用的SDK，方便在CI服务器上执行构建任务。

###如何使用
####1.导入插件  
在Unity3d工程的Assets目录中运行
```
git clone https://github.com/xclouder/SdkManager.git
```
####2.导入SDK
通过菜单`[Window/SdkManager]`打开SDK管理面板，点击右下角的"Install"按钮，选择SDK所在的文件夹。
在已安装的SDK前面勾上/取消，即可启用/禁用此SDK。禁用的SDK在项目打包时会被忽略，启用的SDK目录在Assets/ManagedSDKs下可以看到。

###后续计划
1.添加Hooks。切换SDK时，提供相关钩子进行一些自定义操作，如工程配置修改  
2.支持SDK的Update  
3.支持SDK套餐及其管理，几个SDK引用为一个套餐，切换一个目标平台时，只需切换一个套餐（请原谅我不知怎么想到了套餐这个词）  
4.组件仓库，依赖管理  

