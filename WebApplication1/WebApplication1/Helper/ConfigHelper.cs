﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using WebApplication1.Enum;
using WebApplication1.Model;

namespace WebApplication1.Helper
{
    /// <summary>
    /// 配置帮助类
    /// </summary>
    public class ConfigHelper
    {
        private IConfiguration _Configuration = null;
        private IMemoryCache _MemoryCache = null;
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="memoryCache"></param>
        public ConfigHelper(IConfiguration configuration, IMemoryCache memoryCache)
        {
            this._Configuration = configuration;
            this._MemoryCache = memoryCache;
        }

        private Dictionary<EDBSiteName, ConfigDBConnection> _DBConnectionDic = null;

        /// <summary>
        /// 获取所有数据库连接字符串
        /// </summary>
        /// <returns></returns>
        public Dictionary<EDBSiteName, ConfigDBConnection> GetDBConnectionDic()
        {
            if (this._DBConnectionDic == null || this._DBConnectionDic.Count == 0)
            {
                this._DBConnectionDic = this._MemoryCache.GetOrCreate("DBConnection", cacheEntry =>
                {
                    cacheEntry.SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1));

                    Dictionary<EDBSiteName, ConfigDBConnection> connectionDic = new Dictionary<EDBSiteName, ConfigDBConnection>(0);
                    IConfigurationSection dbSection = this._Configuration.GetSection("DBConnection");

					EDBSiteName siteName = default;
					EDBConnectionType connectionType = default;
                    foreach (IConfigurationSection item in dbSection.GetChildren())
                    {
						connectionType = item.GetValue<EDBConnectionType>("ProviderName");
						siteName = item.GetValue<EDBSiteName>("SiteName");

                        connectionDic.Add(siteName, new ConfigDBConnection
                        {
							DBConnectionType = connectionType,
                            SiteName = siteName,
                            ConnectionStr = item.GetValue<string>("ConnectionStr")
                        });
                    }
                    cacheEntry.SetValue(connectionDic);
                    return connectionDic;
                });
            }

            return this._DBConnectionDic ?? new Dictionary<EDBSiteName, ConfigDBConnection>(0);
        }

    }
}
