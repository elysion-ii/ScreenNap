# テスト容易性向上リファクタリング + テスト整備プラン

作成日: 2026-07-03 / 対象コミット: aad6077 時点 + 構造変更(build/ 小文字化、ScreenNap.Tests 新設)適用後

## 背景と目的

現状、テストは `SanityTest.cs`(スキャフォールド)のみで、実質的なテストが存在しない。
原因は、判断ロジック(何を黒くするか・メニューに何を出すか・名前をどう解決するか)が
Win32 P/Invoke 呼び出しと同じメソッド内に混在しており、ウィンドウを実際に作らないと
検証できない構造になっていること。

本プランは **Functional Core / Imperative Shell** への再編で判断ロジックを純粋関数として
切り出し、xUnit のテーブル駆動テストで仕様を固定することを目的とする。

**アプリの外部挙動は一切変えない**(リファクタリングのみ)。

## 現状分析: テストを阻害している構造

| # | 箇所 | 問題 |
|---|------|------|
| 1 | `Blackout/BlackoutWindow.cs` | `using ScreenNap.App;`(`MonitorIdentity` 参照)— **CLAUDE.md の依存方向ルール違反**(Blackout/ → App/ 禁止)。共有型の置き場がない |
| 2 | `App/BlackoutManager.cs` | `new BlackoutWindow(...)` を直接生成、`User32.IsWindow` を直接呼ぶ。Toggle / Reconcile / ReleaseAll の状態遷移が実ウィンドウなしに検証不能 |
| 3 | `App/MonitorEnumerator.cs` | 名前解決チェーン(QDC → EnumDisplayDevices → プレフィックス除去)が private かつ P/Invoke フォールバックと結合 |
| 4 | `App/ContextMenu.cs` | メニュー項目の構成判断(ラベル・チェック・セパレーター・ID 採番)が `AppendMenuW` 呼び出しと混在。`HandleCommand` のコマンド解釈も同様 |
| 5 | `App/HotkeyManager.cs` | hotkeyId → アクション(識別オーバーレイ or モニター toggle)のマッピングが P/Invoke と混在 |
| 6 | `Blackout/BlackoutWindow.cs` WndProc | カーソル自動非表示の判定(座標差分・アイドルタイムアウト)が `Environment.TickCount64` を直接参照し WndProc に埋没 |
| 7 | `App/IconHelper.cs` | .ico バイナリ解析(オフセット・サイズ検証)は純粋ロジックだが private で P/Invoke と結合 |
| 8 | `App/TrayIcon.cs` | activeCount → アイコン種別・ツールチップ文字列の選択、および 127 文字切り詰めがロジックとして取り出せない |
| 9 | `Logging/Logger.cs` | `DateTime.Now` 直接参照(行フォーマット・ログ削除判定)。時刻はグローバルルール上「引数で渡す入力」 |
| 10 | 全 internal 型 | テストプロジェクトから見えない(`InternalsVisibleTo` 未設定) |

## 方針

- **Functional Core**: 判断ロジックを引数 → 戻り値の純粋関数/純粋状態オブジェクトに抽出。時刻(tick、DateTime)は必ず引数で受け取る。
- **Imperative Shell**: P/Invoke 呼び出しは薄いシェルに残す。シェル側は「取得 → 判断(純粋) → 適用」の並びにする。
- 新フォルダ **`ScreenNap/Core/`** を設け、共有型と純粋ロジックを置く。依存方向は以下に更新:

```
Program.cs → App/ → Core/ → Native/
                  → Blackout/ → Core/
                              → Native/
                  → Native/
```

- `Native/` は宣言のみ(現状維持)。`Core/` は P/Invoke **呼び出し禁止**(Native の struct/定数の型参照のみ可)。
- テストは xUnit テーブル駆動(`[Theory]` + `[InlineData]`/`[MemberData]`)。行順は null → 空 → 最小 → 主要 → 境界(`.claude/rules/testing.md` 準拠)。
- 文化依存(リソース文字列)を検証するテストは `CurrentUICulture` をテスト内で固定する。
- **実ウィンドウ・実タイマー・実ファイルシステム(%LOCALAPPDATA%)に触るテストは書かない。**

