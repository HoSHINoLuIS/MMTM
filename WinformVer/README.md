[README.md](https://github.com/user-attachments/files/27185716/README.md)
# MementoMori 虛寶碼自動領取工具

WinForms 工具，用於批次領取 `https://mememori-game.com/tw/code/` 的虛寶碼。

## 功能

- 自動讀取官方伺服器列表。
- 選擇伺服器並輸入虛寶碼。
- 本機玩家 ID 內容庫，支援新增、修改、刪除。
- 支援從剪貼簿批次匯入玩家 ID，支援複製整個玩家庫。
- 支援領取勾選玩家或領取全部玩家。
- 每位玩家會先呼叫確認介面，再呼叫傳送介面。
- 可勾選「僅確認不兌換」用於檢查玩家 ID、玩家名稱、世界資訊。

## 玩家庫位置

玩家庫儲存為 JSON：

```text
%APPDATA%\MementoMoriCodeRedeemer\players.json
```

每行剪貼簿匯入格式可以是：

```text
123456789
123456789	備註
123456789,備註
```

## 發布命令

```powershell
dotnet publish .\MementoMoriCodeRedeemer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\dist
```
