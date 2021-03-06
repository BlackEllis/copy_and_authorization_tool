﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using ClosedXML.Excel;

namespace tool_commons.modules
{
    class excel_converter_module : character_conversion
    {
        /// <summary>
        /// csvファイル内容をDataTableに格納する
        /// </summary>
        /// <param name="file_name">読み取るファイル（ディレクトも込）</param>
        /// <param name="sheet_name">読み取るシート名</param>
        /// <param name="read_offset_row">読み出し開始行数</param>
        /// <returns></returns>
        public static DataTable read_excel_by_row(string file_name, string read_dir, string sheet_name, int read_offset_row=0)
        {
            DataTable dst_td = new DataTable();
            string read_file = path_extracted(file_name);
            if (read_file == "")
                read_file = read_dir + file_name;
            else
                read_file = file_name;
            List<string> column_names = new List<string>();

            try
            {
                using(FileStream stream = new FileStream(read_file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                using (XLWorkbook work_book = new XLWorkbook(stream, XLEventTracking.Disabled))
                {
                    // シートを開く
                    IXLWorksheet ws = work_book.Worksheet(sheet_name);

                    IXLTable table = ws.RangeUsed().AsTable();
                    int field_count = 0;
                    // フィールド名を取得
                    foreach (IXLTableField field in table.Fields)
                    {
                        if (field_count >= read_offset_row)
                        {
                            column_names.Add(field.Name);
                            dst_td.Columns.Add(field.Name, typeof(string));
                        }
                        ++field_count;
                    }

                    // データを行単位で取得
                    int loop_start = read_offset_row + 1; // 読み取りは1~
                    foreach (IXLTableRow dataRow in table.DataRange.Rows())
                    {
                        DataRow row = dst_td.NewRow();
                        int row_num = dataRow.CellCount()+1; // 読み取りが1~なので＋１する
                        for (int i = loop_start; i < row_num; ++i)
                        {
                            if (dataRow.Cell(i).ValueCached == null)
                                row[column_names[i-1]] = dataRow.Cell(i).Value; // 入力値
                            else
                                row[column_names[i-1]] = dataRow.Cell(i).ValueCached; // 計算結果

                        }
                        List<string> values_list = row.ItemArray.OfType<string>().ToList();
                        values_list.RemoveAll(item => item == ""); // 空要素の削除
                        if (values_list.Count > 1) dst_td.Rows.Add(row);
                    }
                }

                return dst_td;
            }
            catch (Exception e)
            {
                loger_manager.write_log(e.Message, "error");
                return null;
            }
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

    }
}
