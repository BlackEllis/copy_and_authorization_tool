using System;
using System.Collections.Generic;
using file_permission_conversion_comparison_front_creation.modules;
using file_permission_conversion_comparison_front_creation.model;
using System.Data;
using System.IO;
using System.Linq;

namespace file_permission_conversion_comparison_front_creation
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数からパラメータ毎に切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);

            // json設定ファイルの読み取り
            string json_file = "";
            if (hash_array.ContainsKey(constant.RESOURCE_KEY_EXTERNAL))
                json_file = hash_array[constant.RESOURCE_KEY_EXTERNAL];
            else
                json_file = constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME;
            json_module.setup(json_file);

            // 各外部ファイルのディレクトリ先を設定
            string log_dir = get_value_from_json("log_file_dir", constant.LOG_FILE_DIR);
            string export_dir = get_value_from_json("export_dir", constant.EXPORT_DIR);

            setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ

            loger_module.close();
            Console.WriteLine("press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// 引数の連想配列値か引数の基本値から値を取り出す
        /// </summary>
        /// <param name="src_array">取り出す連想配列</param>
        /// <param name="key">連想配列のキー情報</param>
        /// <param name="default_value">基本値</param>
        /// <returns></returns>
        private static string get_value_from_hasharray(Dictionary<string, string> src_array, string key, string default_value)
        {
            if (src_array == null) return default_value;
            if (src_array.ContainsKey(key))
                return src_array[key];
            else
                return default_value;
        }

        /// <summary>
        /// json_moduleから値を取り出す
        /// </summary>
        /// <param name="json_key">取出したいjsonキー情報</param>
        /// <param name="default_value">基本値</param>
        /// <returns></returns>
        private static string get_value_from_json(string json_key, string default_value = "")
        {
            string dst_str = json_module.get_external_resource(json_key);
            if (dst_str == "") dst_str = default_value;

            return dst_str;
        }

        /// <summary>
        /// ログ、エラーファイルのセットアップ関数
        /// </summary>
        /// <param name="args">起動引数を連想配列にしたもの</param>
        /// <param name="log_dir"></param>
        private static void setup_logs(Dictionary<string, string>args, string log_dir)
        {
            // ログファイルの関係設定
            string log_file = get_value_from_json("default_log_filename", constant.DEFAULT_LOG_FILENAME);
            log_file = get_value_from_hasharray(args, constant.RESOURCES_KEY_LOG, log_file);
            string log_encode = get_value_from_json("log_file_encode", constant.LOG_FILE_ENCODE);

            // エラーファイルの関係設定
            string error_file = get_value_from_json("default_error_filename", constant.DEFAULT_ERROR_FILENAME);
            error_file = get_value_from_hasharray(args, constant.RESOURCES_KEY_ERRORLOG, error_file);
            string error_encode = get_value_from_json("error_file_encode", constant.ERROR_FILE_ENCODE);

#if DEBUG
            loger_module.loger_setup(log_file, log_dir, log_encode, "info", true);
            loger_module.loger_setup(error_file, log_dir, error_encode, "error", true);
#else
            loger_module.loger_setup(log_file, log_dir, log_encode, "info");
            loger_module.loger_setup(error_file, log_dir, error_encode, "error");
#endif
        }

        /// <summary>
        /// 設定引数を元にADモジュールオブジェクト生成
        /// </summary>
        /// <returns>ADモジュールオブジェクト</returns>
        private static active_direcory_module get_ad_object()
        {
            Func<string, char, string[]> isolat_from_str = (string src_str, char delimiter) =>
            {
                string pad_str = src_str.Replace(" ", "");
                string[] dst_strs = src_str.Split(delimiter);
                return dst_strs;
            };

            // ADからグループ
            var ad_obj = new active_direcory_module();

            string ad_server_name = get_value_from_json("ad_server_name");
            string ad_access_userid = get_value_from_json("ad_access_userid");
            string ad_access_userpw = get_value_from_json("ad_access_userpw");
            string ad_common_names = get_value_from_json("ad_common_names");
            string ad_organizational_units = get_value_from_json("ad_organizational_units");

            ad_obj.get_group_list(ad_server_name, isolat_from_str(ad_common_names, ','), isolat_from_str(ad_organizational_units, '\''), ad_access_userid, ad_access_userpw);

            return ad_obj;
        }

        /// <summary>
        /// 設定変数からSQL接続用モジュールの生成
        /// </summary>
        /// <returns>SQL接続用モジュールオブジェクト</returns>
        private static mysql_module create_mysql_module()
        {
            string db_host = get_value_from_json("db_server_name");
            string db_name = get_value_from_json("db_name");
            string db_user = get_value_from_json("db_user_name");
            string db_pass = get_value_from_json("db_pass_word");
            mysql_module mysql_connecer = mysql_module.setup_sql(db_host, db_name, db_user, db_pass);

            return mysql_connecer;
        }

        /// <summary>
        /// Jsonファイルに記述されている例外対象を判定する為のListを生成
        /// </summary>
        /// <param name="delimiter">区切り文字</param>
        /// <returns>例外対象のリスト</returns>
        private static List<string> create_exception_targets(char delimiter)
        {
            string exception_targets = get_value_from_json("exception_targets", "");
            return exception_targets.Split(delimiter).ToList<string>();            
        }

    }
}