## フェーズ構成

### Phase 0: テスト基盤(最初に実施)

1. `ScreenNap/ScreenNap.csproj` に追加:
   ```xml
   <ItemGroup>
     <InternalsVisibleTo Include="ScreenNap.Tests" />
   </ItemGroup>
   ```
2. `ScreenNap.Tests/` にフォルダ構成を作る: `Core/`, `App/`, `Blackout/`, `Logging/`, `TestDoubles/`
3. `SanityTest.cs` は Phase 1 以降で実テストが入った時点で削除

受け入れ基準: `dotnet test` が通り、テストコードから internal 型が参照できる。

### Phase 1: 構造移動(挙動変更なし・依存方向違反の解消)

1. `ScreenNap/Core/` を新設し、以下を **移動**(namespace は `ScreenNap.Core` に変更):
   - `App/MonitorIdentity.cs` → `Core/MonitorIdentity.cs`
   - `App/MonitorInfo.cs` → `Core/MonitorInfo.cs`(`RECT` 参照のため Core → Native 依存は許容)
2. `BlackoutWindow.cs` の `using ScreenNap.App;` を `using ScreenNap.Core;` に変更 → **違反解消**
3. 各参照元の using 更新
4. `.claude/CLAUDE.md` の Directory Layout と依存方向図を更新(上記の図に差し替え)

受け入れ基準: ビルド成功、`Blackout/` 内に `ScreenNap.App` への参照が 0 件(`grep` で確認)。

### Phase 2: 抽象化シーム — BlackoutManager を完全テスト可能に(本丸)

**変更**

1. `Core/IBlackoutWindow.cs` を新設:
   ```csharp
   internal interface IBlackoutWindow
   {
       string DevicePath { get; }
       MonitorIdentity Identity { get; }
       bool UserDismissed { get; }
       bool IsAlive { get; }                       // 現行の User32.IsWindow(Handle) 相当
       Action<IBlackoutWindow>? OnDestroyed { get; set; }
       void Destroy();
   }
   ```
2. `Core/IBlackoutWindowFactory.cs` を新設: `IBlackoutWindow? Create(MonitorInfo monitor);`
   (生成失敗 = 現行の `Handle == IntPtr.Zero` は **null 返却** に置き換える)
3. `BlackoutWindow` に `IBlackoutWindow` を実装。`BlackoutWindowFactory`(Blackout/ 内)が
   生成と失敗判定を担う。
4. `BlackoutManager` をコンストラクタ注入に変更:
   `internal BlackoutManager(IBlackoutWindowFactory factory)`
   - `new BlackoutWindow(...)` → `_factory.Create(monitor)`(null なら中断)
   - `User32.IsWindow(kvp.Value.Handle)` → `kvp.Value.IsAlive`
   - これで `BlackoutManager` から P/Invoke 参照が消える → **`App/` から `Core/` へ移動**
5. `Program.cs` で `new BlackoutManager(new BlackoutWindowFactory())` を配線

**テスト**(`TestDoubles/FakeBlackoutWindow.cs` / `FakeBlackoutWindowFactory.cs` を作成。
Fake の `Destroy()` は同期的に `OnDestroyed` を呼ぶ。`IsAlive` は外から書き換え可能にする)

| テスト対象 | ケース |
|-----------|--------|
| `Toggle` | OFF→ON(生成・カウント増・イベント発火)/ ON→OFF(Destroy 経由で除去)/ 生成失敗(factory が null)時に状態不変 / Identity が default のモニターは desired に積まれない |
| `ReleaseAll` | 0 件 / 複数件 / desired がクリアされ再接続でも復元されない |
| `Reconcile` | desired 空なら何もしない / stale ウィンドウ(IsAlive=false)の掃除 / 切断→再接続で復元 / 既に生きているウィンドウは再生成しない / 再接続したが desired にないモニターは無視 / 復元時の生成失敗をスキップ / イベント発火は変化があった時のみ |
| ユーザー閉鎖 | `UserDismissed=true` で OnDestroyed → desired から除去(再接続で復元されない)/ `false`(OS 起因)なら desired 残留 |

