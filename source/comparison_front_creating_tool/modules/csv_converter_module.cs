using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using comparison_front_creating_tool.modules;

namespace comparison_front_creating_tool
{
    class csv_converter_module : character_conversion
    {
        /// <summary>
        /// csvファイルの内容をListへ格納する
        /// </summary>
        /// <param name="file_name">入力ファイル名</param>
        /// <param name="in_encode">入力文字コード</param>
        /// <returns></returns>
        public static List<string[]> read_csv(string file_name, string in_encode)
        {
            List<string[]> dst_list = new List<string[]>();

            try
            {
                // csvファイルを開く
                using (var stream_read = new System.IO.StreamReader(file_name, Encoding.GetEncoding(in_encode)))
                {
                    // ストリームの末尾まで繰り返す
                    while (!stream_read.EndOfStream)
                    {
                        // ファイルから一行読み込む
                        var line = encode_function(stream_read.ReadLine(), in_encode, "utf-8");
                        // 読み込んだ一行をカンマ毎に分けて配列に格納する
                        var values = line.Split(',');

                        dst_list.Add(values);
                    }
                }
            }
            catch (Exception e)
            {
                // ファイルを開くのに失敗したとき
                loger_module.write_log(e.Message, "error", "info");
            }

            return dst_list;
        }

        /// <summary>
        /// データテーブルの内容をcsvファイルに出力
        /// </summary>
        /// <param name="src_dt">出力データ</param>
        /// <param name="file_name">出力ファイル名</param>
        /// <param name="write_encode">出力文字コード</param>
        /// <param name="write_header">ヘッダーを表示フラグ　true:表示 false:非表示</param>
        /// <param name="append">ファイルに追記フラグ</param>
        /// <returns></returns>
        public static bool write_csv(DataTable src_dt, string file_name, string write_encode, bool write_header, bool append=false)
        {
            try
            {
                //書き込むファイルを開く
                using (var sr = new System.IO.StreamWriter(file_name, append, Encoding.GetEncoding(write_encode)))
                {

                    int colCount = src_dt.Columns.Count;
                    int lastColIndex = colCount - 1;

                    //ヘッダを書き込む
                    if (write_header)
                    {
                        for (int i=0; i<colCount; ++i)
                        {
                            //ヘッダの取得
                            string header_field_str = encode_function(src_dt.Columns[i].Caption, "utf-8", write_encode);
                            //フィールドを書き込む
                            sr.Write(header_field_str);
                            //カンマを書き込む
                            if (lastColIndex > i)
                                sr.Write(',');
                        }

                        //改行する
                        sr.Write("\r\n");
                    }

                    //レコードを書き込む
                    foreach (DataRow row in src_dt.Rows)
                    {
                        for (int i=0; i<colCount; ++i)
                        {
                            string field_str = encode_function(row[i].ToString(), "utf-8", write_encode);
                            //フィールドを書き込む
                            sr.Write(field_str);
                            //カンマを書き込む
                            if (lastColIndex > i)
                                sr.Write(',');
                        }

                        //改行する
                        sr.Write("\r\n");
                    }
                }
            }
            catch (Exception e)
            {
                loger_module.write_log(e.Message, "error", "info");
                return false;
            }

            return true;
        }
    }
}
