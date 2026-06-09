\# AGENTS.md



\## 基本方針



\- 回答は日本語で行う。

\- いきなりファイルを変更しない。

\- 変更前に、どのファイルをどう変えるか説明する。

\- Unity Editorでの確認が必要な場合は、その旨を明記する。

\- 既存の挙動を変える場合は、何が変わるか明記する。



\## Unityでの注意



\- 基本的に編集対象は Assets/Scripts 以下のC#スクリプトとする。

\- Scene, Prefab, ProjectSettings, Packages は、明示的に依頼された場合以外は変更しない。

\- SerializeField の変数名を安易に変更しない。

\- public フィールドやInspector参照に影響する変更は事前に警告する。

\- Awake, Start, Update, OnEnable, OnDisable の処理順に注意する。

\- Library, Temp, Obj, Logs, Build, UserSettings は無視する。



\## 作業後の報告



\- 変更したファイル

\- 変更内容

\- 想定される挙動

\- Unity Editorで確認すべき点. また、Script編集後、UnityEditor上でGUIにおいて変更する内容について、可能な限り記述してください。

を簡潔にまとめる。





