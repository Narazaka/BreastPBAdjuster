# Breast PB Adjuster

Breast PB Adjuster

胸のPBをあわせるツール（作りかけなので割とバグってる可能性あり）

## インストール

1. https://vpm.narazaka.net/ から「Add to VCC」ボタンを押してリポジトリをVCCにインストールします。
2. VCCでSettings→Packages→Installed Repositoriesの一覧中で「Narazaka VPM Listing」にチェックが付いていることを確認します。
3. アバタープロジェクトの「Manage Project」から「Breast PB Adjuster」をインストールします。

## 使い方

「Breast PB Adjuster」のprefabをアバターに入れてよしなにする。

PBとかはprefabにあるやつはてきとうなのでアバターからパラメーターうつしてくるの推奨

結合時にアバター側の元のPBを無効にするのでアバターが屈曲ボーンの場合は屈曲ボーン用処置が必要

## 更新履歴

- 0.5.0-beta.0
  - VRCConstraintで追従させるオプション（UseConstraint）を追加
  - 折り返し式ボーン対応（UseSecondBone）を追加
  - 複数配置に対応（全てがUseConstraint=trueである必要があります）
  - 非互換が起きうる変更: UseSecondBone対応のためにIgnore Transform指定が増えています。2番目以降に指定がある場合は修正が必要です。
- 0.4.0でparentがChestでない場合に対応（位置などが非互換です 0.3.x系でやってた人は直す必要があるかも知れません）
- 0.3.0でConstraintを使わない方式に変更（Quest対応のため）
- 0.2.0でボーンの回転互換性が切れてるので0.1.x系でやってた人は直して下さい

## License

[Zlib License](LICENSE.txt)
