# copy_and_authorization_tool

## copy_and_authorization_tool
### 概要
robocopyとコピーしたアクセス権の移植プログラム

1. 外部リソース(Excel)でしてされたコピー元、コピー先情報の取得

2. 同外部リソース(Excel)から対象外フォルダ名の取得

3. robocopyを実行

4. コピーしたディレクトリ、ファイルに元ファイルについていた権限の移植

    > この際にS-ID変換対象が含まれていた場合は変換して登録を行う

## comparison_front_creating_tool
### 概要
異なるAD同士のS-ID対比Xml作成プログラム

1. ADからグループとそのグループに紐づくアカウントを取得

2. グループS-ID対比表（Excel）と突き合わせた結果をシリアライズクラスに格納

3. 別ADから取得したユーザー情報を格納しているDBと突き合わせた結果をシリアライズクラスに格納

4. シリアライズクラスをXMLへ出力

## 実行オプション
+ __EXTERNAL_FILE__

    外部設定ファイル参照フォルダ設定オプション

+ __LOG_FILE__

    ログ出力先設定オプション

+ __ERROR_FILE__

    エラーファイル出力先設定オプション

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
