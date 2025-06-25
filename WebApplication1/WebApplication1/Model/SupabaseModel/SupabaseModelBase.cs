using Supabase.Postgrest.Models;
using System;

namespace WebApplication1.Model.SupabaseModel
{
    public class SupabaseModelBase: BaseModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        public Guid Id { get; set; }
    }
}
