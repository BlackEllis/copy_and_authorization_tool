using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace comparison_front_creating_tool.model
{
    class group_info : ad_unit_base
    {
        public List<user_info> group_members { get; private set; }
        public group_info(string name, string s_id, user_info obj=null)
        {
            account_name = name;
            sid = s_id;
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
