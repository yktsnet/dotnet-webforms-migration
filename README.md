# .NET WebForms Migration

[![CI](https://github.com/kyamakawa-widget/dotnet-webforms-migration/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/kyamakawa-widget/dotnet-webforms-migration/actions/workflows/ci.yml)

レガシーな WebForms 業務アプリを題材に、`.NET 8 Web API + React` への段階的移行を実践するサンプルプロジェクト。

[dotnet-modernization-lab](https://github.com/kyamakawa-widget/dotnet-modernization-lab)（WinForms 移行）の姉妹リポ。**「壊れていないから放置されるレガシー」** という、より現実的なシナリオを扱う。

---

## 1. 概要とゴール

WinForms の問題は一目で明らかだが、WebForms は動いている。ページが表示され、データが保存され、CSV も出力される。**しかし設計は腐っている。**

本プロジェクトの目的は、「動いている」レガシーに対して移行を正当化できる根拠を設計で示すことにある。

**Before Demo:** https://dotnet-webforms-migration-legacy.pages.dev  
**After Demo:** ※ 面談時オンデマンド起動（Cloudflare Tunnel）  
**After API ドキュメント (Swagger UI):** `/api-docs`

### 実践のポイント

- **解読**: AutoPostBack・ViewState・Page_Load 集中という WebForms 固有の問題の特定
- **分離**: UI、Service、Repository 層への責務分離
- **刷新**: .NET 8 Web API と React による再構築
- **品質**: テスタビリティの確保と単体テストの導入

---

## 2. Before: レガシーな密結合の実態

`legacy/AttendanceWebForms/` では、WebForms 時代の典型的な「Page_Load がすべてを知りすぎている」状態を再現している。

### 構成イメージ

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

- **AutoPostBack による UX 劣化**: 部署選択のたびにページ全体がリロードされ、スクロール位置がリセットされる。
- **ViewState の肥大化**: 打刻履歴・集計データを ViewState に保持することでリクエストサイズが膨張する。
- **Page_Load への処理集中**: 初期表示・集計・権限チェックがすべて `Page_Load` に混在し、テスト不能。
- **Response.Write による CSV 出力**: 文字化けが発生しやすく、エラー時の制御が不能。
- **SQL インジェクションのリスク**: 文字列結合による SQL 組み立て。

> **Before デモについて**  
> 静的 HTML による再現。AutoPostBack の白フラッシュ・PostBack 遅延・ViewState 隠しフィールド・文字化け CSV ダウンロードを体感できる。

---

## 3. After: モダンアーキテクチャへの転換

移行後は責務に応じてコンポーネントを完全に分離し、PostBack を廃止する。

### 構成

1. **Frontend (React/TypeScript)**: 状態管理と UI 表示に専念。AutoPostBack・ViewState を持たない。
2. **Backend (ASP.NET Core)**: 勤怠ロジックを Service 層に集約。
3. **Database**: 疎結合なアクセス（Dapper）。

### 移行アプローチ

- **AutoPostBack の廃止**: 部署選択を非同期フェッチに置き換え、ページリロードを排除。
- **ViewState の廃止**: サーバー側の状態管理をやめ、必要なデータは都度 API から取得。
- **Page_Load の解体**: 混在していた処理を `AttendanceService` へ責務分離し、単体テストを可能にする。
- **CSV 出力の正規化**: `Content-Disposition` ヘッダーによる UTF-8 ダウンロードに置き換え。

### 実装エンドポイント

| Method | Path | 説明 |
|---|---|---|
| GET | `/employees` | 社員マスタ取得 |
| POST | `/attendances/clock-in` | 出勤打刻 |
| POST | `/attendances/clock-out` | 退勤打刻 |
| GET | `/attendances/{employeeId}/monthly` | 月次勤怠サマリー取得 |
| GET | `/attendances/{employeeId}/history` | 打刻履歴一覧取得 |
| GET | `/attendances/{employeeId}/monthly/csv` | 月次レポート CSV ダウンロード |

---

## 4. 技術スタック

| Layer | Technology |
|---|---|
| **Frontend** | React, TypeScript, Vite, Tailwind CSS |
| **Backend** | .NET 8 (Minimal API), xUnit |
| **Database** | PostgreSQL (Dapper) |
| **Infrastructure** | Docker Compose, Cloudflare Tunnel |

---

## 5. モダナイゼーションの方針

1. **AutoPostBack の根絶**: 状態変更をすべて非同期 API 呼び出しに置き換え、ブラウザの恩恵を取り戻す。
2. **ViewState レスな設計**: サーバーに状態を持たせず、API ドリブンで必要なデータのみ取得する。
3. **Page_Load の解体 (Service 層)**: 混在処理を切り出し、単体テストで変更の安全性を担保する。
4. **環境の抽象化 (Docker)**: IIS / Windows Server 依存を排除し、どこでも同一手順で起動できる構成へ。
5. **CI/CD のパイプライン化 (GitHub Actions)**: 自動でビルド・テストを実行し、品質を継続的に担保する。

> **Focus & Scope**  
> 本プロジェクトは **「WebForms 固有の問題の解体と構造分離」** に特化している。  
> 認証・認可や本番用 DB の冗長化構成は **対象外 (Out-of-Scope)** としている。

> **インフラ補足**: After デモは面談時のみ VPS 上で起動（`systemctl start`）。  
> Before デモは Cloudflare Pages により常時稼働。

---

## 6. dotnet-modernization-lab との対比

| | [dotnet-modernization-lab](https://github.com/kyamakawa-widget/dotnet-modernization-lab) | dotnet-webforms-migration |
|---|---|---|
| **Before** | WinForms（デスクトップ） | WebForms（レガシー Web） |
| **問題の性質** | 明らかに壊れている | 動いているが設計が腐っている |
| **レガシー固有の問題** | UI フリーズ・LPT1 依存 | AutoPostBack・ViewState |
| **業務ドメイン** | 受注管理 | 勤怠管理 |
| **共通の問題** | コードビハインド密結合・SQL インジェクション・テスト不能 ||

---

## 7. ディレクトリ構造

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
│   └── AttendanceWebForms/      # Before: 静的 HTML デモ（Cloudflare Pages）
├── src/
│   ├── Api/                     # After: .NET 8 Web API
│   ├── Api.Tests/               # xUnit テスト
│   └── Web/                     # After: React Frontend
├── docker-compose.yml
└── README.md
```
