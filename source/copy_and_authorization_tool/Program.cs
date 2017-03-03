using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using tool_commons.model;
using tool_commons.modules;

namespace copy_and_authorization_tool
{
    class Program
    {
        static void Main(string[] args)
        {
            // 起動引数からパラメータ毎に切り分けた連想配列を生成
            Dictionary<string, string> hash_array = utility_tools.argument_decomposition(args);

            try
            {
                Console.WriteLine("read setting from json : start");
                string json_file = "";
                if (hash_array.ContainsKey(constant.RESOURCES_KEY_EXTERNAL))
                    json_file = hash_array[constant.RESOURCES_KEY_EXTERNAL];
                else
                    json_file = constant.RESOURCES_DIR + constant.EXTERNAL_RESOURCE_FILENAME;
                json_module.setup(json_file);

                // 各外部ファイルのディレクトリ先を設定
                string log_dir = get_value_from_json("log_file_dir", constant.LOG_FILE_DIR);
                string export_dir = get_value_from_json("export_dir", constant.EXPORT_DIR);
                string resources_dir = get_value_from_hasharray(hash_array, "RESOURCES_DIR", constant.RESOURCES_DIR);

                Console.WriteLine("read setting from json : start");
                setup_logs(hash_array, log_dir); // ログ、エラーファイルのセットアップ
                Console.WriteLine("read setting from json : end");

                // デシリアライズした結果を連想配列に格納
                Console.WriteLine("deserialize to Dictionary : start");
                var comparison_list = inport_serialize(get_value_from_json("inport_xml_filename"), resources_dir)?.transform_to_dictionary();
                Console.WriteLine("deserialize to Dictionary : end");

                // ロボコピー実行関数
                Console.WriteLine("run robocopy process : start");
                robocopy_process(src_dir:"", dst_dir:"");
                Console.WriteLine("run robocopy process : end");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            //Console.WriteLine("press any key to exit.");
            //Console.ReadKey();
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
        /// XMLデシリアライズ関数
        /// </summary>
        /// <param name="src_file">読み取りファイル</param>
        /// <param name="src_dir">読み取り先</param>
        /// <returns></returns>
        private static comparison_table inport_serialize(string src_file, string src_dir)
        {
            try
            {
                using (StreamReader read_stream = new StreamReader(src_dir + src_file, Encoding.GetEncoding(constant.EXTERNAL_RESOURCE_ENCODE)))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(comparison_table));
                    comparison_table dst_table = (comparison_table)serializer.Deserialize(read_stream);
                    return dst_table;
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return null;
            }
        }

        /// <summary>
        /// ロボコピー実行関数
        /// </summary>
        /// <param name="src_dir">コピー元ディレクト</param>
        /// <param name="dst_dir">コピー先ディレクトリ</param>
        /// <param name="copy_filter">コピーフィルター</param>
        private static void robocopy_process(string src_dir, string dst_dir, string copy_filter="")
        {
            try
            {
                if (src_dir.Equals("") || dst_dir.Equals("")) return;
                if (copy_filter.Equals("")) copy_filter = "*.*";

                using (Process p = new Process())
                {
                    p.StartInfo.Arguments = string.Format("/C ROBOCOPY \"{0}\" \"{1}\" \"{2}\"" + get_value_from_json("robocopy_option"), src_dir, dst_dir, copy_filter);
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    loger_module.write_log(p.StartInfo.Arguments);
                    loger_module.write_log(p.StandardOutput.ReadToEnd());
                    loger_module.write_log(p.StandardError.ReadToEnd(), "error", "info");
                    p.WaitForExit();
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
            }
        }

    }
}
