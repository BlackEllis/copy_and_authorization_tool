# copy_and_authorization_tool

## copy_and_authorization_tool
### 概要
robocopyとコピーしたアクセス権の移植プログラム

1. 外部リソース(Excel)で指定されたコピー元、コピー先情報の取得

2. 同外部リソース(Excel)から対象外フォルダ名の取得

3. robocopyを実行

4. コピーしたディレクトリ、ファイルに元ファイルについていた権限の移植

    > 変換比較用のファイルは _comparison_front_creating_tool_ で行う

## comparison_front_creating_tool
### 概要
異なるAD同士のS-ID対比Xml作成プログラム

1. ADからグループとそのグループに紐づくアカウントを取得

2. グループS-ID対比表（Excel）と突き合わせた結果をシリアライズクラスに格納

3. 別ADから取得したユーザー情報を格納しているDBと突き合わせた結果をシリアライズクラスに格納

4. シリアライズクラスをXMLへ出力

## verification_tool
### 概要
アクセス権の移行が正常に行われているか差分チェックツール

1. 外部リソース(Excel)で指定されたコピー元、コピー先情報の取得
    __※ copy_and_authorization_toolのフォーマットと同じ__

2. 同外部リソース(Excel)から対象外フォルダ名の取得

3. コピー元とコピー先のディレクトリ、ファイルにファイルの権限比較

    > 変換比較用のファイルは _comparison_front_creating_tool_ で行う



## 実行オプション
+ __EXTERNAL_FILE__

    外部設定ファイル参照フォルダ設定オプション

+ __LOG_FILE__

    ログ出力先設定オプション

+ __LOG_LEVEL__

    ログの出力レベル設定

+ __EXTRACTING_FILE__

    抽出ログファイル出力先設定オプション

+ __LOG_LEVEL__

    抽出ログの出力レベル設定


### copy_and_authorization_toolのみ
+ __DIFF_MODE__

    robocopyをテストモードで使用するか用パラメータ
    > True：使用 False：使用しない

### 使用例
```cmd
copy_and_authorization_tool.exe "EXTERNAL_FILE=./resources/external_setting.json" "LOG_FILE=./log/system_log.log"
```


## 使用ライブラリ
+ MySql.Data

+ System.DirectoryServices

+ System.Runtime.Serialization

+ ClosedXML

## ヒルドに必要な物
+ ILMerge [\[Link\]](https://www.microsoft.com/en-us/download/details.aspx?id=17630)

    DLLをExeに埋め込むのに使用

### 注意事項
+ グループのS-IDは対象に含めれないかも。。。 (対比表が必要な為)
