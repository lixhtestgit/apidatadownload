using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PPPayReportTools.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.DB.Base;
using WebApplication1.DB.CMS;
using WebApplication1.DB.Repository;
using WebApplication1.Enum;
using WebApplication1.Extension;
using WebApplication1.Helper;

namespace WebApplication1.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CmsDataExportController : ControllerBase
	{
		//api/CmsDataExport/CopyDataToSite

		private BaseRepository _baseRepository;
		private TB_CountryRepository _tbCountryRepository;
		private TB_UsersRepository _usersRepository;
		private TJ_TB_OrderRepository _tjOrderRepository;
		private TB_OrderBillRepository _orderBillRepository;
		private TB_UserSendAddressRepository _userSendAddressRepository;
		private TB_UserSendAddressOrderRepository _userSendAddressOrderRepository;
		private ExcelHelper _excelHelper;
		private MeShopHelper _meShopHelper;
		private ILogger _logger;
		private object _obj = new object();

		public CmsDataExportController(
			BaseRepository baseRepository,
			TB_CountryRepository tbCountryRepository,
			TB_UsersRepository usersRepository,
			TJ_TB_OrderRepository orderRepository,
			TB_OrderBillRepository orderBillRepository,
			TB_UserSendAddressRepository userSendAddressRepository,
			TB_UserSendAddressOrderRepository userSendAddressOrderRepository,
			ExcelHelper excelHelper,
			MeShopHelper meShopHelper,
			ILogger<CmsDataExportController> logger)
		{
			this._baseRepository = baseRepository;
			this._tbCountryRepository = tbCountryRepository;
			this._usersRepository = usersRepository;
			this._tjOrderRepository = orderRepository;
			this._orderBillRepository = orderBillRepository;
			this._userSendAddressRepository = userSendAddressRepository;
			this._userSendAddressOrderRepository = userSendAddressOrderRepository;
			this._excelHelper = excelHelper;
			this._meShopHelper = meShopHelper;
			this._logger = logger;
		}

        /// <summary>
        /// 复制CMS站点数据到独立做账站点订单表中
        /// api/CmsDataExport/CopyDataToSite
        /// </summary>
        /// <returns></returns>
        [Route("CopyDataToSite")]
		[HttpGet]
		public async Task CopyDataToSite()
		{
			string beginDate = "2022-12-01";
			string endDate = "2023-01-01";

			#region 需求1：排除指定站点（0,1,11,1363,18,19,195,27,34,35,36,37,41,43,6689,6874,6876,6880,6881,6916,7162,7163,7003,7143）平均分配到对应10个站点中

			string tjOrderSql1 = @$"SELECT u.Email UserEmail,
                            (SELECT SUM(ob.BuyCount) FROM dbo.TB_OrderBill ob WHERE ob.SiteID=o.SiteID AND ob.OrderID=o.ID) ProductCount,
                            a.CountryID,
                            o.* FROM dbo.TB_Order o
                            LEFT JOIN dbo.TB_Site s ON s.ID=o.SiteID
                            LEFT JOIN dbo.TB_Users AS u ON o.UserID = U.ID AND o.SiteID = u.SiteID
                            LEFT JOIN dbo.TB_UserSendAddressOrder AS a WITH (NOLOCK) ON o.SiteID = a.SiteID AND a.OrderID = o.ID AND a.AddressID = o.Address1
                            WHERE o.AddTime>'{beginDate}' AND o.AddTime<'{endDate}' AND o.SiteID NOT IN (0,1,11,1363,18,19,195,27,34,35,36,37,41,43,6689,6874,6876,6880,6881,6916,7162,7163,7003,7143)
                            Order By o.AddTime";
			List<TJ_TB_Order> awaitOrderJObjList1 = await this._baseRepository.QueryAsync<TJ_TB_Order>(EDBSiteName.CMS, tjOrderSql1);

			int orderObjPosition1 = 0;
			int orderObjTotalCount1 = awaitOrderJObjList1.Count;

			//同步原始订单到新站点
			Dictionary<int, int> siteZB = new Dictionary<int, int>
			{
				{6546,23 },
				{6938,23 },
				{6903,17 },
				{6691,17 },
				{7027,6 },
				{7207,2 },
				{7211,2 },
				{6738,2 },
				{6983,2 },
				{7204,2 },
				{7203,2 },
				{7224,2 }
			};
			Dictionary<int, int> siteMaxOrderIDDic = new Dictionary<int, int>(12);
			foreach (var item in siteZB)
			{
				int maxOrderID = await this._tjOrderRepository.GetMaxID(item.Key);
				siteMaxOrderIDDic.Add(item.Key, maxOrderID);
			}

			Random random = new Random();

			Func<TJ_TB_Order, Task> syncOrderToNewSiteFunc = async (orderObj) =>
			{
				try
				{
					orderObjPosition1++;
					this._logger.LogInformation($"正在同步需求1第{orderObjPosition1}/{orderObjTotalCount1}个订单...");

					int fpSiteID = 0;
					int randomI = random.Next(1, 101);
					foreach (var item in siteZB)
					{
						randomI = randomI - item.Value;
						if (randomI <= 0)
						{
							fpSiteID = item.Key;
							break;
						}
					}

					//添加订单
					orderObj.OriginID = orderObj.ID;
					orderObj.OriginSiteID = orderObj.SiteID;
					orderObj.SiteID = fpSiteID;

					int insertResult = 0;
					int fpOrderID = 0;
					do
					{
						try
						{
							lock (this._obj)
							{
								fpOrderID = siteMaxOrderIDDic[fpSiteID] + 1;
								siteMaxOrderIDDic[fpSiteID] = fpOrderID;
							}
							orderObj.ID = fpOrderID;

							//检测原始数据是否已经插入统计表
							TJ_TB_Order tjTBOrder = await this._tjOrderRepository.First(EDBSiteName.CMS, m => m.OriginID == orderObj.OriginID && m.OriginSiteID == orderObj.OriginSiteID);
							if (tjTBOrder != null)
							{
								insertResult = 1;
							}
							else
							{
								insertResult = await this._tjOrderRepository.Insert(EDBSiteName.CMS, orderObj);
							}
						}
						catch (Exception)
						{
						}
					} while (insertResult == 0);
				}
				catch (Exception)
				{
					throw;
				}
			};

			int waitObjIndex1 = 0;
			bool allIsSync1 = true;
			List<Task> syncTaskList1 = new List<Task>(10);
			do
			{
				allIsSync1 = true;
				if (syncTaskList1.Count < 10 && waitObjIndex1 <= orderObjTotalCount1 - 1)
				{
					Task syncTask = syncOrderToNewSiteFunc(awaitOrderJObjList1[waitObjIndex1]);
					syncTaskList1.Add(syncTask);
					waitObjIndex1++;
				}
				//检测异步方法是否全部执行完毕
				allIsSync1 = syncTaskList1.Exists(m => m.IsCompleted == false) ? false : true;
				//移除已完成任务，安排下一对象同步
				syncTaskList1.RemoveAll(x => x.IsCompleted == true);
			} while (allIsSync1 == false || waitObjIndex1 < orderObjTotalCount1);

			#endregion

			#region 需求2：将www.beddinginn.com,www.oroyalcars.com(19,1363)将原始订单数据直接搬迁到独立表

			string tjOrderSql2 = @$"SELECT u.Email UserEmail,
                            (SELECT SUM(ob.BuyCount) FROM dbo.TB_OrderBill ob WHERE ob.SiteID=o.SiteID AND ob.OrderID=o.ID) ProductCount,
                            a.CountryID,
                            o.* FROM dbo.TB_Order o
                            LEFT JOIN dbo.TB_Site s ON s.ID=o.SiteID
                            LEFT JOIN dbo.TB_Users AS u ON o.UserID = U.ID AND o.SiteID = u.SiteID
                            LEFT JOIN dbo.TB_UserSendAddressOrder AS a WITH (NOLOCK) ON o.SiteID = a.SiteID AND a.OrderID = o.ID AND a.AddressID = o.Address1
                            WHERE o.AddTime>'{beginDate}' AND o.AddTime<'{endDate}' AND o.SiteID IN (19,1363)
                            Order By o.AddTime";
			List<TJ_TB_Order> awaitOrderJObjList2 = this._baseRepository.QueryAsync<TJ_TB_Order>(EDBSiteName.CMS, tjOrderSql2).Result;
			int orderObjTotalCount2 = awaitOrderJObjList2.Count;
			int orderObjPosition2 = 0;

			//同步原始订单到原站点
			Func<TJ_TB_Order, Task> syncOrderFunc = async (orderObj) =>
			{
				try
				{
					orderObjPosition2++;
					this._logger.LogInformation($"正在同步需求2第{orderObjPosition2}/{orderObjTotalCount2}个订单...");

					//添加订单
					orderObj.OriginID = orderObj.ID;
					orderObj.OriginSiteID = orderObj.SiteID;

					int insertResult = 0;
					do
					{
						try
						{
							//检测原始数据是否已经插入统计表
							TJ_TB_Order tjTBOrder = await this._tjOrderRepository.First(EDBSiteName.CMS, m => m.OriginID == orderObj.OriginID && m.OriginSiteID == orderObj.OriginSiteID);
							if (tjTBOrder != null)
							{
								insertResult = 1;
							}
							else
							{
								insertResult = await this._tjOrderRepository.Insert(EDBSiteName.CMS, orderObj);
							}
						}
						catch (Exception)
						{
						}
					} while (insertResult == 0);
				}
				catch (Exception)
				{
					throw;
				}
			};

			int waitObjIndex2 = 0;
			List<Task> syncTaskList2 = new List<Task>(10);
			bool allIsSync2 = true;
			do
			{
				allIsSync2 = true;
				if (syncTaskList2.Count < 10 && waitObjIndex2 <= orderObjTotalCount2 - 1)
				{
					Task syncTask = syncOrderFunc(awaitOrderJObjList2[waitObjIndex2]);
					syncTaskList2.Add(syncTask);
					waitObjIndex2++;
				}
				//检测异步方法是否全部执行完毕
				allIsSync2 = syncTaskList2.Exists(m => m.IsCompleted == false) ? false : true;
				//移除已完成任务，安排下一对象同步
				syncTaskList2.RemoveAll(x => x.IsCompleted == true);
			} while (allIsSync2 == false || waitObjIndex2 < orderObjTotalCount2);

            #endregion

            #region 需求3

            //执行SQL查询数据整理到Excel中：
            var huizongSQL = @"
            SELECT ID, Name,
            (SELECT SUM(Price_PreCount1) FROM dbo.TJ_TB_Order WHERE SiteID = TB_Site.ID AND AddTime>= '2022-09-01' AND AddTime<'2022-10-01')[9月美金汇总]
            FROM TB_Site where ID IN(6546, 6691, 6738, 6903, 6938, 6983, 7027, 7203, 7204, 7207, 7211, 7224)
            ORDER BY ID
            ";

            var xiangqingSQL = @"
            select o.SiteID[站点ID],
                (SELECT s.Name FROM dbo.TB_Site s WHERE s.ID=o.SiteID)[站点名称],
                ID[订单ID],AddTime[创建时间],o.CurrencyName[币种名称],
                CASE WHEN o.CurrencyPrice IS NULL OR o.CurrencyPrice<=0 THEN o.Price_PreCount1 ELSE o.CurrencyPrice END[多币种金额],
                o.Price_PreCount1[美金金额],
                o.OriginSiteID[原始站点ID],o.OriginID[原始订单ID]
            from dbo.TJ_TB_Order o 
            WHERE AddTime>='2022-09-01' and AddTime<'2022-10-01' 
            AND o.SiteID IN (6546,6691,6738,6903,6938,6983,7027,7203,7204,7207,7211,7224)
            ORDER by AddTime
            ";

            #endregion

            this._logger.LogInformation("订单处理完成！");
		}


		/// <summary>
		/// 赋值CMS数据到MeShop
		/// </summary>
		/// <returns></returns>
		[Route("CopyCmsDataToMeShop")]
		[HttpGet]
		public async Task CopyCmsDataToMeShop()
		{
			//api/CmsDataExport/CopyCmsDataToMeShop

			Dictionary<int, string> syncSiteDic = new Dictionary<int, string>
			{
				//{11,"ericdressfashion" },
				//{1,"tbdressshop" },
				{3,"wigsbuyshop" }
			};

			foreach (var syncSiteItem in syncSiteDic)
			{
				int siteID = syncSiteItem.Key;
				string meshopSiteHostAdmin = syncSiteItem.Value;

				List<TB_Users> tbUserList = await this._usersRepository.GetSyncUsers(siteID);

				int totalCount = tbUserList.Count;
				List<TB_Users> currentSyncUserList = null;
				int syncPageCount = 10000;
				int maxSyncTimes = totalCount / syncPageCount + (totalCount % syncPageCount > 0 ? 1 : 0);
				int currentSyncTime = 1;

				DateTime minRegisterDate = Convert.ToDateTime("2001-01-01");
				do
				{
					List<string> insertSqlList = new List<string>(syncPageCount);
					currentSyncUserList = tbUserList.Skip((currentSyncTime - 1) * syncPageCount).Take(syncPageCount).ToList();
					foreach (var item in currentSyncUserList)
					{
						if (item.Email.Contains("@") && item.Email.Contains("."))
						{
							item.Email = item.Email?.Replace("\r\n", "").Replace(" ", "").Replace("'", "''").Replace("\\","") ?? "";
							if (item.RegDate < minRegisterDate)
							{
								item.RegDate = minRegisterDate;
							}
							insertSqlList.Add($"({item.ID},'{item.Email}','{item.RegDate.AddHours(-8).ToString_yyyyMMddHHmmss()}',1,0,0)");
						}
					}
					string syncSQL = $"INSERT INTO user_info(ID,Email,CreateTime,State,RegisterType,PlatformType) VALUES {string.Join(',', insertSqlList)};";

					try
					{
						int syncCount = await this._meShopHelper.ExecSqlToShop(meshopSiteHostAdmin, syncSQL);
						if (syncCount <= 0)
						{
							this._logger.LogInformation($"同步站点异常:syncSQL={syncSQL}");
						}
					}
					catch (Exception e)
					{
						this._logger.LogError(e, $"同步站点异常:syncSQL={syncSQL}");
					}

					this._logger.LogInformation($"已同步站点{meshopSiteHostAdmin}—{currentSyncTime * syncPageCount}/{totalCount}个订单...");

					currentSyncTime++;
				} while (currentSyncTime <= maxSyncTimes);
			}
		}
	}
}
