# TurnBattle

## 概要
『TurnBattle』は、多彩な魔法やスキルを駆使して戦うターン制のバトルゲームです。
最大の特徴は**「敵の魔法をコピーして記憶（ラーニング）するシステム」**です。プレイヤーは限られたメモリスロット（記憶枠）を管理しながら、敵が使用した魔法を自分のものとし、毎ターン3手先までの行動を戦略的に構築してバトルに挑みます。

## 主要システム

### 1. ターン制アクションキューシステム
- プレイヤーと敵は、毎ターン最大3つまでのアクション（魔法）を予約（キュー）します。
- 全てのアクションを決定後、双方が順番にスキルを発動する実行フェーズへと移行します。
- 各魔法には「MPコスト」や「発動できる順番（スロット）の制約」が設定されており、プレイヤーはリソースと制約の中で最適なコンボを構築する必要があります。

### 2. 魔法記憶（ラーニング）システム
- 戦闘中、プレイヤーターン時に「敵が直前のターンで使用した魔法」を自分のメモリーブック（記憶枠）にコピーすることができます。
- スロットに空きがある場合は自動で記憶されますが、スロットが満杯の場合は、既存の魔法のいずれかを忘れさせて（上書きして）新しい魔法を習得する取捨選択が求められます。
- これにより、敵の強力なスキルを奪いながら戦術をリアルタイムで拡張していくことが可能です。

### 3. 属性スタック・状態異常
- スキルには属性が設定されており（例: 火属性など）、攻撃を当てることで対象に「属性スタック」を蓄積させることができます。
- スタック数に応じて、ターン開始時に継続ダメージ（DoT: Damage over Time）などの追加効果が発生します。

### 4. リッチなバトル演出とUI
- **ダイナミックなカメラワーク**: プレイヤーのターンと敵のターンで視点が滑らかに切り替わるプロジェクションシフトを実装。
- **ヒット演出**: 攻撃ヒット時の画面揺れ（Screen Shake）、ヒットエフェクト、およびポップアップダメージテキストによる爽快感の向上。
- **UIシステム**: スプライトとマスクを活用した独自の縦型メニュー（`SpriteMaskedVerticalMenu`）や、魔法陣のアニメーション演出。

## 技術仕様・開発環境

### 環境・フレームワーク
- **ゲームエンジン**: Unity
- **プログラミング言語**: C#
- **レンダリングパイプライン**: Universal Render Pipeline (URP)
- **入力管理**: Unity Input System (New Input System)
- **テキスト・フォント**: TextMesh Pro
- **パーティクル・エフェクト**: Effekseer (URP連携対応)

### アーキテクチャと設計
- **データ駆動設計**:
  - `ScriptableObject` を活用し、魔法のデータ（`SpellData`）やバトルの調整パラメーター（`BattleTuning`）をコードから分離し、プランナーが調整しやすい設計。
- **UIアーキテクチャ**:
  - MVP (Model-View-Presenter) パターンや Binder パターンを採用し、ロジックとビューを分離（`QueuedSpellMagicCirclePresenter`, `BattleGaugeBinder` など）。
- **ランキング・セーブデータ**:
  - JSONベースのランキングマネージャー (`RankingManager`) を実装。スコアやプレイデータをローカルに永続化し、過去のランキングデータを保持・追記する仕組み。
- **アニメーション・演出管理**:
  - コルーチンを用いたシーケンシャルな演出管理（`DamageSequencePlayer` や `BattleManager` でのターン進行制御）。

## ディレクトリ構成（主要なアセット）
- `Assets/MyStuff/Scripts/`: ゲームのコアロジック、バトルシステム、UI制御などのC#スクリプト。
- `Assets/MyStuff/BattleData/` & `SpellData/`: ScriptableObjectによる各種マスターデータ。
- `Assets/Settings/`: URPやInput Systemなどのプロジェクト設定ファイル。
- `Assets/MyStuff/Prefab/`: 各種画面UIやエフェクトなどのプレハブ。

## スクリプトリファレンス (`Assets/MyStuff/Scripts/`)

プロジェクト内の主要なC#スクリプトは以下の通り、役割ごとに構成されています。

