using System;
using System.IO;
using System.Text;

namespace tool_commons.modules
{
    class loger_module
    {
        public enum E_LOG_LEVEL
        {
            E_ALL = 0xff,
            E_ERROR = 0x01,
            E_WARNING = 0x02,
            E_INFO = 0x04,
            E_USER_LOG = 0x08,
        };

        private E_LOG_LEVEL _output_level;
        private StreamWriter _stream;

        /// <summary>
        /// ログ出力オブジェクトの作成
        /// </summary>
        /// <param name="file_name">出力ファイル名</param>
        /// <param name="output_dir">出力先ディレクトリ</param>
        /// <param name="file_encode">出力エンコード</param>
        /// <param name="debug_flg"></param>
        public static loger_module create_loger(string file_name, string output_dir, string file_encode, E_LOG_LEVEL log_level, bool debug_flg = false)
        {
            try
            {
                string log_file_name = "";
                string log_dir = path_extracted(file_name); // ディレクト情報の抽出
                if (log_dir == "") // パス情報が無ければ、固定値にする
                {
                    log_dir = output_dir;
                    string join_filename = log_dir + file_name;
                    log_file_name = create_log_filename(join_filename, debug_flg);
                }
                else
                    log_file_name = create_log_filename(file_name, debug_flg);

                if (!Directory.Exists(log_dir)) Directory.CreateDirectory(log_dir); // ディレクトが無ければ作成
                if (File.Exists(log_file_name)) return null;

                return new loger_module(log_file_name, file_encode, log_level);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// 引数に渡されたファイル名に日時を付与してユニークな名前を生成
        /// </summary>
        /// <param name="file_name">出力ファイル名</param>
        /// <param name="debug_flg">デバッグフラグ（日時がつかなくなる）</param>
        /// <returns>生成されたファイル名称</returns>
        private static string create_log_filename(string file_name, bool debug_flg)
        {
            string name = "";
            string extension = "";

            DateTime dt_obj = DateTime.Now;
            string dt_stmp = dt_obj.ToString("yyyy-MM-dd_") + dt_obj.ToString("HHmmss");
            int cat_point = file_name.LastIndexOf(".");
            if (cat_point != -1)
            {
                name = file_name.Substring(0, cat_point);
                extension = file_name.Substring(cat_point);
            }
            else
            {
                name = file_name;
                extension = ".log";
            }

            string dst_filename = "";
            if (debug_flg == true)
                dst_filename = name + extension;
            else
                dst_filename = name + "_" + dt_stmp + extension;

            return dst_filename;

        }

        /// <summary>
        /// 最後に出現する区切り文字までを切り出す
        /// </summary>
        /// <param name="path">文字列</param>
        /// <param name="padding_font">区切り文字</param>
        /// <returns>最後に出現した具切り文字までの文字列</returns>
        private static string path_extracted(string path, string padding_font = "/")
        {
            string st_path = path.Replace("\\", padding_font);
            int cat_point = st_path.LastIndexOf(padding_font);
            if (cat_point == -1) return "";

            return st_path.Substring(0, cat_point);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="file_name">ログファイル名</param>
        /// <param name="file_encode">エンコード</param>
        /// <param name="log_level">出力ログレベル</param>
        loger_module(string file_name, string file_encode, E_LOG_LEVEL log_level)
        {
            _output_level = log_level;
            _stream = new StreamWriter(file_name, true, Encoding.GetEncoding(file_encode));
        }

        /// <summary>
        /// オブジェクトの後始末
        /// </summary>
        public void close()
        {
            if (_stream == null) return;

            _stream.Close();
            _stream = null;
        }

        /// <summary>
        /// ログ出力
        /// </summary>
        /// <param name="str">出力文字列</param>
        /// <param name="log_category">ログ種別</param>
        /// <param name="replace_category">差し替えログ種別</param>
        private void write_log(string str, string log_category)
        {
            if (str == "") return;
            if (_stream.Equals(null))
            {
                Console.WriteLine(str); // ログ出力オブジェクトがない場合はコンソールに表示
                return;
            }

            try
            {
                DateTime dt_obj = DateTime.Now;
                string write_str = dt_obj.ToString("HH:mm:ss.fff\t");
                write_str += (!log_category.Equals("")) ? $"[{log_category}]" + ":\t" : "";
                write_str += str;

                _stream.Write(write_str);
                _stream.Flush();
                _stream.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// エラーログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_error(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_ERROR) == 0) return;
            write_log(str, "ERROR");
        }

        /// <summary>
        /// 警告ログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_warning(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_WARNING) == 0) return;
            write_log(str, "WARNING");
        }

        /// <summary>
        /// インフォーメーションログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        public void write_log_info(string str)
        {
            if ((_output_level & E_LOG_LEVEL.E_INFO) == 0) return;
            write_log(str, "INFO");
        }

        /// <summary>
        /// ユーザー定義のログ出力関数
        /// </summary>
        /// <param name="str">出力文字列</param>
        /// <param name="category">出力カテゴリ</param>
        public void write_log_userlog(string str, string category)
        {
            if ((_output_level & E_LOG_LEVEL.E_USER_LOG) == 0) return;
            string str_category = category.ToUpper();
            write_log(str, str_category);
        }
    }
}
