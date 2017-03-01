using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace comparison_front_creating_tool.modules
{
    sealed class loger_module
    {
        private static Dictionary<string, System.IO.StreamWriter> _stream_list;

        /// <summary>
        /// ログ出力オブジェクトの設定
        /// </summary>
        /// <param name="file_name"></param>
        /// <param name="file_encode"></param>
        /// <param name="log_category"></param>
        /// <param name="debug_flg"></param>
        public static void loger_setup(string file_name, string output_dir, string file_encode, string log_category="info", bool debug_flg=false)
        {
            System.IO.StreamWriter st_write_obj;
            try
            {
                if (_stream_list == null) _stream_list = new Dictionary<string, StreamWriter>();
                if (_stream_list.ContainsKey(log_category)) return; // 既に登録済みログ種別であれば後の処理は行わない
                string log_file_name = "";
                string log_dir = path_extracted(file_name); // ディレクト情報の抽出
                if (log_dir == "") // パス情報が無ければ、固定値にする
                {
                    log_dir = output_dir;
                    var join_filename = log_dir + file_name;
                    log_file_name = create_log_filename(join_filename, debug_flg);
                }
                else
                    log_file_name = create_log_filename(file_name, debug_flg);

                if (!Directory.Exists(log_dir)) Directory.CreateDirectory(log_dir); // ディレクトが無ければ作成

                st_write_obj = new StreamWriter(log_file_name, true, Encoding.GetEncoding(file_encode));
                _stream_list.Add(log_category, st_write_obj);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                st_write_obj = null;
            }
        }

        /// <summary>
        /// オブジェクトの後始末
        /// </summary>
        public static void close()
        {
            if (_stream_list == null) return;
            foreach (var stream_obj in _stream_list) {
                stream_obj.Value.Dispose();
                stream_obj.Value.Close();
            }
        }

        /// <summary>
        /// ログ出力
        /// </summary>
        /// <param name="str">出力文字列</param>
        /// <param name="log_category">ログ種別</param>
        /// <param name="replace_category">差し替えログ種別</param>
        public static void write_log(string str="", string log_category = "info", string replace_category="")
        {
            // ログ出力 ラムダ関数
            Func<System.IO.StreamWriter, string, bool> write_log = (StreamWriter st_write, string category) => {
                try
                {
                    if (str != "")
                    {
                        var write_str = (!category.Equals("")) ? category + ": " : "";
                        write_str += str;
                        st_write.Write(write_str);
                        st_write.Flush();
                    }
                    st_write.WriteLine();

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            };

            if ((_stream_list == null) || (_stream_list.Count == 0))
            {
                Console.WriteLine(str); // ログ出力オブジェクトがない場合はコンソールに表示
                return;
            }

            var write_category = replace_category.Equals("") ? log_category : replace_category;
            if (_stream_list.ContainsKey(write_category))
                write_log(_stream_list[write_category], write_category);  // 該当するファイルのストリームがある場合
            else
                write_log(_stream_list.First().Value, write_category); // 該当するファイルのストリームがない場合

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
            var dt_stmp = dt_obj.ToString("yyyy-MM-dd_") + dt_obj.ToString("HHmmss");
            var cat_point = file_name.LastIndexOf(".");
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

            var dst_filename = "";
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
            var st_path = path.Replace("\\", padding_font);
            var cat_point = st_path.LastIndexOf(padding_font);
            if (cat_point == -1) return "";

            return st_path.Substring(0, cat_point);
        }
    }
}
