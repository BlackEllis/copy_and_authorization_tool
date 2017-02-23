namespace file_permission_conversion_json_creation
{
    class constant
    {
        // 引数でのキーワード
        public const string RESOURCE_KEY_EXTERNAL = "EXTERNAL_FILE";
        public const string RESOURCES_KEY_LOG = "LOG_FILE";
        public const string RESOURCES_KEY_ERRORLOG = "ERROR_FILE";

        // デフォルトの外部設定ファイルの情報
        public const string EXTERNAL_RESOURCE_ENCODE = "utf-8";
        public const string EXTERNAL_RESOURCE_FILENAME = "external_setting.json";

        // log ファイル関係定数
        public const string LOG_FILE_ENCODE = "utf-8";
        public const string DEFAULT_LOG_FILENAME = "system.log";

        // error ファイル関係定数
        public const string ERROR_FILE_ENCODE = "utf-8";
        public const string DEFAULT_ERROR_FILENAME = "errors.log";

        // 固定ディレクトリ設定
        public const string RESOURCES_DIR = "./resources/";
        public const string LOG_FILE_DIR = "./log/";
        public const string EXPORT_DIR = "./export/";
    }
}
