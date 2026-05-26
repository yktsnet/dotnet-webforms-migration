# アーキテクチャ概要

## Before: WebForms 密結合

```mermaid
graph TD
    Browser["ブラウザ"]

    subgraph ASPX["❌ Attendance.aspx.cs（Page_Load がすべてを担当）"]
        PL["Page_Load\n初期表示・集計・権限チェックが混在"]
        EVT["ボタンイベント\nbtnClockIn_Click / btnClockOut_Click"]
        SQL["SQL 文字列結合\nインジェクションリスク"]
        RW["Response.Write\nCSV 直接ストリーム出力"]
    end

    VS["__VIEWSTATE\n隠しフィールド肥大化\n（打刻履歴・集計データを保持）"]
    DB[("SQL Server")]

    Browser -->|"フォーム送信 (PostBack)\nページ全体リロード"| ASPX
    ASPX -->|"毎リクエスト集計クエリ実行"| DB
    Browser <-->|"リクエストごとに往復"| VS
```

---

## After: レイヤー分離

```mermaid
graph LR
    React["React / TypeScript\n(UI 層)"]
    API["ASP.NET Core\nMinimal API\n(API 層)"]
    SVC["AttendanceService\n(Service 層)"]
    DAP["Dapper\n(Repository 層)"]
    DB[("PostgreSQL")]

    React -->|"HTTP / JSON\n非同期・ページリロードなし"| API
    API --> SVC
    SVC --> DAP
    DAP --> DB
```

---

## コンポーネント責務

| コンポーネント | 責務 |
|---|---|
| React (src/Web) | 状態管理・表示のみ。PostBack・ViewState を持たない |
| Minimal API (Program.cs) | ルーティング・リクエスト受付 |
| AttendanceService | 月次集計・打刻バリデーション・トランザクション管理 |
| Dapper | パラメータ化クエリによる安全な DB アクセス |
| Docker Compose | 環境依存の排除。ローカル〜本番同一構成 |

---

## Before / After の構造対比

| WebForms の問題 | 対応する After の設計 |
|---|---|
| AutoPostBack → 部署選択のたびに全画面リロード | React の state 更新 → リロードなし |
| ViewState → リクエストに巨大な隠しフィールドが混入 | サーバー側状態管理を廃止。必要時のみ API フェッチ |
| Page_Load 集中 → 初期表示・集計・権限チェックが混在 | AttendanceService へ責務分離。xUnit でテスト可能 |
| Response.Write → 文字化け・エラー制御不能 | `Content-Disposition` エンドポイント。UTF-8 保証 |

---

## インフラ構成

```mermaid
graph LR
    User["ブラウザ（面談時）"]
    Pages["Cloudflare Pages\n（Before デモ・常時稼働）"]
    Tunnel["Cloudflare Tunnel\n（After デモ・オンデマンド）"]
    VPS["VPS (het)\nDocker Compose\napi + web + postgres"]

    User -->|"HTTPS (固定 URL)"| Pages
    User -->|"HTTPS (起動時のみ)"| Tunnel
    Tunnel --> VPS
```

### デモ運用

| | Before | After |
|---|---|---|
| ホスティング | Cloudflare Pages | VPS + Cloudflare Tunnel |
| 起動 | 常時稼働 | 面談前に `systemctl start` |
| URL | 固定（`*.pages.dev`） | 起動のたびに `journalctl` で確認 |