### ⚔️ バトルコアシステム (Battle Core System)
- `BattleManager.cs`: バトルの進行状況やターン管理、魔法アクションのキュー管理を行うゲームのメインシステム。
- `BattleUnit.cs`: プレイヤーおよび敵ユニットのステータス、MP、HPなどを管理するコンポーネント。
- `EnemyAI.cs`: 敵の行動（最大3つの魔法アクション）を決定するAIロジック。
- `DamageCalculator.cs`: 魔法や属性スタックに応じた最終的なダメージ計算ロジック。
- `SpellResolver.cs`: プレイヤーおよび敵が予約した魔法アクションの実行順序や効果解決を処理。
- `BattleEnums.cs`: バトルシステム全体で使用される列挙型（バトルのフェーズ状態など）を定義。
- `BattleTuning.cs`: バトルのダメージ倍率やパラメータ調整用のScriptableObject。
- `StartEnvironment.cs`: バトル開始時の環境設定および初期化。
- `UnitStats.cs`: ユニットの基礎ステータスを定義するデータ構造体。

### 📖 魔法・記憶システム (Magic & Memory System)
- `MagicManager.cs`: ゲーム内で登場する魔法データ全般を統括。
- `MagicMemoryBook.cs`: プレイヤーが敵からコピーした魔法（記憶）のリスト管理や、枠の上限・入れ替え処理を制御。
- `SpellData.cs`: 個々の魔法の効果、消費コスト、属性、演出などを定義するデータクラス（ScriptableObject）。
- `SpellAction.cs`: 実行される魔法のアクション内容（誰から誰へ、どの魔法か）を保持するクラス。
- `RememberedSpell.cs`: プレイヤーが記憶（ラーニング）した魔法の状態やレベルを保持するクラス。
- `ElementStackController.cs`: 火属性など、蓄積することで継続ダメージ等を発生させる「属性スタック」を管理。

### 🎨 バトル演出・アニメーション (Presentation & Animation)
- `DamageSequencePlayer.cs`: ダメージ発生時のヒットストップ、画面揺れ（Screen Shake）、エフェクト再生等の演出シーケンスを統括。
- `DamageNumberPopup.cs`: ダメージや回復を受けた際の数値ポップアップUIの表示制御。
- `DamageTargetAnchor.cs`: ダメージエフェクトやUIポップアップを発生させる基準点（アンカー）を定義。
- `ProjectionShifter.cs`: プレイヤーと敵それぞれのターン開始時に、カメラ視点をダイナミックに切り替える演出制御。
- `AnimatedMagicCircleView.cs`: アニメーションする魔法陣のビュー制御。
- `OneShotParticleCallback.cs`: 再生完了後に自動で消滅等の処理を行うパーティクル用のコールバック。
- `DamageSequenceWorld2DTest.cs`: 2Dワールド空間でのダメージシーケンスのテスト用スクリプト。

### 🖥️ UI・ビュー (UI & View)

#### バトルの状況・ゲージUI
- `BattleGaugeBinder.cs`: HPやMPなどのゲージをUI要素にバインドし、値の変動を同期。
- `BattleStatusWorldView.cs`: ワールド空間におけるユニットステータスの表示制御。
- `WorldBarGauge.cs` / `WorldButton.cs`: ワールド空間に配置されるゲージおよびボタンUI。
- `BattleUnitElementStacksView.cs`: 現在蓄積されている属性スタック数をアイコン等でUI表示。
- `SimpleBattleLogPrinter.cs`: 画面上やコンソールへの簡易的なバトルログ出力。

#### 魔法選択・スロットUI
- `BattleMenuInputGate.cs`: バトルメニューへの入力（操作）の有効・無効を制御。
- `QueuedSpellMagicCirclePresenter.cs`: 予約した魔法を魔法陣UI上に表示・管理するプレゼンター。
- `QueuedSpellSlotsSpriteBinder.cs`: 予約した魔法をアクションスロットに反映させるバインダー。
- `RememberedSpellMenuBinder.cs`: 記憶した魔法一覧メニューのUI構築およびバインダー。
- `EnemyUsedSpellSlotsSpriteBinder.cs`: 敵が直前に使用した魔法をスロットに表示する。
- `RememberedSpellSlotsSpriteBinder.cs`: プレイヤーの魔法記憶枠（メモリースロット）の表示。
- `RememberedSpellIconSlotView.cs` / `RememberedSpellLevelTextSlotView.cs`: 記憶した魔法のアイコンおよびレベルテキストの表示制御。
- `SpellTooltipPresenter.cs`: スロット上の魔法にフォーカスした際の詳細情報（ツールチップ）を表示。

#### 汎用UIシステム
- `SpriteMaskedVerticalMenu.cs`: スプライトマスクを活用した、スクロール可能なリッチな縦型メニュー。
- `SpriteMenuItem.cs` / `SpriteMenuStackController.cs`: 上記メニューの各アイテムおよび階層（スタック）管理。
- `SlotHoverPressFx.cs`: スロットやボタンをホバー・押下した際の拡大縮小などのフィードバックエフェクト。
- `TitleMenuActions.cs`: タイトル画面のメニューアクション（ゲーム開始など）の処理。
