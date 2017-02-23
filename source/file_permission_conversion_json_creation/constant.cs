namespace file_permission_conversion_json_creation
{
    class constant
    {
        // 引数でのキーワード
        public const string resource_key_external = "EXTERNAL_FILE";
        public const string resources_key_log = "LOG_FILE";
        public const string resources_key_errorlog = "ERROR_FILE";

        // デフォルトの外部設定ファイルの情報
        public const string external_resource_encode = "utf-8";
        public const string external_resource_filename = "external_setting.json";

        // log ファイル関係定数
        public const string log_file_encode = "utf-8";
        public const string default_log_filename = "system.log";

        // error ファイル関係定数
        public const string error_file_encode = "utf-8";
        public const string default_error_filename = "errors.log";

        // 固定ディレクトリ設定
        public const string resources_dir = "./resources/";
        public const string log_file_dir = "./log/";
        public const string export_dir = "./export/";
    }
}
