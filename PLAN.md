# PLAN.md — dotnet-webforms-migration

## 概要

社員勤怠管理システムを題材に、`ASP.NET WebForms` から `.NET 8 Web API + React` への移行を実践するサンプルプロジェクト。

`dotnet-modernization-lab`（WinForms → Web API）の姉妹リポとして、**レガシーWebアプリ固有の問題**（ViewState・AutoPostBack・コードビハインド）を解体・再構成するプロセスを提示する。

---

## Before: ASP.NET WebForms の構成

`legacy/AttendanceWebForms/` に以下を再現する。

### 画面構成イメージ

```
+-----------------------------------------------------------+
| [ 勤怠打刻画面 ]                                          |
+-----------------------------------------------------------+
| 社員番号: [ EMP-001 ]  部署: [ 開発部 ▼ ]               |
|                         ↑ AutoPostBack=true               |
|                           選ぶたびにページ全体がリロード  |
| -------------------------------------------------------   |
| [ 出勤 ]  [ 退勤 ]  [ 休憩開始 ]  [ 休憩終了 ]           |
|  ↑ ボタンクリックでポストバック → SQL直書きで記録         |
| -------------------------------------------------------   |
| 今月の出勤日数: 12日   合計時間: 96時間                   |
| ↑ Page_Load のたびにDB集計クエリが走る                    |
| -------------------------------------------------------   |
| [ 月次レポート出力 ]                                      |
|  ↑ Response.Write でCSVを直接ストリーム出力               |
+-----------------------------------------------------------+
```

### 主な課題点

- **AutoPostBack による UX 劣化**: ドロップダウン選択のたびにページ全体がリロードされ、操作感が悪い。
- **ViewState の肥大化**: 打刻履歴や集計データを ViewState に持たせることでリクエストサイズが膨張する。
- **Page_Load への処理集中**: 初期表示・集計・権限チェックがすべて `Page_Load` に混在し、テスト不能。
- **コードビハインドの密結合**: `.aspx.cs` に SQL・業務ロジック・レスポンス生成が直書きされている。
- **SQL インジェクションのリスク**: 文字列結合による SQL 組み立て（WinForms と同様の問題）。

---

## After: .NET 8 Web API + React への転換

### 構成

1. **Frontend (React/TypeScript)**: 打刻操作・履歴表示・月次サマリーをSPAで提供。
2. **Backend (ASP.NET Core / Minimal API)**: 勤怠ロジックを Service 層に集約。
3. **Database**: PostgreSQL（Dapper によるパラメータ化クエリ）。

### 実装予定 API エンドポイント

| Method | Path | 説明 |
| ------ | ---- | ---- |
| GET | `/employees` | 社員マスタ取得 |
| POST | `/attendances/clock-in` | 出勤打刻 |
| POST | `/attendances/clock-out` | 退勤打刻 |
| GET | `/attendances/{employeeId}/monthly` | 月次勤怠サマリー取得 |
| GET | `/attendances/{employeeId}/history` | 打刻履歴一覧取得 |

### 移行アプローチ

- **AutoPostBack の廃止**: 状態変更をすべて非同期 API 呼び出しに置き換え、画面リロードを排除。
- **ViewState の廃止**: サーバー側の状態管理をやめ、必要なデータは都度 API から取得。
- **Page_Load の解体**: 混在していた処理を Service 層・Repository 層へ責務分離。
- **月次集計ロジックの独立**: `AttendanceService` に切り出し、単体テストを可能にする。

---

## 技術スタック

| Layer | Technology |
| ----- | ---------- |
| **Frontend** | React, TypeScript, Vite, Tailwind CSS |
| **Backend** | .NET 8 (Minimal API), xUnit |
| **Database** | PostgreSQL (Dapper) |
| **Infrastructure** | Docker Compose, Cloudflare Tunnel |

---

## dotnet-modernization-lab との対比

| | dotnet-modernization-lab | dotnet-webforms-migration |
|---|---|---|
| **Before** | WinForms（デスクトップ） | WebForms（レガシーWeb） |
| **レガシー固有の問題** | Windows依存・LPT1ポート | ViewState・AutoPostBack |
| **業務ドメイン** | 受注管理 | 勤怠管理 |
| **共通の問題** | コードビハインド密結合・SQLインジェクション・テスト不能 ||

---

## デモ運用について

本プロジェクトのデモは **オンデマンド起動** で運用する。

VPS のメモリ制約上、複数アプリを常時起動せず、面談時のみ以下のコマンドで起動する。

```bash
# 起動（面談前）
sudo systemctl start webforms-migration.service
sudo systemctl start cloudflare-webforms.service

# 停止（面談後）
sudo systemctl stop webforms-migration.service
sudo systemctl stop cloudflare-webforms.service
```

起動後、cloudflared のログから発行された URL を確認して共有する。

```bash
journalctl -u cloudflare-webforms.service -n 10 | grep trycloudflare
```

PostgreSQL は常時起動のまま維持する（データ保持のため）。

---

## ディレクトリ構造（予定）

```
.
├── .github/
│   └── workflows/               # CI/CD（GitHub Actions）
├── docs/
│   ├── architecture.md          # アーキテクチャ図（Mermaid）
│   └── migration-plan.md        # 移行フェーズ定義
├── infrastructure/
│   ├── db/init/
│   │   └── 01_schema.sql
│   └── deploy.sh
├── legacy/
│   └── AttendanceWebForms/      # Before: ASP.NET WebForms サンプル
├── src/
│   ├── Api/                     # After: .NET 8 Web API
│   ├── Api.Tests/               # xUnit テスト
│   └── Web/                     # After: React Frontend
├── docker-compose.yml
└── README.md
```
