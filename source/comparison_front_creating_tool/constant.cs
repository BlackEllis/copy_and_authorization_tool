﻿namespace comparison_front_creating_tool
{
    class constant
    {
        // 引数でのキーワード
        public const string RESOURCES_KEY_EXTERNAL = "EXTERNAL_FILE";               // 外部設定ファイル参照フォルダ設定オプション
        public const string RESOURCES_KEY_LOG = "LOG_FILE";                         // ログ出力先設定オプション
        public const string RESOURCES_KEY_EXTRACTINGLOG = "EXTRACTING_FILE";        // 抽出ログファイル出力先設定オプション

        // デフォルトの外部設定ファイルの情報
        public const string EXTERNAL_RESOURCE_ENCODE = "utf-8";                     // 文字コード
        public const string EXTERNAL_RESOURCE_FILENAME = "external_setting.json";   // 設定ファイル名

        // log ファイル関係定数
        public const string LOG_FILE_ENCODE = "utf-8";                              // 出力ログファイルのデフォルト文字コード
        public const string DEFAULT_LOG_FILENAME = "system.log";                    // 出力ログファイルのデフォルト名

        // 抽出ログ ファイル関係定数
        public const string EXTRACTING_FILE_ENCODE = "utf-8";                            // 出力エラーログファイルのデフォルト文字コード
        public const string DEFAULT_EXTRACTING_FILENAME = "extracting.log";                  // 出力エラーログファイルのデフォルト名

        // 固定ディレクトリ設定
        public const string RESOURCES_DIR = "./resources/";
        public const string LOG_FILE_DIR = "./log/";
        public const string EXPORT_DIR = "./export/";

        public const string EXPORT_XML_FILENAME = "comparison_list.xml";

    }
}