受け入れ基準: 上記全ケース green。`Core/BlackoutManager.cs` に `ScreenNap.Native` の using がない。

### Phase 3: 純粋ロジック抽出(独立タスク群 — 並行実施可)

各項目は独立。1 項目 = 1 コミット目安。

#### 3a. モニター名解決チェーン(`Core/MonitorNameResolver.cs`)

`MonitorEnumerator.ResolveDisplayInfo` の判断部を静的純粋関数に:

```csharp
internal static (string FriendlyName, MonitorIdentity Identity) Resolve(
    string devicePath,
    MonitorDisplayInfo? qdcInfo,          // QueryDisplayConfig の結果(取得済みデータ)
    string? enumDisplayDeviceName)        // EnumDisplayDevices の結果(取得済みデータ)
```

シェル(`MonitorEnumerator`)は I/O で両データを **先に読み**、Resolve に渡す。
※ 現行実装は EnumDisplayDevices を遅延呼び出ししているが、モニター数は高々数個であり
先読みのコストは無視できる(Functional Core 原則を優先)。

テスト(テーブル駆動): qdcInfo あり / qdcInfo の名前が空白 → フォールバック /
EnumDisplayDevices 名あり(Identity は default になること)/ 両方なし + `\\.\DISPLAY1` →
`DISPLAY1` / 両方なし + プレフィックスなしパス → そのまま。

#### 3b. コンテキストメニュー構築 + コマンド解釈(`Core/MenuModel.cs`)

1. 項目モデル: `internal sealed record MenuItem(uint Flags 相当は持たない — bool Checked, bool IsSeparator, int CommandId, string? Text);`
2. 構築(純粋): `MenuModelBuilder.Build(IReadOnlyList<MonitorInfo> monitors, IReadOnlySet<string> activeDevicePaths)` → `IReadOnlyList<MenuItem>`
   - モニター項目(番号・ラベル・チェック)、activeCount>0 のとき separator + ReleaseAll、末尾に separator + Exit
3. 解釈(純粋): `MenuCommandInterpreter.Interpret(int commandId, int monitorCount)` →
   `MenuCommand`(`Kind: Exit / ReleaseAll / ToggleMonitor / None`, `MonitorIndex`)
4. `ContextMenu.Show` はモデルを Win32 メニューに写像するだけ、`HandleCommand` は Interpret の結果で分岐するだけにする

テスト: モニター 0/1/複数、アクティブ混在時のチェックフラグと ReleaseAll 出現、
Interpret は境界(BASE-1, BASE, BASE+count-1, BASE+count, EXIT, RELEASE_ALL)。

#### 3c. ホットキー解釈(`Core/HotkeyInterpreter.cs`)

`Interpret(int hotkeyId)` → `HotkeyAction`(`Identify / ToggleMonitor(index) / None`)。
`HotkeyManager.HandleHotkey` は解釈結果で分岐のみ。
テスト: IDENTIFY / BASE / BASE+8 / BASE+9(範囲外)/ BASE-1 / 無関係 ID。

#### 3d. カーソル自動非表示判定(`Core/CursorIdleTracker.cs`)

WndProc に埋まっている判定を純粋状態オブジェクトへ(時刻は引数):

```csharp
internal sealed class CursorIdleTracker
{
    internal CursorAction OnMouseMove(int x, int y, long tick);  // Show / None
    internal CursorAction OnTimerTick(long tick);                // Hide / None
}
```

- 座標が前回と同一(合成メッセージ)なら None
- 最終移動から `CURSOR_HIDE_TIMEOUT_MS` 経過かつ未非表示なら Hide(1 回だけ)
- 非表示中に移動したら Show

`BlackoutWindow` はインスタンスごとに保持し、WndProc は戻り値に応じて `SetCursor` するだけ。
テスト: 初回移動 / 同座標無視 / タイムアウト境界(直前・ちょうど・超過)/ Hide 後の再 Hide なし / Hide → 移動 → Show → 再タイムアウトで再 Hide。

#### 3e. .ico 解析(`Core/IcoParser.cs`)

`IconHelper` から解析部を分離: `TryGetFirstImage(byte[] icoData, out int offset, out int size)`。
`IconHelper` は読み込みと `CreateIconFromResourceEx` のみ。
テスト: 空配列 / 21 バイト(最小未満)/ offset+size が配列長超過 / 正常データ(手組みバイト列)。

