using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication1.Model.SupabaseModel;

namespace WebApplication1.Helper
{
    /// <summary>
    /// Supabase数据库集成：https://supabase.com/docs/reference/csharp/introduction
    /// </summary>
    public class SupabaseHelper
    {
        private const string SUPABASE_URL = "https://tehrdejtgtckkmsbjgdp.supabase.co";
        private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InRlaHJkZWp0Z3Rja2ttc2JqZ2RwIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTA2NTYwMDIsImV4cCI6MjA2NjIzMjAwMn0.U9QiqAtEGzEpkSZd3FVLXKH7usyYVc7UNlEo1hrF_HI";
        private Supabase.Client client;

        public SupabaseHelper()
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            this.client = new Supabase.Client(SUPABASE_URL, SUPABASE_KEY, options);
            this.client.InitializeAsync().Wait();
        }

        /// <summary>
        /// 根据ID查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> SelectByIdAsync<T>(Guid id) where T : SupabaseModelBase, new()
        {
            var result = await this.client.From<T>().Where(x => x.Id == id).Get();
            return result.Model;
        }

        /// <summary>
        /// 根据Url查询
        /// </summary>
        /// <returns></returns>
        public async Task<SupabaseProducts> SelectByUrlAsync(string url)
        {
            var result = await this.client.From<SupabaseProducts>().Where(x => x.Link == url).Get();
            return result.Model;
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<List<T>> PageAsync<T>(int page, int pageSize) where T : SupabaseModelBase, new()
        {
            var result = await this.client.From<T>().Range((page - 1) * pageSize, page * pageSize).Get();
            return result.Models;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<bool> UpdateAsync<T>(T t) where T : SupabaseModelBase, new()
        {
            var update = await this.client
              .From<T>()
              .Where(x => x.Id == t.Id)
              .Update(t);
            return update.ResponseMessage.IsSuccessStatusCode;
        }

        /// <summary>
        /// 插入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<bool> InsertAsync<T>(T t) where T : SupabaseModelBase, new()
        {
            var insert = await this.client.From<T>().Insert(t);
            return insert.ResponseMessage.IsSuccessStatusCode;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<bool> DeleteAsync<T>(Guid id) where T : SupabaseModelBase, new()
        {
            await this.client
              .From<T>()
              .Where(x => x.Id == id)
              .Delete();
            return true;
        }

        /// <summary>
        /// 根据时间
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteByTimeAsync(DateTime beginTime)
        {
            await this.client.From<SupabaseProducts>().Where(x => x.CreateTime > beginTime).Delete();
            return true;
        }
    }
}
