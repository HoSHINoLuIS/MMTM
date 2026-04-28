[README.md](https://github.com/user-attachments/files/27158405/README.md)
# MementoMori 虚宝码自动领取工具

WinForms 工具，用于批量领取 `https://mememori-game.com/tw/code/` 的虚宝码。

## 功能

- 自动读取官方服务器列表。
- 选择伺服器并输入虚宝码。
- 本地玩家ID内容库，支持新增、修改、删除。
- 支持从剪贴板批量导入玩家ID，支持复制整个玩家库。
- 支持领取勾选玩家或领取全部玩家。
- 每个玩家会先调用确认接口，再调用发送接口。
- 可勾选“仅确认不兑换”用于检查玩家ID、玩家名、世界信息。

## 玩家库位置

玩家库保存为 JSON：

```text
%APPDATA%\MementoMoriCodeRedeemer\players.json
```

每行剪贴板导入格式可以是：

```text
123456789
123456789	备注
123456789,备注
```

## 发布命令

```powershell
dotnet publish .\MementoMoriCodeRedeemer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\dist
```
