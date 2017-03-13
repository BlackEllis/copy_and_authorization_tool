using System.Collections.Generic;

namespace tool_commons.model
{
    class group_info : ad_unit_base
    {
        public List<user_info> group_members { get; private set; }
        public group_info(string src_name, string src_sid, user_info obj=null)
        {
            account_name = src_name;
            sid = src_sid;
            if (obj == null) group_members = new List<user_info>();
            else group_members = new List<user_info>() { obj };
        }

        public void add_user_info(user_info obj)
        {
            if (!group_members.Contains(obj))
                group_members.Add(obj);
        }
    }
}
