# copy_and_authorization_tool

## comparison_front_creating_tool
### 概要
異なるAD同士のS-ID対比Xml作成プログラム

1. ADからグループとそのグループに紐づくアカウントを取得
2. グループS-ID対比表（Excel）と突き合わせた結果をシリアライズクラスに格納
3. 別ADから取得したユーザー情報を格納しているDBと突き合わせた結果をシリアライズクラスに格納
4. シリアライズクラスをXMLへ出力

### 使用ライブラリ
+ MySql.Data
+ System.DirectoryServices
+ System.Runtime.Serialization
+ ClosedXML

### ヒルドに必要な物
+ ILMerge [\[Link\]](https://www.microsoft.com/en-us/download/details.aspx?id=17630)
    DLLをExeに埋め込むのに使用

### 注意事項
+ グループのS-IDは対象に含めれないかも。。。 (対比表が必要な為)
