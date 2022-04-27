# README

本项目包括两个场景：开始界面、主场景

```
开始界面的场景：Assets/Plugins/Excelsior/CSFHI/Scenes/CSFHI_Showroom.unity

主场景：Assets/Scenes/SampleScene.unity

代码放在 Assets/Script，其中 HexGameUI.cs 是废弃的，疑似废弃的（好像也没用到的）还有 HexFeatureManager.cs、HexMapEditor.cs

模型及其贴图素材、动画控制文件、特效模型都放在 Assets/Prefabs

地形贴图、Shader放在 Assets/Materials

六边形网格地形是由代码动态生成的（向 UV 集加点），因此没有地形文件。
```

将 `replay.bin` 放到 `Assets/WebGLTemplates/Template/` 目录下，启动后在浏览器控制台输入指令：

```javascript
fetch("replay.bin").then((r) => r.blob()).then((r) => postMessage({message:"init_replay_player", replay_data: r}))
```

测试选手名称的参考代码如下所示：

```javascript
postMessage({message:"load_players", "players": ["A", "B"]})
```

测试在线模式的参考代码如下所示：

```javascript
postMessage({message:"init_player_player", "token": token})
```