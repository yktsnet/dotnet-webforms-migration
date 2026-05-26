# 移行計画 — ASP.NET WebForms → .NET 8 Web API + React

## 移行の基本方針

「一括書き換え」ではなく、**動作を維持しながら段階的に責務を切り出す**アプローチを採用。
各フェーズは独立して検証可能な単位とする。

WinForms 移行との本質的な違い: WebForms は「動いている」がゆえに放置されやすい。
本移行は「壊れていない理由では移行しない根拠にならない」ことを設計で示す。

---

## フェーズ定義

### Phase 0 — 現状把握・課題整理

**目的**: WebForms 固有の問題点を特定し、移行スコープを確定する。

| 作業 | 詳細 |
|---|---|
| Before デモ作成 | 静的 HTML で AutoPostBack・PostBack 遅延・ViewState・文字化け CSV を再現 |
| 課題分類 | AutoPostBack による UX 劣化・ViewState 肥大化・Page_Load 集中・SQL インジェクションを文書化 |
| スコープ確定 | 認証・DB 冗長化は Out-of-Scope と明示 |

**成果物**: `legacy/AttendanceWebForms/`（Before デモ）、本 `migration-plan.md`

**ステータス**: ✅ 完了

---

### Phase 1 — ロジック抽出（Service 層の確立）

**目的**: Page_Load に混在した処理を独立したクラスへ切り出し、テスト可能にする。

| 作業 | 詳細 |
|---|---|
| 月次集計ロジックの分離 | Page_Load 内の集計処理 → `AttendanceService.GetMonthlySummary()` へ移管 |
| 打刻ロジックの分離 | ボタンイベント内の SQL → `AttendanceService.ClockAsync()` へ移管 |
| 単体テスト追加 | `GetMonthlySummary()` を対象に境界値テスト実装（DB 不要） |

**検証**: `dotnet test` がパス

---

### Phase 2 — API 化（HTTP インターフェースの確立）

**目的**: Service 層を外部から HTTP で呼び出せる Minimal API としてラップする。

| 作業 | 詳細 |
|---|---|
| エンドポイント定義 | 下表参照 |
| パラメータ化クエリ | 文字列結合 SQL → Dapper パラメータバインドに置換 |
| CSV エンドポイント | Response.Write 廃止 → `Content-Disposition` ヘッダーによる正規ダウンロード |
| Swagger 有効化 | `AddSwaggerGen()` + `UseSwaggerUI()` で API 仕様を常時公開 |

**実装エンドポイント**:

| Method | Path | 説明 |
|---|---|---|
| GET | `/employees` | 社員マスタ取得 |
| POST | `/attendances/clock-in` | 出勤打刻 |
| POST | `/attendances/clock-out` | 退勤打刻 |
| GET | `/attendances/{employeeId}/monthly` | 月次勤怠サマリー取得 |
| GET | `/attendances/{employeeId}/history` | 打刻履歴一覧取得 |
| GET | `/attendances/{employeeId}/monthly/csv` | 月次レポート CSV ダウンロード（UTF-8） |

**検証**: Swagger UI（`/api-docs`）で全エンドポイントの動作確認

---

### Phase 3 — フロントエンド化（React への置き換え）

**目的**: WebForms の画面を React/TypeScript で再実装し、PostBack を完全に排除する。

| 作業 | 詳細 |
|---|---|
| 打刻フォーム | 部署選択・社員選択 → ページリロードなしで非同期更新（AutoPostBack の廃止） |
| 打刻ボタン | `POST /attendances/clock-in` 等を非同期呼び出し。UIフリーズなし |
| 今月の集計 | `GET /attendances/{employeeId}/monthly` をフェッチ。PostBack 不要 |
| 打刻履歴タブ | `GET /attendances/{employeeId}/history` の結果を一覧表示 |
| CSV ダウンロード | `/monthly/csv` エンドポイントへのリンク。文字化けなし（UTF-8） |

**検証**: ブラウザから打刻・集計確認・CSV ダウンロードが完結。ページリロードが発生しないこと

---

### Phase 4 — インフラ整備（環境依存の排除）

**目的**: Docker 化により実行環境への依存を排除し、再現性のある構成を確立する。

| 作業 | 詳細 |
|---|---|
| Docker 化 | API / Web / PostgreSQL を `docker-compose.yml` で一元管理 |
| DB 初期化 | `infrastructure/db/init/01_schema.sql` で初回スキーマ投入 |
| デプロイスクリプト | `infrastructure/deploy.sh`（ビルド・転送・再起動） |
| Cloudflare Tunnel | VPS 上でオンデマンド起動。面談時のみ After URL を発行 |
| CI/CD | GitHub Actions で push ごとに build + test を自動実行 |

**検証**: `docker compose up` 1 コマンドで全環境が起動。CI が green

---

## 移行前後の対比

| 観点 | Before (WebForms) | After (.NET 8 + React) |
|---|---|---|
| 部署選択時の挙動 | AutoPostBack → ページ全体リロード | 非同期フェッチ → リロードなし |
| サーバー側状態管理 | ViewState（隠しフィールドが肥大化） | API ドリブン（必要時のみフェッチ） |
| ビジネスロジックの場所 | Page_Load に集中（テスト不能） | `AttendanceService` に集約（テスト可能） |
| CSV 出力 | Response.Write → 文字化け・エラー握りつぶし | 専用エンドポイント → UTF-8・エラーハンドリング有 |
| データアクセス | 文字列結合 SQL（インジェクションリスク） | Dapper パラメータ化クエリ |
| 実行環境 | IIS / Windows Server 依存 | Docker があればどこでも動作 |
