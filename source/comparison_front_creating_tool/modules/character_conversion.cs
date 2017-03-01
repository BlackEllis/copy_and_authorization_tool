namespace comparison_front_creating_tool.modules
{
    class character_conversion
    {
        /// <summary>
        /// 文字コードの変更関数
        /// </summary>
        /// <param name="src_str"></param>
        /// <param name="src_encode"></param>
        /// <param name="dst_encode"></param>
        /// <returns></returns>
        protected static string encode_function(string src_str, string src_encode, string dst_encode)
        {
            if (src_encode.Equals(dst_encode)) return src_str;
            System.Text.Encoding src = System.Text.Encoding.GetEncoding(src_encode);
            System.Text.Encoding dest = System.Text.Encoding.GetEncoding(dst_encode);
            byte[] temp = src.GetBytes(src_str);
            byte[] utf8_temp = System.Text.Encoding.Convert(src, dest, temp);
            string dst_str = dest.GetString(utf8_temp);

            return dst_str;
        }

    }
}
