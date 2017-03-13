using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace copy_and_authorization_tool
{
    class constant
    {
        // 引数でのキーワード
        public const string RESOURCES_KEY_EXTERNAL = "EXTERNAL_FILE";           // 外部設定ファイル参照フォルダ設定オプション
        public const string RESOURCES_KEY_LOG = "LOG_FILE";                     // システムログ出力先設定オプション
        public const string RESOURCES_KEY_EXTRACTINGLOG = "EXTRACTING_FILE";    // 抽出ログ出力先設定オプション
        public const string RESOURCES_KEY_DIFF_MODE = "DIFF_MODE";              // robocopyをテストモードで動作させるオプション

        // デフォルトの外部設定ファイルの情報
        public const string EXTERNAL_RESOURCE_ENCODE = "utf-8";                     // 文字コード
        public const string EXTERNAL_RESOURCE_FILENAME = "external_setting.json";   // 設定ファイル名

        // log ファイル関係定数
        public const string LOG_FILE_ENCODE = "utf-8";                      // 出力ログファイルのデフォルト文字コード
        public const string DEFAULT_LOG_FILENAME = "system.log";            // 出力ログファイルのデフォルト名

        // extracting ファイル関係定数
        public const string EXTRACTING_FILE_ENCODE = "utf-8";                // 出力抽出ログファイルのデフォルト文字コード
        public const string DEFAULT_EXTRACTING_FILENAME = "errors.log";      // 出力抽出ログファイルのデフォルト名

        // 固定ディレクトリ設定
        public const string RESOURCES_DIR = "./resources/";
        public const string LOG_FILE_DIR = "./log/";
        public const string EXPORT_DIR = "./export/";
    }
}
