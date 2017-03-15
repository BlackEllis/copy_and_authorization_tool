using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tool_commons.modules;

namespace verification_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数から各パラメータに切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);

            try
            {
                Console.WriteLine("read setting from json : start");
                json_module.setup(
                    utility_tools.get_value_from_hasharray(hash_array,
                                                            constant.RESOURCES_KEY_EXTERNAL,
                                                            constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME)
                );
                Console.WriteLine("read setting from json : end");

                // 各外部ファイルのディレクトリ先を設定
                string log_dir = json_module.get_external_resource("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = json_module.get_external_resource("export_dir", constant.EXPORT_DIR);
                string resources_dir = utility_tools.get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("log feature setup : start");
                setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ
                Console.WriteLine("log feature setup : end");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// ログ、エラーファイルのセットアップ関数
        /// </summary>
        /// <param name="args">起動引数を連想配列にしたもの</param>
        /// <param name="log_dir"></param>
        private static void setup_logs(Dictionary<string, string> args, string log_dir)
        {
            // ログファイルの関係設定
            string log_file = json_module.get_external_resource("default_log_filename", constant.DEFAULT_LOG_FILENAME);
            log_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_LOG, log_file);
            string log_encode = json_module.get_external_resource("log_file_encode", constant.LOG_FILE_ENCODE);

            // 抽出ログファイルの関係設定
            string extracting_file = json_module.get_external_resource("default_extracting_filename", constant.DEFAULT_EXTRACTING_FILENAME);
            extracting_file = utility_tools.get_value_from_hasharray(args, constant.RESOURCES_KEY_EXTRACTINGLOG, extracting_file);
            string extracting_encode = json_module.get_external_resource("extracting_file_encode", constant.EXTRACTING_FILE_ENCODE);

#if DEBUG
            loger_module.loger_setup(log_file, log_dir, log_encode, "info", true);
            loger_module.loger_setup(extracting_file, log_dir, extracting_encode, "extracting", true);
#else
            loger_module.loger_setup(log_file, log_dir, log_encode, "info");
            loger_module.loger_setup(extracting_file, log_dir, extracting_encode, "extracting");
#endif
        }
    }
}