#### 3f. トレイ状態選択(`Core/TrayState.cs`)

`TrayState.For(int activeCount)` → `(bool UseActiveIcon, string TipText)`(リソース文字列使用)。
`TruncateTip(string text)` → 127 文字切り詰めも純粋関数として公開。
テスト: 0 / 1 / 複数(`string.Format` 結果、`CurrentUICulture` を en と ja で固定して両方検証)、
TruncateTip は 126/127/128 文字の境界。

#### 3g. Logger の純粋部分抽出(`Logging/` 内、優先度: 低)

- `FormatLine(DateTime timestamp, string level, string message)` → string
- `SelectExpiredLogs(IReadOnlyList<(string Path, DateTime LastWrite)> files, DateTime now, int retentionDays)` → 削除対象一覧

シェル(`Write` / `PurgeOldLogs`)は現行のまま、判断だけ差し替え。
テスト: フォーマット(タイムスタンプ固定)/ 保持期間の境界(cutoff ちょうど・前後)。
※ タイムスタンプのフォーマットは `CultureInfo.InvariantCulture` を明示するよう修正する
(現行は暗黙の CurrentCulture 依存 — 潜在バグ。挙動固定のよい機会)。

### Phase 4: 既存純粋ロジックへのテスト追加(リファクタ不要)

- `MonitorInfo.BuildMenuLabel`: index 番号 / Primary 表示 / Active 表示 / 両方(culture 固定、en と ja)
- `MonitorIdentity`: default 比較、等値性(Reconcile のキーとして使われるため仕様として固定)

### Phase 5: ドキュメント整備

1. `.claude/CLAUDE.md`: `Core/` の説明追加、依存方向図の更新(Phase 1 と同時でも可)
2. `.claude/rules/screennap.md`: 「判断ロジックは Core/ に置き P/Invoke 禁止」を 1 行追記
3. `adr/arch-functional-core-shell.md` を新規作成:
   - 決定: UI 自動化テストではなく Functional Core 抽出でテストする
   - 代替案: (a) Win32 UI オートメーション(FlaUI 等)→ NuGet 禁止ルールと環境依存で却下、(b) 実ウィンドウを作る統合テスト → CI 環境のセッション制約で不安定なため却下
   - 適用条件: 新機能の判断ロジックは最初から Core/ に書く

## 実施順序と依存関係

```
Phase 0 ─→ Phase 1 ─→ Phase 2 ─→ Phase 3a〜3g(並行可)─→ Phase 4 ─→ Phase 5
```

- Phase 2 が最大(見積: 3a〜3g の合計と同程度)。Phase 3 の各項目は S サイズで独立
- 各フェーズ完了ごとに: `dotnet build ScreenNap.slnx -c Release` + `dotnet test` + `/audit` 実行
- コミット単位はフェーズ内タスク単位。コミットメッセージは英語(`refactor:` / `test:`)

## やらないこと(Non-goals)

- 挙動変更・機能追加(バグを見つけたら直さず報告のみ。例外: 3g のフォーマット culture 固定は本プランに含む)
- WndProc / メッセージループ / P/Invoke 宣言そのもののテスト
- 実ウィンドウ生成を伴う統合テスト、UI オートメーション
- DI コンテナ導入(コンストラクタ注入のみで足りる)
- WinForms/WPF 等 UI フレームワーク・NuGet パッケージの追加(プロジェクトルールで禁止)

## 完了条件

1. `dotnet test` で全テスト green(目安: 60〜80 ケース)
2. `build/Build.ps1` のテストゲート通過 → EXE 生成成功
3. 手動スモーク: トレイメニュー表示、ブラックアウト ON/OFF、ダブルクリック解除、Ctrl+Shift+Alt+1〜9/0、モニター切断→再接続の復元
4. `Blackout/` から `ScreenNap.App` への参照が 0 件、`Core/` から P/Invoke 呼び出しが 0 件
5. `/audit` でルール違反なし
6. 完了後、本ファイルを `archive/plans/` へ移動(git-commits ルール)
