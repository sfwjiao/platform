using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Weike.Common;
using Weike.Common.Filters;
using Weike.Member;
using Weike.EShop;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.HSSF.Util;
using Weike.WebGlobalMethod;
using Weike;
using System.Data;
using System.Text;
using Weike.CMS;
using Weike.ExcLog;
using Weike.Authorize;
using Weike.Config;
using Newtonsoft.Json;
using Weike.Config.Model;
using Weike.Notify;

namespace manage.dd373.com.Controllers
{
    [ControlInfo("商城管理")]
    public class AdminEShopController : ManageControllerBase
    {

        #region 点卡管理

        [Role("点卡管理", IsAuthorize = true)]
        public ActionResult CardList(int? Page, string cardType, string gameType, string CardName,string gameService,string orderType)
        {   

            DataPages<Weike.EShop.Card> LCard = null;

            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(cardType))
                {
                    where.Append(string.Format(" and CType='{0}'", cardType));
                }
                if (!string.IsNullOrEmpty(gameType))
                {
                    where.Append(string.Format(" and GameService = '{0}'", gameType));
                }
                if (!string.IsNullOrEmpty(CardName))
                {
                    where.Append(string.Format(" and CDTitle like '%{0}%'", CardName));
                }
                if (!string.IsNullOrWhiteSpace(gameService))
                {
                    if (cardType != BssCard.CardType.话费直充.ToString() && cardType != BssCard.CardType.流量充值.ToString() && !string.IsNullOrWhiteSpace(cardType))
                    {
                        return View(LCard);
                    }
                    where.Append(string.Format(" and GameService = '{0}'", gameService));
                }
                string orderKey = "";
                Weike.Common.PagesOrderTypeDesc pagesOrderType = PagesOrderTypeDesc.降序;
                if(!string.IsNullOrEmpty(orderType))
                {
                    string[] strs = orderType.Split('|');
                    if(strs.Length==2)
                    {
                        orderKey = strs[0];
                        if(strs[1]=="asc")
                        {
                            pagesOrderType = PagesOrderTypeDesc.升序;
                        }
                    }
                }
                LCard = new BssCard().GetPageRecord(where.ToString(), string.IsNullOrEmpty(orderKey)? "CreateDate":orderKey, 12, Page ?? 1, pagesOrderType, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("查询点卡列表出错", ex, this.GetType().FullName, "CardList");
            }

            return View(LCard);

        }

        /// <summary>
        /// 订单查询接口
        /// </summary>
        /// <returns></returns>
        [Role("SUP订单查询接口", IsAuthorize = true)]
        public ActionResult OrderQuery(string OrderId)
        {
            try
            {
                BssCardThridOrder bc = new BssCardThridOrder();
                CardThridOrder entity = bc.GetModel(OrderId);
                Shopping sEntity = new BssShopping().GetModel(OrderId);
                if (entity != null && sEntity != null && sEntity.State == BssShopping.ShoppingState.支付成功.ToString())
                {
                    string supOrderNo = "";

                    Weike.Config.SupSDKConfig supConfig = Weike.Config.SupSDKConfig.Instance();

                    string supUserId = supConfig.SupUserId;
                    string partnerId = supConfig.PartnerId;

                    //拼接url 和sign 
                    string url = supConfig.SupQueryOrderUrl + "?";
                    url += "partnerId=" + partnerId + "&" + "ptOrderNo=" + entity.PostThirdOrderNo + (string.IsNullOrEmpty(supOrderNo) ? "" : "supOrderNo=" + supOrderNo + "&") + "&" + "supUserId=" + supUserId + "&";
                    string md5Resouce = "partnerId" + partnerId + "ptOrderNo" + entity.PostThirdOrderNo + (string.IsNullOrEmpty(supOrderNo) ? "" : "supOrderNo" + supOrderNo) + "supUserId" + supUserId + supConfig.PartnerKey + supConfig.SupUserKey;
                    MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
                    string sign = BitConverter.ToString(MD5.ComputeHash(Encoding.GetEncoding("gbk").GetBytes(md5Resouce))).Replace("-", "");
                    url += "sign=" + sign;

                    //打开URL，获取返回的xml字段,解析
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                    myRequest.Method = "Get";
                    HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                    if (myResponse.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding("GB2312"));
                        string res = reader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(res);
                        XmlElement root = doc.DocumentElement;
                        string status = root.ChildNodes[1].InnerText;
                        supOrderNo = root.ChildNodes[2].InnerText;
                        string supOrderSuccessTime = root.ChildNodes[3].InnerText;
                        string supOrderDetail = root.ChildNodes[4].InnerText;
                        string failCode = root.ChildNodes[5].InnerText;
                        string failReason = root.ChildNodes[6].InnerText;

                        string statusStr = "";
                        #region 判断充值状态
                        switch (status)
                        {
                            case "ORDER_SUCCESS":
                                statusStr = "充值成功";
                                break;
                            case "ORDER_UNDERWAY":
                                statusStr = "正在充值";
                                break;
                            case "ORDER_FAILED":
                                statusStr = "充值失败";
                                break;
                            default:
                                statusStr = "正在充值";
                                break;
                        }
                        #endregion

                        if (!string.IsNullOrEmpty(supOrderSuccessTime.Trim()) && supOrderSuccessTime.Length == 14)
                        {
                            //2013/11/29 15:43:49
                            supOrderSuccessTime = supOrderSuccessTime.Substring(0, 4) + "/" + supOrderSuccessTime.Substring(4, 2) + "/" + supOrderSuccessTime.Substring(6, 2) + " " + supOrderSuccessTime.Substring(8, 2) + ":" + supOrderSuccessTime.Substring(10, 2) + ":" + supOrderSuccessTime.Substring(12, 2);
                        }

                        #region 更新数据库订单状态
                        //更新数据库订单状态
                        entity.BillId = supOrderNo;
                        if (!string.IsNullOrEmpty(supOrderSuccessTime.Trim()))
                        {
                            entity.OperateTime = Convert.ToDateTime(supOrderSuccessTime);
                        }
                        entity.FailureInformation = "【" + failCode+"】"+failReason;
                        string errorMsg=string.Empty;
                        BLLCardThridOrderMethod bllCardThird = new BLLCardThridOrderMethod();
                        if (statusStr == "充值失败")
                        {
                            bllCardThird.DealDkOrderFailed(sEntity,entity.BillId, entity.FailureInformation,out  errorMsg);
                        }
                        else if (statusStr == "充值成功")
                        {
                            bllCardThird.DealDkOrderSuccess(sEntity, entity.BillId, out errorMsg);
                        }
                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            MsgHelper.InsertResult(errorMsg);
                        }
                        #endregion

                        #region 将订单Url插入数据库
                        BssSdkPostUrl.InsertUrlData(entity.ID, System.Web.HttpUtility.HtmlEncode(url) + res, Globals.GetUserIP(), BssSdkPostUrl.SdkType.提交SUP.ToString(), BssSdkPostUrl.Status.处理完成.ToString());
                        #endregion
                    }
                    else
                    {
                        MsgHelper.InsertResult("查询订单状态失败，订单编号：" + OrderId + "。");
                    }
                }
                else
                {
                    MsgHelper.InsertResult("查询订单不存在，订单编号：" + OrderId + "。");
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP查询订单状态出错：", ex, this.GetType().FullName, "OrderQuery");
                MsgHelper.InsertResult(ex.Message);

            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        public ActionResult GetSupCard(string g, string t)
        {
            if (!string.IsNullOrEmpty(g))
            {
                try
                {
                    return Json(new Weike.EShop.BssCardList().GetList(string.Format("GameService='{0}' and ProductType=" + (t == "true" ? 2 : 1) + " and IsSale=1 order by createTime asc ", g.Trim())).Tables[0].ToList<Weike.EShop.CardList>());
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("SUP获取点卡列表出错", ex, "Weike.Controllers.AdminEShopController", "GetSupCard");
                }
            }
            return Json("");
        }

        /// <summary>
        /// 获取SUP卡密
        /// </summary>
        /// <returns></returns>
        [Role("获取SUP卡密", IsAuthorize = true)]
        public ActionResult GetSecret(string g)
        {
            string retMsg = "";
            try
            {
                BssShopping bssShop = new BssShopping();
                Shopping entity = bssShop.GetModel(g);
                if (entity != null)
                {
                    Card card = new BssCard().GetModel(Convert.ToInt32(entity.ObjectId));
                    if (card != null)
                    {
                        CardThridList cList = new BssCardThridList().GetModel(card.SupId);
                        if (cList != null)
                        {
                            Weike.Config.SupSDKConfig supConfig = Weike.Config.SupSDKConfig.Instance();

                            string supUserId = supConfig.SupUserId;
                            string partnerId = supConfig.PartnerId;
                            string notifyUrl = supConfig.NotifyUrl;       //异步回调地址

                            string ptOrderNo = g.Replace("DK20", "").Replace("-", "");
                            //拼接url 和sign 
                            string url = supConfig.SupOrderTradeUrl + "?";
                            #region 用字典拼接字符串和 SIGN
                            Dictionary<string, string> SearchDic = new Dictionary<string, string>();
                            //添加传递参数
                            SearchDic.Add("buyerIp", entity.CardRemark);
                            SearchDic.Add("buyNum", entity.Count.ToString());
                            SearchDic.Add("notifyUrl", notifyUrl);
                            SearchDic.Add("partnerId", partnerId);
                            SearchDic.Add("ptOrderNo", ptOrderNo);
                            SearchDic.Add("ptPayTime", DateTime.Now.ToString("yyyyMMddHHmmss"));
                            SearchDic.Add("sum", entity.OtherPrice.ToString());
                            SearchDic.Add("supProductId", cList.ShopCode);
                            SearchDic.Add("supUserId", supUserId);
                            SearchDic.Add("unitPrice", cList.AdvicePrice.ToString());
                            var list = SearchDic.OrderBy(s => BitConverter.ToString(Encoding.ASCII.GetBytes(s.Key)));  //将字典中的对象按ascii码排序
                            MD5CryptoServiceProvider MD5t = new MD5CryptoServiceProvider();
                            string md5Source = "";
                            //循环拼接字符串
                            foreach (var item in list)
                            {
                                if (!string.IsNullOrEmpty(item.Value))
                                {
                                    url += item.Key + "=" + System.Web.HttpUtility.UrlEncode(item.Value.ToString(), Encoding.GetEncoding("GBK")) + "&";
                                    md5Source += item.Key + item.Value;
                                }
                            }
                            md5Source += supConfig.PartnerKey + supConfig.SupUserKey;
                            string sign = BitConverter.ToString(MD5t.ComputeHash(Encoding.GetEncoding("gbk").GetBytes(md5Source))).Replace("-", "");
                            url += "sign=" + sign;
                            #endregion

                            #region 将订单Url插入数据库
                            BssSdkPostUrl.InsertUrlData(entity.ID, System.Web.HttpUtility.HtmlEncode(url), Globals.GetUserIP(), BssSdkPostUrl.SdkType.提交SUP.ToString(), BssSdkPostUrl.Status.处理完成.ToString());
                            #endregion

                            //发送URL，接受返回的结果(xml格式),解析
                            string supOrderNo = "";
                            string supOrderDetail = "";
                            string failedReason = "";
                            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                            myRequest.Method = "Get";
                            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                            if (myResponse.StatusCode == HttpStatusCode.OK)
                            {
                                string res = "";
                                try
                                {
                                    StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding("GB2312"));
                                    res = reader.ReadToEnd();
                                    XmlDocument doc = new XmlDocument();
                                    doc.LoadXml(res);
                                    XmlElement root = doc.DocumentElement;
                                    string status = root.ChildNodes[1].InnerText;
                                    supOrderNo = root.ChildNodes[2].InnerText;
                                    supOrderDetail = root.ChildNodes[4].InnerText;
                                    failedReason = root.ChildNodes[6].InnerText;

                                    if (status == "ORDER_SUCCESS")
                                    {
                                        retMsg = supOrderDetail;
                                    }
                                    else
                                    {
                                        retMsg = "Error:" + failedReason;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogExcDb.Log_AppDebug("SUP提交订单出错：解析xml出错，返回结果:" + res, ex, this.GetType().FullName, "OrderReSubmit");
                                    retMsg = "Error:解析xml出错";
                                }
                            }
                            else
                            {
                                retMsg = "Error:由于网络等原因，SUP没有返回信息,请重新获取卡密";
                            }
                        }
                    }
                    else
                    {
                        retMsg = "Error:" + "提交订单点卡所对应的SUP点卡不存在或已删除，订单编号：" + g;
                    }
                }
                else
                {
                    retMsg = "Error:" + "提交订单点卡不存在或已删除，订单编号：";
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP提交订单出错：", ex, this.GetType().FullName, "OrderReSubmit");
                retMsg = "Error:" + ex;
            }
            return Content(retMsg);
        }

        public string ReturnXmlString(string handleResult, string failedCode, string failedReason)
        {
            var builder = new StringBuilder();
            builder.AppendLine(@"<?xml version=""1.0"" encoding=""gbk""?>");
            builder.AppendLine(@"<response>");
            builder.AppendLine(@"<handleResult>" + handleResult + "</handleResult>");
            builder.AppendLine(@"<failedCode>" + failedCode + "</failedCode>");
            builder.AppendLine(@"<failedReason>" + failedReason + "</failedReason>");
            builder.AppendLine(@"</response>");
            return builder.ToString();
        }


        public string ReturnXmlString(string ptOrderNo, string handleResult, string failedCode, string failedReason)
        {
            var builder = new StringBuilder();
            builder.AppendLine(@"<?xml version=""1.0"" encoding=""gbk""?>");
            builder.AppendLine(@"<response>");
            builder.AppendLine(@"<ptOrderNo>" + ptOrderNo + "</ptOrderNo>");
            builder.AppendLine(@"<handleResult>" + handleResult + "</handleResult>");
            builder.AppendLine(@"<failedCode>" + failedCode + "</failedCode>");
            builder.AppendLine(@"<failedReason>" + failedReason + "</failedReason>");
            builder.AppendLine(@"</response>");
            return builder.ToString();
        }


        [Role("SUP点卡管理", IsAuthorize = true)]
        public ActionResult SupCardList(int? Page, string cardType, string gameType, string CardName)
        {

            DataPages<Weike.EShop.CardList> LCard = null;

            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(cardType))
                {
                    where.Append(string.Format(" and ProductType={0}", cardType));
                }
                if (!string.IsNullOrEmpty(gameType))
                {
                    where.Append(string.Format(" and GameService = '{0}'", gameType));
                }
                if (!string.IsNullOrEmpty(CardName))
                {
                    where.Append(string.Format(" and Name like '%{0}%'", CardName));
                }

                LCard = new BssCardList().GetPageRecord(where.ToString(), "cast(SupProductId as int)", 12, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP点卡管理出错", ex, this.GetType().FullName, "SupCardList");
            }

            return View(LCard);

        }

        [Role("SUP游戏管理", IsAuthorize = true)]
        public ActionResult SupGameList(int? Page)
        {

            DataPages<Weike.EShop.CardGameCategory> LGame = null;

            try
            {
                LGame = new BssCardGameCategory().GetPageRecord("1=1", "CreateTime", 12, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP游戏管理出错", ex, this.GetType().FullName, "SupGameList");
            }

            return View(LGame);

        }

        [Role("SUP退款列表", IsAuthorize = true)]
        public ActionResult SupRefundList(int? Page, string orderNo, string refundState)
        {
            StringBuilder where = new StringBuilder("1=1 ");
            if (!string.IsNullOrEmpty(orderNo))
            {
                where.Append(string.Format(" and PtOrderNo='{0}'", orderNo));
            }
            if (!string.IsNullOrEmpty(refundState))
            {
                where.Append(string.Format(" and IsBack = '{0}'", refundState));
            }

            DataPages<Weike.EShop.CardRefundList> LGame = null;

            try
            {
                LGame = new BssCardRefundList().GetPageRecord(where.ToString(), "IsBack,CreateTime", 12, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP退款列表出错", ex, this.GetType().FullName, "SupRefundList");
            }

            return View(LGame);

        }


        [Role("SUP余额查询接口", IsAuthorize = true)]
        public ActionResult SupBalanceQuery()
        {
            if (IsPost)
            {
                try
                {
                    Weike.Config.SupSDKConfig supConfig = Weike.Config.SupSDKConfig.Instance();
                    string supUserId = supConfig.SupUserId;
                    string supUserKey = supConfig.SupUserKey;
                    string partnerId = supConfig.PartnerId;
                    string parentKey = supConfig.PartnerKey;
                    string checkUserUrl = supConfig.SupCheckUserUrl;


                    //拼接url和sign
                    string url = checkUserUrl + "?";
                    url += "partnerId=" + partnerId + "&" + "supUserId=" + supUserId + "&";
                    string md5Resouce = "partnerId" + partnerId + "supUserId" + supUserId + parentKey + supUserKey;
                    MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
                    string sign = BitConverter.ToString(MD5.ComputeHash(Encoding.GetEncoding("gbk").GetBytes(md5Resouce))).Replace("-", "");
                    url += "sign=" + sign;

                    //打开URL，获取返回的xml字段,解析
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                    myRequest.Method = "Get";
                    HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
                    if (myResponse.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.GetEncoding("GB2312"));
                        string res = reader.ReadToEnd();
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(res);
                        XmlElement root = doc.DocumentElement;
                        string balanceMoney = root.ChildNodes[1].InnerText;

                        ViewData["BalanceMoney"] = balanceMoney;
                    }
                    else
                    {
                        MsgHelper.InsertResult("查询SUP余额失败");
                    }

                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("查询SUP余额出错：", ex, this.GetType().FullName, "BalanceQuery");
                    MsgHelper.InsertResult(ex.Message);

                }
            }
            return View();
        }


        [Role("SUP客户退款", IsAuthorize = true)]
        public ActionResult SupRefund(string reId)
        {
            try
            {
                BssCardRefundList bssRe = new BssCardRefundList();
                CardRefundList refund = bssRe.GetModel(Convert.ToInt32(reId));
                if (refund != null)
                {
                    BssMembers bssMe = new BssMembers();
                    Members member = bssMe.GetModel(refund.UserId);
                    if (member != null)
                    {
                        //更新SUP退款列表状态
                        refund.IsBack = true;

                        //更新订单状态
                        BssCardOrders bssOrder = new BssCardOrders();
                        CardOrders order = bssOrder.GetModel(refund.PtOrderNo);
                        Shopping shop = null;
                        if (order != null)
                        {
                            order.Status = refund.ReType == 1 ? "充值失败" : "部分退款";
                            order.UpdateTime = DateTime.Now;

                            BssShopping bssShop = new BssShopping();
                            shop = bssShop.GetModel(order.ShoppingId);
                            shop.State = BssShopping.ShoppingState.交易取消.ToString();
                            shop.ProcessingTime = DateTime.Now;
                            shop.StateType = (int)BssShopping.ShoppingStateType.处理完成;
                        }

                        //更新用户余额
                        member.M_Money = member.M_Money + refund.RePrice;

                        #region 退款记录
                        MoneyHistory mhModel = new MoneyHistory();
                        mhModel.CreateDate = DateTime.Now;
                        mhModel.OrdID = order == null ? "" : order.ShoppingId;
                        mhModel.OperaType = Weike.EShop.BssMoneyHistory.HistoryType.退款记录.ToString();
                        mhModel.State = Weike.EShop.BssMoneyHistory.HistoryState.成功.ToString();
                        mhModel.SumMoney = refund.RePrice;
                        mhModel.UID = member.M_ID;
                        #endregion

                        AccCardOrdersMethod orderMethod = new AccCardOrdersMethod();
                        orderMethod.AsyncRefundMoney(order, shop, member, mhModel, refund, refund.RePrice);


                    }
                }

                MsgHelper.Insert("oprationgsuccess", "SUP客户退款成功，订单号:" + refund.PtOrderNo);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP客户退款错误", ex, this.GetType().FullName, "SupRefund");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }



        [Role("设置游戏热门状态", IsAuthorize = true)]
        public ActionResult SupGameHot(string cid)
        {
            try
            {
                Weike.EShop.BssCardGameCategory bll = new Weike.EShop.BssCardGameCategory();

                Weike.EShop.CardGameCategory model = bll.GetModel(cid);

                model.IsHot = !model.IsHot;

                bll.Update(model);

                MsgHelper.Insert("oprationgsuccess", "设置游戏状态成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏热门状态设置错误", ex, this.GetType().FullName, "SupGameHot");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("设置游戏销售状态", IsAuthorize = true)]
        public ActionResult SupGameShow(string cid)
        {
            try
            {
                Weike.EShop.BssCardGameCategory bll = new Weike.EShop.BssCardGameCategory();

                Weike.EShop.CardGameCategory model = bll.GetModel(cid);

                model.IsShow = !model.IsShow;

                bll.Update(model);

                MsgHelper.Insert("oprationgsuccess", "设置游戏状态成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏销售状态设置错误", ex, this.GetType().FullName, "SupGameShow");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("删除游戏", IsAuthorize = true)]
        public ActionResult SupGameDel(string cid)
        {
            try
            {
                new Weike.EShop.BssCardGameCategory().Delete(cid);

                MsgHelper.Insert("oprationgsuccess", "删除游戏成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除游戏错误", ex, this.GetType().FullName, "SupGameDel");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("添加点卡", IsAuthorize = true)]
        public ActionResult CardNew(string game, int? ctype, string QuDaoCard, string title, string supcard, string proportion, string isPromotion, string mianzhi, string price, int? ccount, string ishot, string cnum, string cpwd, decimal? cinprice, string Provice, string phonetitle)
        {
            if (IsPost)
            {
                try
                {
                    //if (string.IsNullOrEmpty(supcard.Trim()))
                    //{
                    //    MsgHelper.Insert("oprationgsuccess", "请选择对应的SUP卡");
                    //    return View();
                    //} 
                    string imgpath = Globals.AttachSitePic_UploadNoWater("img", 80, 80);
                    new Weike.EShop.BssCard().Add(new Weike.EShop.Card
                    {
                        SupId = supcard.Trim(),
                        Proportion = proportion,
                        IsPromotion = isPromotion == "promotion" ? true : false,
                        CACardID = cnum,
                        CACardPassword = cpwd,
                        CCount = ccount,
                        CINPrice = cinprice.Value,
                        Price = price,
                        MianZhi = mianzhi,
                        IsHot = ishot == "hot" ? true : false,
                        GameService = ctype.Value == (int)BssCard.CardType.话费直充 || ctype.Value == (int)BssCard.CardType.流量充值 ? Provice : ctype.Value == (int)BssCard.CardType.QQ充值服务 ? "" : game,
                        CDTitle = ctype.Value == (int)BssCard.CardType.话费直充 || ctype.Value == (int)BssCard.CardType.流量充值 ? phonetitle : title,
                        CreateDate = DateTime.Now,
                        CType = ctype.Value == (int)BssCard.CardType.自动卡密 ? Weike.EShop.BssCard.CardType.自动卡密.ToString() : ctype.Value == (int)BssCard.CardType.话费直充 ? BssCard.CardType.话费直充.ToString() : ctype.Value == (int)BssCard.CardType.流量充值 ? BssCard.CardType.流量充值.ToString() : ctype.Value == (int)BssCard.CardType.QQ充值服务 ? BssCard.CardType.QQ充值服务.ToString() : Weike.EShop.BssCard.CardType.点卡直冲.ToString(),
                        CImg = imgpath,
                        CSn = Weike.EShop.BssShop.CreateShopSN("DK"),
                        QuDaoType = QuDaoCard.ToInt32()
                    });
                    if (QuDaoCard.ToInt32() == (int)BssCard.QuDaoType.星启天渠道 || QuDaoCard.ToInt32() == (int)BssCard.QuDaoType.SUP渠道)
                    {
                        BssCardThridList bss = new BssCardThridList();
                        CardThridList model = bss.GetModel(supcard);
                        if (model != null && mianzhi==model.FacePriceValue)
                        {
                            model.SalePrice = Convert.ToDecimal(price);
                            model.IsSale = true;
                            bss.Update(model);
                        }
                    }
                    MsgHelper.Insert("oprationgsuccess", "添加点卡成功");
                }
                catch (Exception ex)
                {
                    MsgHelper.Insert("oprationgsuccess", "添加点卡失败：" + ex);
                    LogExcDb.Log_AppDebug("添加点卡出错", ex, this.GetType().FullName, "CardNew");
                }

                return RedirectToAction("CardList");
            }

            return View();
        }


        [Role("设置点卡状态", IsAuthorize = true)]
        public ActionResult CardHot(int? cid)
        {
            try
            {
                Weike.EShop.BssCard bll = new Weike.EShop.BssCard();

                Weike.EShop.Card model = bll.GetModel(cid ?? 0);

                model.IsHot = !model.IsHot;

                bll.Update(model);

                MsgHelper.Insert("oprationgsuccess", "设置点卡状态成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("点卡状态设置错误", ex, this.GetType().FullName, "CardHot");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("删除点卡", IsAuthorize = true)]
        public ActionResult CardDel(int? cid)
        {
            try
            {
                new Weike.EShop.BssCard().Delete(cid ?? 0);

                MsgHelper.Insert("oprationgsuccess", "删除点卡成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除点卡错误", ex, this.GetType().FullName, "CardDel");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("编辑点卡", IsAuthorize = true)]
        public ActionResult CardUpload(int? cid, string game, string QuDaoCard, int? ctype, string title, string supcard, string Qiangame, string proportion, string isPromotion, string mianzhi, string price, int? ccount, string ishot, string cnum, string cpwd, decimal? cinprice, string Provice, string phonetitle)
        {
            Weike.EShop.Card model = null;
            Weike.EShop.BssCard bll = new Weike.EShop.BssCard();
            try
            {
                model = bll.GetModel(cid ?? 0);

                if (IsPost)
                {
                    string imgpath = Request.Files["img"].ContentLength < 1 ? model.CImg : Globals.AttachSitePic_UploadNoWater("img", 80, 80);
                    model.SupId = (string.IsNullOrEmpty(supcard.Trim()) ? "" : supcard.Trim());
                    model.Proportion = proportion;
                    model.IsPromotion = isPromotion == "promotion" ? true : false;
                    model.CACardID = cnum;
                    model.CACardPassword = cpwd;
                    model.CCount = ccount;
                    model.CINPrice = cinprice.Value;
                    model.Price = price;
                    model.MianZhi = mianzhi;
                    model.IsHot = ishot == "hot" ? true : false;
                    model.GameService = ctype.Value == (int)BssCard.CardType.话费直充 || ctype.Value == (int)BssCard.CardType.流量充值 ? Provice : ctype.Value == (int)BssCard.CardType.QQ充值服务 ? "" : game;
                    model.CDTitle = ctype.Value == (int)BssCard.CardType.话费直充 || ctype.Value == (int)BssCard.CardType.流量充值 ? phonetitle : title;
                    model.CType = ctype.Value == (int)BssCard.CardType.自动卡密 ? Weike.EShop.BssCard.CardType.自动卡密.ToString() : ctype.Value == (int)BssCard.CardType.话费直充 ? BssCard.CardType.话费直充.ToString() : ctype.Value == (int)BssCard.CardType.流量充值 ? BssCard.CardType.流量充值.ToString() : ctype.Value == (int)BssCard.CardType.QQ充值服务 ? BssCard.CardType.QQ充值服务.ToString() : Weike.EShop.BssCard.CardType.点卡直冲.ToString();
                    model.CImg = imgpath;
                    model.QuDaoType = QuDaoCard.ToInt32();
                    bll.Update(model);

                    if (QuDaoCard.ToInt32() == (int)BssCard.QuDaoType.星启天渠道 ||QuDaoCard.ToInt32() == (int)BssCard.QuDaoType.SUP渠道)
                    {
                        BssCardThridList bssCard = new BssCardThridList();
                        CardThridList card = bssCard.GetModel(supcard);
                        if (card != null && mianzhi == card.FacePriceValue)
                        {
                            card.SalePrice =Convert.ToDecimal( price);
                            card.IsSale = true;
                            bssCard.Update(card);
                        }
                    }
                    MsgHelper.Insert("oprationgsuccess", "编辑点卡成功");
                    return RedirectToAction("CardList");
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑点卡出错", ex, this.GetType().FullName, "CardUpload");
            }


            if (model == null)
                return RedirectToAction("CardList");

            return View(model);
        }


        [Role("SUP设置点卡状态", IsAuthorize = true)]
        public ActionResult SupCardHot(int? cid)
        {
            try
            {
                Weike.EShop.BssCardList bll = new Weike.EShop.BssCardList();

                Weike.EShop.CardList model = bll.GetModel(cid ?? 0);

                model.IsHot = !model.IsHot;

                bll.Update(model);

                MsgHelper.Insert("oprationgsuccess", "设置点卡状态成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP点卡状态设置错误", ex, this.GetType().FullName, "SupCardHot");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("SUP设置点卡销售状态", IsAuthorize = true)]
        public ActionResult SupCardSale(int? cid)
        {
            try
            {
                Weike.EShop.BssCardList bll = new Weike.EShop.BssCardList();

                Weike.EShop.CardList model = bll.GetModel(cid ?? 0);
                if (model.IsSale == 1)
                    model.IsSale = 0;
                else
                    model.IsSale = 1;

                bll.Update(model);

                MsgHelper.Insert("oprationgsuccess", "设置点卡销售状态成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP点卡状态设置错误", ex, this.GetType().FullName, "SupCardSale");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("SUP删除点卡", IsAuthorize = true)]
        public ActionResult SupCardDel(int? cid)
        {
            try
            {
                new Weike.EShop.BssCardList().Delete(cid ?? 0);

                MsgHelper.Insert("oprationgsuccess", "删除点卡成功");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP删除点卡错误", ex, this.GetType().FullName, "SupCardDel");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("SUP编辑点卡", IsAuthorize = true)]
        public ActionResult SUPCardEdit(int? cid, string price, string ishot, string isSale)
        {
            Weike.EShop.CardList model = null;
            Weike.EShop.BssCardList bll = new Weike.EShop.BssCardList();
            try
            {
                model = bll.GetModel(cid ?? 0);

                if (IsPost)
                {
                    model.SalePrice = price;
                    model.IsHot = ishot == "hot" ? true : false;
                    model.IsSale = isSale == "sale" ? 1 : 0;
                    model.CImg = Request.Files["img"].ContentLength < 1 ? model.CImg : Globals.AttachSitePic_UploadNoWater("img", 80, 80);

                    bll.Update(model);

                    MsgHelper.Insert("oprationgsuccess", "编辑点卡成功");
                    return RedirectToAction("SupCardList");
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑点卡出错", ex, this.GetType().FullName, "SUPCardUpload");
            }


            if (model == null)
                return RedirectToAction("SupCardList");

            return View(model);
        }


        [Role("SUP出售点卡详情", IsAuthorize = true)]
        public ActionResult SupCardViewDetail(string cid, string ordOperState, string whoerror)
        {
            Weike.EShop.CardOrders model = null;
            Weike.EShop.BssCardOrders bll = new Weike.EShop.BssCardOrders();
            try
            {
                model = bll.GetModel(cid);
                if (IsPost)
                {
                    if (model.Status == "支付成功" || model.Status == "审核中")
                    {
                        #region 处理订单

                        #region 改变订单状态

                        Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                        string StrAdmin_Content = string.Format("当前管理员：用户名[{0}]。", adminmodel.A_Name);
                        string failReason = (whoerror == "b" ? "买方原因，客服交易失败" : "卖方原因，客服交易失败");

                        model.Status = ordOperState;
                        model.SupOrderSuccessTime = DateTime.Now;
                        if (ordOperState == "交易取消")
                        {
                            model.FailedCode = whoerror;
                            model.FailedReason = failReason;
                        }
                        model.UpdateTime = DateTime.Now;
                        #endregion


                        //原表订单
                        Shopping sEntity = null;
                        BssShopping bsShop = new BssShopping();
                        sEntity = bsShop.GetModel(model.ShoppingId);
                        if (sEntity != null)
                        {
                            sEntity.State = ordOperState == "交易取消" ? Weike.EShop.BssShopping.ShoppingState.交易取消.ToString() : Weike.EShop.BssShopping.ShoppingState.交易成功.ToString();
                            sEntity.State = ordOperState;
                            sEntity.ProcessingTime = DateTime.Now;
                            sEntity.SID = adminmodel.A_ID;
                            sEntity.StateType = (int)BssShopping.ShoppingStateType.处理完成;
                        }

                        #region 资金操作
                        MoneyHistory mhModel = null;
                        Members mEntity = null;
                        Card card = null;
                        CardList cardlist = null;
                        decimal changeMoney = 0;
                        if (ordOperState == "交易取消")
                        {
                            BssMembers bm = new BssMembers();
                            mEntity = bm.GetModel(model.UserId);
                            if (mEntity != null)
                            {
                                //更新用户余额
                                mEntity.M_Money = mEntity.M_Money + Convert.ToDecimal(model.SaleSumPrice);

                                string msg = "DD373温馨提示！ 您好，您的点卡订单编号为" + model.ShoppingId + "的订单交易取消！请注意查收自己账户的资金！";
                                BssMembersMessage.AddMeg(mEntity.M_ID, msg);

                                #region 退款记录
                                mhModel = new MoneyHistory();
                                mhModel.CreateDate = DateTime.Now;
                                mhModel.OrdID = model.ShoppingId;
                                mhModel.OperaType = Weike.EShop.BssMoneyHistory.HistoryType.退款记录.ToString();
                                mhModel.State = Weike.EShop.BssMoneyHistory.HistoryState.成功.ToString();
                                mhModel.SumMoney = model.SaleSumPrice;
                                mhModel.UID = mEntity.M_ID;
                                #endregion

                                changeMoney = Convert.ToDecimal(model.SaleSumPrice);
                            }

                            BssCardList bssCa = new BssCardList();
                            cardlist = bssCa.GetModel(model.SupProductId);
                            if (cardlist != null)
                            {
                                card = new BssCard().GetModelBySupId(cardlist.ID.ToString(),(int)BssCard.QuDaoType.SUP渠道);
                                if (card != null)
                                {
                                    card.CCount = card.CCount.Value + model.BuyNum;
                                }
                            }

                        }
                        else
                        {
                            string msg = "DD373温馨提示！恭喜您，您的点卡订单编号为" + model.ShoppingId + "的订单交易成功！";
                            BssMembersMessage.AddMeg(model.UserId, msg);
                        }

                        AccCardOrdersMethod orderMethod = new AccCardOrdersMethod();
                        orderMethod.AsyncUpdateStateAndAddMoney(model, mEntity, mhModel, sEntity, card, changeMoney);

                        #endregion

                        //插入订单备注记录
                        BssShoppingRemarkInfo.InsertShoppingRemarkInfo(sEntity.ID, StrAdmin_Content);

                        #endregion
                    }
                    else
                        MsgHelper.Insert("msgSupOrder", "您在干什么呢？");
                }
            }
            catch (Exception ex)
            {
                MsgHelper.Insert("msgSupOrder", "处理点卡订单失败，" + ex + "。");
                LogExcDb.Log_AppDebug("SUP出售点卡详情出错", ex, this.GetType().FullName, "SupCardViewDetail");
            }


            if (model == null)
                return RedirectToAction("SupCardOrder");

            return View(model);
        }


        [Role("SUP点卡出售订单管理", IsAuthorize = true)]
        public ActionResult SupCardOrder(int? Page, string cardType, string orderState, string Btype, string UserName, string timetype, string StartTime, string EntTime)
        {
            StringBuilder where = new StringBuilder("1=1 ");
            if (!string.IsNullOrEmpty(cardType))
            {
                where.Append(string.Format(" and SupProductId in (select SupProductId from CardList where ProductType={0})", cardType));
            }
            if (!string.IsNullOrEmpty(orderState))
            {
                where.Append(string.Format(" and Status = '{0}'", orderState));
            }
            if (!string.IsNullOrEmpty(UserName))
            {
                if (Btype == "id")
                    where.Append(string.Format(" and ID = '{0}'", UserName));
                else
                {
                    Members m = new BssMembers().GetModelByName(UserName);
                    if (m != null)
                        where.Append(string.Format(" and UserId={0}", m.M_ID));
                }
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "SupOrderSuccessTime" : "PtPayTime"));
            StringBuilder fileds = new StringBuilder(" SupProductId,UserId,ID,SupOrderNo,BuyNum,SaleSumPrice,Status,CreateTime,SupOrderSuccessTime ");
            DataPages<Weike.EShop.CardOrders> LShop = null;
            try
            {
                LShop = new BssCardOrders().GetPageRecord(where.ToString(), "case Status when '支付成功' then 1 when '正在充值' then 2 when '审核中' then 3 when '充值失败' then 4 else 5  end asc,CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, fileds.ToString());

                Admins adminModel = BLLAdmins.GetCurrentAdminUserInfo();
                if (adminModel != null && "wangzheyongle9|admin|tcwuzhe|ddmiaojinli|langying".Contains(adminModel.A_Name))
                {
                    ViewData["TotalPrice"] = new AccCardOrders().GetToatlPrice(where.ToString());
                    ViewData["TotalCount"] = new AccCardOrders().GetToatlCount(where.ToString());
                    ViewData["TotalINPrice"] = new AccCardOrders().GetToatlINPrice(where.ToString());
                }
                else
                { ViewData["TotalPrice"] = ""; ViewData["TotalCount"] = ""; ViewData["TotalINPrice"] = ""; }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("SUP点卡出售订单管理", ex, this.GetType().FullName, "SupCardOrder");
            }
            
            return View(LShop);
        }

        #region  尚景点卡
        /// <summary>
        /// 获取点卡游戏列表
        /// </summary>
        /// <returns></returns>
        [Role("获取点卡游戏列表", IsAuthorize = true)]
        public ActionResult GetCardGameList(int? QuDaoType)
        {
            List<CardThirdGame> gamelist = new List<CardThirdGame>();

            try
            {
                BssCardThirdGame bssCardThirdGame = new BssCardThirdGame();
                string sqlWhere = string.Empty;
                if (QuDaoType.HasValue)
                {
                    sqlWhere = string.Format("QuDaoType={0}", QuDaoType);
                }
                List<CardThirdGame> list = bssCardThirdGame.GetModelList(sqlWhere);
                return Json(list);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取点卡的游戏列表", ex, "Weike.EShop.BssCardGameCategory", "GetCardGameList");
            }

            return Json("");
        }
        /// <summary>
        /// 尚景订单再次提交接口
        /// </summary>
        /// <param name="OrderNo"></param>
        /// <returns></returns>
        [Role("尚景订单再次提交接口", IsAuthorize = true)]
        public ActionResult ShangJOrderReSubmit(string OrderNo)
        {
            try
            {
                ShangJingSdkConfig Instance = ShangJingSdkConfig.Instance();
                //查找对应点卡信息
                Card cards = null;
                CardShangJingList cardlist = null;
                Shopping sEntity = null;
                CardShangJingOrder entity = new BssCardShangJingOrder().GetModel(OrderNo);
                sEntity = new BssShopping().GetModel(entity.ShoppingId);
                if (sEntity != null &&entity!=null&& (entity.RechargeState == "审核中"||entity.RechargeState=="支付成功"))
                {
                    cards = new BssCard().GetModel(sEntity.ObjectId);
                    if (cards != null)
                    {
                        cardlist = new BssCardShangJingList().GetModelByItemID(cards.SupId.ToInt32());
                        string url = Instance.SubmitUrl;
                        if (cardlist != null)
                        {
                            BLLCardShangJingOrderMethod BllMethod = new BLLCardShangJingOrderMethod();
                            bool ret= BllMethod.SubmitShangJOrder(OrderNo, cards, null);
                            if (ret)
                            {
                                MsgHelper.Insert("megCkSellCard", "充值失败！");
                            }
                            else
                            {
                                MsgHelper.Insert("megCkSellCard", "重新下单成功！");
                            }
                        }
                    }
                }
                if (cardlist == null)
                {
                    MsgHelper.Insert("megCkSellCard", "暂时没有流量对应的点卡，请编辑更新点卡。");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("尚景提交订单出错：", ex, this.GetType().FullName, "ShangJOrderReSubmit");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }
        /// <summary>
        /// 尚景订单查询接口
        /// </summary>
        /// <returns></returns>
        [Role("尚景订单查询接口", IsAuthorize = true)]
        public ActionResult ShangJOrderQuery(string OrderNo)
        {
            try
            {
                BssCardThridOrder bssc = new BssCardThridOrder();
                CardThridOrder entity = bssc.GetModel(OrderNo);
                Shopping shoppingModel = new BssShopping().GetModel(OrderNo);
                ShangJingSdkConfig Instance = ShangJingSdkConfig.Instance();
                if (entity != null && shoppingModel != null && shoppingModel.State!=BssShopping.ShoppingState.交易成功.ToString()&&shoppingModel.State!=BssShopping.ShoppingState.交易取消.ToString())
                {
                    if (!string.IsNullOrWhiteSpace(entity.BillId))
                    {
                        Members mModel = new BssMembers().GetModel(entity.UserId);
                        string url = Instance.QueryUrl;
                        #region 将订单Url插入数据库
                        DateTime Stime = Convert.ToDateTime("1970-01-01 00:00:00");
                        DateTime NowTime = DateTime.Now;
                        TimeSpan ts = NowTime - Stime;
                        string data = "account=" + Instance.ShangJaccount + "&action=Query&orderID=" + entity.BillId + "&timeStamp=" + ts.TotalSeconds.ToInt32();
                        string temp = Instance.ShangJapiKey + data + Instance.ShangJapiKey;
                        string sign = "&sign=" + Globals.PwdToMD5(temp).ToLower();
                        BssSdkPostUrl.InsertUrlData(OrderNo, System.Web.HttpUtility.HtmlEncode(url + data + sign), Globals.GetUserIP(), BssSdkPostUrl.SdkType.提交尚景.ToString(), BssSdkPostUrl.Status.处理完成.ToString());
                        #endregion
                        //创建订单
                        string res = new BLLCardShangJingOrderMethod().CreateShangJingOrder(url, data + sign, Encoding.UTF8);
                        JObject json = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(res);
                        string respCode = json.SelectToken("respCode").ToString().Replace("\"", "").Trim();
                        string respMsg = json.SelectToken("respMsg").ToString().Replace("\"", "").Trim();
                        string reason = "【" + respCode + "】:" + respMsg;
                        string statusStr = "下单成功";
                        if (!string.IsNullOrEmpty(respCode))
                        {
                            #region 判断充值状态
                            switch (respCode)
                            {
                                case "0000":
                                    statusStr = "下单成功";
                                    break;
                                case "0001":
                                    statusStr = "充值中";
                                    break;
                                case "0002":
                                    statusStr = "充值成功";
                                    break;
                                case "0003":
                                    statusStr = "充值失败";
                                    break;
                                default:
                                    statusStr = "充值中";
                                    break;
                            }
                            #endregion
                            string errMsg = "";
                            if (statusStr == "充值失败")
                            {
                                new BLLCardThridOrderMethod().DealDkOrderFailed(shoppingModel, "", "提交第三方平台失败，" + reason, out errMsg);
                            }
                            else if (statusStr == "充值成功")
                            {
                                 new BLLCardThridOrderMethod().DealDkOrderSuccess(shoppingModel,"", out errMsg);
                            }
                            MsgHelper.Insert("paypassworderror",reason);
                            return Redirect(Request.UrlReferrer == null ? "Card.shtml" : Request.UrlReferrer.ToUriStringNoProtocol());
                        }
                    }
                    else
                    {
                        MsgHelper.InsertResult("查询订单尚景编号不存在，订单编号：" + OrderNo + "。");
                    }
                }
                else
                {
                    MsgHelper.InsertResult("查询订单不存在，订单编号：" + OrderNo + "。");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("尚景订单状态出错：", ex, this.GetType().FullName, "ShangJOrderQuery");
                MsgHelper.InsertResult(ex.Message);
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }
        /// <summary>
        /// 获取尚景点卡销售商品列表
        /// </summary>
        /// <returns></returns>
        public ActionResult GetSJCardList(string op)
        {
            try
            {
                string title = op.Substring(op.Length - 2);
                int Operator = (int)Enum.Parse(typeof(BssCardShangJingList.OperatorEnum),title);
                List<CardShangJingList> list = new Weike.EShop.BssCardShangJingList().GetModelList(string.Format(" Operator={0} order by price ", Operator));
                return Json(list);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取尚景点卡销售商品列表", ex, "Weike.Controllers.CardsController", "GetSJCardList");
            }
            return Json("");
        }
        #endregion

        #region 尚景流量点卡
        [Role("添加尚景流量点卡", IsAuthorize = true)]
        [HttpGet]
        public ActionResult AddShangJingCard()
        {
            return View();
        }
        [Role("添加尚景流量点卡", IsAuthorize = true)]
        [HttpPost]
        public ActionResult AddShangJingCard(string name,  string shopcode,string faceValue, decimal price)
        {
            if (string.IsNullOrEmpty(name))
            {
                MsgHelper.InsertResult("商品名称不能为空!");
            }
            if (string.IsNullOrEmpty(shopcode))
            {
                MsgHelper.InsertResult("商品编号不能为空!");
            }
            if (string.IsNullOrEmpty(faceValue))
            {
                MsgHelper.InsertResult("面值不能为空!");
            }
            if (price < 0)
            {
                MsgHelper.InsertResult("价格必须大于0!");
            }
            try
            {
                BssCardThridList bss = new BssCardThridList();
                CardThridList model = bss.GetModel((int)BssCard.QuDaoType.尚景渠道, shopcode, (int)BssCardThridList.ProductTypeEnum.流量);
                if (model!=null&&model.ItemName == name) 
                {
                    MsgHelper.InsertResult("该商品已存在!");
                }
                else
                {
                    model = new CardThridList();
                    model.AdvicePrice = model.SalePrice = price;
                    model.CardThirdGameId = "";
                    model.CreateTime = DateTime.Now;
                    model.FacePriceValue = faceValue;
                    model.Id = Globals.GetPrimaryID();
                    model.IsSale = true;
                    model.ItemName = name;
                    model.NumberChoice = "1";
                    model.PlatformId = (int)BssCard.QuDaoType.尚景渠道;
                    model.ProductType = (int)BssCardThridList.ProductTypeEnum.流量;
                    model.ShopCode = shopcode;
                    model.Stock = -1;
                    bss.Add(model);
                    MsgHelper.InsertResult("添加成功!");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("添加尚景流量数据出错", ex, this.GetType().FullName, "AddShangJingCard");
            }
            return View();
        }
        #endregion

        #endregion
        #region 第三方点卡商品(点卡、流量、话费)
        [Role("第三方点卡商品列表", IsAuthorize = true)]
        public ActionResult CardThridList(int? Page, int? platform, string cardType, string CardName, string ShopCode, string Sale)
        {
            DataPages<CardThridList> LCard = null;

            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(cardType))
                {
                    where.Append(string.Format(" and ProductType={0}", cardType));
                }
                if (!string.IsNullOrEmpty(ShopCode))
                {
                    where.Append(string.Format(" and ShopCode='{0}'", ShopCode));
                }
                if (!string.IsNullOrEmpty(Sale))
                {
                    where.Append(string.Format(" and IsSale={0}", Sale.ToInt32()));
                }
                if (!string.IsNullOrEmpty(CardName))
                {
                    where.Append(string.Format(" and ItemName like '%{0}%'", CardName));
                }

                if (platform.HasValue)
                {
                    where.Append(string.Format(" and PlatformId ={0}", platform.Value));
                }
                LCard = new BssCardThridList().GetPageRecord(where.ToString(), "CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "Id,Stock,PlatformId,IsSale,CreateTime,ItemName,ProductType,ShopCode,FacePriceValue,NumberChoice,AdvicePrice,SalePrice");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("第三方点卡商品管理出错", ex, this.GetType().FullName, "CardThridList");
            }

            return View(LCard);
        }
        [Role("设置第三方点卡状态", IsAuthorize = true)]
        public ActionResult CardThridSale(string cid)
        {
            string msg = "设置失败!";
            if (!string.IsNullOrWhiteSpace(cid))
            {
                CardThridList model = new BssCardThridList().GetModel(cid);
                if (model != null)
                {
                    model.IsSale = !model.IsSale;
                    if (new BssCardThridList().Update(model))
                    {
                        msg = "设置成功！";
                    }
                }
            }
            MsgHelper.InsertResult(msg);
            return RedirectToAction("CardThridList");
        }
        [Role("获取第三方点卡商品", IsAuthorize = true)]
        [HttpGet]
        public ActionResult GetCardThrid(string cid)
        {
            CardThridList model = null;
            if (!string.IsNullOrWhiteSpace(cid))
            {
                model = new BssCardThridList().GetModel(cid);
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        [Role("编辑第三方点卡商品", IsAuthorize = true)]
        [HttpPost]
        public ActionResult UpdateCardThrid(string cid, bool IsSale, decimal salePrice)
        {
            if (string.IsNullOrWhiteSpace(cid))
            {
                return Content("该点卡不存在!");
            }
            if (salePrice <= 0)
            {
                return Content("售价必须大于0");
            }
            try
            {
                BssCardThridList bss=new BssCardThridList();
                CardThridList model = bss.GetModel(cid);
                if (model != null)
                {
                    model.IsSale = IsSale;
                    model.SalePrice = salePrice;
                    bss.Update(model);
                    return Content("");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑第三方点卡失败。", ex, this.GetType().FullName, "UpdateCardThrid");
            }
            return Content("编辑失败!");
        }
        [Role("获取第三方点卡商品数据集", IsAuthorize = true)]
        [HttpGet]
        public ActionResult GetCardThirdList(int? platform, int? cardType, string Provice, string Phonetitle,string thirdGameId)
        {
            try
            {
                List<CardThridList> list = BssCardThridList.GetCardList(platform, cardType, Provice, Phonetitle, thirdGameId);
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取第三方点卡数据失败", ex, this.GetType().FullName, "GetCardThird");
            }
            return Content("");
        }
        [Role("更新所有第三方点卡商品", IsAuthorize = true)]
        public ActionResult GetCardThridListUpdate(int platformId,int productType)
        {
            if (platformId == (int)BssCard.QuDaoType.星启天渠道)
            {
                string reslut = new BLLCardThridOrderMethod().GetXQTCardList(productType);
                if (string.IsNullOrWhiteSpace(reslut))
                {
                    MsgHelper.InsertResult("更新成功!");
                }
                else
                {
                    MsgHelper.InsertResult(reslut);
                }
            }
            return RedirectToAction("CardThridList");
        }
        [Role("查询第三方点卡商品订单状态", IsAuthorize = true)]
         public ActionResult CardThridQuery(string OrderId)
         {
             string resMsg = new BLLCardThridOrderMethod().QueryXqtOrderState(OrderId);
             MsgHelper.InsertResult(resMsg);
             return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : string.Format("/admineshop/USellNewCardOrder?sid={0}&t={1}", OrderId,new Random().Next()));
         }
        /// <summary>
         /// 第三方点卡订单审核
        /// </summary>
        /// <returns></returns>
        [Role("第三方点卡订单审核", IsAuthorize = true)]
         public ActionResult ShThirdDkOrder(string OrderId,string dealState)
        {
            try
            {
                string errMsg = "";
                if (string.IsNullOrWhiteSpace(dealState)||dealState=="审核成功")
                {
                    dealState = BssShopping.ShoppingState.支付成功.ToString();
                }
                else
                {
                    dealState = BssShopping.ShoppingState.交易取消.ToString();
                }
                bool res = new BLLCardThridOrderMethod().ShThirdDkOrder(OrderId,dealState, out errMsg);

                if (!res)
                    MsgHelper.Insert("megCkSellCard", errMsg);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("第三方点卡订单审核：", ex, this.GetType().FullName, "ShThirdDkOrder");
                MsgHelper.Insert("megCkSellCard", ex.Message);
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [HttpGet]
        public ActionResult GetXQTFaceValue(string Id)
        {
            try
            {
                string reslut = new BLLCardThridOrderMethod().GetXQTCardFaceValues(Id);
                return Content(reslut);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取星启天点卡面值失败", ex, this.GetType().FullName, "GetXQTFaceValue");
            }
            return Content("([])");
        }

        #endregion


        #region 用户发布的出售信息

        [Role("出售管理", IsAuthorize = true)]
        public ActionResult USells(int? Page, string Stype, string key, string ShopState, string GameCategoryProperty, string DealType, string GameId,string GameOtherId, string GameShopTypeId, string issort, string youhuo, string ShopInfoType, string StartTime, string EntTime)
        {
            DataPages<Weike.EShop.Shop> Lshop = null;

            StringBuilder where = new StringBuilder("1=1");

            if (string.IsNullOrEmpty(Stype))
                Stype = "s";

            if (Stype == "s" && !string.IsNullOrEmpty(key))
                where.Append(string.Format(" and ShopID = '{0}'", key));//商品ID
            else if (!string.IsNullOrEmpty(key))
                where.Append(string.Format("and PublicUser = ({0})", Weike.Member.BssMembers.GetLinkNameID(key)));//根据用户名获取ID
            //类型
            if (!string.IsNullOrEmpty(GameCategoryProperty))
            {
                string[] GameCategoryPropertyItem = GameCategoryProperty.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (GameCategoryPropertyItem.Length == 1)
                {
                    where.Append(" and ShopType in (select ID from GameShopType where Property = '" + GameCategoryPropertyItem[0] + "')");
                }
                else if (GameCategoryPropertyItem.Length > 1)
                {
                    string GameCategoryPropertywhere = "";
                    foreach (string item in GameCategoryPropertyItem)
                    {
                        GameCategoryPropertywhere = GameCategoryPropertywhere + "'" + item + "',";
                    }
                    GameCategoryPropertywhere = GameCategoryPropertywhere.TrimEnd(',');
                    where.AppendFormat(" and ShopType in (select ID from GameShopType where Property in({0}))", GameCategoryPropertywhere);
                }
            }
            //交易状态
            if (!string.IsNullOrEmpty(ShopState))
            {
                if (ShopState != "")
                    where.Append(string.Format(" and ShopState='{0}'", ShopState));
            }
            //交易类型
            if (!string.IsNullOrEmpty(DealType))
            {
                if (DealType != "")
                    where.Append(string.Format(" and DealType='{0}'", DealType));
            }
            if (!string.IsNullOrEmpty(youhuo))
            {
                where.Append(string.Format(" and publiccount>0"));
            }
            if (!string.IsNullOrEmpty(ShopInfoType))
            {
                where.Append(string.Format(" and exists(select 1 from ShopOtherInfo where Shop.ShopId=ShopOtherInfo.ShopNo and ShopOtherInfo.ConfigId={0})", ShopInfoType));
            }
            string gcwhere = string.Empty;

            if (!string.IsNullOrEmpty(GameId))
            {
                bool isLike = true;
                string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                if (!string.IsNullOrEmpty(gameGUID))
                {
                    if (isLike)
                        gcwhere += string.Format(" and GameGUID like '{0}%' ", gameGUID);
                    else
                        gcwhere += string.Format(" and GameGUID='{0}' ", gameGUID);
                }

                if (!string.IsNullOrEmpty(GameShopTypeId))
                {
                    GameShopType gameShopTypeModel = new BssGameShopType().GetModel(GameShopTypeId);
                    if (gameShopTypeModel != null)
                    {
                        if (gameShopTypeModel.CurrentLevelType == (int)Weike.EShop.BssGameRoute.LevelType.商品子类型)
                        {
                            gcwhere += string.Format(" and ShopType = '{0}' and ShopTypeCate = '{1}'", gameShopTypeModel.ParentId, gameShopTypeModel.ID);
                        }
                        else
                        {
                            gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ID);
                        }
                    }
                    else
                    {
                        return View();
                    }
                }
            }

            string orderd = "createdate";
            if (!string.IsNullOrEmpty(issort))
            {
                orderd = "singleprice";
            }

            where.Append(gcwhere);
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where.Append(string.Format(" and CreateDate between '{0}' and '{1}'", STime, ETime));
            StringBuilder stringfileds = new StringBuilder("id,Title,PublicUser,GameType,GameGUID,ShopType,ShopTypeCate,ShopID,Price,PublicCount,OverDay,DealType,ShopState,CreateDate,TimeId");
            try
            {
                Lshop = new BssShop().GetPageRecord(where.ToString(), orderd, 10, Page ?? 1, issort == "u" ? PagesOrderTypeDesc.升序 : PagesOrderTypeDesc.降序, stringfileds.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售管理", ex, this.GetType().FullName, "USells");
            }

            return View(Lshop);
        }

        [Role("帐号待审核列表", IsAuthorize = true)]
        public ActionResult ZHWaitUsells(int? Page, string Stype, string key, string ShopState, string GameCategoryProperty, string DealType, string GameId, string GameOtherId, string GameShopTypeId)
        {
            DataPages<Weike.EShop.ShopAccount> Lshop = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1");

                if (string.IsNullOrEmpty(Stype))
                    Stype = "s";

                if (Stype == "s" && !string.IsNullOrEmpty(key))
                    where.Append(string.Format(" and ShopID = '{0}'", key));//商品ID
                else if (!string.IsNullOrEmpty(key))
                    where.Append(string.Format("and PublicUser = ({0})", Weike.Member.BssMembers.GetLinkNameID(key)));//根据用户名获取ID
                //类型
                if (!string.IsNullOrEmpty(GameCategoryProperty))
                    where.Append(" and ShopType in (select ID from GameShopType where Property = '" + GameCategoryProperty + "') ");

                where.Append(" and DealType='3' and ShopState='等待审核' ");

                string gcwhere = string.Empty;
                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            gcwhere += string.Format(" and GameGUID like '{0}%' ", gameGUID);
                        else
                            gcwhere += string.Format(" and GameGUID='{0}' ", gameGUID);
                    }

                    if (!string.IsNullOrEmpty(GameShopTypeId))
                    {
                        GameShopType gameShopTypeModel = new BssGameShopType().GetModel(GameShopTypeId);
                        if (gameShopTypeModel != null)
                        {
                            if (gameShopTypeModel.CurrentLevelType == (int)Weike.EShop.BssGameRoute.LevelType.商品子类型)
                            {
                                gcwhere += string.Format(" and ShopType = '{0}' and ShopTypeCate = '{1}'", gameShopTypeModel.ParentId, gameShopTypeModel.ID);
                            }
                            else
                            {
                                gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ID);
                            }
                        }
                        else
                        {
                            return View();
                        }
                    }
                }

                where.Append(gcwhere);

                string accountWhere = string.Format(" exists(select 1 from Shop where {0} and Shop.ShopID=ShopAccount.ShopNo)", where.ToString());

                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                if (adminmodel != null)
                {
                    accountWhere += " and sid=" + adminmodel.A_ID;
                }

                Lshop = new BssShopAccount().GetPageRecord(accountWhere, "CreateTime", 10, Page ?? 1, PagesOrderTypeDesc.降序, "*");

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("帐号待审核列表出错", ex, this.GetType().FullName, "ZHWaitUsells");
            }

            return View(Lshop);
        }

        [Role("待审核列表", IsAuthorize = true)]
        public ActionResult WaitUSells(int? Page, string Stype, string key, string ShopState, string GameCategoryProperty, string DealType, string GameId,string GameOtherId, string GameShopTypeId)
        {
            DataPages<Weike.EShop.Shop> Lshop = null;

            StringBuilder where = new StringBuilder("1=1");

            if (string.IsNullOrEmpty(Stype))
                Stype = "s";

            if (Stype == "s" && !string.IsNullOrEmpty(key))
                where.Append(string.Format(" and ShopID = '{0}'", key));//商品ID
            else if (!string.IsNullOrEmpty(key))
                where.Append(string.Format("and PublicUser = ({0})", Weike.Member.BssMembers.GetLinkNameID(key)));//根据用户名获取ID
            //类型
            if (!string.IsNullOrEmpty(GameCategoryProperty))
                where.Append(" and ShopType in (select ID from GameShopType where Property = '" + GameCategoryProperty + "') ");
            //交易状态

            if (!string.IsNullOrEmpty(ShopState))
            {

                where.Append(string.Format(" and ShopState='{0}'", ShopState));
            }
            else where.Append(string.Format(" and ShopState='{0}'", "等待审核"));

            //交易类型
            if (!string.IsNullOrEmpty(DealType))
            {
                if (DealType != "")
                    where.Append(string.Format(" and DealType='{0}'", DealType));
            }

            string gcwhere = string.Empty;
            if (!string.IsNullOrEmpty(GameId))
            {
                bool isLike = true;
                string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                if (!string.IsNullOrEmpty(gameGUID))
                {
                    if (isLike)
                        gcwhere += string.Format(" and GameGUID like '{0}%' ", gameGUID);
                    else
                        gcwhere += string.Format(" and GameGUID='{0}' ", gameGUID);
                }

                if (!string.IsNullOrEmpty(GameShopTypeId))
                {
                    GameShopType gameShopTypeModel = new BssGameShopType().GetModel(GameShopTypeId);
                    if (gameShopTypeModel != null)
                    {
                        if (gameShopTypeModel.CurrentLevelType == (int)Weike.EShop.BssGameRoute.LevelType.商品子类型)
                        {
                            gcwhere += string.Format(" and ShopType = '{0}' and ShopTypeCate = '{1}'", gameShopTypeModel.ParentId, gameShopTypeModel.ID);
                        }
                        else
                        {
                            gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ID);
                        }
                    }
                    else
                    {
                        return View(Lshop);
                    }
                }
            }

            where.Append(gcwhere);
            StringBuilder stringfileds = new StringBuilder("id,Title,PublicUser,GameType,GameGUID,ShopType,ShopTypeCate,ShopID,Price,PublicCount,OverDay,SinglePriceUnit,DealType,ShopState,CreateDate,TimeId");
            try
            {
                Lshop = new BssShop().GetPageRecord(where.ToString(), "CreateDate", 10, Page ?? 1, PagesOrderTypeDesc.降序, stringfileds.ToString());

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售管理", ex, this.GetType().FullName, "USells");
            }

            return View(Lshop);
        }

        [Role("快速审核出售记录", IsAuthorize = true)]
        public ActionResult USellPass(int? sID, string t, string td, string reason, string returnurl, string remark)
        {
            try
            {
                Weike.EShop.BssShop bll = new Weike.EShop.BssShop();
                Weike.EShop.Shop model = bll.GetModel(sID ?? 0);
                DateTime publicTime = model.CreateDate.Value;
                if (t == "p")
                {
                    if (model.ShopState == Weike.EShop.BssShop.ShopState.等待审核.ToString())
                    {
                        model.ShopState = Weike.EShop.BssShop.ShopState.审核成功.ToString();
                        if (model.DealType == (int)BssShop.EDealType.担保)
                        {
                            model.ShopState = BLLShopInfoMethod.GetShopStateByUserState(model);
                        }
                        model.CreateDate = DateTime.Now;
                    }
                    GameInfoModel InfoModel = new BLLGame().GetGameInfoModel(model.GameType, model.ShopType, false);
                    Game gameModel = null;
                    if (InfoModel != null && InfoModel.GameModel != null)
                    {
                        gameModel = InfoModel.GameModel;
                    }
                    else
                    {
                        gameModel = new BssGame().GetModel(model.GameGUID.Split('|')[0], false);
                    }
                    if (gameModel != null && gameModel.GameName.Contains("魔侠传") && td == "y")
                    {
                        //魔侠传申诉通道关闭
                        ShopOtherInfo si = new ShopOtherInfo();
                        si.ConfigId = (int)BssShopOtherInfo.InfoType.申诉通道已关闭;
                        si.ShopNo = model.ShopID;
                        si.CreateTime = DateTime.Now;
                        new BssShopOtherInfo().Add(si);
                    }                   
                }
                else
                {
                    //资金退还修改，改到商品被删除时

                    ShopFailedReasonConfig sfrcModel = new BssShopFailedReasonConfig().GetModel(reason.ToInt32());
                    if (sfrcModel != null)
                    {
                        ShopFailedReason sfrModel = new ShopFailedReason();
                        sfrModel.CreateTime = DateTime.Now;
                        sfrModel.OrderId = model.ShopID;
                        sfrModel.ReasonId = sfrcModel.ID;
                        sfrModel.ReasonContent = sfrcModel.ReasonContent + remark;
                        new BssShopFailedReason().Add(sfrModel);

                        reason = sfrcModel.ReasonContent;
                    }

                    if (t == "xiajia") model.ShopState = Weike.EShop.BssShop.ShopState.用户下架.ToString();
                    else model.ShopState = Weike.EShop.BssShop.ShopState.审核失败.ToString();
                }
                bll.Update(model);
                if (model.ShopState == Weike.EShop.BssShop.ShopState.审核成功.ToString() && !string.IsNullOrEmpty(model.AccountNo))
                {
                    string GameId = model.GameGUID.Split('|')[0].ToString();
                    List<Shop> shoplist = new BssShop().GetModelList(0,string.Format("GameGuid like '{0}%' and AccountNo='{1}'and ID<>'{2}' and ShopState='{3}'", GameId, model.AccountNo,model.ID,Weike.EShop.BssShop.ShopState.等待审核.ToString()),"ID","ID");
                    if (shoplist != null)
                    {
                        foreach (Shop sp in shoplist)
                        {
                            Shop shopmodel = new BssShop().GetModel(sp.ID);
                            if (shopmodel !=null && shopmodel.ShopState == Weike.EShop.BssShop.ShopState.等待审核.ToString())
                            {
                                shopmodel.ShopState = Weike.EShop.BssShop.ShopState.审核失败.ToString();
                                bll.UpdateShopState(shopmodel, Weike.EShop.BssShop.ShopState.等待审核.ToString());
                            }                         
                        }   
                    }                                   
                }
                Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                BssModifyMembersRecording.AddCaoZuoRecording(model.PublicUser, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.审核帐号, model.ShopID, model.ShopState + "зы" + publicTime.ToString() + "зы" + model.GameType);
                MsgHelper.Insert("oprationgsuccess", "审核操作成功!编号为：" + model.ShopID);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("快速审核失败", ex, this.GetType().FullName, "USellPass");
            }
            if (string.IsNullOrEmpty(returnurl))
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            else
                return Redirect(returnurl);
        }

        [Role("编辑出售记录", IsAuthorize = true)]
        public ActionResult USellUpload(int? sID, string tilte, int? pcount, string shopstate, string descript, string dd373, string dddeclare, string reason, string remark)
        {
            Weike.EShop.Shop model = null;

            try
            {
                Weike.EShop.BssShop bll = new Weike.EShop.BssShop();
                model = bll.GetModel(sID ?? 0);

                if (IsPost && model != null)
                {
                    if (shopstate != BssShop.ShopState.审核失败.ToString() && shopstate != model.ShopState && model.ShopState != BssShop.ShopState.等待审核.ToString())
                    {
                        MsgHelper.InsertResult("非等待审核商品不能编辑为非审核失败状态");
                        return View(model);
                    }

                    if (shopstate == BssShop.ShopState.审核失败.ToString() && string.IsNullOrEmpty(reason) && string.IsNullOrEmpty(remark))
                    {
                        MsgHelper.InsertResult("失败原因必填");
                        return View(model);
                    }

                    Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                    BssModifyMembersRecording.AddCaoZuoRecording(model.PublicUser, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.编辑商品, model.ShopID, string.Format("客服编辑商品，编辑前标题：{0}，状态：{1}；编辑后标题：{2}，状态：{3}。", model.Title, model.ShopState, tilte, shopstate));

                    model.Title = tilte;
                    model.PublicCount = pcount ?? 0;
                    model.Detail = descript;
                    model.ShopState = shopstate;
                    if (model.DealType == (int)BssShop.EDealType.担保)
                    {
                        model.ShopState = BLLShopInfoMethod.GetShopStateByUserState(model);
                    }
                    if (shopstate == "审核成功")
                        model.CreateDate = DateTime.Now;
                    if (shopstate == "审核失败")
                    {
                        #region 记录失败原因
                        if (!string.IsNullOrEmpty(reason))
                        {
                            ShopFailedReasonConfig sfrcModel = new BssShopFailedReasonConfig().GetModel(reason.ToInt32());
                            BssShopFailedReason bssSfr = new BssShopFailedReason();
                            ShopFailedReason chosenSfr = bssSfr.GetModelByOrderId(model.ShopID);
                            if (chosenSfr != null)
                            {
                                if (sfrcModel != null && sfrcModel.ID != chosenSfr.ReasonId)
                                {
                                    chosenSfr.ReasonId = sfrcModel.ID;
                                    chosenSfr.ReasonContent = sfrcModel.ReasonContent;
                                    bssSfr.Update(chosenSfr);
                                }
                            }
                            else
                            {
                                if (sfrcModel != null)
                                {
                                    ShopFailedReason sfrModel = new ShopFailedReason();
                                    sfrModel.CreateTime = DateTime.Now;
                                    sfrModel.OrderId = model.ShopID;
                                    sfrModel.ReasonId = sfrcModel.ID;
                                    sfrModel.ReasonContent = sfrcModel.ReasonContent + remark;
                                    bssSfr.Add(sfrModel);
                                }
                            }
                        }
                        #endregion
                    }

                    bll.Update(model);

                    MsgHelper.Insert("oprationgsuccess", "编辑出售商品成功");

                    return RedirectToAction("USells");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑出售记录", ex, this.GetType().FullName, "USellUpload");
            }

            if (model == null)
                return RedirectToAction("USells");


            return View(model);
        }

        /// <summary>
        /// 用户订单的审核
        /// </summary>
        /// <param name="sID"></param>
        /// <returns></returns>
        [Role("用户订单的审核", IsAuthorize = true)]
        public ActionResult USellUploadView(int? sID)
        {
            Weike.EShop.Shop model = null;
            try
            {
                Weike.EShop.BssShop bll = new Weike.EShop.BssShop();
                model = bll.GetModel(sID ?? 0);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑出售记录", ex, this.GetType().FullName, "USellUpload");
            }

            if (model == null)
                return Content("");

            return View(model);
        }

        /// <summary>
        /// 更新商品标题或描述
        /// </summary>
        /// <param name="sID"></param>
        /// <returns></returns>
        [Role("更新商品标题或描述", IsAuthorize = true)]
        public ActionResult ChangeShopTitle(int sID, string stype, string changeContent)
        {
            string resContent = "";
            try
            {
                Weike.EShop.BssShop bll = new Weike.EShop.BssShop();
                Shop shopModel = bll.GetModel(sID);
                if (shopModel != null)
                {
                    if (!string.IsNullOrEmpty(stype) && stype.ToLower() == "title")
                    {
                        resContent = shopModel.Title;
                    }
                    else
                    {
                        resContent = shopModel.Detail;
                    }
                    if (IsPost)
                    {
                        if (stype.ToLower() == "title")
                        {
                            shopModel.Title = changeContent;
                        }
                        else
                        {
                            shopModel.Detail = changeContent;
                        }
                        bll.Update(shopModel);

                        return Content("");
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新商品标题或描述", ex, this.GetType().FullName, "ChangeShopTitle");
            }
            return Content(resContent);
        }

        [Role("出售订单管理", IsAuthorize = true)]
        public ActionResult USellOrder(int? Page, string Stype, string key, string GameCategoryProperty, string DelState, string GameId,string GameOtherId, string GameShopTypeId, int? DealType, string Btype, string ShopID, string StartTime, string EntTime, string SName, string SpType, string UType, string SGroup)
        {
            StringBuilder where = new StringBuilder("1=1 ");
            StringBuilder shopwhere = new StringBuilder();
            if (string.IsNullOrEmpty(Stype))
                Stype = "s";

            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;

            if (!string.IsNullOrEmpty(key))
            {
                if (Stype == "s")
                    where.Append(string.Format(" and ID like '{0}%'", key.Trim()));
                else
                    where.Append(string.Format(" and UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
            }
            #region  用户类型关联订单类型
            if (!string.IsNullOrEmpty(UType))
            {
                string[] UTypeItem = UType.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); //把用户类型转化为数组
                if (UTypeItem != null && UTypeItem.Length > 0)
                {
                    string[] SpTypeArry = null;
                    if (!string.IsNullOrEmpty(SpType)) //判断是否选择订单类型
                    {
                        SpTypeArry = SpType.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    string StrUType = string.Empty;
                    foreach (string utypestr in UTypeItem)
                    {
                        if (utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.安卓代充 || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.苹果代充
                            || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.手游首充号代充 || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.游戏礼包激活码
                            || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.商家代练 || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.代练工作室
                            || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.点券商城)
                        {
                            continue;
                        }

                        if (utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.担保自主大卖家 || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.增值服务
                            || utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.担保自动铺货)
                        {
                            if (SpTypeArry != null && SpTypeArry.Length > 0)
                            {
                                foreach (string str in SpTypeArry)
                                {
                                    if (str == BssShopping.ShoppingType.出售交易.ToString())
                                    {
                                        if (!StrUType.Contains(BssShopping.ShoppingType.出售交易.ToString()))
                                        {
                                            StrUType = StrUType + BssShopping.ShoppingType.出售交易.ToString() + ",";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!StrUType.Contains(BssShopping.ShoppingType.出售交易.ToString()))
                                {
                                    StrUType = StrUType + BssShopping.ShoppingType.出售交易.ToString() + ",";
                                }
                            }
                        }
                        if (utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.点券卖家直冲)
                        {
                            if (SpTypeArry != null && SpTypeArry.Length > 0)
                            {
                                foreach (string str in SpTypeArry)
                                {
                                    if (str == BssShopping.ShoppingType.点券商城.ToString())
                                    {
                                        if (!StrUType.Contains(BssShopping.ShoppingType.点券商城.ToString()))
                                        {
                                            StrUType = StrUType + BssShopping.ShoppingType.点券商城.ToString() + ",";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!StrUType.Contains(BssShopping.ShoppingType.点券商城.ToString()))
                                {
                                    StrUType = StrUType + BssShopping.ShoppingType.点券商城.ToString() + ",";
                                }
                            }
                        }
                        if (utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.散人商城)
                        {
                            if (SpTypeArry != null && SpTypeArry.Length > 0)
                            {
                                foreach (string str in SpTypeArry)
                                {
                                    if (str == BssShopping.ShoppingType.会员商城.ToString())
                                    {
                                        if (!StrUType.Contains(BssShopping.ShoppingType.会员商城.ToString()))
                                        {
                                            StrUType = StrUType + BssShopping.ShoppingType.会员商城.ToString() + ",";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!StrUType.Contains(BssShopping.ShoppingType.会员商城.ToString()))
                                {
                                    StrUType = StrUType + BssShopping.ShoppingType.会员商城.ToString() + ",";
                                }
                            }
                        }
                        if (utypestr.ToInt32() == (int)BssMerchantsInfo.MerchantType.商家收货)
                        {
                            if (SpTypeArry != null && SpTypeArry.Length > 0)
                            {
                                foreach (string str in SpTypeArry)
                                {
                                    if (str == BssShopping.ShoppingType.商家收货.ToString())
                                    {
                                        if (!StrUType.Contains(BssShopping.ShoppingType.商家收货.ToString()))
                                        {
                                            StrUType = StrUType + BssShopping.ShoppingType.商家收货.ToString() + ",";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (!StrUType.Contains(BssShopping.ShoppingType.商家收货.ToString()))
                                {
                                    StrUType = StrUType + BssShopping.ShoppingType.商家收货.ToString() + ",";
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(StrUType))
                    {
                        return View();
                    }
                    SpType = StrUType;
                }
            }
            #endregion

            if (!string.IsNullOrEmpty(ShopID))
            {
                if (Btype == "id")
                {
                    shopwhere.Append(string.Format(" and OrderShopSnapshot.ShopId='{0}'", ShopID));
                }
                else
                {
                    Members m = new BssMembers().GetModelByName(ShopID);
                    if (m != null)
                    {
                        where.AppendFormat(" and SellerId={0}", m.M_ID);
                    }
                }
            }

            if (!string.IsNullOrEmpty(SName))
                where.Append(string.Format(" and sid ={0} ", SName));
            switch (SGroup)
            {
                case "miansell":
                    where.Append(" and SellerId in(select members.m_id from MemberChargeCredit left join members on members.m_name=MemberChargeCredit.m_name)");
                    break;
                case "zhiding":
                    shopwhere.AppendFormat(" and exists(select 1 from OrderShopSnapshotOtherInfo where OrderShopSnapshotOtherInfo.OrderShopSnapshotId=OrderShopSnapshot.ID and OrderShopSnapshotOtherInfo.ConfigId={0}) ", (int)BssShopOtherInfo.InfoType.指定买家购买);
                    break;
            }

            if (DealType.HasValue)
                shopwhere.Append("  and OrderShopSnapshot.DealType='" + DealType.Value.ToString() + "'");
            if (!string.IsNullOrEmpty(GameCategoryProperty))
            {
                string[] GameCategoryPropertyItem = GameCategoryProperty.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (GameCategoryPropertyItem.Length == 1)
                {
                    shopwhere.Append(" and OrderShopSnapshot.ShopType in (select ID from GameShopType where Property = '" + GameCategoryPropertyItem[0] + "')");
                }
                else if (GameCategoryPropertyItem.Length > 1)
                {
                    string GameCategoryPropertywhere = "";
                    foreach (string item in GameCategoryPropertyItem)
                    {
                        GameCategoryPropertywhere = GameCategoryPropertywhere + "'" + item + "',";
                    }
                    GameCategoryPropertywhere = GameCategoryPropertywhere.TrimEnd(',');
                    shopwhere.AppendFormat(" and OrderShopSnapshot.ShopType in (select ID from GameShopType where Property in({0}))", GameCategoryPropertywhere);
                }
            }

            if (!string.IsNullOrEmpty(DelState) && DelState != "等待支付")
            {
                where.Append(string.Format(" and State='{0}'", DelState));
            }
            else
            {
                where.Append(" and State<>'等待支付'");
            }

            //判断游戏是否为空
            if (!string.IsNullOrEmpty(GameId))
            {
                bool isLike = true;
                string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                if (!string.IsNullOrEmpty(gameGUID))
                {
                    if (isLike)
                        where.AppendFormat(" and GameGuid like '{0}%' ", gameGUID);
                    else
                        where.AppendFormat(" and GameGuid='{0}' ", gameGUID);
                }
                if (!string.IsNullOrEmpty(GameShopTypeId))
                {
                    bool isTypeLike = true;
                    string shopTypeGuid = BLLGame.GetGameShopTypeIdentifyStrByShopTypeId(GameShopTypeId, out isTypeLike);

                    if (!string.IsNullOrEmpty(shopTypeGuid))
                    {
                        if (isTypeLike)
                        {
                            where.AppendFormat(" and ShopTypeGuid like'{0}%' ", shopTypeGuid);
                        }
                        else
                        {
                            where.AppendFormat(" and ShopTypeGuid ='{0}' ", shopTypeGuid);
                        }
                    }
                }
            }

            //判断是否选择订单类型
            if (!string.IsNullOrEmpty(SpType) && SpType.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length > 0)
            {
                if (!SpType.Contains("会员商城") && !SpType.Contains("商城托管") && !SpType.Contains("点券商城") && !SpType.Contains("商家收货")
                    && !SpType.Contains("求购交易") && !SpType.Contains("出售交易") && !SpType.Contains("代收交易") && !SpType.Contains("降价交易"))
                {
                    return View();
                }
                string[] SpTypeItem = SpType.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder whereItem = new StringBuilder();
                if (SpType.Contains("会员商城") || SpType.Contains("商城托管") || SpType.Contains("点券商城"))
                {
                    #region 获取订单类型字符串
                    string Objectstr = "";
                    if (SpTypeItem.Length == 1)
                    {
                        Objectstr = string.Format(" Shopping.ObjectType ='{0}' ", SpTypeItem[0] == "商城托管" ? "会员商城" : SpTypeItem[0]);
                    }
                    else if (SpTypeItem.Length > 1)
                    {
                        string SpTypewhere = "";
                        foreach (string item in SpTypeItem)
                        {
                            string itemwhere = item;
                            if (item == "商城托管")
                            {
                                itemwhere = "会员商城";
                            }
                            if (!SpTypewhere.Contains(itemwhere) && (itemwhere.Contains("会员商城") || itemwhere.Contains("点券商城")))
                            {
                                SpTypewhere = SpTypewhere + "'" + itemwhere + "',";
                            }
                        }
                        SpTypewhere = SpTypewhere.TrimEnd(',');
                        Objectstr = string.Format(" Shopping.ObjectType in ({0}) ", SpTypewhere);
                    }
                    #endregion

                    if (shopwhere.Length > 10)
                    {
                        whereItem.AppendFormat(" or (exists (select 1 from OrderShopSnapshot where OrderShopSnapshot.OrderID=Shopping.ID and OrderShopSnapshot.CreateTime between '{0}' and '{1}' and OrderShopSnapshot.DealType={2} {3}) and {4})", STime, ETime, (int)BssOrderShopSnapshot.DealType.商城, shopwhere.ToString(), Objectstr);
                    }
                    else
                    {
                        whereItem.AppendFormat(" or {0}", Objectstr);
                    }

                    if (SpType.Contains("商城托管") && !SpType.Contains("会员商城") && !SpType.Contains("点券商城"))
                    {
                        whereItem.AppendFormat(" and exists(select 1 from MembersMallOrder where OrderId=ID and len(TgFormGuid)>10 and MembersMallOrder.CreateTime between '{0}' and '{1}')", DateTime.Parse(STime).AddHours(-2).ToString("yyyy-MM-dd HH:mm"), ETime);
                    }
                }
                if (SpType.Contains("商家收货"))
                {
                    if (shopwhere.Length > 10)
                    {
                        whereItem.AppendFormat(" or (exists (select 1 from OrderShopSnapshot where OrderShopSnapshot.OrderID=Shopping.ID and OrderShopSnapshot.DealType={0} and OrderShopSnapshot.CreateTime between '{1}' and '{2}' {3}) and Shopping.ObjectType='商家收货')", (int)BssOrderShopSnapshot.DealType.收货, STime, ETime, shopwhere);
                    }
                    else
                    {
                        whereItem.Append(" or Shopping.ObjectType='商家收货'");
                    }
                }
                if (SpType.Contains("求购交易") || SpType.Contains("出售交易") || SpType.Contains("代收交易") || SpType.Contains("降价交易"))
                {
                    #region 获取订单类型字符串
                    string Objectother = "";
                    if (SpTypeItem.Length == 1)
                    {
                        Objectother = string.Format(" Shopping.ObjectType ='{0}' ", SpTypeItem[0]);
                    }
                    else if (SpTypeItem.Length > 1)
                    {
                        string SpTypewhere = "";
                        foreach (string item in SpTypeItem)
                        {
                            if (!SpTypewhere.Contains(item) && (item == "求购交易" || item == "出售交易" || item == "代收交易" || item == "降价交易"))
                            {
                                SpTypewhere = SpTypewhere + "'" + item + "',";
                            }
                        }
                        SpTypewhere = SpTypewhere.TrimEnd(',');
                        Objectother = string.Format(" Shopping.ObjectType in ({0}) ", SpTypewhere);
                    }
                    #endregion

                    if (shopwhere.ToString().Length > 10)
                    {
                        whereItem.AppendFormat(" or(exists (select 1 from OrderShopSnapshot where Shopping.ID=OrderShopSnapshot.OrderId and OrderShopSnapshot.CreateTime between '{0}' and '{1}' {2}) and {3}) ", STime, ETime, shopwhere.ToString(), Objectother);
                    }
                    else
                    {
                        whereItem.AppendFormat(" or {0} ", Objectother);
                    }
                }
                string strwheritme = whereItem.ToString().TrimStart(" or".ToCharArray());
                if (!string.IsNullOrWhiteSpace(strwheritme))
                {
                    where.Append(" and (" + strwheritme + ")");
                }
            }
            else
            {
                if (shopwhere.Length > 10) //判断是否有查询条件
                {
                    where.AppendFormat(" and exists (select 1 from OrderShopSnapshot where Shopping.ID=OrderShopSnapshot.OrderId and OrderShopSnapshot.CreateTime between '{0}' and '{1}' {2})", STime, ETime, shopwhere.ToString());
                }

                where.Append(" and ObjectType in('出售交易','降价交易','求购交易','代收交易','会员商城','商家收货','点券商城')");
            }

            where.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "processingtime" : "createdate"));
            StringBuilder fileds = new StringBuilder(" ID,ObjectId,ObjectType,Price,State,CreateDate,UserID,ProcessingTime,Count,SID,SQQ ");
            DataPages<Weike.EShop.Shopping> LShop = null;
            try
            {
                LShop = new BssShopping().GetPageRecordByRead(where.ToString(), "CreateDate", 20, Page ?? 1, PagesOrderTypeDesc.降序, fileds.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单管理", ex, this.GetType().FullName, "USellOrder");
            }
            Session["ManageSumMoneySql"] = where.ToString();

            return View(LShop);
        }

        [Role("新订单管理", IsAuthorize = true)]
        public ActionResult USellNewCardOrder(int? Page, string key, string Stype, string DelState, string GameCategory, string GameCDtitle, string ShopID, string StartTime, string EntTime, string SName, string CardType)
        {
            StringBuilder where = new StringBuilder("1=1 ");
            if (string.IsNullOrEmpty(Stype))
                Stype = "s";
            //订单编号和买家
            if (!string.IsNullOrEmpty(key))
            {
                if (Stype == "s")
                    where.Append(string.Format(" and ID like '{0}%'", key.Trim()));
                else
                    where.Append(string.Format(" and UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
            }
            //客服查询
            if (!string.IsNullOrEmpty(SName))
                where.Append(string.Format(" and sid ={0} ", SName));
            //支付状态
            if (!string.IsNullOrEmpty(DelState) && DelState != "等待支付")
            {
                where.Append(string.Format(" and State='{0}'", DelState));
            }
            else
            {
                where.Append(" and State<>'等待支付'");
            }
            //点卡订单
            where.Append("and ObjectType in('点卡商城')");
            #region 查询Card表
            if (!string.IsNullOrEmpty(GameCategory) || !string.IsNullOrEmpty(ShopID) || !string.IsNullOrEmpty(CardType) || !string.IsNullOrEmpty(GameCDtitle))
            {
                string gcwhere = string.Empty;
                gcwhere = string.Format("and ObjectId in (select id from card where 1=1");
                //按游戏查找
                if (!string.IsNullOrEmpty(GameCategory))
                {
                    gcwhere += string.Format("and gameservice='{0}'", GameCategory);
                }
                //按ID编号和商品标题查找
                if (!string.IsNullOrEmpty(ShopID))
                {
                    gcwhere += string.Format("and Csn='{0}'", ShopID);
                }
                //按点卡类型查找
                if (!string.IsNullOrEmpty(CardType))
                {
                    gcwhere += string.Format("and CType='{0}'", CardType);
                }
                if (!string.IsNullOrEmpty(GameCDtitle))
                {
                    gcwhere += string.Format("and CDTitle='{0}'", GameCDtitle);
                }
                gcwhere += ")";
                where.Append(gcwhere);
            }
            #endregion
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where.Append(string.Format("and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "processingtime" : "createdate"));
            StringBuilder fileds = new StringBuilder(" UserID,ObjectId,ObjectType,ID,Count,Price,State,CreateDate,SID,SQQ,ProcessingTime ");
            DataPages<Weike.EShop.Shopping> LShop = null;
            try
            {
                LShop = new BssShopping().GetPageRecord(where.ToString(), "case State when '支付成功' then 1 when '正在发货' then 1 when '发货完成' then 1 when '等待处理' then 2 else 3  end asc,CreateDate", 20, Page ?? 1, PagesOrderTypeDesc.降序, fileds.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单管理", ex, this.GetType().FullName, "USellOrder");
            }

            Session["ManageSumMoneySql"] = where.ToString();

            return View(LShop);
        }

        [Role("点卡订单导出", IsAuthorize = true)]
        public ActionResult USellNewCardOrderImport(string Stype, string key, string DelState, string GameId, string StartTime, string EntTime, string SName)
        {
            StringBuilder sbSql = new StringBuilder();
            sbSql.Append("select sp.id,cd.cdtitle,cd.mianzhi,sp.price,sp.createdate,sp.processingtime,a.a_realname,sp.state,c.ptpaytime,c.supordersuccesstime,c.failedcode,c.failedreason,DATEDIFF(Minute,sp.createdate,sp.processingtime) as diffTime from shopping as sp left join cardorders as c on sp.id=c.shoppingid left join Card as cd on cd.id=sp.objectid left join admins as a on a.a_id=sp.sid where 1=1 ");
            if (!string.IsNullOrEmpty(key))
            {
                if (Stype == "s")
                    sbSql.Append(string.Format(" and sp.id like '{0}%'", key.Trim()));
                else
                    sbSql.Append(string.Format(" and sp.UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
            }

            if (!string.IsNullOrEmpty(SName))
                sbSql.Append(string.Format(" and sp.sid ={0}", SName));
            if (!string.IsNullOrEmpty(DelState))
            {
                sbSql.Append(string.Format(" and sp.State='{0}'", DelState));
            }
            sbSql.Append(" and sp.ObjectType='点卡商城'");
            string gcwhere = string.Empty;
            if (!string.IsNullOrEmpty(GameId))
            {
                string GK = GameId.Substring(GameId.Length - 1).ToUpper().Trim();
                string GC = GameId.Substring(0, GameId.Length - 1);
                switch (GK)
                {
                    case "G":
                        sbSql.AppendFormat(" and cd.GameService='{0}'", GC);
                        break;
                    default:
                        break;
                }
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-7).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            sbSql.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "sp.processingtime" : "sp.createdate"));
            sbSql.Append(" order by sp.createdate asc");
            try
            {
                DataSet ds = new BssShopping().GetListQuery(sbSql.ToString());

                if (ds != null && ds.Tables[0] != null)
                {
                    HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                    ISheet sheet = hssfworkbook.CreateSheet("点卡订单列表");
                    NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "DD373 Team";
                    NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                    si.Subject = "http://www.dd373.com/";
                    hssfworkbook.DocumentSummaryInformation = dsi;
                    hssfworkbook.SummaryInformation = si;

                    IRow rowtop = sheet.CreateRow(0);

                    IFont font = hssfworkbook.CreateFont();
                    font.FontName = "宋体";
                    font.FontHeightInPoints = 11;

                    ICellStyle style = hssfworkbook.CreateCellStyle();
                    style.SetFont(font);

                    //生成标题
                    string[] tits = new string[] { "序号", "订单编号", "游戏", "面值", "金额", "购买时间", "处理时间", "订单状态", "取消原因", "处理客服", "时间差", "类型" };
                    for (int i = 0; i < tits.Length; i++)
                    {
                        if (i == 1 || i == 5 || i == 6)
                        {
                            sheet.SetColumnWidth(i, 18 * 400);
                        }
                        else
                        {
                            sheet.SetColumnWidth(i, 18 * 200);
                        }

                        ICell cell = rowtop.CreateCell(i);
                        cell.SetCellValue(tits[i]);
                        cell.CellStyle = style;
                    }

                    string type = "";
                    DataTable dt = ds.Tables[0];
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        type = "";
                        int diffTime = dt.Rows[i]["diffTime"].ToString().ToInt32();
                        if (diffTime <= 5)
                            type = "A";
                        else if (diffTime > 5 && diffTime <= 10)
                            type = "B";
                        else if (diffTime > 10 && diffTime <= 20)
                            type = "C";
                        else
                            type = "D";

                        IRow row = sheet.CreateRow(i + 1);

                        //序号
                        ICell cell = row.CreateCell(0);
                        cell.SetCellValue(i + 1);
                        cell.CellStyle = style;

                        //订单编号
                        cell = row.CreateCell(1);
                        cell.SetCellValue(dt.Rows[i]["id"].ToString());
                        cell.CellStyle = style;

                        //游戏
                        cell = row.CreateCell(2);
                        cell.SetCellValue(dt.Rows[i]["cdtitle"].ToString());
                        cell.CellStyle = style;

                        //面值
                        cell = row.CreateCell(3);
                        cell.SetCellValue(dt.Rows[i]["mianzhi"].ToString());
                        cell.CellStyle = style;

                        //金额
                        cell = row.CreateCell(4);
                        cell.SetCellValue(dt.Rows[i]["price"].ToString());
                        cell.CellStyle = style;

                        //购买时间
                        cell = row.CreateCell(5);
                        cell.SetCellValue(DateTime.Parse(dt.Rows[i]["CreateDate"].ToString()).ToString("yyyy-MM-dd HH:mm"));
                        cell.CellStyle = style;

                        //处理时间
                        cell = row.CreateCell(6);
                        cell.SetCellValue(DateTime.Parse(dt.Rows[i]["ProcessingTime"].ToString()).ToString("yyyy-MM-dd HH:mm"));
                        cell.CellStyle = style;

                        //订单状态
                        cell = row.CreateCell(7);
                        cell.SetCellValue(dt.Rows[i]["State"].ToString());
                        cell.CellStyle = style;

                        //取消原因
                        cell = row.CreateCell(8);
                        cell.SetCellValue(dt.Rows[i]["failedreason"].ToString());
                        cell.CellStyle = style;

                        //处理客服
                        cell = row.CreateCell(9);
                        cell.SetCellValue(dt.Rows[i]["a_realname"].ToString());
                        cell.CellStyle = style;

                        //时间差
                        cell = row.CreateCell(10);
                        cell.SetCellValue(dt.Rows[i]["diffTime"].ToString());
                        cell.CellStyle = style;

                        //类型
                        cell = row.CreateCell(11);
                        cell.SetCellValue(type);
                        cell.CellStyle = style;
                    }

                    string fileName = string.Format("点卡订单_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                    string excelFileName = string.Format("{0}.xls", fileName);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        hssfworkbook.Write(ms);

                        FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                        if (!Directory.Exists(FI.DirectoryName))
                            Directory.CreateDirectory(FI.DirectoryName);
                        FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                        ms.WriteTo(fileUpload);
                        fileUpload.Close();
                        fileUpload = null;
                    }

                    //Excel文件路径
                    string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                    //Excel的Zip文件路径
                    string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                    //Excel的Zip文件下载路径
                    string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                    //将文件压缩
                    string errMsg = "";
                    bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                    if (retZip)
                    {
                        //压缩成功删除文件
                        FileInfo fi = new FileInfo(excelFile);
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }

                    return Redirect(excelZipPath);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("点卡订单导出", ex, this.GetType().FullName, "USellOrderImport");
            }
            return RedirectToAction("USellNewCardOrder");
        }

        [Role("获取订单总额", IsAuthorize = true)]
        public ActionResult GetOrderSumMoney()
        {
            try
            {
                if (Session["ManageSumMoneySql"] != null)
                {
                    string sqlWhere = Session["ManageSumMoneySql"].ToString();
                    if (!string.IsNullOrWhiteSpace(sqlWhere))
                    {
                        string TotalPrice = new AccShopping().GetToatlPrice(sqlWhere);
                        string TotalCount = new AccShopping().GetToatlCount(sqlWhere);
                        string TotalINPrice = new AccShopping().GetToatlINPrice(sqlWhere);

                        if (!string.IsNullOrWhiteSpace(TotalPrice))
                        {
                            return Content(string.Format("{0}元/{1}个/{2}", TotalPrice, TotalCount, TotalINPrice));
                        }
                    }
                    else
                    {
                        return Content("nosession");
                    }
                }
                else
                {
                    return Content("nosession");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取订单总额", ex, this.GetType().FullName, "GetOrderSumMoney");
            }
            return Content("");
        }

        [Role("出售订单转单", IsAuthorize = true)]
        public ActionResult USellOrderZD(string Stype, string key, string GameCategoryProperty, string DelState, string GameId,string GameOtherId, string GameShopTypeId, int? DealType, string Btype, string ShopID, string StartTime, string EntTime, string SName, string SpType, string ZdKefu)
        {
            StringBuilder where = new StringBuilder("1=1 ");
            StringBuilder shopwhere = new StringBuilder(" 1=1 ");
            if (string.IsNullOrEmpty(Stype))
                Stype = "s";

            if (!string.IsNullOrEmpty(key))
            {
                if (Stype == "s")
                    where.Append(string.Format(" and ID like '{0}%'", key.Trim()));
                else
                    where.Append(string.Format(" and UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
            }
            if (!string.IsNullOrEmpty(ShopID))
            {
                if (Btype == "id")
                {
                    shopwhere.Append(string.Format(" and ShopId='{0}'", ShopID));
                }
                else
                {
                    Members m = new BssMembers().GetModelByName(ShopID);
                    if (m != null)
                        shopwhere.Append(string.Format(" and publicuser={0}", m.M_ID));
                }
            }

            if (!string.IsNullOrEmpty(SName))
                where.Append(string.Format(" and sid ={0} ", SName));
            if (!string.IsNullOrEmpty(SpType))
                where.Append(string.Format(" and objecttype ='{0}' ", SpType));

            if (DealType.HasValue)
                shopwhere.Append("  and DealType='" + DealType.Value.ToString() + "'");
            else
            {
                return RedirectToAction("USellOrder", new { t = "s" });
            }
            if (!string.IsNullOrEmpty(GameCategoryProperty))
                shopwhere.Append(" and ShopType in (select ID from GameShopType where Property = '" + GameCategoryProperty + "')");

            if (!string.IsNullOrEmpty(DelState) && DelState != "等待支付")
            {
                where.Append(string.Format(" and State='{0}'", DelState));
            }
            else
            {
                where.Append(" and State not in('等待支付','交易成功','交易取消','部分完成')");
            }

            where.Append("and ObjectType in('出售交易','代收交易','降价交易','求购交易')");

            string gcwhere = string.Empty;

            if (!string.IsNullOrEmpty(GameId))
            {
                bool isLike = true;
                string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                if (!string.IsNullOrEmpty(gameGUID))
                {
                    if (isLike)
                        gcwhere += string.Format(" and GameGUID like '{0}%' ", gameGUID);
                    else
                        gcwhere += string.Format(" and GameGUID='{0}' ", gameGUID);
                }

                if (!string.IsNullOrEmpty(GameShopTypeId))
                {
                    GameShopType gameShopTypeModel = new BssGameShopType().GetModel(GameShopTypeId);
                    if (gameShopTypeModel != null)
                    {
                        if (gameShopTypeModel.CurrentLevelType == (int)Weike.EShop.BssGameRoute.LevelType.商品子类型)
                        {
                            gcwhere += string.Format(" and ShopType = '{0}' and ShopTypeCate = '{1}'", gameShopTypeModel.ParentId, gameShopTypeModel.ID);
                        }
                        else
                        {
                            gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ID);
                        }
                    }
                    else
                    {
                        return RedirectToAction("USellOrder", new { t = "s" });
                    }
                }
            }
            if (!string.IsNullOrEmpty(gcwhere))
                shopwhere.Append(gcwhere);

            if (shopwhere.Length > 10)
                where.AppendFormat(" and exists (select 1  from OrderShopSnapshot where {0} and OrderShopSnapshot.OrderID=Shopping.ID) ", shopwhere.ToString());

            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where.Append(string.Format("and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "processingtime" : "createdate"));

            try
            {
                int kefuId = ZdKefu.ToInt32();
                BssShopping bssSp = new BssShopping();
                List<Shopping> spList = bssSp.GetModelList(string.Format(" {0} order by case State when '支付成功' then 1 when '正在发货' then 1 when '发货完成' then 1 when '等待处理' then 2 else 3  end asc,CreateDate desc", where));
                BssShoppingAssembly bssSpa = new Weike.EShop.BssShoppingAssembly();
                ShoppingAssembly saModel = null;
                foreach (Shopping sp in spList)
                {
                    if (sp.State != BssShopping.ShoppingState.交易成功.ToString() && sp.State != BssShopping.ShoppingState.部分完成.ToString() && sp.State != BssShopping.ShoppingState.交易取消.ToString() && sp.State != BssShopping.ShoppingState.等待支付.ToString())
                        saModel = bssSpa.GetNoModelBySpId(sp.ID);
                    if (saModel != null)
                    {
                        Weike.CMS.ServiceQQ sqqModel = new Weike.CMS.BssServiceQQ().GetOnlineModelBySid(kefuId);
                        if (sqqModel != null)
                        {
                            sp.SQQ = sqqModel.S_QQ;
                        }
                        sp.SID = kefuId;

                        #region 插入客服订单流水
                        ShoppingAssembly nsaModel = new ShoppingAssembly();
                        nsaModel.BuyerName = saModel.BuyerName;
                        nsaModel.CreateTime = sp.CreateDate.Value;
                        nsaModel.GameAccount = saModel.GameAccount;
                        nsaModel.ProcessTime = Weike.Common.Globals.MinDateValue;
                        nsaModel.Remark = BssShoppingAssembly.SourceType.客服转单.ToString();
                        nsaModel.ResType = BssShoppingAssembly.ResType.未处理.ToString();
                        nsaModel.SellerName = saModel.SellerName;
                        nsaModel.ShopId = saModel.ShopId;
                        nsaModel.ShoppingId = saModel.ShoppingId;
                        nsaModel.SID = kefuId;
                        nsaModel.SortNo = saModel.SortNo + 1;
                        nsaModel.SourceType = BssShoppingAssembly.SourceType.客服转单.ToString();
                        nsaModel.ObjectType = saModel.ObjectType;
                        #endregion

                        //更新原来流水状态
                        saModel.ProcessTime = DateTime.Now;
                        saModel.Remark = saModel.Remark + "|" + BssShoppingAssembly.SourceType.客服转单.ToString();
                        saModel.ResType = BssShoppingAssembly.ResType.转单处理.ToString();

                        bssSpa.AsyncUpdateAssembly(saModel, nsaModel, sp);

                        //插入订单备注记录
                        BssShoppingRemarkInfo.InsertShoppingRemarkInfo(sp.ID, string.Format("管理[{0}]批量转单", BLLAdmins.GetCurrentAdminUserInfo().A_RealName));
                    }
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单转单", ex, this.GetType().FullName, "USellOrderZD");
            }
            return RedirectToAction("USellOrder", new { t = "s" });
        }


        [Role("出售订单导出", IsAuthorize = true)]
        public ActionResult USellOrderImport(string Stype, string key, string GameCategoryProperty, string DelState, string GameId, string GameOtherId, string GameShopTypeId, int? DealType, string Btype, string ShopID, string StartTime, string EntTime, string SName, string SpType)
        {
            try
            {
                if (!SpType.Contains(BssShopping.ShoppingType.会员商城.ToString()))
                {
                    #region 非商城类订单导出
                    List<ShoppingEvaluationType> setList = null;
                    if (DealType.HasValue)
                    {
                        setList = new BssShoppingEvaluationType().GetModelList(string.Format(" BuyType='{0}' and DealType={1} and Enalbed=1 order by SortNo asc", BssShoppingEvaluationType.BuyType.买家.ToString(), DealType.Value.ToString()));
                    }
                    StringBuilder sbSql = new StringBuilder();
                    sbSql.Append("select sp.id,s.shopid,s.dealtype,sp.ObjectType,s.GameType,s.ShopType,tp.GameShopTypeName as cname,tp.Property as pname,sp.price,sp.createdate,sa.ProcessTime as sprocessingtime,sp.processingtime,a.a_realname,sp.state,DATEDIFF(Minute,sp.createdate,sp.processingtime) as diffTime,DATEDIFF(Minute,sa.ProcessTime,sp.processingtime) as sdiffTime,sfrc.BuyType,sfrc.ReasonContent,bm.m_id as buyer,sm.m_id as seller ");
                    if (setList != null)
                    {
                        foreach (ShoppingEvaluationType set in setList)
                        {
                            sbSql.Append(string.Format(" ,se{0}.EvaluationValue as sev{0}", set.ID));
                        }
                    }

                    sbSql.Append(" from shopping as sp left join shop as s on sp.objectid=s.id left join GameShopType as tp on tp.ID=s.ShopType left join admins as a on a.a_id=sp.sid left join ShopFailedReason as sfr on sp.id=sfr.OrderId left join ShopFailedReasonConfig as sfrc on sfr.ReasonId=sfrc.ID left join members as bm on bm.m_id=sp.userid left join members as sm on sm.m_id=s.PublicUser left join ShoppingAssembly as sa on sp.id=sa.ShoppingId and sa.ResType='开始处理'  ");

                    if (setList != null)
                    {
                        foreach (ShoppingEvaluationType set in setList)
                        {
                            sbSql.Append(string.Format(" left join ShoppingEvaluation as se{0} on se{0}.ShoppingId=sp.id and se{0}.EvaluationTypeId={0}", set.ID));
                        }
                    }

                    sbSql.Append(" where 1=1 ");
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (Stype == "s")
                            sbSql.Append(string.Format(" and sp.id like '{0}%'", key.Trim()));
                        else
                            sbSql.Append(string.Format(" and sp.UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
                    }

                    if (string.IsNullOrEmpty(Stype))
                        Stype = "s";
                    if (!string.IsNullOrEmpty(ShopID))
                    {
                        if (Btype == "id")
                            sbSql.Append(string.Format(" and s.shopid like '{0}%'", ShopID));
                        else
                        {
                            Members m = new BssMembers().GetModelByName(ShopID);
                            if (m != null)
                                sbSql.Append(string.Format(" and s.publicuser ={0}", m.M_ID));
                        }
                    }

                    if (!string.IsNullOrEmpty(SName))
                        sbSql.Append(string.Format(" and sp.sid ={0}", SName));

                    if (DealType.HasValue)
                        sbSql.Append(" and s.dealtype='" + DealType.Value.ToString() + "'");
                    if (!string.IsNullOrEmpty(GameCategoryProperty))
                        sbSql.Append(" and s.ShopType in (select ID from GameShopType where Property = '" + GameCategoryProperty + "')");
                    if (!string.IsNullOrEmpty(DelState))
                    {
                        sbSql.Append(string.Format(" and sp.State='{0}'", DelState));
                    }

                    sbSql.Append(" and sp.ObjectType in('出售交易','降价交易','求购交易','代收交易')");

                    string gcwhere = string.Empty;
                    if (!string.IsNullOrWhiteSpace(GameId))
                    {
                        bool isLike = true;
                        string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                        if (!string.IsNullOrEmpty(gameGUID))
                        {
                            if (isLike)
                                sbSql.AppendFormat(" and s.GameGuId like '{0}%' ", gameGUID);
                            else
                                sbSql.AppendFormat(" and s.GameGuId='{0}' ", gameGUID);
                        }
                        if (!string.IsNullOrEmpty(GameShopTypeId))
                        {
                            sbSql.AppendFormat(" and s.ShopType='{0}'", GameShopTypeId);
                        }
                    }
                    string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                    string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                    sbSql.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "sp.processingtime" : "sp.createdate"));
                    sbSql.Append(" order by sp.createdate asc");

                    DataSet ds = new BssShopping().GetListQuery(sbSql.ToString());

                    if (ds != null && ds.Tables[0] != null)
                    {
                        HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                        ISheet sheet = hssfworkbook.CreateSheet("订单列表");
                        NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                        dsi.Company = "DD373 Team";
                        NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                        si.Subject = "http://www.dd373.com/";
                        hssfworkbook.DocumentSummaryInformation = dsi;
                        hssfworkbook.SummaryInformation = si;

                        IRow rowtop = sheet.CreateRow(0);

                        IFont font = hssfworkbook.CreateFont();
                        font.FontName = "宋体";
                        font.FontHeightInPoints = 11;

                        ICellStyle style = hssfworkbook.CreateCellStyle();
                        style.SetFont(font);

                        //生成标题
                        string[] tits = new string[] { "序号", "订单编号", "商品类型", "游戏区服", "分类", "属性", "订单类型", "金额", "购买时间", "开始处理时间", "处理时间", "订单状态", "卖/买家原因", "取消原因", "处理客服", "类型", "时间差", "开始时间差", "买家DD用户名", "卖家DD用户名" };
                        for (int i = 0; i < tits.Length; i++)
                        {
                            if (i == 1 || i == 10 || i == 11 || i == 12)
                            {
                                sheet.SetColumnWidth(i, 18 * 400);
                            }
                            else
                            {
                                sheet.SetColumnWidth(i, 18 * 200);
                            }

                            ICell cell = rowtop.CreateCell(i);
                            cell.SetCellValue(tits[i]);
                            cell.CellStyle = style;
                        }

                        if (setList != null)
                        {
                            for (int i = 0; i < setList.Count; i++)
                            {
                                ShoppingEvaluationType setModel = setList[i + 22];
                                sheet.SetColumnWidth(i + 22, 18 * 200);
                                ICell cell = rowtop.CreateCell(i + 22);
                                cell.SetCellValue((setModel.TypeName == "" ? "附加评论" : setModel.TypeName) + "（买家评价）");
                                cell.CellStyle = style;
                            }
                        }


                        string type = "";

                        DataTable dt = ds.Tables[0];
                        BLLGame bllGame = new BLLGame();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string gameType = dt.Rows[i]["GameType"].ToString();
                            string shopType = dt.Rows[i]["ShopType"].ToString();
                            GameInfoModel InfoModel = bllGame.GetGameInfoModel(gameType, shopType, false);
                            string gameInfo = bllGame.GetGameInfoModelByOtherName(InfoModel);
                            type = "";
                            int diffTime = dt.Rows[i]["diffTime"].ToString().ToInt32();
                            if (diffTime <= 5)
                                type = "A";
                            else if (diffTime > 5 && diffTime <= 10)
                                type = "B";
                            else if (diffTime > 10 && diffTime <= 20)
                                type = "C";
                            else
                                type = "D";


                            IRow row = sheet.CreateRow(i + 1);

                            //序号
                            ICell cell = row.CreateCell(0);
                            cell.SetCellValue(i + 1);
                            cell.CellStyle = style;

                            //订单编号
                            cell = row.CreateCell(1);
                            cell.SetCellValue(dt.Rows[i]["id"].ToString());
                            cell.CellStyle = style;

                            //商品类型
                            cell = row.CreateCell(2);
                            cell.SetCellValue(((BssShop.EDealType)dt.Rows[i]["DealType"].ToString().ToInt32()).ToString());
                            cell.CellStyle = style;

                            if (dt.Rows[i]["DealType"].ToString() != "3")
                            {

                                //游戏
                                cell = row.CreateCell(3);
                                cell.SetCellValue(gameInfo);
                                cell.CellStyle = style;

                                //分类
                                cell = row.CreateCell(4);
                                cell.SetCellValue(dt.Rows[i]["cname"].ToString());
                                cell.CellStyle = style;

                                //属性
                                cell = row.CreateCell(5);
                                cell.SetCellValue(dt.Rows[i]["pname"].ToString());
                                cell.CellStyle = style;
                            }
                            else
                            {
                                //游戏
                                cell = row.CreateCell(3);
                                cell.SetCellValue(gameInfo);
                                cell.CellStyle = style;

                                //分类
                                cell = row.CreateCell(4);
                                cell.SetCellValue("");
                                cell.CellStyle = style;

                                //属性
                                cell = row.CreateCell(5);
                                cell.SetCellValue("");
                                cell.CellStyle = style;
                            }

                            //订单类型
                            cell = row.CreateCell(6);
                            cell.SetCellValue(dt.Rows[i]["objecttype"].ToString());
                            cell.CellStyle = style;

                            //金额
                            cell = row.CreateCell(7);
                            cell.SetCellValue(dt.Rows[i]["Price"].ToString());
                            cell.CellStyle = style;

                            //购买时间
                            cell = row.CreateCell(8);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["CreateDate"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            if (!string.IsNullOrEmpty(dt.Rows[i]["sprocessingtime"].ToString()))
                            {
                                //开始处理时间
                                cell = row.CreateCell(9);
                                cell.SetCellValue(DateTime.Parse(dt.Rows[i]["sprocessingtime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                                cell.CellStyle = style;
                            }
                            else
                            {
                                //开始处理时间
                                cell = row.CreateCell(9);
                                cell.SetCellValue("");
                                cell.CellStyle = style;
                            }

                            //处理时间
                            cell = row.CreateCell(10);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["ProcessingTime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //订单状态
                            cell = row.CreateCell(11);
                            cell.SetCellValue(dt.Rows[i]["State"].ToString());
                            cell.CellStyle = style;

                            //卖/买家原因
                            cell = row.CreateCell(12);
                            cell.SetCellValue(dt.Rows[i]["BuyType"].ToString());
                            cell.CellStyle = style;

                            //取消原因
                            cell = row.CreateCell(13);
                            cell.SetCellValue(dt.Rows[i]["ReasonContent"].ToString());
                            cell.CellStyle = style;

                            //处理客服
                            cell = row.CreateCell(14);
                            cell.SetCellValue(dt.Rows[i]["a_realname"].ToString());
                            cell.CellStyle = style;

                            //类型
                            cell = row.CreateCell(15);
                            cell.SetCellValue(type);
                            cell.CellStyle = style;

                            //时间差
                            cell = row.CreateCell(16);
                            cell.SetCellValue(dt.Rows[i]["diffTime"].ToString());
                            cell.CellStyle = style;

                            //开始时间差
                            cell = row.CreateCell(17);
                            cell.SetCellValue(dt.Rows[i]["sdiffTime"].ToString());
                            cell.CellStyle = style;

                            //买家DD用户名
                            cell = row.CreateCell(18);
                            cell.SetCellValue(dt.Rows[i]["buyer"].ToString());
                            cell.CellStyle = style;

                            //卖家DD用户名
                            cell = row.CreateCell(19);
                            cell.SetCellValue(dt.Rows[i]["seller"].ToString());
                            cell.CellStyle = style;

                            if (setList != null)
                            {
                                for (int sei = 0; sei < setList.Count; sei++)
                                {
                                    if (string.IsNullOrEmpty(dt.Rows[i]["sev" + setList[sei].ID + ""].ToString()))
                                    {
                                        //评价有关
                                        cell = row.CreateCell(20 + sei);
                                        cell.SetCellValue("");
                                        cell.CellStyle = style;
                                    }
                                    else
                                    {
                                        //评价有关
                                        cell = row.CreateCell(20 + sei);
                                        cell.SetCellValue(dt.Rows[i]["sev" + setList[sei].ID + ""].ToString());
                                        cell.CellStyle = style;
                                    }
                                }
                            }
                        }

                        string fileName = string.Format("订单列表_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                        string excelFileName = string.Format("{0}.xls", fileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            hssfworkbook.Write(ms);

                            FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                            if (!Directory.Exists(FI.DirectoryName))
                                Directory.CreateDirectory(FI.DirectoryName);
                            FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                            ms.WriteTo(fileUpload);
                            fileUpload.Close();
                            fileUpload = null;
                        }

                        //Excel文件路径
                        string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                        //Excel的Zip文件路径
                        string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                        //Excel的Zip文件下载路径
                        string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                        //将文件压缩
                        string errMsg = "";
                        bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                        if (retZip)
                        {
                            //压缩成功删除文件
                            FileInfo fi = new FileInfo(excelFile);
                            if (fi.Exists)
                            {
                                fi.Delete();
                            }
                        }

                        return Redirect(excelZipPath);
                    }
                    #endregion
                }
                else
                {
                    #region 商城类订单导出
                    StringBuilder sbSql = new StringBuilder();
                    sbSql.Append("select sp.id,s.GameType,s.ShopType,tp.GameShopTypeName as cname,mo.GamePerQuantity,mo.NumUnit,mo.SinglePrice,sp.price,sp.createdate,sp.processingtime,a.a_realname,sp.state,DATEDIFF(Minute,sp.createdate,sp.processingtime) as diffTime,bm.m_id as buyer,sm.m_id as seller from shopping as sp left join MembersMallShop as s on sp.objectid=s.id left join MembersMallOrder as mo on sp.id=mo.orderid left join GameShopType as tp on tp.ID=s.ShopType left join admins as a on a.a_id=sp.sid left join members as bm on bm.m_id=sp.userid left join members as sm on sm.m_id=s.M_ID");
                    sbSql.Append(" where 1=1 and sp.objecttype ='会员商城' ");
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (Stype == "s")
                            sbSql.Append(string.Format(" and sp.id like '{0}%'", key.Trim()));
                        else
                        {
                            Members m = new BssMembers().GetModelByName(key.Trim());
                            if (m != null)
                                sbSql.Append(string.Format(" and sp.UserID ={0}", m.M_ID));
                        }
                    }
                    if (Btype == "p" && !string.IsNullOrEmpty(ShopID))
                    {
                        Members m = new BssMembers().GetModelByName(ShopID);
                        if (m != null)
                            sbSql.Append(string.Format(" and s.M_ID ={0}", m.M_ID));
                    }

                    if (!string.IsNullOrEmpty(SName))
                        sbSql.Append(string.Format(" and sp.sid ={0}", SName));

                    if (!string.IsNullOrEmpty(DelState))
                    {
                        sbSql.Append(string.Format(" and sp.State='{0}'", DelState));
                    }

                    string gcwhere = string.Empty;
                    if (!string.IsNullOrWhiteSpace(GameId))
                    {
                        bool isLike = true;
                        string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                        if (!string.IsNullOrEmpty(gameGUID))
                        {
                            if (isLike)
                                sbSql.AppendFormat(" and s.GameGuId like '{0}%' ", gameGUID);
                            else
                                sbSql.AppendFormat(" and s.GameGuId='{0}' ", gameGUID);
                        }

                        if (!string.IsNullOrEmpty(GameShopTypeId))
                        {
                            sbSql.AppendFormat(" and s.ShopType='{0}'", GameShopTypeId);
                        }
                    }
                    string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                    string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                    sbSql.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "sp.processingtime" : "sp.createdate"));
                    sbSql.Append(" order by sp.createdate asc");

                    DataSet ds = new BssShopping().GetListQuery(sbSql.ToString());

                    if (ds != null && ds.Tables[0] != null)
                    {
                        HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                        ISheet sheet = hssfworkbook.CreateSheet("商城订单列表");
                        NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                        dsi.Company = "DD373 Team";
                        NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                        si.Subject = "http://www.dd373.com/";
                        hssfworkbook.DocumentSummaryInformation = dsi;
                        hssfworkbook.SummaryInformation = si;

                        IRow rowtop = sheet.CreateRow(0);

                        IFont font = hssfworkbook.CreateFont();
                        font.FontName = "宋体";
                        font.FontHeightInPoints = 11;

                        ICellStyle style = hssfworkbook.CreateCellStyle();
                        style.SetFont(font);

                        //生成标题
                        string[] tits = new string[] { "序号", "订单编号", "游戏区服", "分类", "订单数量", "游戏币单价", "订单总价", "购买时间", "处理时间", "订单状态", "处理客服", "时间差", "买家DD用户名", "卖家DD用户名" };
                        for (int i = 0; i < tits.Length; i++)
                        {
                            if (i == 1 || i == 6 || i == 7 || i == 9 || i == 10)
                            {
                                sheet.SetColumnWidth(i, 18 * 400);
                            }
                            else
                            {
                                sheet.SetColumnWidth(i, 18 * 200);
                            }

                            ICell cell = rowtop.CreateCell(i);
                            cell.SetCellValue(tits[i]);
                            cell.CellStyle = style;
                        }

                        DataTable dt = ds.Tables[0];
                        BLLGame bllGame = new BLLGame();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string gameType = dt.Rows[i]["GameType"].ToString();
                            string shopType = dt.Rows[i]["ShopType"].ToString();
                            GameInfoModel InfoModel = bllGame.GetGameInfoModel(gameType, shopType, false);
                            string gameInfo = bllGame.GetGameInfoModelByOtherName(InfoModel);
                            IRow row = sheet.CreateRow(i + 1);

                            //序号
                            ICell cell = row.CreateCell(0);
                            cell.SetCellValue(i + 1);
                            cell.CellStyle = style;

                            //订单编号
                            cell = row.CreateCell(1);
                            cell.SetCellValue(dt.Rows[i]["id"].ToString());
                            cell.CellStyle = style;

                            //游戏
                            cell = row.CreateCell(2);
                            cell.SetCellValue(gameInfo);
                            cell.CellStyle = style;

                            //分类
                            cell = row.CreateCell(3);
                            cell.SetCellValue(dt.Rows[i]["cname"].ToString());
                            cell.CellStyle = style;

                            //订单数量
                            cell = row.CreateCell(4);
                            cell.SetCellValue(dt.Rows[i]["GamePerQuantity"].ToString() + dt.Rows[i]["NumUnit"].ToString());
                            cell.CellStyle = style;

                            //游戏币单价
                            cell = row.CreateCell(5);
                            cell.SetCellValue(dt.Rows[i]["SinglePrice"].ToString() + "元/" + dt.Rows[i]["NumUnit"].ToString());
                            cell.CellStyle = style;

                            //订单总价
                            cell = row.CreateCell(6);
                            cell.SetCellValue(dt.Rows[i]["price"].ToString());
                            cell.CellStyle = style;

                            //购买时间
                            cell = row.CreateCell(7);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["CreateDate"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //处理时间
                            cell = row.CreateCell(8);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["ProcessingTime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //订单状态
                            cell = row.CreateCell(9);
                            cell.SetCellValue(dt.Rows[i]["State"].ToString());
                            cell.CellStyle = style;

                            //处理客服
                            cell = row.CreateCell(10);
                            cell.SetCellValue(dt.Rows[i]["a_realname"].ToString());
                            cell.CellStyle = style;

                            //时间差
                            cell = row.CreateCell(11);
                            cell.SetCellValue(dt.Rows[i]["diffTime"].ToString());
                            cell.CellStyle = style;

                            //买家DD用户名
                            cell = row.CreateCell(12);
                            cell.SetCellValue(dt.Rows[i]["buyer"].ToString());
                            cell.CellStyle = style;

                            //卖家DD用户名
                            cell = row.CreateCell(13);
                            cell.SetCellValue(dt.Rows[i]["seller"].ToString());
                            cell.CellStyle = style;
                        }

                        string fileName = string.Format("商城订单_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                        string excelFileName = string.Format("{0}.xls", fileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            hssfworkbook.Write(ms);

                            FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                            if (!Directory.Exists(FI.DirectoryName))
                                Directory.CreateDirectory(FI.DirectoryName);
                            FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                            ms.WriteTo(fileUpload);
                            fileUpload.Close();
                            fileUpload = null;
                        }

                        //Excel文件路径
                        string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                        //Excel的Zip文件路径
                        string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                        //Excel的Zip文件下载路径
                        string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                        //将文件压缩
                        string errMsg = "";
                        bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                        if (retZip)
                        {
                            //压缩成功删除文件
                            FileInfo fi = new FileInfo(excelFile);
                            if (fi.Exists)
                            {
                                fi.Delete();
                            }
                        }

                        return Redirect(excelZipPath);
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单导出", ex, this.GetType().FullName, "USellOrderImport");
            }
            return RedirectToAction("USellOrder", new { key = "test" });
        }

        [Role("订单详情导出", IsAuthorize = true)]
        public ActionResult OrderDetailImport(string Stype, string key, string GameCategoryProperty, string DelState, string GameId, string GameOtherId, string GameShopTypeId, int? DealType, string Btype, string ShopID, string StartTime, string EntTime, string SName, string SpType)
        {
            try
            {
                if (!SpType.Contains(BssShopping.ShoppingType.会员商城.ToString()))
                {
                    #region 非商城类订单

                    List<ShoppingEvaluationType> setList = null;
                    if (DealType.HasValue)
                    {
                        setList = new BssShoppingEvaluationType().GetModelList(string.Format(" BuyType='{0}' and DealType={1} and Enalbed=1 order by SortNo asc", BssShoppingEvaluationType.BuyType.买家.ToString(), DealType.Value.ToString()));
                    }
                    StringBuilder sbSql = new StringBuilder();
                    sbSql.Append("select sp.id,s.shopid,s.dealtype,sp.ObjectType,s.GameType,s.ShopType, tp.GameShopTypeName as cname,tp.Property as pname,sp.price,sp.createdate,sa.ProcessTime as sprocessingtime,sp.processingtime,a.a_realname,sp.state,DATEDIFF(Minute,sp.createdate,sp.processingtime) as diffTime,DATEDIFF(Minute,sa.ProcessTime,sp.processingtime) as sdiffTime,sfrc.BuyType,sfrc.ReasonContent,bm.m_name as buyer,sm.m_name as seller,si.PayAccountMoney,si.PayDyqMoney,si.PayBankMoney,si.BankType,si.BankOrderId,si.PayFwMoney,si.ReturnDyqMoney,si.SxfMoney,si.CxSxfMoney,si.TgSxfMoney ");
                    if (setList != null)
                    {
                        foreach (ShoppingEvaluationType set in setList)
                        {
                            sbSql.Append(string.Format(" ,se{0}.EvaluationValue as sev{0}", set.ID));
                        }
                    }

                    sbSql.Append(" from shopping as sp left join shop as s on sp.objectid=s.id left join GameShopType as tp on tp.ID=s.ShopType left join admins as a on a.a_id=sp.sid left join ShoppingInfo as si on sp.id=si.OrderId left join ShopFailedReason as sfr on sp.id=sfr.OrderId left join ShopFailedReasonConfig as sfrc on sfr.ReasonId=sfrc.ID left join members as bm on bm.m_id=sp.userid left join members as sm on sm.m_id=s.PublicUser left join ShoppingAssembly as sa on sp.id=sa.ShoppingId and sa.ResType='开始处理'  ");

                    if (setList != null)
                    {
                        foreach (ShoppingEvaluationType set in setList)
                        {
                            sbSql.Append(string.Format(" left join ShoppingEvaluation as se{0} on se{0}.ShoppingId=sp.id and se{0}.EvaluationTypeId={0}", set.ID));
                        }
                    }

                    sbSql.Append(" where 1=1 ");
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (Stype == "s")
                            sbSql.Append(string.Format(" and sp.id like '{0}%'", key.Trim()));
                        else
                            sbSql.Append(string.Format(" and sp.UserID = ({0})", Weike.Member.BssMembers.GetLinkNameID(key.Trim())));
                    }

                    if (string.IsNullOrEmpty(Stype))
                        Stype = "s";
                    if (!string.IsNullOrEmpty(ShopID))
                    {
                        if (Btype == "id")
                            sbSql.Append(string.Format(" and s.shopid like '{0}%'", ShopID));
                        else
                        {
                            Members m = new BssMembers().GetModelByName(ShopID);
                            if (m != null)
                                sbSql.Append(string.Format(" and s.publicuser ={0}", m.M_ID));
                        }
                    }

                    if (!string.IsNullOrEmpty(SName))
                        sbSql.Append(string.Format(" and sp.sid ={0}", SName));

                    if (DealType.HasValue)
                        sbSql.Append(" and s.dealtype='" + DealType.Value.ToString() + "'");
                    if (!string.IsNullOrEmpty(GameCategoryProperty))
                        sbSql.Append(" and s.ShopType in (select ID from GameShopType where Property = '" + GameCategoryProperty + "')");
                    if (!string.IsNullOrEmpty(DelState))
                    {
                        sbSql.Append(string.Format(" and sp.State='{0}'", DelState));
                    }

                    sbSql.Append(" and sp.ObjectType in('出售交易','降价交易','求购交易','代收交易')");

                    string gcwhere = string.Empty;
                    if (!string.IsNullOrEmpty(GameId))
                    {
                        bool isLike = true;
                        string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                        if (!string.IsNullOrEmpty(gameGUID))
                        {
                            if (isLike)
                                sbSql.AppendFormat(" and s.GameGuId like '{0}%' ", gameGUID);
                            else
                                sbSql.AppendFormat(" and s.GameGuId='{0}' ", gameGUID);
                        }

                        if (!string.IsNullOrEmpty(GameShopTypeId))
                        {
                            sbSql.AppendFormat(" and s.ShopType='{0}'", GameShopTypeId);
                        }
                    }
                    string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                    string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                    sbSql.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "sp.processingtime" : "sp.createdate"));
                    sbSql.Append(" order by sp.createdate asc");

                    DataSet ds = new BssShopping().GetListQuery(sbSql.ToString());

                    if (ds != null && ds.Tables[0] != null)
                    {
                        HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                        ISheet sheet = hssfworkbook.CreateSheet("订单资金详情");
                        NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                        dsi.Company = "DD373 Team";
                        NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                        si.Subject = "http://www.dd373.com/";
                        hssfworkbook.DocumentSummaryInformation = dsi;
                        hssfworkbook.SummaryInformation = si;

                        IRow rowtop = sheet.CreateRow(0);

                        IFont font = hssfworkbook.CreateFont();
                        font.FontName = "宋体";
                        font.FontHeightInPoints = 11;

                        ICellStyle style = hssfworkbook.CreateCellStyle();
                        style.SetFont(font);

                        //生成标题
                        string[] tits = new string[] { "序号", "订单编号", "商品类型", "游戏区服", "分类", "属性", "订单类型", "金额", "购买时间", "开始处理时间", "处理时间", "订单状态", "卖/买家原因", "取消原因", "处理客服", "类型", "时间差", "开始时间差", "买家DD用户名", "卖家DD用户名", "余额支付金额", "抵用券支付金额", "银行卡支付金额", "银行卡类型", "银行卡订单号", "买家其他服务费用支付金额", "买家赠送抵用券金额", "卖家手续费", "卖家促销手续费", "卖家推广手续费" };
                        for (int i = 0; i < tits.Length; i++)
                        {
                            if (i == 1 || i == 10 || i == 11 || i == 12)
                            {
                                sheet.SetColumnWidth(i, 18 * 400);
                            }
                            else
                            {
                                sheet.SetColumnWidth(i, 18 * 200);
                            }

                            ICell cell = rowtop.CreateCell(i);
                            cell.SetCellValue(tits[i]);
                            cell.CellStyle = style;
                        }

                        string type = "";
                        DataTable dt = ds.Tables[0];
                        BLLGame bllGame = new BLLGame();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string gameType = dt.Rows[i]["GameType"].ToString();
                            string shopType = dt.Rows[i]["ShopType"].ToString();
                            GameInfoModel InfoModel = bllGame.GetGameInfoModel(gameType, shopType, false);
                            string gameInfo = bllGame.GetGameInfoModelByOtherName(InfoModel);
                            type = "";
                            int diffTime = dt.Rows[i]["diffTime"].ToString().ToInt32();
                            if (diffTime <= 5)
                                type = "A";
                            else if (diffTime > 5 && diffTime <= 10)
                                type = "B";
                            else if (diffTime > 10 && diffTime <= 20)
                                type = "C";
                            else
                                type = "D";


                            IRow row = sheet.CreateRow(i + 1);

                            //序号
                            ICell cell = row.CreateCell(0);
                            cell.SetCellValue(i + 1);
                            cell.CellStyle = style;

                            //订单编号
                            cell = row.CreateCell(1);
                            cell.SetCellValue(dt.Rows[i]["id"].ToString());
                            cell.CellStyle = style;

                            //商品类型
                            cell = row.CreateCell(2);
                            cell.SetCellValue(((BssShop.EDealType)dt.Rows[i]["DealType"].ToString().ToInt32()).ToString());
                            cell.CellStyle = style;

                            if (dt.Rows[i]["DealType"].ToString() != "3")
                            {
                                //游戏
                                cell = row.CreateCell(3);
                                cell.SetCellValue(gameInfo);
                                cell.CellStyle = style;

                                //分类
                                cell = row.CreateCell(4);
                                cell.SetCellValue(dt.Rows[i]["cname"].ToString());
                                cell.CellStyle = style;

                                //属性
                                cell = row.CreateCell(5);
                                cell.SetCellValue(dt.Rows[i]["pname"].ToString());
                                cell.CellStyle = style;
                            }
                            else
                            {
                                //游戏
                                cell = row.CreateCell(3);
                                cell.SetCellValue(gameInfo);
                                cell.CellStyle = style;

                                //分类
                                cell = row.CreateCell(4);
                                cell.SetCellValue("");
                                cell.CellStyle = style;

                                //属性
                                cell = row.CreateCell(5);
                                cell.SetCellValue("");
                                cell.CellStyle = style;
                            }

                            //订单类型
                            cell = row.CreateCell(6);
                            cell.SetCellValue(dt.Rows[i]["objecttype"].ToString());
                            cell.CellStyle = style;

                            //金额
                            cell = row.CreateCell(7);
                            cell.SetCellValue(dt.Rows[i]["Price"].ToString());
                            cell.CellStyle = style;

                            //购买时间
                            cell = row.CreateCell(8);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["CreateDate"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            if (!string.IsNullOrEmpty(dt.Rows[i]["sprocessingtime"].ToString()))
                            {
                                //开始处理时间
                                cell = row.CreateCell(9);
                                cell.SetCellValue(DateTime.Parse(dt.Rows[i]["sprocessingtime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                                cell.CellStyle = style;
                            }
                            else
                            {
                                //开始处理时间
                                cell = row.CreateCell(9);
                                cell.SetCellValue("");
                                cell.CellStyle = style;
                            }

                            //处理时间
                            cell = row.CreateCell(10);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["ProcessingTime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //订单状态
                            cell = row.CreateCell(11);
                            cell.SetCellValue(dt.Rows[i]["State"].ToString());
                            cell.CellStyle = style;

                            //卖/买家原因
                            cell = row.CreateCell(12);
                            cell.SetCellValue(dt.Rows[i]["BuyType"].ToString());
                            cell.CellStyle = style;

                            //取消原因
                            cell = row.CreateCell(13);
                            cell.SetCellValue(dt.Rows[i]["ReasonContent"].ToString());
                            cell.CellStyle = style;

                            //处理客服
                            cell = row.CreateCell(14);
                            cell.SetCellValue(dt.Rows[i]["a_realname"].ToString());
                            cell.CellStyle = style;

                            //类型
                            cell = row.CreateCell(15);
                            cell.SetCellValue(type);
                            cell.CellStyle = style;

                            //时间差
                            cell = row.CreateCell(16);
                            cell.SetCellValue(dt.Rows[i]["diffTime"].ToString());
                            cell.CellStyle = style;

                            //开始时间差
                            cell = row.CreateCell(17);
                            cell.SetCellValue(dt.Rows[i]["sdiffTime"].ToString());
                            cell.CellStyle = style;

                            //买家DD用户名
                            cell = row.CreateCell(18);
                            cell.SetCellValue(dt.Rows[i]["buyer"].ToString());
                            cell.CellStyle = style;

                            //卖家DD用户名
                            cell = row.CreateCell(19);
                            cell.SetCellValue(dt.Rows[i]["seller"].ToString());
                            cell.CellStyle = style;

                            //余额支付金额
                            cell = row.CreateCell(20);
                            cell.SetCellValue(dt.Rows[i]["PayAccountMoney"].ToString());
                            cell.CellStyle = style;

                            //抵用券支付金额
                            cell = row.CreateCell(21);
                            cell.SetCellValue(dt.Rows[i]["PayDyqMoney"].ToString());
                            cell.CellStyle = style;

                            //银行卡支付金额
                            cell = row.CreateCell(22);
                            cell.SetCellValue(dt.Rows[i]["PayBankMoney"].ToString());
                            cell.CellStyle = style;

                            //银行卡类型
                            cell = row.CreateCell(23);
                            cell.SetCellValue(dt.Rows[i]["BankType"].ToString());
                            cell.CellStyle = style;

                            //银行卡订单号
                            cell = row.CreateCell(24);
                            cell.SetCellValue(dt.Rows[i]["BankOrderId"].ToString());
                            cell.CellStyle = style;

                            //买家其他服务费用支付金额
                            cell = row.CreateCell(25);
                            cell.SetCellValue(dt.Rows[i]["PayFwMoney"].ToString());
                            cell.CellStyle = style;

                            //买家赠送抵用券金额
                            cell = row.CreateCell(26);
                            cell.SetCellValue(dt.Rows[i]["ReturnDyqMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家手续费
                            cell = row.CreateCell(27);
                            cell.SetCellValue(dt.Rows[i]["SxfMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家促销手续费
                            cell = row.CreateCell(28);
                            cell.SetCellValue(dt.Rows[i]["CxSxfMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家推广手续费
                            cell = row.CreateCell(29);
                            cell.SetCellValue(dt.Rows[i]["TgSxfMoney"].ToString());
                            cell.CellStyle = style;
                        }

                        string fileName = string.Format("订单资金_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                        string excelFileName = string.Format("{0}.xls", fileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            hssfworkbook.Write(ms);

                            FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                            if (!Directory.Exists(FI.DirectoryName))
                                Directory.CreateDirectory(FI.DirectoryName);
                            FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                            ms.WriteTo(fileUpload);
                            fileUpload.Close();
                            fileUpload = null;
                        }

                        //Excel文件路径
                        string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                        //Excel的Zip文件路径
                        string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                        //Excel的Zip文件下载路径
                        string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                        //将文件压缩
                        string errMsg = "";
                        bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                        if (retZip)
                        {
                            //压缩成功删除文件
                            FileInfo fi = new FileInfo(excelFile);
                            if (fi.Exists)
                            {
                                fi.Delete();
                            }
                        }

                        return Redirect(excelZipPath);

                    }
                    #endregion
                }
                else
                {
                    #region 商城类订单
                    StringBuilder sbSql = new StringBuilder();
                    sbSql.Append("select sp.id,tp.GameShopTypeName as cname,s.GameType,s.ShopType,mo.GamePerQuantity,mo.NumUnit,mo.SinglePrice,sp.price,sp.createdate,sp.processingtime,a.a_realname,sp.state,DATEDIFF(Minute,sp.createdate,sp.processingtime) as diffTime,sfrc.BuyType,sfrc.ReasonContent,bm.m_name as buyer,sm.m_name as seller,si.PayAccountMoney,si.PayDyqMoney,si.PayBankMoney,si.BankType,si.BankOrderId,si.PayFwMoney,si.ReturnDyqMoney,si.SxfMoney,si.CxSxfMoney,si.TgSxfMoney from shopping as sp left join MembersMallShop as s on sp.objectid=s.id left join MembersMallOrder as mo on sp.id=mo.orderid  left join GameShopType as tp on tp.ID=s.ShopType left join admins as a on a.a_id=sp.sid left join ShoppingInfo as si on sp.id=si.OrderId left join ShopFailedReason as sfr on sp.id=sfr.OrderId left join ShopFailedReasonConfig as sfrc on sfr.ReasonId=sfrc.ID left join members as bm on bm.m_id=sp.userid left join members as sm on sm.m_id=s.M_ID");
                    sbSql.Append(" where 1=1 and sp.objecttype ='会员商城' ");
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (Stype == "s")
                            sbSql.Append(string.Format(" and sp.id like '{0}%'", key.Trim()));
                        else
                        {
                            Members m = new BssMembers().GetModelByName(key.Trim());
                            if (m != null)
                                sbSql.Append(string.Format(" and sp.UserID ={0}", m.M_ID));
                        }
                    }
                    if (Btype == "p" && !string.IsNullOrEmpty(ShopID))
                    {
                        Members m = new BssMembers().GetModelByName(ShopID);
                        if (m != null)
                            sbSql.Append(string.Format(" and s.M_ID ={0}", m.M_ID));
                    }

                    if (!string.IsNullOrEmpty(SName))
                        sbSql.Append(string.Format(" and sp.sid ={0}", SName));

                    if (!string.IsNullOrEmpty(DelState))
                    {
                        sbSql.Append(string.Format(" and sp.State='{0}'", DelState));
                    }

                    string gcwhere = string.Empty;
                    if (!string.IsNullOrEmpty(GameId))
                    {
                        bool isLike = true;
                        string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                        if (!string.IsNullOrEmpty(gameGUID))
                        {
                            if (isLike)
                                sbSql.AppendFormat(" and s.GameGuId like '{0}%' ", gameGUID);
                            else
                                sbSql.AppendFormat(" and s.GameGuId='{0}' ", gameGUID);
                        }

                        if (!string.IsNullOrEmpty(GameShopTypeId))
                        {
                            sbSql.AppendFormat(" and s.ShopType='{0}'", GameShopTypeId);
                        }
                    }
                    string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                    string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                    sbSql.Append(string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "sp.processingtime" : "sp.createdate"));
                    sbSql.Append(" order by sp.createdate asc");

                    DataSet ds = new BssShopping().GetListQuery(sbSql.ToString());

                    if (ds != null && ds.Tables[0] != null)
                    {
                        HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                        ISheet sheet = hssfworkbook.CreateSheet("商城订单资金列表");
                        NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                        dsi.Company = "DD373 Team";
                        NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                        si.Subject = "http://www.dd373.com/";
                        hssfworkbook.DocumentSummaryInformation = dsi;
                        hssfworkbook.SummaryInformation = si;

                        IRow rowtop = sheet.CreateRow(0);

                        IFont font = hssfworkbook.CreateFont();
                        font.FontName = "宋体";
                        font.FontHeightInPoints = 11;

                        ICellStyle style = hssfworkbook.CreateCellStyle();
                        style.SetFont(font);

                        //生成标题
                        string[] tits = new string[] { "序号", "订单编号", "游戏区服", "分类", "订单数量", "游戏币单价", "订单总价", "购买时间", "处理时间", "订单状态", "取消原因", "处理客服", "时间差", "买家DD用户名", "卖家DD用户名", "余额支付金额", "抵用券支付金额", "银行卡支付金额", "银行卡类型", "银行卡订单号", "买家其他服务费用支付金额", "买家赠送抵用券金额", "卖家手续费", "卖家促销手续费", "卖家推广手续费" };
                        for (int i = 0; i < tits.Length; i++)
                        {
                            if (i == 1 || i == 6 || i == 7 || i == 9 || i == 10)
                            {
                                sheet.SetColumnWidth(i, 18 * 400);
                            }
                            else
                            {
                                sheet.SetColumnWidth(i, 18 * 200);
                            }

                            ICell cell = rowtop.CreateCell(i);
                            cell.SetCellValue(tits[i]);
                            cell.CellStyle = style;
                        }

                        DataTable dt = ds.Tables[0];
                        BLLGame bllGame = new BLLGame();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string gameType = dt.Rows[i]["GameType"].ToString();
                            string shopType = dt.Rows[i]["ShopType"].ToString();
                            GameInfoModel InfoModel = bllGame.GetGameInfoModel(gameType, shopType, false);
                            string gameInfo = bllGame.GetGameInfoModelByOtherName(InfoModel);
                            IRow row = sheet.CreateRow(i + 1);

                            //序号
                            ICell cell = row.CreateCell(0);
                            cell.SetCellValue(i + 1);
                            cell.CellStyle = style;

                            //订单编号
                            cell = row.CreateCell(1);
                            cell.SetCellValue(dt.Rows[i]["id"].ToString());
                            cell.CellStyle = style;

                            //游戏
                            cell = row.CreateCell(2);
                            cell.SetCellValue(gameInfo);
                            cell.CellStyle = style;

                            //分类
                            cell = row.CreateCell(3);
                            cell.SetCellValue(dt.Rows[i]["cname"].ToString());
                            cell.CellStyle = style;

                            //订单数量
                            cell = row.CreateCell(4);
                            cell.SetCellValue(dt.Rows[i]["GamePerQuantity"].ToString() + dt.Rows[i]["NumUnit"].ToString());
                            cell.CellStyle = style;

                            //游戏币单价
                            cell = row.CreateCell(5);
                            cell.SetCellValue(dt.Rows[i]["SinglePrice"].ToString() + "元/" + dt.Rows[i]["NumUnit"].ToString());
                            cell.CellStyle = style;

                            //订单总价
                            cell = row.CreateCell(6);
                            cell.SetCellValue(dt.Rows[i]["price"].ToString());
                            cell.CellStyle = style;

                            //购买时间
                            cell = row.CreateCell(7);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["CreateDate"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //处理时间
                            cell = row.CreateCell(8);
                            cell.SetCellValue(DateTime.Parse(dt.Rows[i]["ProcessingTime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                            cell.CellStyle = style;

                            //订单状态
                            cell = row.CreateCell(9);
                            cell.SetCellValue(dt.Rows[i]["State"].ToString());
                            cell.CellStyle = style;

                            //取消原因
                            cell = row.CreateCell(10);
                            cell.SetCellValue(dt.Rows[i]["ReasonContent"].ToString());
                            cell.CellStyle = style;

                            //处理客服
                            cell = row.CreateCell(11);
                            cell.SetCellValue(dt.Rows[i]["a_realname"].ToString());
                            cell.CellStyle = style;

                            //时间差
                            cell = row.CreateCell(12);
                            cell.SetCellValue(dt.Rows[i]["diffTime"].ToString());
                            cell.CellStyle = style;

                            //买家DD用户名
                            cell = row.CreateCell(13);
                            cell.SetCellValue(dt.Rows[i]["buyer"].ToString());
                            cell.CellStyle = style;

                            //卖家DD用户名
                            cell = row.CreateCell(14);
                            cell.SetCellValue(dt.Rows[i]["seller"].ToString());
                            cell.CellStyle = style;

                            //余额支付金额
                            cell = row.CreateCell(15);
                            cell.SetCellValue(dt.Rows[i]["PayAccountMoney"].ToString());
                            cell.CellStyle = style;

                            //抵用券支付金额
                            cell = row.CreateCell(16);
                            cell.SetCellValue(dt.Rows[i]["PayDyqMoney"].ToString());
                            cell.CellStyle = style;

                            //银行卡支付金额
                            cell = row.CreateCell(17);
                            cell.SetCellValue(dt.Rows[i]["PayBankMoney"].ToString());
                            cell.CellStyle = style;

                            //银行卡类型
                            cell = row.CreateCell(18);
                            cell.SetCellValue(dt.Rows[i]["BankType"].ToString());
                            cell.CellStyle = style;

                            //银行卡订单号
                            cell = row.CreateCell(19);
                            cell.SetCellValue(dt.Rows[i]["BankOrderId"].ToString());
                            cell.CellStyle = style;

                            //买家其他服务费用支付金额
                            cell = row.CreateCell(20);
                            cell.SetCellValue(dt.Rows[i]["PayFwMoney"].ToString());
                            cell.CellStyle = style;

                            //买家赠送抵用券金额
                            cell = row.CreateCell(21);
                            cell.SetCellValue(dt.Rows[i]["ReturnDyqMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家手续费
                            cell = row.CreateCell(22);
                            cell.SetCellValue(dt.Rows[i]["SxfMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家促销手续费
                            cell = row.CreateCell(23);
                            cell.SetCellValue(dt.Rows[i]["CxSxfMoney"].ToString());
                            cell.CellStyle = style;

                            //卖家推广手续费
                            cell = row.CreateCell(24);
                            cell.SetCellValue(dt.Rows[i]["TgSxfMoney"].ToString());
                            cell.CellStyle = style;
                        }

                        string fileName = string.Format("商城订单资金_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                        string excelFileName = string.Format("{0}.xls", fileName);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            hssfworkbook.Write(ms);

                            FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                            if (!Directory.Exists(FI.DirectoryName))
                                Directory.CreateDirectory(FI.DirectoryName);
                            FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                            ms.WriteTo(fileUpload);
                            fileUpload.Close();
                            fileUpload = null;
                        }

                        //Excel文件路径
                        string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                        //Excel的Zip文件路径
                        string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                        //Excel的Zip文件下载路径
                        string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                        //将文件压缩
                        string errMsg = "";
                        bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                        if (retZip)
                        {
                            //压缩成功删除文件
                            FileInfo fi = new FileInfo(excelFile);
                            if (fi.Exists)
                            {
                                fi.Delete();
                            }
                        }

                        return Redirect(excelZipPath);

                    }
                    #endregion

                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单导出", ex, this.GetType().FullName, "OrderDetailImport");
            }

            return RedirectToAction("USellOrder", new { key = "test" });
        }

        /// <summary>
        /// 处理商品编号搜索
        /// </summary>
        /// <param name="ShopID"></param>
        /// <param name="tTyes">出售交易/点卡商城/求购交易</param>
        /// <returns></returns>
        public static string GetGUIDTODID(string ShopID, string tTyes)
        {
            string strhtml = string.Empty;
            if (ShopID != null)
            {
                if (ShopID.Trim() != "")
                {

                    switch (tTyes.ToUpper().Trim())
                    {
                        case "S"://出售交易
                            Shop m_shop = new BssShop().GetModelTOShopID(ShopID);
                            strhtml = m_shop != null ? string.Format(" and ObjectId='{0}'", m_shop.ID) : "";
                            break;

                        case "C"://点卡商城
                            Card m_card = new BssCard().GetModelToShopID(ShopID);
                            strhtml = m_card != null ? string.Format(" and ObjectId='{0}'", m_card.Id) : "";
                            break;

                        case "B"://求购交易
                            NeedDeal m_need = new BssNeedDeal().GetModelGUID(ShopID);
                            strhtml = m_need != null ? string.Format(" and ObjectId='{0}'", m_need.ID) : "";
                            break;
                    }
                }
            }
            return strhtml;
        }

        [Role("出售订单详情和处理", IsAuthorize = true)]
        public ActionResult OrderDetail(string orderId, string dealState, string remark, string reason, string kfType, string kfId, string delayDay, string SucMp, string SucReason, string SucRemark, decimal? fhNum,string addTime, string TimeType)
        {
            Weike.EShop.Shopping orderModel = null;
            Weike.EShop.BssShopping bssShopping = new Weike.EShop.BssShopping();

            try
            {
                orderModel = bssShopping.GetModel(orderId);

                if (orderModel == null || orderModel.State == BssShopping.ShoppingState.等待支付.ToString())
                    return RedirectToAction("littleUSellOrderAssembly", new { DelState = "nodeal" });

                ViewData["price"] = orderModel.Price;
                ViewData["id"] = null;
                PersonShopDistribution perShopDistModel = new BssPersonShopDistribution().GetModelByOrderId(orderModel.ID);
                if (perShopDistModel != null)
                {
                    ViewData["price"] = perShopDistModel.Price * perShopDistModel.Amount;
                    ViewData["id"] = perShopDistModel.ShopDistributionId;
                }

                //首次打开添加流程
                ShoppingStartAddAssembly(orderModel.ID);

                Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();

                if (IsGet)
                {
                    if (adminModel.A_Memo.Contains("实习客服"))
                    {
                        ShoppingAssembly saDealModel = new BssShoppingAssembly().GetModelBySpIdAndSid(orderModel.ID, adminModel.A_ID);
                        if (saDealModel == null)
                        {
                            return RedirectToAction("littleUSellOrderAssembly", new { DelState = "nodeal" });
                        }
                    }
                }
                if (IsPost)
                {
                    if (orderModel.State == BssShopping.ShoppingState.交易成功.ToString() || orderModel.State == BssShopping.ShoppingState.交易取消.ToString() || orderModel.State == BssShopping.ShoppingState.部分完成.ToString())
                    {
                        MsgHelper.InsertResult("该订单已经处理过，请不要重复提交");
                        return View(orderModel);
                    }

                    if (adminModel.A_Memo.Contains("实习客服"))
                    {
                        ShoppingAssembly saDealModel = new BssShoppingAssembly().GetModelBySpIdAndSid(orderModel.ID, adminModel.A_ID);
                        if (saDealModel == null)
                        {
                            return RedirectToAction("littleUSellOrderAssembly", new { DelState = "nodeal" });
                        }
                    }

                    string msgInfo = "";

                    if (string.IsNullOrEmpty(dealState))
                    {
                        msgInfo = "请选择提交状态";
                        MsgHelper.InsertResult(msgInfo);
                        return View(orderModel);
                    }

                    if (BssAdmins.IsJtKefu(adminModel))
                    {
                        if (dealState != BssShopping.ShoppingState.截图完成.ToString() && dealState != BssShopping.ShoppingState.转单处理.ToString() && dealState != BssShopping.ShoppingState.截图失败.ToString())
                        {
                            msgInfo = "截图客服只能提交截图完成或截图失败状态";
                            MsgHelper.InsertResult(msgInfo);
                            return View(orderModel);
                        }
                    }
                    if (!BssAdmins.IsYiChangKefu(adminModel))
                    {
                        if (new BssShoppingAssembly().GetModelBySpIdAndSidAndSType(orderModel.ID, adminModel.A_ID, BssShoppingAssembly.SourceType.截图完成.ToString()) != null)
                        {
                            if (dealState != BssShopping.ShoppingState.验证完成.ToString() && dealState != BssShopping.ShoppingState.转单处理.ToString())
                            {
                                msgInfo = "验证客服只能提交验证完成状态";
                                MsgHelper.InsertResult(msgInfo);
                                return View(orderModel);
                            }
                        }
                    }

                    bool res = new BLLAdminOrderMethod().UploadOrderDealMethod(orderModel, dealState, remark, reason, kfType, kfId, delayDay, SucMp, SucReason, SucRemark, out msgInfo, addTime,TimeType, fhNum);
                    MsgHelper.InsertResult(msgInfo);

                    orderModel = new BssShopping().GetModel(orderModel.ID);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单详情和处理", ex, this.GetType().FullName, "OrderDetail");
            }
            return View(orderModel);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Role("订单图片展示", IsAuthorize = true)]
        [HttpGet]
        public ActionResult OrderImages(int shopId)
        {
            List<string> list = null;
            try
            {
                Shop shop = new BssShop().GetModel(shopId);
                if (shop != null)
                {
                    list = BLLPicUrlInfo.GetShopImageByShopModel(shop);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单图片展示", ex, this.GetType().FullName, "OrderImages");
            }
            return View(list);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Role("商品快照图片展示", IsAuthorize = true)]
        [HttpGet]
        public ActionResult OrderShopSnapshotImages(string OrderId) 
        {
            List<string> list = null;
            try
            {
                OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(OrderId);
                if (orderShopModel != null)
                {
                    list = BLLPicUrlInfo.GetShopImageByOrderShopSnapshotModel(orderShopModel);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单图片展示", ex, this.GetType().FullName, "OrderImages");
            }
            return View(list);
        }
        /// <summary>
        /// 订单部分发货，计算显示内容
        /// </summary>
        /// <returns></returns>
        [Role("订单部分发货计算退还金额", IsAuthorize = true)]
        public ActionResult OrderBFFHShowByFhnum(string ShoppingID, decimal FHNum) 
        {
            BssShopping bssShoping = new BssShopping();
            BssShop bssShop = new BssShop();
            try
            {
                Shopping shoping = bssShoping.GetModel(ShoppingID);
                if (shoping != null) 
                {
                    OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shoping.ID);
                    if (orderShopModel != null && (orderShopModel.DealType == 1 || orderShopModel.DealType == 2 || orderShopModel.DealType == 4)) 
                    {
                        if (FHNum > 0 && FHNum < orderShopModel.Num * shoping.Count) 
                        {
                            bool IsShowBfwc = Weike.WebGlobalMethod.BLLShoppingMethod.IsShowBfwcShopping(shoping);//是否普通商品
                            if (IsShowBfwc)
                            {
                                bool HanShui = false;
                                decimal QuanBuNum = orderShopModel.Num * (shoping.Count.HasValue ? shoping.Count.Value : 1);
                                string ShuiHouStr = "";
                                decimal ShaoFa = 0M;
                                decimal TuiHuanPrice = 0.00M;
                                decimal ShiShouSXF = 0.00M;//实收手续费

                                ShaoFa = QuanBuNum - FHNum;
                                decimal FHBL = FHNum / QuanBuNum;//发货比例
                                decimal FHPrice = FHBL * shoping.Price;//发货数量价格
                                TuiHuanPrice = Math.Round(shoping.Price - FHPrice, 2);
                                double shuihou = 0.0;

                                if (orderShopModel.DealType != 4)
                                {
                                    HanShui = BLLShoppingMethod.IsHanShui(shoping, FHNum, out ShuiHouStr,out shuihou);

                                    shoping.Price = FHPrice;
                                    ShiShouSXF = BLLShopSxfMethod.GetKouChuMoney(shoping);

                                }
                                else
                                {
                                    decimal attributeSxf = BLLShopSxfMethod.GetAttributeSxf(orderShopModel.ShopId);
                                    ShiShouSXF = BLLShopSxfMethod.GetKouChuMoney_SY(orderShopModel.GameType, orderShopModel.ShopType, FHPrice, attributeSxf);
                                }
                                return Json(new { HanShui = HanShui, ShuiHouStr = ShuiHouStr, ShaoFa = ShaoFa, TuiHuanPrice = TuiHuanPrice, ShiShouSXF = ShiShouSXF }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单部分发货，计算显示内容出错：", ex, this.GetType().FullName, "OrderBFFHShowByFhnum");
            }
            return Content("");
        }
        /// <summary>
        /// 商城订单部分发货，计算显示内容
        /// </summary>
        /// <returns></returns>
        [Role("商城订单部分发货计算退还金额", IsAuthorize = true)]
        public ActionResult MallOrderBFFHShowByFhnum(string ShoppingID, decimal FHNum) 
        {
            BssShopping bssShoping = new BssShopping();
            BssMembersMallOrder bssMallOrder = new BssMembersMallOrder();
            BssMembersMallShop bssMallShop = new BssMembersMallShop();
            try
            {
                Shopping shoping = bssShoping.GetModel(ShoppingID);
                if (shoping != null)
                {
                    MembersMallOrder orderModel = bssMallOrder.GetModel(shoping.ID);
                    if (orderModel!=null)
                    {
                        decimal QuanBuNum = orderModel.GamePerQuantity.Value * orderModel.BuyAmount;
                        if (FHNum > 0 && FHNum < QuanBuNum)
                        {
                            bool HanShui = false;
                            string ShuiHouStr = "";
                            decimal ShaoFa = 0M;
                            decimal TuiHuanPrice = 0.00M;
                            decimal ShiShouSXF = 0.00M;//实收手续费
                            double shuihou = 0.0;

                            HanShui = BLLShoppingMethod.IsHanShui(shoping, FHNum, out ShuiHouStr,out shuihou);
                            ShaoFa = QuanBuNum - FHNum;
                            decimal FHBL = FHNum / QuanBuNum;//发货比例
                            decimal FHPrice = FHBL * shoping.Price;//发货数量价格
                            TuiHuanPrice = Math.Round(shoping.Price - FHPrice, 2);
                            shoping.Price = FHPrice;
                            ShiShouSXF = BLLShopSxfMethod.GetMallKouChuMoney(shoping);
                            return Json(new { HanShui = HanShui, ShuiHouStr = ShuiHouStr, ShaoFa = ShaoFa, TuiHuanPrice = TuiHuanPrice, ShiShouSXF = ShiShouSXF }, JsonRequestBehavior.AllowGet);
                        
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商城订单部分发货，计算显示内容出错：", ex, this.GetType().FullName, "OrderBFFHShowByFhnum");
            }
            return Content("");
        }
             
        [Role("处理点卡订单", IsAuthorize = true)]
        public ActionResult USellCardOrderUpload(string sid, string ordOperState, string remark, string whoerror)
        {
            Weike.EShop.Shopping model = null;

            Weike.EShop.BssShopping bll = new Weike.EShop.BssShopping();

            try
            {
                model = bll.GetModel(sid);
                BssThirdOrderSdkUrlInfo bssThirdOrderSdkUrlInfo = new BssThirdOrderSdkUrlInfo();
                List<ThirdOrderSdkUrlInfo> sdkUrlInfoList = bssThirdOrderSdkUrlInfo.GetModelList(string.Format("OrderId='{0}' and SdkType in ({1},{2},{3}) and State={4}", model.ID, (int)BssThirdOrderSdkUrlInfo.SdkType.星启天提交订单, (int)BssThirdOrderSdkUrlInfo.SdkType.尚景提交订单, (int)BssThirdOrderSdkUrlInfo.SdkType.SUP提交订单, (int)BssThirdOrderSdkUrlInfo.State.未处理));
                if (sdkUrlInfoList != null && sdkUrlInfoList.Count > 0)
                {
                    ViewBag.IsShowTS = true;
                }
                else
                {
                    ViewBag.IsShowTS = false;
                }
                if (IsPost)
                {
                    if (model.State == BssShopping.ShoppingState.等待支付.ToString() || model.State == BssShopping.ShoppingState.支付成功.ToString() )
                    {
                        #region 判断交易取消
                        if (ordOperState == "交易取消")
                        {
                          
                            if (sdkUrlInfoList != null && sdkUrlInfoList.Count > 0)
                            {
                                MsgHelper.Insert("megCkSellCard", "存在未处理的第三方推送，请先取消第三方推送再取消订单!");
                                return View(model);
                            }
                        }
                        #region 订单处理
                        string outMsg = "";
                        if (ordOperState == "交易取消")
                        {
                            new BLLCardThridOrderMethod().DealDkOrderFailed(model, "", "客服操作订单交易取消", out outMsg);
                        }
                        else
                        {
                            new BLLCardThridOrderMethod().DealDkOrderSuccess(model, "", out outMsg);
                        }
                        #endregion

                        //积分处理
                        new BLLMemberMethod().AutoCredit(model, whoerror == "b" ? true : false, ordOperState == "交易取消" ? false : true);
                        MsgHelper.Insert("megCkSellCard", "处理点卡订单成功");
                        #endregion
                    }
                    else
                        MsgHelper.Insert("megCkSellCard", "您在干什么呢？");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("处理点卡订单", ex, this.GetType().FullName, "USellCardOrderUpload");
            }


            if (model == null)
                return RedirectToAction("USellOrder");

            return View(model);
        }
         [Role("取消第三方推送消息", IsAuthorize = true)]
        public ActionResult SetThirdSdkUrlInfoState(string OrderId)
        {
            try
            {
                Shopping model = new BssShopping().GetModel(OrderId);
                if (model != null && model.State == BssShopping.ShoppingState.支付成功.ToString())
                {
                    BssThirdOrderSdkUrlInfo bssThirdOrderSdkUrlInfo = new BssThirdOrderSdkUrlInfo();
                    List<ThirdOrderSdkUrlInfo> sdkUrlInfoList = bssThirdOrderSdkUrlInfo.GetModelList(string.Format("OrderId='{0}' and SdkType in ({1},{2},{3}) and State={4}", model.ID, (int)BssThirdOrderSdkUrlInfo.SdkType.星启天提交订单, (int)BssThirdOrderSdkUrlInfo.SdkType.尚景提交订单, (int)BssThirdOrderSdkUrlInfo.SdkType.SUP提交订单, (int)BssThirdOrderSdkUrlInfo.State.未处理));
                    if (sdkUrlInfoList != null && sdkUrlInfoList.Count > 0)
                    {
                        foreach (ThirdOrderSdkUrlInfo item in sdkUrlInfoList)
                        {
                            item.State = (int)BssThirdOrderSdkUrlInfo.State.处理失败;
                            item.EditTime = DateTime.Now;
                            bssThirdOrderSdkUrlInfo.Update(item);
                        }
                        MsgHelper.Insert("megCkSellCard","取消第三方推送消息成功!");
                    }
                    else
                    {
                        MsgHelper.Insert("megCkSellCard", "不存在未处理的推送记录!");
                    }
                }
            }
            catch (Exception ex)
            {
                MsgHelper.InsertResult("操作失败!");
                LogExcDb.Log_AppDebug("取消第三方推送消息失败", ex, this.GetType().FullName, "SetThirdSdkUrlInfoState");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        #region 柜台充值订单

        [Role("柜台充值订单", IsAuthorize = true)]
        public ActionResult OfflinePay(int? Page, DateTime? Ds, DateTime? De, string state, string PSN, string uname, string bankType)
        {
            #region 搜索功能

            DataPages<Weike.EShop.OfflinePay> Lshop = null;

            string where = "1=1";

            if (Ds.HasValue && De.HasValue)
                where = string.Format(" CreateDate between '{0}' and '{1}'", Ds.Value.ToString(), De.Value.ToString());
            if (!string.IsNullOrEmpty(state))
            {
                where += string.Format(" and State='{0}'", ((Weike.EShop.BssOfflinePay.PayState)state.ToInt32()).ToString());
            }
            if (!string.IsNullOrEmpty(uname))
            {
                Members m = new BssMembers().GetModelByName(uname.Trim());
                where += string.Format(string.Format(" and uid={0}", m == null ? 0 : m.M_ID));
            }
            if (!string.IsNullOrEmpty(PSN))
                where += string.Format(" and PSN = '{0}'", PSN.Trim());
            if (!string.IsNullOrEmpty(bankType))
                where += string.Format(" and BankType = '{0}'", bankType.ToInt32());
            try
            {
                Lshop = new BssOfflinePay().GetPageRecord(where, "CreateDate", 16, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("柜台充值订单", ex, this.GetType().FullName, "OfflinePay");
            }
            #endregion

            #region 统计功能

            try
            {
                ViewData["Sum"] = new BssOfflinePay().GetSingle("select sum(convert(decimal(38,2),ordermoney)) from dbo.OfflinePay where state='充值成功'");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("统计功能柜台充值总金额出错", ex, this.GetType().FullName, "OfflinePay");
            }

            #endregion

            return View(Lshop);
        }

        [Role("删除柜台充值订单", IsAuthorize = true)]
        public ActionResult OfflineDel(int? pID)
        {
            try
            {
                new Weike.EShop.BssOfflinePay().Delete(pID ?? 0);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除台充值订单", ex, this.GetType().FullName, "OfflineDel");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("处理柜台充值订单", IsAuthorize = true)]
        public ActionResult OfflinePass(int? pID, string payState)
        {
            Weike.EShop.OfflinePay model = null;
            Weike.EShop.BssOfflinePay bll = new Weike.EShop.BssOfflinePay();

            try
            {
                model = bll.GetModel(pID ?? 0);

                if (IsPost)
                {
                    if (model.State == BssOfflinePay.PayState.充值中.ToString())
                    {

                        model.State = payState;
                        bll.Update(model);

                        if (payState.Trim() == "充值成功")
                        {

                            #region 帐户充值

                            try
                            {
                                Weike.Member.BssMembers membll = new BssMembers();
                                Weike.Member.Members memmodel = membll.GetModel(model.UID);

                                if (memmodel != null)
                                {
                                    memmodel.M_Money += Convert.ToDecimal(model.OrderMoney);
                                    //membll.Update(memmodel);
                                    membll.AddMembersMoney(Convert.ToDecimal(model.OrderMoney), memmodel.M_ID);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogExcDb.Log_AppDebug("处理台充值订单帐户充值", ex, this.GetType().FullName, "OfflinePass");
                            }

                            #endregion

                            #region 资金记录处理

                            try
                            {
                                BssMoneyHistory bssMh = new BssMoneyHistory();
                                List<MoneyHistory> mhList = bssMh.GetModelList(string.Format(" OrdID='{0}'", model.PSN.Trim()));
                                foreach (MoneyHistory mh in mhList)
                                {
                                    mh.State = BssMoneyHistory.HistoryState.成功.ToString();
                                    bssMh.Update(mh);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogExcDb.Log_AppDebug("处理台充值订单资金", ex, this.GetType().FullName, "OfflinePass");
                            }

                            #endregion
                            string msgs = string.Format("DD373温馨提示！恭喜您，您的账户充值订单号[{0}]后台已经处理成功,充值金额为：[{1}]元，请及时查看自己账户金额。", model.PSN, model.OrderMoney);
                            BssMembersMessage.AddMeg(model.UID, msgs);
                            MsgHelper.Insert("megOffline", "处理柜台充值订单成功");
                        }
                        else
                        {
                            #region 资金记录处理

                            try
                            {
                                BssMoneyHistory bssMh = new BssMoneyHistory();
                                List<MoneyHistory> mhList = bssMh.GetModelList(string.Format(" OrdID='{0}'", model.PSN.Trim()));
                                foreach (MoneyHistory mh in mhList)
                                {
                                    mh.State = BssMoneyHistory.HistoryState.失败.ToString();
                                    bssMh.Update(mh);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogExcDb.Log_AppDebug("处理台充值订单资金", ex, this.GetType().FullName, "OfflinePass");
                            }

                            #endregion
                            string msgs = string.Format("DD373温馨提示！您好，您的账户充值订单号[{0}]后台处理台充值订单失败,充值金额为：[{1}]元，请及时查看自己账户金额。详细请联系客服！", model.PSN, model.OrderMoney);
                            BssMembersMessage.AddMeg(model.UID, msgs);
                            MsgHelper.Insert("megOffline", "处理柜台充值订单成功");
                        }

                        BssOfflineRemittance bssOr = new BssOfflineRemittance();
                        OfflineRemittance orModel = bssOr.GetModel(model.PSN);
                        if (orModel != null)
                        {
                            orModel.State = BssOfflineRemittance.ReState.处理完成.ToString();
                            orModel.EditTime = DateTime.Now;
                            bssOr.Update(orModel);
                        }

                        return View(model);
                    }
                    else
                    {
                        MsgHelper.Insert("megOffline", "您在干什么呢？");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("处理台充值订单", ex, this.GetType().FullName, "OfflinePass");
            }

            if (model == null)
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());

            return View(model);
        }

        #endregion

        #endregion

        #region 用户发布的求购信息

        [Role("求购管理", IsAuthorize = true)]
        public ActionResult UNeeds(int? Page, string NeedState, string key, string Stype,string GameId, string GameOtherId, string GameShopTypeId)
        {
            DataPages<Weike.EShop.NeedDeal> lNeed = null;
            string where = "1=1";
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (string.IsNullOrEmpty(Stype))
                        Stype = "s";

                    if (Stype == "s")
                        where = string.Format("GUID = '{0}'", key);
                    else
                        where = string.Format("PublicUser = ({0})", Weike.Member.BssMembers.GetLinkNameID(key));
                }
                if (!string.IsNullOrEmpty(NeedState))
                {
                    if (NeedState != "")
                        where += string.Format(" and NeedState='{0}'", NeedState);
                }
                string gcwhere = string.Empty;
                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            gcwhere += string.Format(" and GameGUID like '{0}%' ", gameGUID);
                        else
                            gcwhere += string.Format(" and GameGUID='{0}' ", gameGUID);
                    }

                    if (!string.IsNullOrEmpty(GameShopTypeId))
                    {
                        GameShopType gameShopTypeModel = new BssGameShopType().GetModel(GameShopTypeId);
                        if (gameShopTypeModel != null)
                        {
                            if (gameShopTypeModel.CurrentLevelType == (int)Weike.EShop.BssGameRoute.LevelType.商品子类型)
                            {
                                gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ParentId);
                            }
                            else
                            {
                                gcwhere += string.Format(" and ShopType = '{0}'", gameShopTypeModel.ID);
                            }
                        }
                        else
                        {
                            return View();
                        }
                    }
                }
                where += gcwhere;
                lNeed = new BssNeedDeal().GetPageRecord(where, "CreateDate", 10, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("求购管理", ex, this.GetType().FullName, "UNeeds");
            }


            return View(lNeed);
        }


        [Role("快速审核求购记录", IsAuthorize = true)]
        public ActionResult UNPass(int? nID)
        {

            try
            {
                Weike.EShop.BssNeedDeal bll = new Weike.EShop.BssNeedDeal();
                Weike.EShop.NeedDeal model = bll.GetModel(nID ?? 0);
                if (model != null)
                {
                    new BLLNeedDealMethod().UnpassNeedDeal(model);

                    MsgHelper.Insert("oprationgsuccess", "快速审核求购记录成功!编号为：" + model.GUID);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("快速审核求购记录", ex, this.GetType().FullName, "UNeeds");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("删除求购记录", IsAuthorize = true)]
        public ActionResult UNDel(int? nID)
        {
            try
            {
                Weike.EShop.BssNeedDeal bllnd = new Weike.EShop.BssNeedDeal();
                Weike.EShop.NeedDeal nb = bllnd.GetModel(nID ?? 0);
                if (nb != null)
                {
                    new BLLNeedDealMethod().UnpassNeedDeal(nb);//先审核失败，方便退款
                }

                bllnd.Delete(nID ?? 0);

                MsgHelper.Insert("oprationgsuccess", "删除求购记录成功!编号为：" + nb.GUID);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除求购记录", ex, this.GetType().FullName, "删除求购记录");
                return RedirectToAction("UNeeds");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("编辑求购记录", IsAuthorize = true)]
        public ActionResult UNUpload(int? nID, string title, string descript)
        {
            Weike.EShop.NeedDeal model = null;
            Weike.EShop.BssNeedDeal bll = new Weike.EShop.BssNeedDeal();
            try
            {
                model = bll.GetModel(nID ?? 0);
                if (model == null)
                    return RedirectToAction("UNeeds");

                if (IsPost)
                {
                    model.GameTitle = title;
                    model.Descript = descript;

                    bll.Update(model);

                    MsgHelper.Insert("oprationgsuccess", "编辑求购记录成功");
                    return RedirectToAction("UNeeds");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除求购记录", ex, this.GetType().FullName, "删除求购记录");
            }

            return View(model);
        }
        #endregion


        #region 会员资金详细
        [Role("会员资金-积分详细", IsAuthorize = true)]
        public ActionResult MoneyHistoryList(int? Page, string OperaType, string PayType, string OrdID, string M_ID, string SxfType, string StartTime, string EntTime, string State)
        {
            DataPages<Weike.EShop.MoneyHistory> Lmoney = null;
            #region 条件判断
            string where = "1=1";
            if (!string.IsNullOrEmpty(M_ID))
            {
                Members mModel = new BssMembers().GetModelByName(M_ID.Trim());
                if (mModel == null)
                {
                    return View(Lmoney);
                }
                else
                {
                    where += string.Format(" and UID = {0}", mModel.M_ID);
                }
            }
            if (!string.IsNullOrEmpty(SxfType))
            {
                where += string.Format(" and ordid like '{0}%'", SxfType);
            }

            StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            if (!string.IsNullOrEmpty(StartTime))
            {
                where += string.Format(" and createdate>'{0}'", StartTime);
            }

            if (!string.IsNullOrEmpty(EntTime))
            {
                where += string.Format(" and createdate<'{0}'", EntTime);
            }
            if (!string.IsNullOrEmpty(OperaType))
            {
                where += string.Format(" and OperaType='{0}'", OperaType);
                if (OperaType == "充值记录" && !string.IsNullOrEmpty(PayType))
                {
                    where += string.Format(" and PayType='{0}'", PayType);
                }
            }
            if (!string.IsNullOrEmpty(State))
            {
                where += string.Format(" and State='{0}'", State);
            }
            if (!string.IsNullOrEmpty(OrdID))
                where += string.Format(" and OrdID = '{0}'", OrdID.Trim());



            if (IsPost)
            {
                if (!string.IsNullOrEmpty(Request.Form["total"]))
                {
                    string strjf = "支付记录,提现记录,押金记录,退款记录,充值记录,收款记录,手续费,托管手续费,资金收回,推广手续费,促销手续费,系统充值,系统扣除,押金扣除,推广转入,退还押金,原路退款";
                    string strfen = "积分获取,积分使用,退返积分";

                    if (!string.IsNullOrEmpty(OperaType) && strjf.Contains(OperaType))
                        ViewData["SumMoney"] = string.Format("总资金：{0}元", BssMoneyHistory.GetMoneyWhereSum(where));
                    else
                        ViewData["SumMoney"] = string.Format("总资金：{0}元", BssMoneyHistory.GetMoneyWhereSum(where + " and OperaType in('支付记录','提现记录','押金记录','退款记录','充值记录','收款记录','手续费','资金收回','托管手续费','推广手续费','促销手续费','系统充值','系统扣除','押金扣除','推广转入','退还押金','原路退款')"));
                    if (!string.IsNullOrEmpty(OperaType) && strfen.Contains(OperaType))
                        ViewData["SumJiFeng"] = string.Format("总积分：{0}积分", BssMoneyHistory.GetMoneyWhereSum(where));
                    else
                        ViewData["SumJiFeng"] = string.Format("总积分：{0}积分", BssMoneyHistory.GetMoneyWhereSum(where + " and OperaType in('积分获取','积分使用','退返积分')"));
                }
            }
            #endregion

            try
            {
                Lmoney = new BssMoneyHistory().GetPageRecordByRead(where, "CreateDate desc,ID", 20, Page ?? 1, PagesOrderTypeDesc.降序, "ID,OrdID,SumMoney,OperaType,CreateDate,State,PayType,UID,LastMoney,AfterMoney");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("用户资金明细列表出错", ex, this.GetType().FullName, "MoneyHistoryList");
            }

            return View(Lmoney);

        }
        [Role("会员资金-锁定资金详细", IsAuthorize = true)]
        public ActionResult MoneyLockList(int? Page, string M_ID, string ObjID, string type)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(M_ID))
            {
                where += string.Format(" and UID in(select M_ID from Members where M_Name='{0}')", M_ID);
            }
            if (!string.IsNullOrEmpty(ObjID))
            {
                where += string.Format(" and  ObjectGUID='{0}'", ObjID);
            }
            if (!string.IsNullOrEmpty(type))
            {
                where += string.Format(" and  LType='{0}'", ((BssMoneyLock.MoneyLockType)(Convert.ToInt32(type))).ToString());
            }
            DataPages<Weike.EShop.MoneyLock> Lmoney = null;

            try
            {
                Lmoney = new BssMoneyLock().GetPageRecord(where, "CreateDate", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");

                Admins adminModel = BLLAdmins.GetCurrentAdminUserInfo();
                if (adminModel != null && "wangzheyongle9|admin|tcwuzhe|ddmiaojinli|langying".Contains(adminModel.A_Name))
                {
                    ViewData["sumprice"] = Convert.ToDecimal(new BssMoneyLock().GetSingle(" select sum(price) from moneylock where " + where));

                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("用户锁定资金列表出错", ex, this.GetType().FullName, "MoneyLockList");
            }
            return View(Lmoney);
        }

        [Role("会员资金-安全保障资金找回", IsAuthorize = true)]
        public ActionResult ReturnMoneyLock(string mlid)
        {
            int id = Convert.ToInt32(mlid);
            BssMoneyLock bss = new BssMoneyLock();
            MoneyLock ml = bss.GetModel(id);
            if (ml != null)
            {
                try
                {
                    Shopping sp = new BssShopping().GetModel(ml.ObjectGUID);
                    if (sp != null)
                    {
                        //记录操作
                        Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                        BssModifyMembersRecording.AddCaoZuoRecording(ml.UID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.追回资金, sp.ID, sp.ID + DateTime.Now.ToString() + sp.State);

                        BssMembers bssm = new BssMembers();
                        Members m = new BssMembers().GetModel(sp.UserID);
                        Members sellModel = new BssMembers().GetModel(ml.UID);
                        //记录资
                        new BLLMoneyhistory().Insert(ml.UID, ml.Price, ml.ObjectGUID, BssMoneyHistory.HistoryType.资金收回, sellModel.M_Money.Value, sellModel.M_Money.Value);

                        List<MoneyHistory> listmh = new List<MoneyHistory>();
                        BssMoneyHistory bmh = new BssMoneyHistory();
                        MoneyHistory mh = new MoneyHistory();
                        listmh = bmh.GetModelList(string.Format(" Ordid='{0}'", ml.ObjectGUID));

                        if (listmh.Count > 0)
                        {
                            mh = listmh.First(l => l.OperaType == BssMoneyHistory.HistoryType.收款记录.ToString());
                            if (mh != null)
                            {
                                mh.State = BssMoneyHistory.HistoryState.失败.ToString();
                                bmh.Update(mh);
                            }
                        }

                        //删除记录
                        bss.Delete(ml.ID);

                        //返钱
                        MoneyHistory fqmh = listmh.First(l => l.OperaType == BssMoneyHistory.HistoryType.支付记录.ToString());
                        if (fqmh != null)
                        {
                            m.M_Money += Convert.ToDecimal(fqmh.SumMoney);
                            new BLLMoneyhistory().Insert(m.M_ID, Convert.ToDecimal(fqmh.SumMoney), ml.ObjectGUID, BssMoneyHistory.HistoryType.退款记录, m.M_Money.Value - fqmh.SumMoney, m.M_Money.Value);
                            //bssm.Update(m);
                            bssm.AddMembersMoney(Convert.ToDecimal(fqmh.SumMoney), m.M_ID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("安全保障资金找回", ex, this.GetType().FullName, "ReturnMoneyLock");
                }
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("会员资金-安全保障资金延时", IsAuthorize = true)]
        public ActionResult LaterMoneyLock(string mlid, string day)
        {
            int id = mlid.ToInt32();
            BssMoneyLock bss = new BssMoneyLock();
            MoneyLock ml = null;
            string userName = "";
            try
            {
                ml = bss.GetModel(id);
                Members mModel = new BssMembers().GetModel(ml.UID);
                if (mModel != null)
                    userName = mModel.M_Name;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取安全保障资金信息", ex, this.GetType().FullName, "UpdateMoneyLockTime");
            }

            if (IsPost)
            {
                try
                {
                    int dayTime = day.ToInt32();

                    if (ml != null)
                    {
                        DateTime oldTime = ml.CreateDate.Value;

                        ml.CreateDate = ml.CreateDate.Value.AddDays(dayTime);
                        bss.Update(ml);

                        //记录操作
                        Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                        BssModifyMembersRecording.AddCaoZuoRecording(ml.UID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.延迟锁定资金, ml.ObjectGUID, string.Format("会员:{0} 订单ID:{1}锁定资金延迟{2}天，更新前时间:{3}，更新后时间:{4}", userName, ml.ObjectGUID, day, oldTime.ToString("yyyy-MM-dd HH:mm"), ml.CreateDate.Value.ToString("yyyy-MM-dd HH:mm")));

                        MsgHelper.Insert("megCkSell", string.Format("操作成功，会员:{0} 订单ID:{1}锁定资金已经延迟{2}天，更新前时间:{3}，更新后时间:{4}", userName, ml.ObjectGUID, day, oldTime.ToString("yyyy-MM-dd HH:mm"), ml.CreateDate.Value.ToString("yyyy-MM-dd HH:mm")));
                    }

                    return Content("");
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("更新安全保障资金信息", ex, this.GetType().FullName, "UpdateMoneyLockTime");
                }
            }

            return Content(userName);
        }

        #endregion

        #region 申请处理修改会员资金
        [Role("申请修改会员资金", IsAuthorize = true)]
        public ActionResult ApplyMoneyChange(int? Page, string mname, decimal? jine, int? nbRemark, int? wbRemark, int? jifen, string ClassManager, bool? IsAbnormal, bool? IsLoss, decimal? Penalty, int? ChangeType, string Department, string nbRemarkt, string wbRemarkt, string OrderId)
        {

            if (IsPost)
            {
                bool IsCanApply = true;
                Members mModel = new BssMembers().GetModelByName(mname);
                if (mModel != null)
                {
                    if (jine < 0 && mModel.M_Money < -jine)
                    {
                        IsCanApply = false;
                        MsgHelper.Insert("MCA", "会员余额小于申请减少余额");
                    }
                    else if (jifen < 0)
                    {
                        MembersInfo miModel = new BssMembersInfo().GetModel(mModel.M_ID);
                        if (miModel == null || miModel.Score < -jifen)
                        {
                            IsCanApply = false;
                            MsgHelper.Insert("MCA", "会员积分小于申请减少积分");
                        }
                    }
                }
                else
                {
                    IsCanApply = false;
                    MsgHelper.Insert("MCA", "会员名不存在");
                }

                if (IsCanApply)
                {
                    try
                    {
                        MoneyChangeApply MCA = new MoneyChangeApply();
                        MCA.applyname = BLLAdmins.GetManageCurrentUserName();
                        MCA.applytime = DateTime.Now;
                        MCA.m_name = mname;
                        MCA.changeprice = jine.Value;
                        MCA.changejifen = jifen.Value;
                        ShopFailedReasonConfig config = new BssShopFailedReasonConfig().GetModel(wbRemark.Value);
                        if (config != null)
                        {
                            if (!string.IsNullOrEmpty(config.ReasonContent))
                            {
                                MCA.applyremark = config.ReasonContent;
                            }
                            else
                            {
                                MCA.applyremark = wbRemarkt;
                            }
                        }
                        config = null;
                        config = new BssShopFailedReasonConfig().GetModel(nbRemark.Value);
                        if (config != null)
                        {
                            if (!string.IsNullOrEmpty(config.ReasonContent))
                            {
                                MCA.nbapplyremark = config.ReasonContent;
                            }
                            else
                            {
                                MCA.nbapplyremark = nbRemarkt;
                            }
                        }
                        MCA.applyimgs = Globals.Attachment_UploadNoWater("UpImg");
                        MCA.state = (int)BssMoneyChangeApply.MState.等待处理;
                        MCA.ChangeType = ChangeType.Value;
                        MCA.ClassManager = ClassManager;
                        MCA.IsAbnormal = IsAbnormal.Value;
                        MCA.IsLoss = IsLoss.Value;
                        MCA.Penalty = Penalty.Value;
                        MCA.Department = Department;
                        MCA.OrderId = OrderId;

                        bool suc = new BLLMoneyHistoryMethod().AddMoneyChangeApply(MCA, mModel);
                        MsgHelper.Insert("MCA", suc ? "申请成功" : "申请出错，请稍后再试");
                    }
                    catch (Exception ex)
                    {
                        MsgHelper.Insert("MCA", "申请出错");
                        LogExcDb.Log_AppDebug("申请修改会员资金：", ex, this.GetType().FullName, "ApplyMoneyChange");
                    }
                }
            }

            Weike.EShop.ApplyMoneyChangeView model = null;
            string applyname = BLLAdmins.GetManageCurrentUserName();
            try
            {
                //获取页面所需数据Model
                model = new Weike.EShop.BLLApplyMoneyChange().GetApplyMoneyChangeView(Page, applyname);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("用户资金明细列表出错", ex, this.GetType().FullName, "MoneyChangeApplyList");
            }
            return View(model);
        }
        [Role("处理会员资金修改申请", IsAuthorize = true)]
        public ActionResult DealMoneyChange(int? mca_id, string state, string remark)
        {
            BssMoneyChangeApply bss = new BssMoneyChangeApply();
            MoneyChangeApply changeMoneyModel = bss.GetModel(mca_id.Value);

            if (IsPost)
            {
                if (changeMoneyModel.state == (int)BssMoneyChangeApply.MState.等待处理)
                {
                    try
                    {
                        
                        string msg = "";
                        Members memberModel;
                        int State = state.ToInt32();
                        //提交处理
                        bool res = new BLLMoneyHistoryMethod().MoneyChangeApplyDealMethod(changeMoneyModel, state.ToInt32(), remark, out memberModel, out msg);

                        if (res)
                        {
                            //站内信通知
                            if (State == (int)BssMoneyChangeApply.MState.处理成功)
                            {
                                string ordermsg = string.Empty;
                                if (!string.IsNullOrWhiteSpace(changeMoneyModel.OrderId))
                                {
                                    ordermsg = "，订单编号：" + changeMoneyModel.OrderId;
                                }

                                MembersInfo miModel = new BssMembersInfo().GetModel(memberModel.M_ID);
                                string msgInfo = string.Format("DD373提醒：客服通过后台系统处理对您的资金进行了更改，修改类型：{0}" + ordermsg + "，变动原因：{1}，处理前资金：{2}，处理后资金：{3}，处理前积分：{4}，处理后积分：{5}", ((BssMoneyChangeApply.ChangeType)changeMoneyModel.ChangeType).ToString(), changeMoneyModel.applyremark, memberModel.M_Money.Value + (changeMoneyModel.changeprice > 0 ? 0 : -changeMoneyModel.changeprice), memberModel.M_Money.Value + (changeMoneyModel.changeprice > 0 ? changeMoneyModel.changeprice : 0), miModel.Score - changeMoneyModel.changejifen, miModel.Score);
                                BssMembersMessage.AddMeg(memberModel.M_ID, msgInfo);
                            }
                            if (string.IsNullOrEmpty(msg))
                            {
                                msg = "处理结果提交成功";
                            }
                        }
                        MsgHelper.InsertResult(msg);
                            
                    }
                    catch (Exception ex)
                    {
                        LogExcDb.Log_AppDebug("处理资金变动申请出错", ex, this.GetType().FullName, "DealMoneyChange");
                        return View(changeMoneyModel);
                    }
                }
            }
            return View(changeMoneyModel);
        }

        [Role("资金修改申请列表", IsAuthorize = true)]
        public ActionResult ApplyMoneyChangeList(int? Page, string userName, string applyName, string processName, string state, string timetype, string StartTime, string EntTime, string Department, string ChangeType, string OrderID)
        {
            DataPages<Weike.EShop.MoneyChangeApply> Lmoney = null;
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += string.Format(" and m_name='{0}' ", userName.Trim());
            }
            if (!string.IsNullOrEmpty(applyName))
            {
                Admins applyadmin = new BssAdmins().GetModel(applyName.ToInt32());
                where += string.Format(" and applyname='{0}' ", applyadmin != null ? applyadmin.A_Name : "");
            }
            if (!string.IsNullOrEmpty(processName))
            {
                Admins proadmin = new BssAdmins().GetModel(processName.ToInt32());
                where += string.Format(" and processname='{0}' ", proadmin != null ? proadmin.A_Name : "");
            }
            if (!string.IsNullOrEmpty(state))
            {
                where += string.Format(" and state={0} ", state);
            }
            if (!string.IsNullOrEmpty(Department))
            {
                where += string.Format(" and Department='{0}'", Department);
            }
            if (!string.IsNullOrEmpty(ChangeType))
            {
                where += string.Format(" and ChangeType={0}", ChangeType);
            }
            if (!string.IsNullOrWhiteSpace(OrderID))
            {
                where += string.Format(" and OrderId='{0}'", OrderID);
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where += string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "processtime" : "applytime");
            try
            {
                Lmoney = new BssMoneyChangeApply().GetPageRecord(where, "applytime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("用户资金明细列表出错", ex, this.GetType().FullName, "MoneyChangeApplyList");
            }
            return View(Lmoney);
        }

        [Role("资金修改申请导出", IsAuthorize = true)]
        public ActionResult ApplyMoneyChangeListImport(string userName, string applyName, string processName, string state, string timetype, string StartTime, string EntTime, string Department, string ChangeType)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(userName))
            {
                where += string.Format(" and m_name='{0}' ", userName.Trim());
            }
            if (!string.IsNullOrEmpty(applyName))
            {
                Admins applyadmin = new BssAdmins().GetModel(applyName.ToInt32());
                where += string.Format(" and applyname='{0}' ", applyadmin != null ? applyadmin.A_Name : "");
            }
            if (!string.IsNullOrEmpty(processName))
            {
                Admins proadmin = new BssAdmins().GetModel(processName.ToInt32());
                where += string.Format(" and processname='{0}' ", proadmin != null ? proadmin.A_Name : "");
            }
            if (!string.IsNullOrEmpty(state))
            {
                where += string.Format(" and state={0} ", state);
            }
            if (!string.IsNullOrEmpty(Department))
            {
                where += string.Format(" and Department='{0}'", Department);
            }
            if (!string.IsNullOrEmpty(ChangeType))
            {
                where += string.Format(" and ChangeType={0}", ChangeType);
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where += string.Format(" and {2} between '{0}' and '{1}'", STime, ETime, Request["timetype"] == "p" ? "processtime" : "applytime");
            try
            {
                List<MoneyChangeApply> list = new BssMoneyChangeApply().GetModelList(where + " order by applytime asc");
                if (list != null)
                {
                    HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                    ISheet sheet = hssfworkbook.CreateSheet("资金修改记录");
                    NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "DD373 Team";
                    NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                    si.Subject = "http://www.dd373.com/";
                    hssfworkbook.DocumentSummaryInformation = dsi;
                    hssfworkbook.SummaryInformation = si;

                    IRow rowtop = sheet.CreateRow(0);

                    IFont font = hssfworkbook.CreateFont();
                    font.FontName = "宋体";
                    font.FontHeightInPoints = 11;

                    ICellStyle style = hssfworkbook.CreateCellStyle();
                    style.SetFont(font);

                    //生成标题
                    string[] tits = new string[] { "序号", "会员名", "修改资金（元）", "修改积分（分）", "申请人", "内部原因", "外部原因", "班次主管", "是否异常", "是否有损失", "行政罚款", "修改类型", "处理结果", "状态", "处理人", "申请日期", "处理日期", "订单编号" };
                    for (int i = 0; i < tits.Length; i++)
                    {
                        if (i == 5 || i == 6 || i == 12 || i == 15 || i == 16)
                        {
                            sheet.SetColumnWidth(i, 18 * 400);
                        }
                        else
                        {
                            sheet.SetColumnWidth(i, 18 * 200);
                        }

                        ICell cell = rowtop.CreateCell(i);
                        cell.SetCellValue(tits[i]);
                        cell.CellStyle = style;
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        MoneyChangeApply recordModel = list[i];

                        IRow row = sheet.CreateRow(i + 1);
                        //序号
                        ICell cell = row.CreateCell(0);
                        cell.SetCellValue(i + 1);
                        cell.CellStyle = style;

                        //会员名
                        cell = row.CreateCell(1);
                        cell.SetCellValue(recordModel.m_name);
                        cell.CellStyle = style;

                        //修改资金
                        cell = row.CreateCell(2);
                        cell.SetCellValue(recordModel.changeprice.ToString("0.00"));
                        cell.CellStyle = style;

                        //修改积分
                        cell = row.CreateCell(3);
                        cell.SetCellValue(recordModel.changejifen.Value.ToString("0.00"));
                        cell.CellStyle = style;

                        //申请人
                        Admins appModel = new BssAdmins().GetUserName(recordModel.applyname);
                        cell = row.CreateCell(4);
                        cell.SetCellValue(appModel.A_RealName);
                        cell.CellStyle = style;

                        //内部原因
                        cell = row.CreateCell(5);
                        cell.SetCellValue(recordModel.nbapplyremark);
                        cell.CellStyle = style;

                        //外部原因
                        cell = row.CreateCell(6);
                        cell.SetCellValue(recordModel.applyremark);
                        cell.CellStyle = style;

                        //班次主管
                        cell = row.CreateCell(7);
                        cell.SetCellValue(recordModel.ClassManager);
                        cell.CellStyle = style;

                        //是否异常
                        cell = row.CreateCell(8);
                        cell.SetCellValue(recordModel.IsAbnormal ? "是" : "否");
                        cell.CellStyle = style;

                        //是否有损失
                        cell = row.CreateCell(9);
                        cell.SetCellValue(recordModel.IsLoss ? "是" : "否");
                        cell.CellStyle = style;

                        //行政罚款
                        cell = row.CreateCell(10);
                        cell.SetCellValue(recordModel.Penalty.ToString("0.00"));
                        cell.CellStyle = style;

                        //修改类型
                        cell = row.CreateCell(11);
                        cell.SetCellValue(((Weike.EShop.BssMoneyChangeApply.ChangeType)recordModel.ChangeType).ToString());
                        cell.CellStyle = style;

                        //处理结果
                        cell = row.CreateCell(12);
                        cell.SetCellValue(recordModel.processremark);
                        cell.CellStyle = style;

                        //状态
                        cell = row.CreateCell(13);
                        cell.SetCellValue(((BssMoneyChangeApply.MState)recordModel.state).ToString());
                        cell.CellStyle = style;

                        //处理人
                        Admins proModel = new BssAdmins().GetUserName(recordModel.processname);
                        cell = row.CreateCell(14);
                        cell.SetCellValue(proModel.A_RealName);
                        cell.CellStyle = style;

                        //申请日期
                        cell = row.CreateCell(15);
                        cell.SetCellValue(recordModel.applytime.ToString("yyyy-MM-dd HH:mm:ss"));
                        cell.CellStyle = style;

                        //处理日期
                        cell = row.CreateCell(16);
                        cell.SetCellValue((recordModel.processtime == null ? "" : recordModel.processtime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                        cell.CellStyle = style;

                        //订单编号
                        cell = row.CreateCell(17);
                        cell.SetCellValue((recordModel.OrderId));
                        cell.CellStyle = style;
                    }

                    string fileName = string.Format("资金修改记录_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                    string excelFileName = string.Format("{0}.xls", fileName);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        hssfworkbook.Write(ms);

                        FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                        if (!Directory.Exists(FI.DirectoryName))
                            Directory.CreateDirectory(FI.DirectoryName);
                        FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                        ms.WriteTo(fileUpload);
                        fileUpload.Close();
                        fileUpload = null;
                    }

                    //Excel文件路径
                    string excelFile = Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                    //Excel的Zip文件路径
                    string excelZipFile = Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                    //Excel的Zip文件下载路径
                    string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                    //将文件压缩
                    string errMsg = "";
                    bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                    if (retZip)
                    {
                        //压缩成功删除文件
                        FileInfo fi = new FileInfo(excelFile);
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }

                    return Redirect(excelZipPath);

                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("资金修改申请导出", ex, this.GetType().FullName, "ApplyMoneyChangeListImport");
            }
            return RedirectToAction("ApplyMoneyChangeList");
        }

        [Role("资金修改申请查询当前会员资金", IsAuthorize = true)]
        public ActionResult ApplyMermberMoney(string name)
        {
            string money = "";
            try
            {
                Weike.Member.Members member = new Weike.Member.BssMembers().GetModelByName(name);
                if (member != null)
                {
                    money = member.M_Money.ToString();
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("资金修改申请查询当前会员资金失败", ex, this.GetType().FullName, "ApplyMermberMoney");
            }
            return Content(money);
        }

        #endregion

        [Role("修改会员充值状态", IsAuthorize = true)]
        public ActionResult UpdateStateMoneyHistory(int id, string state)
        {
            BssMoneyHistory bssmoneyhistory = new BssMoneyHistory();
            try
            {
                MoneyHistory m = bssmoneyhistory.GetModel(id);
                if (m != null)
                {
                    m.State = state;
                    bssmoneyhistory.Update(m);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("修改充值记录状态失败", ex, this.GetType().FullName, "UpdateStateMoneyHistory");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        #region 会员财务充值统计
        [Role("会员财务充值统计", IsAuthorize = true)]
        public ActionResult FinanceCyufStatistic(int? Page, string StartTime, string EntTime, string CyufWay, string UID)
        {
            DataPages<Weike.EShop.MoneyHistory> MoneyHistoryPDSList = null;
            try
            {
                string strwhere = " OperaType='充值记录'";
                string stime = "1999-01-01";
                string etime = DateTime.MaxValue.ToString();
                strwhere += " and State='" + BssMoneyHistory.HistoryState.成功.ToString() + "' ";

                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
                strwhere += string.Format("and CreateDate BETWEEN '{0}' AND '{1}'"
                    , StartTime != null ? StartTime != "" ? StartTime : stime : stime
                    , EntTime != null ? EntTime != "" ? EntTime : etime : etime);

                if (CyufWay != null && CyufWay != "")
                    strwhere += string.Format(" and PayType='{0}'", (BssMoneyHistory.PayMoneyType)int.Parse(CyufWay));

                if (!string.IsNullOrEmpty(UID))
                {
                    Weike.Member.Members nowmember = new Weike.Member.BssMembers().GetModelByName(UID);
                    strwhere += string.Format(" and UID={0}", nowmember.M_ID);
                }
                MoneyHistoryPDSList = new BssMoneyHistory().GetPageRecord(strwhere, "CreateDate", 12, Page ?? 1, PagesOrderTypeDesc.降序, "*");

                ViewData["MoneyHistoryPDSList"] = MoneyHistoryPDSList;
                ViewData["MomeySum"] = Weike.EShop.BssMoneyHistory.GetMoneyWhereSum(strwhere);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取会员财务充值统计分页出错：", ex, this.GetType().FullName, "FinanceCyufStatistic");
            }

            return View(MoneyHistoryPDSList);
        }
        #endregion

        [Role("挂单获取游戏商品属性", IsAuthorize = true)]
        public ActionResult GetShopAttributeType(string GUID, string TypeID)
        {
            if (string.IsNullOrEmpty(GUID) || string.IsNullOrEmpty(TypeID))
            {
                return Content("");
            }
            StringBuilder sbhtml = new StringBuilder();
            try
            {
                BssShopAttributeType bssAttribute = new BssShopAttributeType();
                List<ShopAttributeType> attributeList = bssAttribute.GetModelList(string.Format(" GameId='{0}' and TypeId='{1}' order by OrderNo asc", GUID, TypeID));
                if (attributeList != null && attributeList.Count > 0)
                {
                    BssShopAttributeTypeValue bssAttValue = new BssShopAttributeTypeValue();
                    foreach (ShopAttributeType attType in attributeList)
                    {
                        sbhtml.AppendFormat("<span style='margin-right:20px;'><label>{0}：</label>", attType.TypeName);
                        string attrName = Weike.EShop.BssShopAttributeType.AttPrefix + attType.ID;
                        if (attType.FormType == (int)BssShopAttributeType.AttFormType.选择框)
                        {
                            sbhtml.AppendFormat("<select name='{0}' class='input2 req AttSelect'><option value=''>请选择</option>", attrName);
                            List<ShopAttributeTypeValue> attValueList = bssAttValue.GetModelList(string.Format(" AttributeTypeId='{0}' order by OrderNo asc", attType.ID));
                            foreach (ShopAttributeTypeValue attValue in attValueList)
                            {
                                sbhtml.AppendFormat("<option value='{0}'>{1}</option>", attValue.ID, attValue.TypeName);
                            }
                            sbhtml.Append("</select>");
                        }
                        else if (attType.FormType == (int)BssShopAttributeType.AttFormType.横向选择)
                        {
                            List<ShopAttributeTypeValue> attValueList = bssAttValue.GetModelList(string.Format(" AttributeTypeId='{0}' order by OrderNo asc", attType.ID));
                            for (int stvNo = 0; stvNo < attValueList.Count; stvNo++)
                            {
                                sbhtml.AppendFormat("<input type=radio name='{0}' value='{1}' {2}/>{3}", attrName, attValueList[stvNo].ID, stvNo == 0 ? "checked='checked'" : "", attValueList[stvNo].TypeName);
                            }
                        }
                        else
                        {
                            if (attType.FormType == (int)BssShopAttributeType.AttFormType.整数文本)
                            {
                                sbhtml.AppendFormat("<input name='{0}' type='text' class='input2 int'/>", attrName);
                            }
                            else
                            {
                                sbhtml.AppendFormat("<input name='{0}' type='text' class='input2 req'/>", attrName);
                            }
                            if (attType.TypeName.Contains("等级"))
                            {
                                sbhtml.Append(" 级");
                            }
                        }
                        sbhtml.Append("</span>");
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("挂单获取游戏商品属性出错", ex, this.GetType().FullName, "GetShopAttributeType");
            }
            return Content(sbhtml.ToString());
        }

        [Role("订单转单", IsAuthorize = true)]
        public ActionResult ZhuanDan(string spid, string sid, string remark, string PageType)
        {
            BssShoppingAssembly bssSpa = new Weike.EShop.BssShoppingAssembly();
            ShoppingAssembly saModel = bssSpa.GetNoModelBySpId(spid);
            if (saModel != null)
            {
                BssShopping bs = new BssShopping();
                Shopping s = bs.GetModel(spid);
                int oldSid = 0;
                if (s != null)
                {
                    oldSid = s.SID;
                    Weike.CMS.ServiceQQ sqqModel = new Weike.CMS.BssServiceQQ().GetOnlineModelBySid(Convert.ToInt32(sid));
                    if (sqqModel != null)
                    {
                        s.SQQ = sqqModel.S_QQ;
                    }
                    s.SID = Convert.ToInt32(sid);

                }

                #region 插入客服订单流水
                ShoppingAssembly nsaModel = new ShoppingAssembly();
                nsaModel.BuyerName = saModel.BuyerName;
                nsaModel.CreateTime = s.CreateDate.Value;
                nsaModel.GameAccount = saModel.GameAccount;
                nsaModel.ProcessTime = Weike.Common.Globals.MinDateValue;
                nsaModel.Remark = BssShoppingAssembly.SourceType.客服转单.ToString();
                nsaModel.ResType = BssShoppingAssembly.ResType.未处理.ToString();
                nsaModel.SellerName = saModel.SellerName;
                nsaModel.ShopId = saModel.ShopId;
                nsaModel.ShoppingId = saModel.ShoppingId;
                nsaModel.SID = Convert.ToInt32(sid);
                nsaModel.SortNo = saModel.SortNo + 1;
                nsaModel.SourceType = BssShoppingAssembly.SourceType.客服转单.ToString();
                nsaModel.ObjectType = saModel.ObjectType;
                #endregion

                //更新原来流水状态
                saModel.ProcessTime = DateTime.Now;
                saModel.Remark = saModel.Remark + "|" + BssShoppingAssembly.SourceType.客服转单.ToString();
                saModel.ResType = BssShoppingAssembly.ResType.转单处理.ToString();

                bssSpa.AsyncUpdateAssembly(saModel, nsaModel, s);

                //插入订单备注记录
                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(s.ID, remark);

                //发送订单买卖家聊天信息给聊天系统
                BLLAdminOrderMethod.ChangeOrderChatMemberInfo(s.ID);

                if (oldSid == 0)
                {
                    BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(s.ID, BssShoppingDbAutoAssembly.AssType.买家.ToString(), "订单分配客服，等待客服或者卖家处理");

                    //订单通知
                    //阿里云MQ消息订阅
                    bool msgSend = BLLAliyunMQMethod.SendMessage("PaySuccessSendMsgMethod", s, 5);
                    if (!msgSend)
                    {
                        //消息订阅创建失败调用方法执行
                        BLLShopOrderPayMethod.PaySuccessSendMsgMethod(s);
                    }
                }

                //发送订单客服提示信息给聊天系统
                BLLChatSystemMsg.SendOrderKfSysMsgToChat(s);
            }

            Weike.CMS.Admins admin = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
            if (admin.A_Memo.Contains("实习客服"))
                return RedirectToAction("littleUSellOrder", new { t = 's', SName = admin.A_ID });
            else
                if (!string.IsNullOrEmpty(PageType) && PageType.Contains("symanage"))
                    return RedirectToAction("SYSellOrder", "AdminMobileGame", new { SName = admin.A_ID });
                else
                    return RedirectToAction("USellOrder", new { t = 's', SName = admin.A_ID });
        }

        [Role("其他类型客服订单转单", IsAuthorize = true)]
        public ActionResult OtherZhuanDan(string spid, string sid, string remark, string PageType, string stype)
        {
            BssShoppingAssembly bssSpa = new Weike.EShop.BssShoppingAssembly();
            BssShopping bs = new BssShopping();
            Shopping s = bs.GetModel(spid);
            ShoppingAssembly saModel = bssSpa.GetKfjtModelBySpId(spid);
            if (saModel != null && s != null)
            {
                #region 插入截图客服订单流水
                ShoppingAssembly nsaModel = new ShoppingAssembly();
                nsaModel.BuyerName = saModel.BuyerName;
                nsaModel.CreateTime = s.CreateDate.Value;
                nsaModel.GameAccount = saModel.GameAccount;
                nsaModel.ProcessTime = Weike.Common.Globals.MinDateValue;
                nsaModel.Remark = BssShoppingAssembly.SourceType.客服转单.ToString();
                nsaModel.ResType = BssShoppingAssembly.ResType.未处理.ToString();
                nsaModel.SellerName = saModel.SellerName;
                nsaModel.ShopId = saModel.ShopId;
                nsaModel.ShoppingId = saModel.ShoppingId;
                nsaModel.SID = Convert.ToInt32(sid);
                nsaModel.SortNo = saModel.SortNo + 1;
                nsaModel.SourceType = stype == "jt" ? BssShoppingAssembly.SourceType.客服截图.ToString() : BssShoppingAssembly.SourceType.截图完成.ToString();
                nsaModel.ObjectType = saModel.ObjectType;
                #endregion

                //更新原来流水状态
                saModel.ProcessTime = DateTime.Now;
                saModel.Remark = saModel.Remark + "|" + BssShoppingAssembly.SourceType.客服转单.ToString();
                saModel.ResType = BssShoppingAssembly.ResType.转单处理.ToString();

                bssSpa.AsyncUpdateAssembly(saModel, nsaModel, s);

                //插入订单备注记录
                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(s.ID, remark);

            }

            Weike.CMS.Admins admin = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
            if (admin.A_Memo.Contains("实习客服"))
                return RedirectToAction("littleUSellOrder", new { t = 's', SName = admin.A_ID });
            else
                if (!string.IsNullOrEmpty(PageType) && PageType.Contains("symanage"))
                    return RedirectToAction("SYSellOrder", "AdminMobileGame", new { SName = admin.A_ID });
                else
                    return RedirectToAction("USellOrder", new { t = 's', SName = admin.A_ID });
        }
        [Role("挂单", IsAuthorize = true)]
        public ActionResult SetShopDan(string minshopprice, string maxshopprice, string minsignalprice, string count, string time, string maxsignalprice, string type, string GameOtherId, string GameShopTypeId, string name, string signalcount, string SType, string singlenum, string shopprice, string quicksell, string ShopTitle, string ShopCateType)
        {
            if (IsPost)
            {
                try
                {
                    GameInfoModel infoModel = new BLLGame().GetGameInfoModel(GameOtherId, GameShopTypeId, false);
                    string numUnit = "";
                    GameShopType typeModel = null;
                  
                 
                    Members m = new BssMembers().GetModelByName(name);
                    if (m == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('用户不存在');</script>");
                        return View();
                    }
                    GameRoute lastRoute = new BLLGameRoute().GetLastGameRoute(infoModel.GameModel.ID);
                    GameOther gameServer = infoModel.GameOtherList != null && lastRoute!=null ? infoModel.GameOtherList.FirstOrDefault(s => s.CurrentLevelType == lastRoute.LevelType) : null;
                    if (infoModel == null || infoModel.GameModel == null || gameServer == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏失败');</script>");
                        return View();
                    }
                    if (infoModel.GameModel.GameType == BssGame.GameType.手机游戏.ToString())
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('此功能暂不支持手机游戏使用!');</script>");
                        return View();
                    }
                    if (string.IsNullOrEmpty(GameShopTypeId))
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择商品类型');</script>");
                        return View();
                    }
                    if (!string.IsNullOrEmpty(GameShopTypeId))
                    {
                        typeModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品类型) : null;
                        if (infoModel.GameModel != null && typeModel != null)
                        {
                            numUnit = new BssGameUnits().GetOneUnitByGameIdAndShopTypeId(infoModel.GameModel.ID, typeModel.ID);
                        }
                    }
                    GameShopType typeCateModel = null;
                    if (!string.IsNullOrEmpty(ShopCateType))
                    {
                        typeCateModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品子类型) : null;
                        if (typeCateModel == null)
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取小分类出错，请重新发布！');</script>");
                            return View();
                        }
                    }
                    BssShop bssshop = new BssShop();
                    string gameGuid = infoModel.GameModel.GameIdentify;
                    foreach (GameOther item in infoModel.GameOtherList)
                    {
                        gameGuid += "|" + item.GameIdentify;
                    }

                    //非担保商品获取卖家游戏账号和角色
                    string gameAccount = "";
                    string roleName = "";

                    if (type != "2")
                    {
                        try
                        {
                            #region 获取卖家游戏账号和角色
                            BssFormFields ffbll = new BssFormFields();
                            BssFormValue fvbll = new BssFormValue();

                            List<FormValue> lstFvtt = fvbll.GetList(string.Format("ObjectID='{0}'", quicksell)).Tables[0].ToList<FormValue>();
                            foreach (FormValue fv in lstFvtt)
                            {
                                FormFields ffmodel = ffbll.GetModel(fv.FldGuid);
                                if (ffmodel == null)
                                    continue;
                                if (ffmodel.FldName.Contains("游戏账号") || ffmodel.FldName.Contains("游戏帐号"))
                                    gameAccount = fv.FVValue;
                                if (ffmodel.FldName.Contains("角色"))
                                    roleName = fv.FVValue;
                            }
                            #endregion
                        }
                        catch (Exception exz)
                        {
                            LogExcDb.Log_AppDebug("推广挂单处理账号信息出错", exz, this.GetType().FullName, "SetShopDan");
                        }
                    }
                    #region 对于有属性的商品获取属性和添加标题内容

                    List<ShopAttributes> attList = new List<ShopAttributes>();
                    string titleAdd = "";
                    string titleEndAdd = "";
                    BssShopAttributeTypeValue.GetShopAttributes(out titleAdd, out titleEndAdd, out attList);
                    #region 将要显示在标题的属性信息填充标题

                    string titleDesc = "";
                    titleDesc = titleDesc + "【";
                    titleDesc = titleDesc + titleAdd;

                    titleDesc = titleDesc + "】";
                    if (titleDesc == "【】" || titleDesc == "【普通交易 】")
                        titleDesc = "";

                    if (titleAdd.Contains("快捷交易"))
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('发布快捷交易类型必须押金赔付');</script>");
                        return View();
                    }
                    #endregion

                    #endregion
                    //判断是否随机发货时间
                    MemberChargeCredit mccModel = new BssMemberChargeCredit().GetModelBuyName(name);

                    if (count.ToInt32() > 10)
                        count = "10";

                    string[] Titles = { "【绿色商品，安全迅速】", "【买的放心，用的安心】", "【安全可靠，即买即发】" };
                    for (int i = 0; i < count.ToInt32(); i++)
                    {
                        Random rand = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));

                        DateTime publicTime = DateTime.Now;
                        if (mccModel != null && mccModel.isRandTime)
                        {
                            publicTime = publicTime.AddMinutes(0 - rand.Next(0, 20));
                        }

                        string shopId = BssShop.CreateShopSN(type.ToInt32(), publicTime);

                        Shop shop = new Shop();
                        shop.CreateDate = publicTime;
                        shop.TimeId = BssTimeConfig.DEFAULT_TIMECONFIGID;
                        shop.DealType = type.ToInt32();
                        shop.GameType = GameOtherId;
                        shop.GameGUID = gameGuid;
                        shop.ShopType = typeModel == null ? "" : typeModel.ID;
                        shop.ShopTypeCate = typeCateModel == null ? "" : typeCateModel.ID;
                        shop.ShopTypeGuid = typeModel == null ? "" : typeCateModel != null ? typeModel.GameShopTypeIdentify + "|" + typeCateModel.GameShopTypeIdentify : typeModel.GameShopTypeIdentify; ;
                        shop.NumUnit = numUnit;
                        shop.OutMoneyType = 1;
                        shop.OverDay = time.ToInt32();
                        if (SType == "1")
                        {
                            shop.Price = rand.Next(minshopprice.ToInt32(), maxshopprice.ToInt32());
                            int l = minsignalprice.Length - minsignalprice.IndexOf('.') - 1;
                            shop.SinglePrice = decimal.Parse((rand.Next((minsignalprice.ToDouble2() * Math.Pow(10, l)).ToInt32(), (maxsignalprice.ToDouble2() * Math.Pow(10, l)).ToInt32()) / Math.Pow(10, l)).ToString());
                            shop.Num = Math.Round((shop.Price / shop.SinglePrice), 2);
                        }
                        else if (SType == "2")
                        {
                            shop.Price = shopprice.ToInt32();
                            shop.Num = Convert.ToDecimal(singlenum);
                            shop.SinglePrice = Convert.ToDecimal(shop.Price.ToDouble2() / shop.Num.ToDouble2());
                        }
                        shop.PublicCount = signalcount.ToInt32();
                        shop.PublicUser = m.M_ID;
                        shop.ShopID = shopId;
                        shop.ShopState = BssShop.ShopState.审核成功.ToString();
                        //给标题赋值
                        string title = "";
                        if (SType == "2" && typeModel != null && typeModel.Property != BssGameShopType.Property.游戏币.ToString() && typeModel.Property != BssGameShopType.Property.直冲点券.ToString())
                        {
                            title = titleDesc + ShopTitle + titleEndAdd;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(titleEndAdd))
                            {
                                title = titleDesc + string.Format("{0}{1}={2}元{3}", shop.Num, shop.NumUnit, shop.Price, titleEndAdd);
                            }
                            else
                            {
                                title = titleDesc + string.Format("{0}{1}={2}元{3}", shop.Num, shop.NumUnit, shop.Price, Titles[rand.Next(3)]);
                            }
                        }
                        if (title.Length > 100)
                            title = title.Substring(0, 100);
                        shop.Title = title;
                        shop.SinglePriceUnit = "元/" + shop.NumUnit;
                        shop.GUID = quicksell;
                        shop.AccountNo = gameAccount;
                        shop.RoleName = roleName;
                        shop.ExpirationTime = DateTime.Now.AddDays(time.ToInt32());
                        shop.SortingWeight = BssMembers.GetMembersSortingWeight(infoModel.GameModel.ID, m, shop, 0, 100, "", false, false);
                        shop.TotalCount = shop.PublicCount;

                        if (shop.DealType == (int)BssShop.EDealType.担保)
                        {
                            MembersInfo mi = new BssMembersInfo().GetModel(m.M_ID);
                            if (mi != null && mi.IsOnline == (int)BssMembersInfo.IsOnline.离线)
                            {
                                shop.ShopState = BssShop.ShopState.卖家隐身.ToString();
                            }
                        }

                        int shopID = bssshop.Add(shop);
                        #region 写入商品属性
                        try
                        {
                            ShopAttributes attModel = null;
                            BssShopAttributes bssAtt = new BssShopAttributes();
                            foreach (ShopAttributes att in attList)
                            {
                                attModel = new ShopAttributes();
                                attModel.AttributeId = att.AttributeId;
                                attModel.CreateTime = DateTime.Now;
                                attModel.ShopId = shopID;
                                attModel.ShopNo = shopId;
                                attModel.AttributeValue = att.AttributeValue;
                                attModel.AttributeTypeId = att.AttributeTypeId;
                                bssAtt.Add(attModel);
                            }
                        }
                        catch (Exception exa)
                        {
                            LogExcDb.Log_AppDebug("写入商品属性出错", exa, this.GetType().FullName, "Finish");
                        }
                        #endregion
                    }
                    MsgHelper.Insert("res", "<a style='color:#FF0000'>挂单成功!</a>");
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("挂单出错", ex, Server + "|" + this.GetType().FullName, "SetShopDan");
                }
            }
            return View();
        }

        [Role("其他挂单", IsAuthorize = true)]
        public ActionResult SetZhShopDan(string GameId,string GameOtherId, string GameShopTypeId, string SellerName, string ShopTitle, string ShopDetail, string ShopPrice, string PublicCount, string ShopCount, string OverDay)
        {
            if (IsPost)
            {
                try
                {
                    if (string.IsNullOrEmpty(GameId))
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏类型失败');</script>");
                        return View();
                    }
                    GameInfoModel infoModel = new BLLGame().GetGameInfoModel(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, GameShopTypeId, false);
                    if (infoModel == null || infoModel.GameModel == null) 
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏信息失败');</script>");
                        return View();
                    }
                    if (infoModel.GameModel.GameType == BssGame.GameType.手机游戏.ToString())
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('此功能暂不支持手机游戏使用!');</script>");
                        return View();
                    }
                    GameShopType typeModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品类型) : null;
                    if (typeModel == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取商品类型失败');</script>");
                        return View();
                    }
                    if (infoModel.GameOtherList == null || infoModel.GameOtherList.Count <= 0)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏类型失败');</script>");
                        return View();
                    }
                    if (!typeModel.IsAll && infoModel.GameOtherList.Count <2)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏类型失败');</script>");
                        return View();
                    }

                    Members sellerModel = new BssMembers().GetModelByName(SellerName);
                    if (sellerModel == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('卖家用户不存在');</script>");
                        return View();
                    }

                    if ((typeModel.Property == BssGameShopType.Property.帐号.ToString() || typeModel.Property == BssGameShopType.Property.激活帐号.ToString()) && (string.IsNullOrEmpty(PublicCount) || (PublicCount != "0" && PublicCount != "1")))
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('账号商品单价数量只能为0或者1');</script>");
                        return View();
                    }
                    GameShopType typeCateModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品子类型) : null;
                    typeCateModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品子类型) : null;
                    if (typeCateModel == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取小分类出错，请重新发布！');</script>");
                        return View();
                    }
                    #region 对于有属性的商品获取属性和添加标题内容

                    List<ShopAttributes> attList = new List<ShopAttributes>();
                    string titleAdd = "";
                    string titleEndAdd = "";
                    BssShopAttributeTypeValue.GetShopAttributes(out titleAdd, out titleEndAdd, out attList);
                    #region 将要显示在标题的属性信息填充标题

                    string titleDesc = "";
                    titleDesc = titleDesc + "【";
                    titleDesc = titleDesc + titleAdd;

                    titleDesc = titleDesc + "】";
                    if (titleDesc == "【】" || titleDesc == "【普通交易 】")
                        titleDesc = "";

                    if (titleAdd.Contains("快捷交易"))
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('发布快捷交易类型必须押金赔付');</script>");
                        return View();
                    }
                    #endregion

                    #endregion
                    BssShop bssShop = new BssShop();
                    Shop shop = null;
                    string gameGuid = infoModel.GameModel.GameIdentify;
                    foreach (GameOther item in infoModel.GameOtherList)
                    {
                        gameGuid += "|" + item.GameIdentify;
                    }
                    if (ShopCount.ToInt32() > 10)
                        ShopCount = "10";

                    for (int i = 0; i < ShopCount.ToInt32(); i++)
                    {
                        string shopId = BssShop.CreateShopSN(typeModel == null ? 3 : 2);

                        shop = new Shop();
                        shop.CreateDate = DateTime.Now;
                        shop.TimeId = BssTimeConfig.DEFAULT_TIMECONFIGID;
                        shop.DealType = typeModel == null ? 3 : 2;
                        shop.GameType = typeModel.IsAll ? infoModel.GameModel.ID :infoModel.GameOtherList[infoModel.GameOtherList.Count-1].ID;
                        shop.GameGUID = typeModel.IsAll ? infoModel.GameModel.GameIdentify : gameGuid;
                        shop.ShopType = GameShopTypeId;
                        shop.ShopTypeCate = typeCateModel == null ? "" : typeCateModel.ID;
                        shop.ShopTypeGuid = typeModel == null ? "" : typeModel.GameShopTypeIdentify + "|" + typeCateModel != null ? typeCateModel.GameShopTypeIdentify : "";
                        shop.NumUnit = "";
                        shop.OutMoneyType = 1;
                        shop.OverDay = OverDay.ToInt32();
                        shop.Price = decimal.Parse(ShopPrice);
                        shop.SinglePrice = decimal.Parse(ShopPrice);
                        shop.Num = 1;
                        shop.PublicCount = PublicCount.ToInt32();
                        shop.PublicUser = sellerModel.M_ID;
                        shop.ShopID = shopId;
                        shop.ShopState = BssShop.ShopState.审核成功.ToString();
                        shop.Title = titleDesc + ShopTitle + titleEndAdd;
                        shop.Detail = ShopDetail;
                        shop.SinglePriceUnit = "";
                        shop.GUID = "";
                        shop.AccountNo = "";
                        shop.RoleName = "";
                        shop.ExpirationTime = DateTime.Now.AddDays(OverDay.ToInt32());
                        shop.SortingWeight = BssMembers.GetMembersSortingWeight(infoModel.GameModel.ID, sellerModel, shop, 0, 100, "", false, false);
                        shop.TotalCount = shop.PublicCount;

                        if (shop.DealType == (int)BssShop.EDealType.担保)
                        {
                            MembersInfo mi = new BssMembersInfo().GetModel(sellerModel.M_ID);
                            if (mi != null && mi.IsOnline == (int)BssMembersInfo.IsOnline.离线)
                            {
                                shop.ShopState = BssShop.ShopState.卖家隐身.ToString();
                            }
                        }

                        int shopID = bssShop.Add(shop);
                        #region 写入商品属性
                        try
                        {
                            ShopAttributes attModel = null;
                            BssShopAttributes bssAtt = new BssShopAttributes();
                            foreach (ShopAttributes att in attList)
                            {
                                attModel = new ShopAttributes();
                                attModel.AttributeId = att.AttributeId;
                                attModel.CreateTime = DateTime.Now;
                                attModel.ShopId = shopID;
                                attModel.ShopNo = shopId;
                                attModel.AttributeValue = att.AttributeValue;
                                attModel.AttributeTypeId = att.AttributeTypeId;
                                bssAtt.Add(attModel);
                            }
                        }
                        catch (Exception exa)
                        {
                            LogExcDb.Log_AppDebug("写入商品属性出错", exa, this.GetType().FullName, "Finish");
                        }
                        #endregion
                    }
                    MsgHelper.Insert("res", "<a style='color:#FF0000'>挂单成功!</a>");
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("其他挂单出错", ex, Server + "|" + this.GetType().FullName, "SetZhShopDan");
                }
            }
            return View();
        }

        private bool FldContainsListItem(string fldName, List<string> list)
        {
            foreach (string str in list)
            {
                if (fldName.Contains(str))
                {
                    return true;
                }
            }
            return false;
        }

        [Role("获取帐号商品模版", IsAuthorize = true)]
        public ActionResult DownloadAccShopExcel(string ServerId, string ShopType)
        {
            try
            {
                GameInfoModel infoModel = new BLLGame().GetGameInfoModel(ServerId, ShopType, false);
                if (infoModel == null || infoModel.GameModel == null || infoModel.GameOtherList == null)
                {
                    MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏失败');</script>");
                    return RedirectToAction("ImportAccShop");
                }
                GameRoute lastRoute = new BLLGameRoute().GetLastGameRoute(infoModel.GameModel.ID);
                if (lastRoute == null )
                {
                    MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏配置失败');</script>");
                    return RedirectToAction("ImportAccShop");
                }
                GameOther serverModel = infoModel.GameOtherList.FirstOrDefault(s => s.CurrentLevelType == lastRoute.LevelType);
                if (serverModel == null || serverModel.CurrentLevelType != lastRoute.LevelType || !serverModel.IsEnabled)
                {
                    MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择" + lastRoute .RouteName+ "');</script>");
                    return RedirectToAction("ImportAccShop");
                }
                GameShopType typeModel = infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品类型);
                if (typeModel == null)
                {
                    MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择商品类型');</script>");
                    return RedirectToAction("ImportAccShop");
                }

                bool isTxGame = BssGameCompany.IsTencentGame(infoModel.GameModel.ID);
                string retUrl = Request.Url.Host;

                HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                ISheet sheet1 = hssfworkbook.CreateSheet("帐号商品信息");
                NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "DD373 Team";
                NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                si.Subject = "http://www.dd373.com/";
                hssfworkbook.DocumentSummaryInformation = dsi;
                hssfworkbook.SummaryInformation = si;

                IRow row1 = sheet1.CreateRow(0);

                IFont font1 = hssfworkbook.CreateFont();
                font1.Boldweight = (short)FontBoldWeight.Bold;
                font1.FontName = "宋体";
                font1.FontHeightInPoints = 11;
                ICellStyle style1 = hssfworkbook.CreateCellStyle();
                style1.SetFont(font1);

                List<string> fldNames = new List<string>() { "游戏帐号", "游戏账号", "游戏密码" };
                List<string> fldTxExtNames = new List<string>() { "密保问题1", "密保问题2", "密保问题3", "答案1", "答案2", "答案3" };//腾讯游戏额外字段
                List<string> fldJDQSNames = new List<string>() {  "绑定邮箱", "邮箱密码"};//绝地求生额外字段

                //生成标题
                List<FormFields> ffList = new BssFormFields().GetModelList(string.Format(" ObjectID='{0}' and TypeID='{1}' and ObjectType='{2}'", infoModel.GameModel.ID, typeModel.ID, FormFields.FormType.寄售表单.ToString()));
                if (ffList != null && ffList.Count > 0)
                {
                    sheet1.SetColumnWidth(0, 18 * 256);

                    ICell cellTile = row1.CreateCell(0);
                    cellTile.SetCellValue("商品标题");
                    cellTile.CellStyle = style1;

                    sheet1.SetColumnWidth(1, 18 * 256);
                    ICell cellMemo = row1.CreateCell(1);
                    cellMemo.SetCellValue("商品描述");
                    cellMemo.CellStyle = style1;

                    sheet1.SetColumnWidth(2, 18 * 256);
                    ICell cellPrice = row1.CreateCell(2);
                    cellPrice.SetCellValue("价格");
                    cellPrice.CellStyle = style1;
                    int cellNo = 3;
                    foreach (FormFields ff in ffList)
                    {
                        if ((FldContainsListItem(ff.FldName, fldNames) && !ff.FldName.Contains("确认")) || (isTxGame && FldContainsListItem(ff.FldName, fldTxExtNames)) || (infoModel.GameModel.GameName.Contains("绝地求生") && FldContainsListItem(ff.FldName, fldJDQSNames)))
                        {
                            sheet1.SetColumnWidth(cellNo, 18 * 256);

                            ICell cell = row1.CreateCell(cellNo);
                            cell.SetCellValue(ff.FldName);
                            cell.CellStyle = style1;
                            cellNo++;
                        }
                    }
                }

                string adminName = BLLAdmins.GetCurrentAdminUserInfo().A_Name;

                string downloadfilename = infoModel.GameModel.GameName + ".xls";
                using (MemoryStream ms = new MemoryStream())
                {
                    hssfworkbook.Write(ms);

                    FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/Template/{0}/{1}", adminName, downloadfilename)));
                    if (!Directory.Exists(FI.DirectoryName))
                        Directory.CreateDirectory(FI.DirectoryName);
                    FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/Template/{0}/{1}", adminName, downloadfilename)), FileMode.Create);
                    ms.WriteTo(fileUpload);
                    fileUpload.Close();
                    fileUpload = null;
                }
                retUrl = string.Format("/Template/{0}/{1}", adminName, downloadfilename);

                return Redirect(retUrl);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取帐号商品模版出错", ex, this.GetType().FullName, "DownloadAccShopExcel");
            }

            return View();
        }


        [Role("批量导入帐号商品", IsAuthorize = true)]
        public ActionResult ImportAccShop(string GameOtherId, string GameShopTypeId, string SellerName,string OverDay,string aqbz, string BuyPwd)
        {
            if (IsPost)
            {
                try
                {
                    GameInfoModel infoModel = null;
                    GameOther serverModel = null;
                    if (OverDay.ToInt32()<=0)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('有效时长不能小于0');</script>");
                        return View();
                    }
                    if (!string.IsNullOrEmpty(GameOtherId) && GameOtherId.Length > 1)
                    {
                        infoModel = new BLLGame().GetGameInfoModel(GameOtherId, GameShopTypeId, false);
                        if (infoModel == null || infoModel.GameModel == null || infoModel.GameOtherList == null)
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('获取游戏失败');</script>");
                            return View();
                        }
                        if (infoModel.GameModel.GameType == BssGame.GameType.手机游戏.ToString())
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('此功能暂不支持手机游戏使用!');</script>");
                            return View();
                        }
                        GameRoute lastRoute = new BLLGameRoute().GetLastGameRoute(infoModel.GameModel.ID);
                        if (lastRoute == null)
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('游戏配置错误，无法上传！');</script>");
                            return View();
                        }
                        serverModel = infoModel.GameOtherList.FirstOrDefault(s => s.CurrentLevelType == lastRoute.LevelType);
                        if (serverModel == null  || !serverModel.IsEnabled)
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择"+lastRoute.RouteName+"');</script>");
                            return View();
                        }
                        
                    }
                    else
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择游戏区服');</script>");
                        return View();
                    }
                    GameShopType typeModel = infoModel.GameShopTypeList != null ? infoModel.GameShopTypeList.FirstOrDefault(s => s.CurrentLevelType == (int)BssGameRoute.LevelType.商品类型) : null;
                    if (typeModel == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择商品类型');</script>");
                        return View();
                    }
                    if (typeModel.Property != BssGameShopType.Property.帐号.ToString() && typeModel.Property != BssGameShopType.Property.激活帐号.ToString())
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('只能选择属性为帐号或激活帐号商品类型');</script>");
                        return View();
                    }
                    Members sellerModel = new BssMembers().GetModelByName(SellerName);
                    if (sellerModel == null)
                    {
                        MsgHelper.Insert("res", "<script type='text/javascript'>alert('卖家用户不存在');</script>");
                        return View();
                    }
                    if (typeModel.IsAll)
                    {
                        serverModel = null;
                    }
                    string gameGuid = infoModel.GameModel.GameIdentify;
                    if ((typeModel.IsAll && infoModel.GameOtherList.FirstOrDefault(f => f.ID == typeModel.ParentId) != null) || !typeModel.IsAll)
                    {
                        foreach (GameOther item in infoModel.GameOtherList)
                        {
                            gameGuid += "|" + item.GameIdentify;
                            if (typeModel.IsAll&&item.ID == typeModel.ParentId)
                            {
                                serverModel = item;
                                break;
                            }
                        }
                    }
                    
                    List<GameTypeSafeSellConfig> safeConfigList = new BssGameTypeSafeSellConfig().GetModelListByGameIdAndShopType(infoModel.GameModel.ID, typeModel.ID);
                    int MinSellCount = safeConfigList != null && safeConfigList.Count > 0 ? safeConfigList.OrderByDescending(p => p.MinSellCount).FirstOrDefault().MinSellCount : 0; ;//获取集合中最大的 最小出售数量
                    decimal MinSellPercent = safeConfigList != null && safeConfigList.Count > 0 ? safeConfigList.OrderByDescending(p => p.MinSellPercent).FirstOrDefault().MinSellPercent : 0; //获取集合中最大的 最小成交率
                    bool CanFreeSafe = false;
                    if ((sellerModel.M_SellerPraiseNum >= MinSellCount && (Convert.ToDecimal(sellerModel.M_SellerPraiseNum) / Convert.ToDecimal(sellerModel.M_SellerSoldNum > 0 ? sellerModel.M_SellerSoldNum : 1)) >= MinSellPercent / 100))
                    {
                        CanFreeSafe = true;
                    }
                    GameTypeSafeSellConfig safeConfigModel = null;
                    if (!string.IsNullOrEmpty(aqbz))
                    {
                        safeConfigModel = safeConfigList.FirstOrDefault(f => f.ID == aqbz);
                    }
                    else
                    {
                        if (!CanFreeSafe)
                        {
                            MsgHelper.Insert("res", "<script type='text/javascript'>alert('请选择找回包赔');</script>");
                            return View();
                        }
                    }
                    List<FormFields> ffList = new BssFormFields().GetModelList(string.Format(" ObjectID='{0}' and TypeID='{1}' and ObjectType='{2}'",infoModel.GameModel.ID, typeModel.ID, FormFields.FormType.寄售表单.ToString()));
                    DataTable dt = ImportExcelToDatatable("ExcelFile");
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        BssShop bssShop = new BssShop();
                        Shop shop = null;
                        var length = dt.Columns.Count;
                        string title = "";
                        string memo = "";
                        string price = "";
                        string accountText = "";
                        string passowrd = "";
                        int successCount = 0;
                        foreach (DataRow dr in dt.Rows)
                        {
                            try
                            {
                                title = dr[0].ToString();
                                memo = dr[1].ToString();
                                price = dr[2].ToString();
                                passowrd = dr[4].ToString();
                                accountText = dr[3].ToString();
                                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(price) || string.IsNullOrEmpty(accountText) || string.IsNullOrEmpty(passowrd))
                                    continue;
                                if (dr[2].ToString().ToInt32() <= 0)//判断价格是否大于0
                                {
                                    continue;
                                }
                                #region 帐号商品判断是否重复发布和允许当前会员发布
                                try
                                {
                                    if (!string.IsNullOrEmpty(accountText))
                                    {
                                        if (BssShop.GameAccountReSell(sellerModel.M_ID, infoModel.GameModel.ID, accountText))
                                        {
                                            continue;
                                        }
                                        bool IsCheckAccSell = true;
                                        GameCompanyInfo gciModel = new BssGameCompanyInfo().GetModel(infoModel.GameModel.ID);
                                        if (gciModel != null && gciModel.CompanyId == 1)
                                        {
                                            string pattern = @"(^1\d{10}$)";
                                            if (System.Text.RegularExpressions.Regex.IsMatch(accountText, pattern))
                                            {
                                                IsCheckAccSell = false;
                                            }
                                        }
                                        if (IsCheckAccSell)
                                        {
                                            if (!BssShopAccount.GameAccountSameUserSell(infoModel.GameModel.ID, accountText, sellerModel.M_ID))//判断该帐号是否允许该用户发布
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                                catch (Exception exre)
                                {
                                    LogExcDb.Log_AppDebug("提交我要卖判断账号商品重复发布出错", exre, this.GetType().FullName, "Finish");
                                }
                                #endregion
                                #region 验证标题和描述是否包含屏蔽关键词，若有则跳出不插入数据库
                                List<KeyValues> ListKey = new BLLKeyValues().GetModelList(string.Format(" category=8 and Enalbed=1"));
                                StringBuilder sb = new StringBuilder("(");
                                foreach (KeyValues key in ListKey)
                                {
                                    sb.Append(key.Value);
                                    sb.Append("|");
                                }
                                if (sb.Length > 2)
                                    sb.Remove(sb.Length - 1, 1);
                                sb.Append(")");

                                string sbStr = sb.ToString();
                                if (sbStr.Length < 3)
                                    sbStr = "";

                                if (!string.IsNullOrEmpty(sbStr))
                                {
                                    MatchCollection mc = Regex.Matches(title, sbStr, RegexOptions.IgnoreCase);
                                    if (mc.Count != 0)
                                    {
                                        continue;
                                    }
                                    MatchCollection mc1 = Regex.Matches(memo, sbStr, RegexOptions.IgnoreCase);
                                    if (mc1.Count != 0)
                                    {
                                        continue;
                                    }
                                }
                                #endregion
                                string ShopGuid = Guid.NewGuid().ToString();
                                //插入表单
                                string errMsg = string.Empty;
                                bool isRight = true;
                                for (int i = 3; i < length; i++)
                                {
                                    FormFields ffModel = ffList.Where(f => f.FldName == dt.Columns[i].ColumnName).FirstOrDefault();
                                    isRight = BLLFormValueMethod.CheckFormValue(ffModel, dr[i].ToString(), out errMsg);//验证表单内容
                                    if (!isRight)
                                    {
                                        break;
                                    }
                                    if (ffModel != null)
                                    {
                                        FormValue fvModel = new FormValue();
                                        fvModel.createtime = DateTime.Now;
                                        fvModel.FldGuid = ffModel.FldGuid;
                                        fvModel.FVValue = dr[i].ToString();
                                        fvModel.ObjectID = ShopGuid;
                                        fvModel.FVGuid = Guid.NewGuid().ToString();
                                        new BssFormValue().Add(fvModel);
                                    }
                                }
                                if (!isRight)
                                {
                                    continue;
                                }
                                //生成商品
                                shop = new Shop();
                                shop.CreateDate = DateTime.Now;
                                shop.TimeId = BssTimeConfig.DEFAULT_TIMECONFIGID;
                                shop.DealType = 3;
                                shop.GameType = serverModel == null ? infoModel.GameModel.ID : serverModel.ID;
                                shop.GameGUID = gameGuid;
                                shop.ShopType = typeModel.ID;
                                shop.ShopTypeGuid = typeModel.GameShopTypeIdentify + "|";
                                shop.NumUnit = "";
                                shop.OutMoneyType = safeConfigModel != null ? 3 : 1;
                                shop.OverDay = OverDay.ToInt32();
                                shop.Price = decimal.Parse(price);
                                shop.SinglePrice = decimal.Parse(price);
                                shop.Num = 1;
                                shop.PublicCount = 1;
                                shop.PublicUser = sellerModel.M_ID;
                                shop.ShopID = BssShop.CreateShopSN(3);
                                shop.ShopState = BssShop.ShopState.审核成功.ToString();
                                shop.Title = Globals.NoHTML(title);
                                shop.Detail = Globals.NoHTML(memo).Replace(" ", "&nbsp;").Replace("\n", "<br/>"); 
                                shop.SinglePriceUnit = "";
                                shop.GUID = ShopGuid;
                                shop.AccountNo = dr[3].ToString();
                                shop.RoleName = "";
                                shop.ExpirationTime = string.IsNullOrEmpty(BuyPwd.Trim()) ? DateTime.Now.AddDays(OverDay.ToInt32()) : DateTime.Now.AddMinutes(20);
                                shop.SortingWeight = BssMembers.GetMembersSortingWeight(infoModel.GameModel.ID, sellerModel, shop, 0, 100, "", false, false);
                                shop.TotalCount = shop.PublicCount;
                                bssShop.Add(shop);

                                #region 指定买家购买
                                if (!string.IsNullOrEmpty(BuyPwd.Trim()))
                                {
                                    ShopOtherInfo otherInfoModel = new ShopOtherInfo();
                                    otherInfoModel.ConfigId = (int)BssShopOtherInfo.InfoType.指定买家购买;
                                    otherInfoModel.ConfigValues = BuyPwd.Trim();
                                    otherInfoModel.CreateTime = DateTime.Now;
                                    otherInfoModel.ShopNo = shop.ShopID;
                                    new BssShopOtherInfo().Add(otherInfoModel);
                                }
                                #endregion
                                #region 安全保障记录
                                if ( safeConfigModel != null)
                                {
                                    ShopOtherInfo otherInfoModel = new ShopOtherInfo();
                                    otherInfoModel.ConfigId = (int)BssShopOtherInfo.InfoType.商品安全保障;
                                    otherInfoModel.ConfigValues = safeConfigModel.PassDay.ToString();
                                    otherInfoModel.CreateTime = DateTime.Now;
                                    otherInfoModel.ShopNo = shop.ShopID;
                                    new BssShopOtherInfo().Add(otherInfoModel);
                                }
                                #endregion
                                #region 增加商品附属信息
                                ShopAccount account = new ShopAccount();
                                account.AccountNo = dr[3].ToString();
                                account.CreateTime = DateTime.Now;
                                account.GameId = infoModel.GameModel.ID;
                                account.M_ID = sellerModel.M_ID;
                                account.ShopNo = shop.ShopID;
                                account.Sid = 0;
                                account.ShopId = Weike.Config.BLLSequences.GetSequencesIntNo("ShopAccount", 500000000);
                                account.OrderState = "";
                                account.OrderId = "";
                                new BssShopAccount().Add(account);
                                #endregion
                                successCount++;
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                        if (successCount > 0)
                        {
                            MsgHelper.Insert("res", "<a style='color:#FF0000'>导入成功" + successCount + "条，失败" + (dt.Rows.Count - successCount) + "条!</a>");
                        }
                        else
                        {
                            MsgHelper.Insert("res", "<a style='color:#FF0000'>导入失败，没有可导入的数据!</a>");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("批量导入帐号商品出错", ex, Server + "|" + this.GetType().FullName, "ImportAccShop");
                }
            }
            return View();
        }

        public DataTable ImportExcelToDatatable(string fileName)
        {
            DataTable dt = new DataTable();
            try
            {
                HttpPostedFileBase file = Request.Files[fileName];
                if (file.ContentLength > 0)
                {
                    Stream excelFileStream = file.InputStream;
                    IWorkbook workbook = new HSSFWorkbook(excelFileStream);
                    ISheet sheet = workbook.GetSheetAt(0);
                    DataTable table = new DataTable();

                    IRow headerRow = sheet.GetRow(0);//第一行为标题行
                    int cellCount = headerRow.LastCellNum;//LastCellNum = PhysicalNumberOfCells
                    int rowCount = sheet.LastRowNum;//LastRowNum = PhysicalNumberOfRows - 1

                    //handling header.
                    for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                    {
                        DataColumn column = new DataColumn(headerRow.GetCell(i).StringCellValue);
                        table.Columns.Add(column);
                    }

                    for (int i = (sheet.FirstRowNum + 1); i <= rowCount; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        DataRow dataRow = table.NewRow();

                        if (row != null)
                        {
                            for (int j = row.FirstCellNum; j < cellCount; j++)
                            {
                                if (row.GetCell(j) != null)
                                    dataRow[j] = row.GetCell(j).ToString();
                            }
                        }

                        table.Rows.Add(dataRow);
                    }

                    dt = table;
                }
                else
                {
                    MsgHelper.Insert("res", "<a style='color:#FF0000'>没有选择文件!</a>");
                }
            }
            catch (Exception ex)
            {
                MsgHelper.Insert("res", "<a style='color:#FF0000'>模板格式错误!</a>");
                LogExcDb.Log_AppDebug("导入Excel出错", ex, this.GetType().FullName, "ImportExcelToDatatable");
            }
            return dt;
        }

        public ActionResult GetGameTypeSafeSellConfig(string GameId, string TypeId)
        {
            StringBuilder str = new StringBuilder();
            try
            {
                List<GameTypeSafeSellConfig> list = new BssGameTypeSafeSellConfig().GetModelListByGameIdAndShopType(GameId, TypeId);
                if (list != null && list.Count > 0)
                {
                    return Json(list, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取安全保障数据出错。", ex, this.GetType().FullName, "GetGameTypeSafeSellConfig");
            }
            return Content("");
        }

        [Role("修改订单状态", IsAuthorize = true)]
        public ActionResult ChangeShoppingState(FormCollection f)
        {
            if (IsPost)
            {
                if (f["type"] != "0" && f["type"] != "1")
                {
                    MsgHelper.Insert("CSS", "请选择正确的订单状态");
                    return View();
                }

                BssShopping bsshopping = new BssShopping();
                if (bsshopping.Exists(f["GM"]))
                {
                    string remarkInfo = "";
                    string oldState = "";
                    Shopping sp = bsshopping.GetModel(f["GM"]);
                    oldState = sp.State;
                    if (f["type"] == "0")
                    {
                        remarkInfo = "失败方：【其他】" + f["remark"] + "，手动修改订单状态：用户名[" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "]";
                        
                        sp.State = BssShopping.ShoppingState.交易取消.ToString();
                    }
                    else if (f["type"] == "1")
                    {
                        remarkInfo = "手动修改订单状态";

                        sp.State = BssShopping.ShoppingState.交易成功.ToString();
                    }
                    sp.StateType = (int)BssShopping.ShoppingStateType.处理完成;

                    BssShoppingAssembly bssSpa = new Weike.EShop.BssShoppingAssembly();
                    ShoppingAssembly saModel = null;
                    ShoppingAssembly nsaModel = null;

                    if (f["type"] != "2")
                    {
                        saModel = bssSpa.GetNoModelBySpId(f["GM"]);
                        if (saModel != null)
                        {
                            saModel.ResType = f["type"] == "0" ? "交易取消" : "交易成功";
                            saModel.ProcessTime = DateTime.Now;
                            saModel.Remark = saModel.Remark + (f["type"] == "0" ? "交易取消" : "交易成功");
                        }
                    }
                    else
                    {
                        saModel = bssSpa.GetLastModelBySpId(f["GM"]);
                        if (saModel != null)
                        {
                            #region 插入客服订单流水
                            nsaModel = new ShoppingAssembly();
                            nsaModel.BuyerName = saModel.BuyerName;
                            nsaModel.CreateTime = sp.CreateDate.Value;
                            nsaModel.GameAccount = saModel.GameAccount;
                            nsaModel.ProcessTime = Weike.Common.Globals.MinDateValue;
                            nsaModel.Remark = "手动修改订单状态系统分单";
                            nsaModel.ResType = BssShoppingAssembly.ResType.未处理.ToString();
                            nsaModel.SellerName = saModel.SellerName;
                            nsaModel.ShopId = saModel.ShopId;
                            nsaModel.ShoppingId = saModel.ShoppingId;
                            nsaModel.SID = sp.SID;
                            nsaModel.SortNo = saModel.SortNo + 1;
                            nsaModel.SourceType = BssShoppingAssembly.SourceType.客服转单.ToString();
                            nsaModel.ObjectType = saModel.ObjectType;
                            #endregion

                            if (saModel.ResType == "未处理")
                            {
                                saModel.ResType = "系统转单";
                                saModel.ProcessTime = DateTime.Now;
                                saModel.Remark = saModel.Remark + "|系统转单";
                            }
                        }
                    }
                    //提交事务
                    bssSpa.AsyncUpdateAssemblyWithNoState(saModel, nsaModel, sp);

                    //插入订单备注记录
                    BssShoppingRemarkInfo.InsertShoppingRemarkInfo(sp.ID, remarkInfo);

                    MsgHelper.Insert("CSS", f["GM"] + "状态修改成功，修改前状态" + oldState + "，修改后状态" + sp.State);
                    BssModifyMembersRecording.AddCaoZuoRecording(sp.UserID, Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo().A_ID, (int)BssModifyMembersRecording.ECate.修改订单状态, sp.ID, string.Format("订单编号{0},状态改为{1}", f["GM"], f["type"] == "0" ? "交易取消" : "交易成功"));
                    return View();
                }
                else
                {
                    MsgHelper.Insert("CSS", "GM单号不存在");
                    return View();
                }
            }
            return View();
        }
        [Role("处理单量前三名", IsAuthorize = true)]
        public ActionResult AccessShoppingSort()
        {
            KfOrderSortModel kfSortModel = null;
            try
            {
                Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                if (adminModel != null)
                {
                    DateTime currTime = DateTime.Now;

                    Weike.Common.RedisHelp.RedisHelper redisHelper = new Weike.Common.RedisHelp.RedisHelper();
                    string md5Key = "manage.dd373.com.Controllers/AccessShoppingSort";
                    string redisKeyName = Globals.MD5(md5Key.ToUpper());

                    //判断缓存是否存在该记录
                    kfSortModel = redisHelper.StringGet<KfOrderSortModel>(redisKeyName);
                    if (kfSortModel == null)
                    {
                        //寄售客服显示数据
                        string sqlJS = string.Format("select top(9) sid,count(1) c from shopping left join shop on shop.id=shopping.objectid where shopping.ProcessingTime between '{0}' and '{1}' and state in('{2}','{3}') and dealtype=1 group by sid order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.部分完成.ToString());
                        DataTable dtJS = new BssShopping().GetListQuery(sqlJS).Tables[0];

                        //担保客服显示数据
                        string sqlDB = string.Format("select top(9) sid,count(1) c from shopping left join shop on shop.id=shopping.objectid where shopping.ProcessingTime between '{0}' and '{1}' and state in('{2}','{3}') and dealtype=2 group by sid order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.部分完成.ToString());
                        DataTable dtDB = new BssShopping().GetListQuery(sqlDB).Tables[0];

                        //截图客服显示数据
                        string sqlJT = string.Format("select top(9) sa.SID,count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID where sa.ProcessTime between '{0}' and '{1}' and sa.SourceType ='{2}' and sa.ResType ='{3}' and sp.State in ('{4}','{5}','{6}') group by sa.SID order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShoppingAssembly.SourceType.客服截图.ToString(), BssShoppingAssembly.ResType.截图完成.ToString(), BssShopping.ShoppingState.等待处理.ToString(), BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.一般过户.ToString());
                        DataTable dtJT = new BssShoppingAssembly().GetListQuery(sqlJT).Tables[0];

                        //话务客服显示数据
                        string sqlDH = string.Format("select top(9) sa.SID,count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID where sa.ProcessTime between '{0}' and '{1}' and sa.SourceType ='{2}' and sp.State not in ('{3}','{4}') group by sa.SID order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShoppingAssembly.SourceType.转给话务.ToString(), BssShopping.ShoppingState.支付成功.ToString(), BssShopping.ShoppingState.交易取消.ToString());
                        DataTable dtDH = new BssShoppingAssembly().GetListQuery(sqlDH).Tables[0];

                        //账号客服显示数据
                        string sqlYJ = string.Format("select top(9) sa.SID,count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID left join shop as s on sp.objectid=s.id where sa.ProcessTime between '{0}' and '{1}' and sa.SourceType in('{2}','{3}','{4}','{5}','{6}') and sa.ResType in('{7}','{8}','{9}','{10}','{11}') and sp.State in ('{12}','{13}','{14}') and s.dealtype=3 group by sa.SID order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShoppingAssembly.SourceType.等待处理.ToString(), BssShoppingAssembly.SourceType.话务转回.ToString(), BssShoppingAssembly.SourceType.开始处理.ToString(), BssShoppingAssembly.SourceType.客服转单.ToString(), BssShoppingAssembly.SourceType.确认购买.ToString(), BssShoppingAssembly.ResType.等待处理.ToString(), BssShoppingAssembly.ResType.交易成功.ToString(), BssShoppingAssembly.ResType.交易取消.ToString(), BssShoppingAssembly.ResType.确认购买.ToString(), BssShoppingAssembly.ResType.验证帐号.ToString(), BssShopping.ShoppingState.等待处理.ToString(), BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.一般过户.ToString());
                        DataTable dtZH = new BssShoppingAssembly().GetListQuery(sqlYJ).Tables[0];

                        //验证客服显示数据
                        string sqlYZ = string.Format("select top(9) sa.SID,count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID where sa.ProcessTime between '{0}' and '{1}' and sa.SourceType ='{2}' and sa.ResType not in('{3}','{4}') and sp.State not in ('{5}','{6}') group by sa.SID order by c desc", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShoppingAssembly.SourceType.验证帐号.ToString(), BssShoppingAssembly.ResType.交易取消.ToString(), BssShoppingAssembly.ResType.转单处理.ToString(), BssShopping.ShoppingState.支付成功.ToString(), BssShopping.ShoppingState.交易取消.ToString());
                        DataTable dtYZ = new BssShoppingAssembly().GetListQuery(sqlYZ).Tables[0];

                        kfSortModel = new KfOrderSortModel();
                        kfSortModel.Notice_JS = dtJS;
                        kfSortModel.Notice_DB = dtDB;
                        kfSortModel.Notice_JT = dtJT;
                        kfSortModel.Notice_DH = dtDH;
                        kfSortModel.Notice_ZH = dtZH;
                        kfSortModel.Notice_YZ = dtYZ;

                        //计算到第二天零点的TimeSpan
                        TimeSpan ts = DateTime.Parse(currTime.AddDays(1).ToShortDateString()) - currTime;
                        //将数据保存保存
                        redisHelper.StringSet<KfOrderSortModel>(redisKeyName, kfSortModel, ts);
                    }

                    if (kfSortModel != null)
                    {
                        #region 查询该客服昨天和今天数据
                        string sqlZJ = string.Format("select count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID where sa.ProcessTime between '{0}' and '{1}' and sa.ResType not in('{2}','{3}','{4}','{5}','{6}') and sp.State not in ('{7}','{8}','{9}','{10}','{11}') and sa.SID={12}", currTime.AddDays(-1).ToShortDateString(), currTime.ToShortDateString(), BssShoppingAssembly.ResType.未处理.ToString(), BssShoppingAssembly.ResType.交易取消.ToString(), BssShoppingAssembly.ResType.转单处理.ToString(), BssShoppingAssembly.ResType.等待处理.ToString(), BssShoppingAssembly.ResType.开始处理.ToString(), BssShopping.ShoppingState.支付成功.ToString(), BssShopping.ShoppingState.交易取消.ToString(), BssShopping.ShoppingState.正在发货.ToString(), BssShopping.ShoppingState.发货完成.ToString(), BssShopping.ShoppingState.等待处理.ToString(), adminModel.A_ID);
                        string sqlZZ = string.Format("select count(distinct sa.ShoppingId) c from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID where sa.ProcessTime >='{0}' and sa.ResType not in('{1}','{2}','{3}','{4}','{5}') and sp.State not in ('{6}','{7}','{8}','{9}','{10}') and sa.SID={11}", currTime.ToShortDateString(), BssShoppingAssembly.ResType.未处理.ToString(), BssShoppingAssembly.ResType.交易取消.ToString(), BssShoppingAssembly.ResType.转单处理.ToString(), BssShoppingAssembly.ResType.等待处理.ToString(), BssShoppingAssembly.ResType.开始处理.ToString(), BssShopping.ShoppingState.支付成功.ToString(), BssShopping.ShoppingState.交易取消.ToString(), BssShopping.ShoppingState.正在发货.ToString(), BssShopping.ShoppingState.发货完成.ToString(), BssShopping.ShoppingState.等待处理.ToString(), adminModel.A_ID);
                        #endregion

                        md5Key = "manage.dd373.com.Controllers/AccessShoppingSort/AdminID:" + adminModel.A_ID;
                        redisKeyName = Globals.MD5(md5Key.ToUpper());

                        //判断缓存是否存在该记录
                        string kfLastDay = redisHelper.StringGet<string>(redisKeyName);
                        if (string.IsNullOrWhiteSpace(kfLastDay))
                        {
                            kfLastDay = new BssShoppingAssembly().GetSingle(sqlZJ).ToString();
                            //计算到第二天零点的TimeSpan
                            TimeSpan ts = DateTime.Parse(currTime.AddDays(1).ToShortDateString()) - currTime;
                            //将数据保存保存
                            redisHelper.StringSet<string>(redisKeyName, kfLastDay, ts);
                        }

                        kfSortModel.KfLastDay = kfLastDay;
                        kfSortModel.KfToday = new BssShoppingAssembly().GetSingle(sqlZZ).ToString();

                        kfSortModel.AdminModel = adminModel;

                        if (adminModel.A_Memo.Trim() == "实习客服")
                        {
                            //当前客服分配的客服类型
                            ServiceQQ sqqModel = new BssServiceQQ().GetModelBySid(adminModel.A_ID);
                            if (sqqModel != null)
                                kfSortModel.SqqModel = sqqModel;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服通知单量统计", ex, this.GetType().FullName, "AccessShoppingSort");
            }
            return View(kfSortModel);
        }

        /// <summary>
        /// 获取要调用的接口信息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="res"></param>
        /// <param name="method"></param>
        private void GetResMsg(string type, out string res, out string method)
        {

            res = "";
            method = "";
            switch (type)
            {
                case "Add":
                    res = "拨打电话通知卖家补货";
                    method = "寄售卖家补货已接听";
                    break;
                case "Out":
                    res = "拨打电话通知卖家下线";
                    method = "寄售卖家下线已接听";
                    break;
                case "Buy":
                    res = "拨打电话通知买家联系客服";
                    method = "担保买家接听";
                    break;
                case "Sell":
                    res = "拨打电话通知买家联系客服";
                    method = "担保卖家已接听";
                    break;
                case "JAQ":
                    res = "拨打电话通知卖家解除安全模式";
                    method = "寄售卖家解除安全模式";
                    break;
                case "SFH":
                    res = "拨打电话通知卖家发货";
                    method = "通知担保卖家发货";
                    break;
                case "BSure":
                    res = "拨打电话通知买家确认收货";
                    method = "担保买家确认收货";
                    break;
                case "JsSAddQQ":
                    res = "拨打电话通知寄售卖家添加qq";
                    method = "寄售通知卖家加qq";
                    break;
                case "BuyerAddQQ":
                    res = "拨打电话通知买家加QQ";
                    method = "寄售通知买家加QQ";
                    break;
                case "GamePaiduiCallBuyer":
                    res = "游戏排队拨打电话通知买家";
                    method = "游戏排队通知买家";
                    break;
                case "GameCloseCallBuyer":
                    res = "游戏维护拨打电话通知买家";
                    method = "游戏维护通知买家";
                    break;
                case "GamePaiduiCallSeller":
                    res = "游戏排队拨打电话通知卖家";
                    method = "游戏排队通知卖家";
                    break;
                case "BuerGameMsg":
                    res = "拨打电话通知买家注意游戏私聊";
                    method = "买家注意游戏私聊";
                    break;
                case "BuyerAccTrade":
                    res = "打电话通知买家接受交易";
                    method = "通知买家接受交易";
                    break;
                case "BuyerZhuanHuawu":
                    res = "拨打电话通知买家订单转入话务";
                    method = "通知买家订单转入话务";
                    break;
                case "BuyerGotoTrade":
                    res = "拨打电话通知买家来交易";
                    method = "买家来交易";
                    break;
                case "MallCallBuyerTrade":
                    res = "商城联系买家上线";
                    method = "商城联系买家上线";
                    break;
                case "ShCallSellerTrade":
                    res = "通知发货方上线";
                    method = "卖家发货通知";
                    break;
                case "SySellFh":
                    res = "通知卖家发货";
                    method = "手游联系卖家发货";
                    break;
                case "SyBuySh":
                    res = "通知买家收货";
                    method = "手游买家联系收货";
                    break;
                case "SyBuySure":
                    res = "通知买家确认收货";
                    method = "手游联系买家确认收货";
                    break;
                default:
                    res = "未知提交类型";
                    method = "";
                    break;
            }
        }

        /// <summary>
        /// 拨打电话通知方法
        /// </summary>
        /// <param name="shoppingId"></param>
        /// <returns></returns>
        [Role("拨打电话通知方法", IsAuthorize = true)]
        public ActionResult CallUserAboutShoppingMethod(string shoppingId, string type)
        {
            string res = "";
            string method = "";
            GetResMsg(type, out res, out method);
            if (string.IsNullOrEmpty(method))
            {
                return Content(res);
            }

            try
            {
                BssShopping bllShopping = new BssShopping();
                Shopping shopping = bllShopping.GetModel(shoppingId);
                if (shopping != null)
                {
                    BssMembers bllMember = new BssMembers();
                    Members buyer = bllMember.GetModel(shopping.UserID);
                    Members seller = bllMember.GetModel(shopping.SellerId.Value);

                    OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shopping.ID);
                    if (orderShopModel != null)
                    {
                        string ShopGUID = orderShopModel.FormGuid;
                        string GameGUID = orderShopModel.GameGuid;
                        string ShopType = orderShopModel.ShopType;
                        string ShopID = shopping.ID;
                        if (seller != null && buyer != null)
                        {
                            string phone = "";
                            string rphone = "";
                            string userId = "";
                            string objectId = "";

                            if (method.Contains("买家"))
                            {
                                rphone = buyer.M_Phone.Trim();
                                userId = buyer.M_ID.ToString();
                                objectId = shopping.ID;
                            }
                            else if (method.Contains("卖家"))
                            {
                                rphone = seller.M_Phone.Trim();
                                userId = seller.M_ID.ToString();
                                objectId = shopping.ObjectType == BssShopping.ShoppingType.会员商城.ToString() || shopping.ObjectType == BssShopping.ShoppingType.商家收货.ToString() || shopping.ObjectType == BssShopping.ShoppingType.点券商城.ToString() ? "" : orderShopModel.FormGuid;
                            }

                            if (!string.IsNullOrEmpty(objectId))
                            {
                                BssFormValue fvbll = new BssFormValue();
                                List<FormValue> lstFrmt = fvbll.GetList(string.Format("ObjectID='{0}'", objectId)).Tables[0].ToList<FormValue>();
                                var a = (from item in lstFrmt orderby item.orderid ascending select item);
                                List<FormValue> lstFrm = a.ToList<FormValue>();
                                FormFields ffmodel = null;
                                BssFormFields ffbll = new BssFormFields();
                                foreach (FormValue fv in lstFrm)
                                {
                                    ffmodel = ffbll.GetModel(fv.FldGuid);
                                    if (ffmodel != null && ffmodel.FldName.Contains("电话"))
                                    {
                                        phone = fv.FVValue.Trim();
                                        if (Regex.IsMatch(phone, @"^((\(\d{3}\))|(\d{3}\-))?(\(0\d{2,3}\)|0\d{2,3}-)?[1-9]\d{6,7}$"))
                                        {
                                            phone = rphone;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(phone) || !string.IsNullOrEmpty(rphone))
                            {
                                if (string.IsNullOrEmpty(phone))
                                    phone = rphone;
                                string result = "";
                                string gameIdityfy = GameGUID.Split('|').Length > 0 ? GameGUID.Split('|')[0] : "";
                                Game gameModel = !string.IsNullOrWhiteSpace(gameIdityfy) ? new BssGame().GetModelByIdentify(gameIdityfy, false) : null;
                                GameShopType typeModel = new BssGameShopType().GetModel(ShopType);
                                string serverName = (gameModel != null ? gameModel.GameName : "") + "/" + (typeModel != null ? typeModel.GameShopTypeName : "");
                                bool isSend = BssSendMsgInfo.SendPhoneMethod(userId.ToInt32(), method, phone, rphone, shoppingId, ShopID, serverName, BssSendMsgInfo.MsgType.客服发送.ToString(), out result);
                                if (isSend && result != "Over")
                                {
                                    res += "成功";

                                    if (shopping.ObjectType == BssShopping.ShoppingType.出售交易.ToString() || shopping.ObjectType == BssShopping.ShoppingType.会员商城.ToString())
                                    {
                                        string spAssInfo = "";
                                        //更新订单QQ后缀（买家加B，卖家加S）
                                        if (type == "JsSAddQQ")
                                        {
                                            spAssInfo = BssShoppingDbAutoAssembly.AssemblyInfo.客服拨打电话联系卖家添加QQ.ToString();
                                        }
                                        else if (type == "BuyerAddQQ")
                                        {
                                            spAssInfo = BssShoppingDbAutoAssembly.AssemblyInfo.客服拨打电话联系买家添加QQ.ToString();
                                        }
                                        if (!string.IsNullOrEmpty(spAssInfo))
                                        {
                                            List<ShoppingDbAutoAssembly> ShoppingDbAutoAssemblylist = new BssShoppingDbAutoAssembly().GetModelList(string.Format(" ShoppingId='{0}'", shopping.ID));
                                            bool query = ShoppingDbAutoAssemblylist.Exists(p => p.AssemblyInfo == spAssInfo);
                                            if (!query)
                                            {
                                                BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(shopping.ID, BssShoppingDbAutoAssembly.AssType.客服.ToString(), spAssInfo);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (result == "Over")
                                    {
                                        res = "拨打电话失败,请不要重复拨打，单个任务最多两次！";
                                    }
                                    else if (result == "outlimit")
                                    {
                                        res = "拨打电话失败,电话任务排队超限，请稍后再尝试拨打！";
                                    }
                                    else
                                    {
                                        res += "失败,请重点击！";
                                    }
                                }
                            }
                            else
                            {
                                res += "失败,该订单卖家电话信息为空！";
                            }
                        }
                        else
                        {
                            res += "失败,无法查找到该订单卖家信息！";
                        }
                    }
                    else
                    {
                        res += "失败,无法查找到该订单商品信息！";

                    }
                }
                else
                {
                    res += "失败,无法查找到该订单信息！";
                }


            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("拨打电话通知方法", ex, this.GetType().FullName, "CallUserAboutShoppingMethod");
            }

            return Content(res);
        }

        /// <summary>
        /// 客服发送短信
        /// </summary>
        /// <param name="shoppingId"></param>
        /// <param name="msgInfo"></param>
        /// <returns></returns>
        [Role("客服发送短信", IsAuthorize = true)]
        public ActionResult SendMsgToSeller(string shoppingId, string msgInfo, string msgType)
        {
            string res = "";

            try
            {
                BssShopping bllShopping = new BssShopping();
                Shopping shopping = bllShopping.GetModel(shoppingId);
                if (shopping != null)
                {
                    OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shopping.ID);
                    if (orderShopModel != null)
                    {
                        string phoneNo = "";
                        string addRemark = "";
                        int mid = 0;
                        if (msgType == "s")
                        {
                            string fvGuid = orderShopModel.FormGuid;
                            string Shopid = orderShopModel.ShopId;
                            Members sellerModel = new BssMembers().GetModel(shopping.SellerId.Value); 

                            if (sellerModel != null)
                            {
                                if (!string.IsNullOrEmpty(fvGuid) && !string.IsNullOrEmpty(Shopid))
                                {
                                    ShopFormChange sfcModel = new BssShopFormChange().GetModelBySpIdAndUserId(sellerModel.M_ID, Shopid);
                                    BssFormValue.GetPhoneNo(fvGuid, sfcModel != null ? sfcModel.IsChange : false, out phoneNo);
                                }
                                mid = sellerModel.M_ID;
                                if (string.IsNullOrEmpty(phoneNo))
                                {
                                    phoneNo = sellerModel.M_Phone;
                                }
                                addRemark = "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "给卖家发送短信";
                            }
                            else
                            {
                                res = "发送短信失败，无法获取卖家信息";
                            }
                        }
                        else
                        {
                            Members buyerModel = new BssMembers().GetModel(shopping.UserID);
                            if (buyerModel != null)
                            {
                                mid = buyerModel.M_ID;
                                ShopFormChange sfcModel = new BssShopFormChange().GetModelBySpIdAndUserId(buyerModel.M_ID, shopping.ID);
                                BssFormValue.GetPhoneNo(shopping.ID, sfcModel != null ? sfcModel.IsChange : false, out phoneNo);
                                if (string.IsNullOrEmpty(phoneNo))
                                {
                                    phoneNo = buyerModel.M_Phone;
                                }
                                addRemark = "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "给买家发送短信";
                            }
                            else
                            {
                                res = "发送短信失败，无法获取卖家信息";
                            }
                        }
                        if (!string.IsNullOrEmpty(phoneNo))
                        {
                            bool isSend = BssSendMsgInfo.SendMsgMethodDuanXinWang(msgInfo, phoneNo, mid, BssSendMsgInfo.MsgType.客服发送.ToString());
                            if (isSend)
                            {
                                res = "发送短信成功";
                                //插入订单备注记录
                                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, addRemark);
                            }
                            else
                            {
                                res = "发送短信失败";
                            }
                        }
                        else
                        {
                            res = "发送短信失败，无法获取联系电话";
                        }
                    }
                    else
                    {
                        res = "发送短信失败，无法获取订单商品信息";
                    }
                }
                else
                {
                    res = "发送短信失败，无法获取订单信息";
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服发送短信", ex, this.GetType().FullName, "SendMsgToSeller");
            }

            return Content(res);
        }
        
        /// <summary>
        /// 获取预设短信集合
        /// </summary>
        /// <param name="shoppingId">客服类型</param>
        /// <param name="receiverType">接收对象</param>
        /// <returns></returns>
        [Role("获取预设短信对话框", Description = "获取预设短信对话框", IsAuthorize = true)]
        public ActionResult GetSendPresetMsgTemplates(string shoppingId, int receiverType)
        {
            #region 获取客服类型

            var shopping = new BssShopping().GetModel(shoppingId);
            if (shopping == null) return Json(new { success = false, msg = "获取客服类型错误" });

            var dealType = 0;
            var orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shopping.ID);
            if (orderShopModel != null) dealType = orderShopModel.DealType;

            var serviceQqType = 0;
            if (dealType == (int) BssOrderShopSnapshot.DealType.商城)
            {
                var mallShopModel = new BssMembersMallShop().GetModel(shopping.ObjectId);
                serviceQqType = mallShopModel.IsTg && !string.IsNullOrEmpty(mallShopModel.TgFormGuid) ? (int)ServiceQQ.EDealType.寄售 : (int)ServiceQQ.EDealType.商家;
            }
            else if (dealType == (int) BssOrderShopSnapshot.DealType.收货)
            {
                serviceQqType = (int)ServiceQQ.EDealType.商家;
            }
            else
            {
                var shopModel = new BssShop().GetModel(shopping.ObjectId);
                if (shopModel == null) return Json(new { success = false, msg = "获取客服类型错误" });

                var gameInfoModel = new BLLGame().GetGameInfoModel(shopModel.GameType, shopModel.ShopType);
                if (gameInfoModel == null) return Json(new { success = false, msg = "获取客服类型错误" });

                var isDs = shopping.ObjectType == BssShopping.ShoppingType.代收交易.ToString();
                var isChangeOrNeed = shopping.ObjectType == BssShopping.ShoppingType.求购交易.ToString() ||
                                     shopping.ObjectType == BssShopping.ShoppingType.降价交易.ToString();

                serviceQqType = BLLServiceQQMethod.GetServiceQqType(shopModel, gameInfoModel, shopModel.Price, isDs, isChangeOrNeed);
            }

            #endregion

            var bss = new BssSendPresetMsgTemplate();

            var where = "Enabled=1";
            where += " AND DealType = " + serviceQqType;
            where += " AND ReceiverType = " + receiverType;

            try
            {
                var ds = bss.GetList(where);
                var sendPresetMsgTemplates = bss.DataTableToList(ds.Tables[0]);

                return Json(new { success = true, data = sendPresetMsgTemplates });
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("编辑预设短信出错", ex, this.GetType().FullName, "GetSendPresetMsgTemplates");
                return Json(new { success = false, msg = "获取短信出错" });
            }
        }

        /// <summary>
        /// 发送预设短信
        /// </summary>
        /// <param name="shoppingId"></param>
        /// <param name="contentid"></param>
        /// <param name="receiverType"></param>
        /// <returns></returns>
        [Role("发送预设短信", Description = "发送预设短信", IsAuthorize = true)]
        public ActionResult SendPresetMsg(string shoppingId, string contentid, int receiverType)
        {
            try
            {
                var shopping = new BssShopping().GetModel(shoppingId);
                if (shopping == null) return Json(new { success = false, msg = "发送短信失败，无法获取订单信息" });

                var orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shopping.ID);
                if (orderShopModel == null) return Json(new { success = false, msg = "发送短信失败，无法获取订单商品信息" });

                var sendPresetMsgTemplate = new BssSendPresetMsgTemplate().GetModel(contentid);
                if (sendPresetMsgTemplate == null) return Json(new { success = false, msg = "发送短信不存在" });

                string phoneNo = "";
                string addRemark;
                int mid;
                if (receiverType == (int)BssSendPresetMsgTemplate.ReceiverType.卖家 && shopping.SellerId.HasValue)
                {
                    var sellerModel = new BssMembers().GetModel(shopping.SellerId.Value);
                    if (sellerModel == null) return Json(new { success = false, msg = "发送短信失败，无法获取卖家信息" });

                    if (!string.IsNullOrEmpty(orderShopModel.FormGuid) && !string.IsNullOrEmpty(orderShopModel.ShopId))
                    {
                        var sfcModel = new BssShopFormChange().GetModelBySpIdAndUserId(sellerModel.M_ID, orderShopModel.ShopId);
                        BssFormValue.GetPhoneNo(orderShopModel.FormGuid, sfcModel != null ? sfcModel.IsChange : false, out phoneNo);
                    }
                    mid = sellerModel.M_ID;
                    if (string.IsNullOrEmpty(phoneNo))
                    {
                        phoneNo = sellerModel.M_Phone;
                    }
                    addRemark = "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "给卖家发送短信";
                }
                else
                {
                    var buyerModel = new BssMembers().GetModel(shopping.UserID);
                    if (buyerModel == null) return Json(new { success = false, msg = "发送短信失败，无法获取卖家信息" });

                    mid = buyerModel.M_ID;
                    var sfcModel = new BssShopFormChange().GetModelBySpIdAndUserId(buyerModel.M_ID, shopping.ID);
                    BssFormValue.GetPhoneNo(shopping.ID, sfcModel != null ? sfcModel.IsChange : false, out phoneNo);
                    if (string.IsNullOrEmpty(phoneNo))
                    {
                        phoneNo = buyerModel.M_Phone;
                    }
                    addRemark = "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "给买家发送短信";
                }

                if (string.IsNullOrEmpty(phoneNo)) return Json(new { success = false, msg = "发送短信失败，无法获取联系电话" });

                var isSend = BssSendPresetMsgTemplate.SendPresetMsgInfo(sendPresetMsgTemplate.TemplateContent, phoneNo, mid, BssSendMsgInfo.MsgType.客服发送.ToString());
                if (!isSend) return Json(new { success = false, msg = "发送短信失败" });

                //插入订单备注记录
                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, addRemark);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服发送短信", ex, this.GetType().FullName, "SendMsgToSeller");
                return Json(new { success = false, msg = "发送短信失败" });
            }
        }

        /// <summary>
        /// 允许修改表单
        /// </summary>
        /// <param name="shoppingId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [Role("允许修改表单", IsAuthorize = true)]
        public ActionResult FormAllowChange(string shoppingId, string type)
        {
            string res = "";

            try
            {
                BssShopping bllShopping = new BssShopping();
                Shopping shopping = bllShopping.GetModel(shoppingId);
                if (shopping != null)
                {
                    OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(shopping.ID);
                    if (orderShopModel != null)
                    {
                        BssShopFormChange bssFm = new BssShopFormChange();
                        if (type == "s")
                        {
                            ShopFormChange sfModel = bssFm.GetModelBySpIdAndUserId(shopping.SellerId.Value, orderShopModel.ShopId);
                            if (sfModel == null)
                            {
                                sfModel = new ShopFormChange();
                                sfModel.CreateTime = DateTime.Now;
                                sfModel.IsChange = false;
                                sfModel.SpId = orderShopModel.ShopId;
                                sfModel.UserId = shopping.SellerId.Value;
                                bssFm.Add(sfModel);

                                res = "允许修改表单操作成功";
                                //插入订单备注记录
                                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "允许卖家修改表单");

                                //站内信通知
                                string ddmsg = "";
                                if (shopping.ObjectType == BssShopping.ShoppingType.会员商城.ToString())
                                {
                                    ddmsg = "您的商城商品已经有买家购买,您的游戏账号填写错误，请您到“我的DD373”→“我的商城订单”更新游戏账号，或 <a href=\"//mall.dd373.com/ChangeShopForm.shtml?spId=" + shoppingId + "\">点击此处</a> 跳转到修改页面，修改完成后请及时联系交易客服";
                                }
                                else
                                {
                                    ddmsg = "您出售的商品，商品编号：" + orderShopModel.ShopId + "已经有买家购买,您的游戏账号填写错误，请您到“我的DD373”→“我的出售订单”更新游戏账号，或 <a href=\"//trading.dd373.com/ChangeShopForm.html?spId=" + orderShopModel.ShopId + "\">点击此处</a> 跳转到修改页面，修改完成后请及时联系交易客服";
                                }
                                BssMembersMessage.AddMeg(shopping.SellerId.Value, ddmsg);
                            }
                            else
                            {
                                res = "系统已存在该申请";
                            }
                        }
                        else if (type == "b")
                        {
                            ShopFormChange sfModel = bssFm.GetModelBySpIdAndUserId(shopping.UserID, shopping.ID);
                            if (sfModel == null)
                            {
                                sfModel = new ShopFormChange();
                                sfModel.CreateTime = DateTime.Now;
                                sfModel.IsChange = false;
                                sfModel.SpId = shopping.ID;
                                sfModel.UserId = shopping.UserID;
                                bssFm.Add(sfModel);

                                res = "允许修改表单操作成功";

                                //插入订单备注记录
                                BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, "管理员：" + BLLAdmins.GetCurrentAdminUserInfo().A_RealName + "允许买家修改表单");

                                //站内信通知
                                string ddmsg = "您购买的订单编号：" + shopping.ID + "角色名称信息填写错误，请您到“我的DD373”→“我购买的商品”更新收货帐号信息，或 <a href=\"//trading.dd373.com/ChangeShoppingForm.html?spId=" + shopping.ID + "\">点击此处</a> 跳转到修改页面，修改完成后请及时联系交易客服";
                                BssMembersMessage.AddMeg(shopping.UserID, ddmsg);
                            }
                            else
                            {
                                res = "系统已存在该申请";
                            }

                        }
                    }
                    else
                    {
                        res = "允许修改表单操作失败，无法获取订单商品信息";
                    }
                }
                else
                {
                    res = "允许修改表单操作失败，无法获取订单信息";
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("允许修改表单", ex, this.GetType().FullName, "FormAllowChange");
            }

            return Content(res);
        }

        [Role("短信发送记录", IsAuthorize = true)]
        public ActionResult SendMsgList(int? Page, string userName, string phoneNo, string companyName, string MsgType, string StartTime, string EntTime)
        {

            DataPages<Weike.Member.SendMsgInfo> LMsg = null;

            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(userName))
                {
                    Members memberModel = new BssMembers().GetModelByName(userName);
                    if (memberModel == null)
                    {
                        return View(LMsg);
                    }
                    where.Append(string.Format(" and userid={0}", memberModel.M_ID));
                }
                if (!string.IsNullOrEmpty(phoneNo))
                {
                    where.Append(string.Format(" and mobilephone = '{0}'", phoneNo));
                }
                if (!string.IsNullOrEmpty(companyName))
                {
                    where.Append(string.Format(" and CompanyName = '{0}'", companyName));
                }
                if (!string.IsNullOrEmpty(MsgType))
                {
                    where.Append(string.Format(" and MsgType = '{0}'", MsgType));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format("and createtime between '{0}' and '{1}'", STime, ETime));

                LMsg = new BssSendMsgInfo().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "ID");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("短信发送记录出错", ex, this.GetType().FullName, "SendMsgList");
            }

            return View(LMsg);

        }


        [Role("登录短信发送记录", IsAuthorize = true)]
        public ActionResult SendMsgListForLogin(int? Page, string ip, string phoneNo, string Status, string StartTime, string EntTime)
        {

            DataPages<Weike.Member.PhoneLoginRecord> LMsg = null;

            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(ip))
                {
                    where.Append(string.Format(" and IP='{0}'", ip));
                }
                if (!string.IsNullOrEmpty(phoneNo))
                {
                    where.Append(string.Format(" and Phone = '{0}'", phoneNo));
                }
                if (!string.IsNullOrEmpty(Status))
                {
                    where.Append(string.Format(" and Status = '{0}'", Status));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format("and createtime between '{0}' and '{1}'", STime, ETime));

                LMsg = new BssPhoneLoginRecord().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("登录短信发送记录错", ex, this.GetType().FullName, "SendMsgListForLogin");
            }

            return View(LMsg);

        }


        [Role("出售订单流水辅助管理", IsAuthorize = true)]
        public ActionResult littleUSellOrderAssembly(int? Page, string Stype, string key, string untype, string username, string gameAccount, string DelState, string Sqq, string StartTime, string EntTime)
        {
            Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
            StringBuilder where = new StringBuilder("1=1 ");
            if ((!string.IsNullOrEmpty(key)) && !string.IsNullOrEmpty(key.Trim()))
            {
                if (Stype == "gm")
                    where.Append(string.Format(" and ShoppingId = '{0}'", key.Trim()));
                else
                    where.Append(string.Format(" and ShopId = '{0}' ", key.Trim()));
            }
            if (!string.IsNullOrEmpty(username))
            {
                Page = 1;
                if (untype == "buser")
                    where.Append(string.Format(" and BuyerName='{0}'", username));
                else
                    where.Append(string.Format(" and SellerName ='{0}'", username));
            }
            if (!string.IsNullOrEmpty(gameAccount))
            {
                Page = 1;
                where.Append(string.Format(" and GameAccount='{0}'", gameAccount));
            }
            if (!string.IsNullOrEmpty(DelState))
            {
                if (DelState != "nodeal" && DelState != "delay")
                {
                    where.Append(" and ResType<>'未处理'");
                }
                else if (DelState == "delay")
                {
                    where.Append(" and SourceType='" + BssShoppingAssembly.ResType.延时处理.ToString() + "' and ResType='未处理'");
                }
                else
                {
                    where.Append(" and ResType='未处理'");
                }
            }
            if (!string.IsNullOrEmpty(Sqq))
            {
                where.Append(string.Format(" and ShoppingId in(select id from shopping where sid={0} and sqq='{1}')", adminModel.A_ID, Sqq));
            }
            if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(gameAccount))
                where.Append(string.Format(" and sid ={0} ", adminModel.A_ID));
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where.Append(string.Format(" and CreateTime between '{0}' and '{1}' ", STime, ETime));
            StringBuilder fileds = new StringBuilder(" ShoppingId,ObjectType,SourceType,SID,ResType,BuyerName,GameAccount,CreateTime,ProcessTime ");
            DataPages<Weike.EShop.ShoppingAssembly> LShop = null;
            try
            {
                LShop = new BssShoppingAssembly().GetPageRecord(where.ToString(), "case ResType when '未处理' then 1 else 2  end asc, case SourceType when '延时到期' then -1 when '客服介入' then 1 when '申请取消' then 0 when '等待审核' then 2 when '等待处理' then 4 when '延时处理' then 5  else 3 end asc,CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, fileds.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售订单流水辅助管理", ex, this.GetType().FullName, "littleUSellOrderAssembly");
            }
            return View(LShop);
        }


        [Role("订单处理流水记录", IsAuthorize = true)]
        public ActionResult ShoppingAssemblyList(int? Page, string shoppingId, string account, string StartTime, string EntTime, string SName, string SourceType, string ResType, string State)
        {
            DataPages<Weike.EShop.ShoppingAssembly> LSa = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(shoppingId))
                {
                    where.Append(string.Format(" and ShoppingId='{0}'", shoppingId));
                }
                if (!string.IsNullOrEmpty(account))
                {
                    where.Append(string.Format(" and GameAccount='{0}'", account));
                }
                if (!string.IsNullOrEmpty(SName))
                {
                    where.Append(string.Format(" and sid={0}", SName));
                }
                if (!string.IsNullOrEmpty(SourceType))
                {
                    where.Append(string.Format(" and SourceType='{0}'", SourceType));
                }
                if (!string.IsNullOrEmpty(ResType))
                {
                    where.Append(string.Format(" and ResType='{0}'", ResType));
                }
                if (!string.IsNullOrEmpty(State))
                {
                    where.Append(string.Format(" and exists (select 1 from shopping where shopping.id=ShoppingAssembly.ShoppingId and shopping.state='{0}')", State));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format("and createtime between '{0}' and '{1}'", STime, ETime));

                LSa = new BssShoppingAssembly().GetPageRecord(where.ToString(), "createtime desc,SortNo", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单处理流水记录出错", ex, this.GetType().FullName, "ShoppingAssemblyList");
            }

            return View(LSa);

        }

        [Role("订单处理流水导出", IsAuthorize = true)]
        /// <summary>
        /// 订单处理流水导出
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        public ActionResult ShoppingAssemblyImport(string shoppingId, string account, string DealType, string StartTime, string EntTime, string SName, string SourceType, string ResType)
        {
            StringBuilder sbSql = new StringBuilder();

            sbSql.Append("select sp.id,s.shopid,sp.ObjectType,s.GameType,sp.createdate,sp.processingtime,sa.SourceType,sa.ResType,sp.State,a.A_RealName,sa.CreateTime,sa.AddTime,sa.ProcessTime,DATEDIFF(Minute,sa.CreateTime,sa.ProcessTime) as diffTime,DATEDIFF(Minute,sa.AddTime,sa.ProcessTime) as diffTimea,sfrc.BuyType,sfrc.ReasonContent,sp.UserID,s.PublicUser from ShoppingAssembly as sa left join shopping as sp on sa.ShoppingId=sp.ID left join shop as s on s.id=sp.objectid  left join admins as a on sa.sid=a.a_id left join ShopFailedReason as sfr on sp.id=sfr.OrderId left join ShopFailedReasonConfig as sfrc on sfr.ReasonId=sfrc.ID where 1=1 ");

            if (!string.IsNullOrEmpty(shoppingId))
            {
                sbSql.Append(string.Format(" and sa.ShoppingId='{0}'", shoppingId));
            }
            if (!string.IsNullOrEmpty(account))
            {
                sbSql.Append(string.Format(" and sa.GameAccount='{0}'", account));
            }
            if (!string.IsNullOrEmpty(SName))
            {
                sbSql.Append(string.Format(" and sa.sid={0}", SName));
            }
            if (!string.IsNullOrEmpty(SourceType))
            {
                sbSql.Append(string.Format(" and sa.SourceType='{0}'", SourceType));
            }
            if (!string.IsNullOrEmpty(ResType))
            {
                sbSql.Append(string.Format(" and sa.ResType='{0}'", ResType));
            }
            if (!string.IsNullOrEmpty(DealType))
            {
                sbSql.Append(string.Format(" and s.dealtype='{0}'", DealType));
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            sbSql.Append(string.Format("and sa.createtime between '{0}' and '{1}'", STime, ETime));
            sbSql.Append(" order by sa.CreateTime asc");

            try
            {
                HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                ISheet sheet1 = hssfworkbook.CreateSheet("订单流水列表");
                NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "DD373 Team";
                NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                si.Subject = "http://www.dd373.com/";
                hssfworkbook.DocumentSummaryInformation = dsi;
                hssfworkbook.SummaryInformation = si;

                IRow row1 = sheet1.CreateRow(0);

                IFont font1 = hssfworkbook.CreateFont();
                font1.Boldweight = (short)FontBoldWeight.Bold;
                font1.FontName = "宋体";
                font1.FontHeightInPoints = 12;
                ICellStyle style1 = hssfworkbook.CreateCellStyle();
                style1.SetFont(font1);
                style1.Alignment = HorizontalAlignment.Center;

                IFont font2 = hssfworkbook.CreateFont();
                font2.FontName = "宋体";
                font2.FontHeightInPoints = 11;
                ICellStyle style2 = hssfworkbook.CreateCellStyle();
                style2.SetFont(font2);

                //生成标题
                string[] tits = new string[] { "序号", "订单编号", "商品编号", "订单类型", "游戏区服", "订单创建时间", "订单处理时间", "流水来源", "流水处理结果", "订单状态", "客服", "流水创建时间", "流水分配时间", "流水处理时间", "创建时间差", "增加时间差", "失败原因类型", "失败原因", "买家ID", "卖家ID" };
                for (int i = 0; i < tits.Length; i++)
                {
                    sheet1.SetColumnWidth(i, 15 * 256);

                    ICell cell = row1.CreateCell(i);
                    cell.SetCellValue(tits[i]);
                    cell.CellStyle = style1;
                }

                DataSet ds = new BssShoppingAssembly().GetListQuery(sbSql.ToString());
                BLLGame bllGame=new BLLGame();
                if (ds != null && ds.Tables[0] != null)
                {
                    IRow row = null;
                    ICell nocell = null;
                    DataTable dt = ds.Tables[0];
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        row = sheet1.CreateRow(i + 1);
                        for (int j = 0; j < tits.Length; j++)
                        {
                            nocell = row.CreateCell(j);
                            if (j == 4)
                            {
                                string gameInfo = bllGame.GetGameInfoModelByOtherName(dt.Rows[i][j - 1].ToString(), "/", false);
                                nocell.SetCellValue(gameInfo);
                            }
                            else
                            {
                                nocell.SetCellValue(j == 0 ? (i + 1).ToString() : dt.Rows[i][j - 1].ToString());
                            }
                            nocell.CellStyle = style2;
                        }
                    }
                }
                string adminName = BLLAdmins.GetCurrentAdminUserInfo().A_Name;
                string downloadfilename = "客服流水详情_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xls";
                using (MemoryStream ms = new MemoryStream())
                {
                    hssfworkbook.Write(ms);

                    FileInfo FI = new FileInfo(Server.MapPath(string.Format("~/ExcelFile/{0}/{1}", adminName, downloadfilename)));
                    if (!Directory.Exists(FI.DirectoryName))
                        Directory.CreateDirectory(FI.DirectoryName);
                    FileStream fileUpload = new FileStream(Server.MapPath(string.Format("~/ExcelFile/{0}/{1}", adminName, downloadfilename)), FileMode.Create);
                    ms.WriteTo(fileUpload);
                    fileUpload.Close();
                    fileUpload = null;
                }
                string retUrl = Request.Url.Host;
                retUrl = string.Format("/ExcelFile/{0}/{1}", adminName, downloadfilename);

                return Redirect(retUrl);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单处理流水导出", ex, this.GetType().FullName, "ShoppingAssemblyImport");
            }

            return View();

        }

        /// <summary>
        /// 可用在线客服验证
        /// </summary>
        /// <returns></returns>
        [Role("可用在线客服验证", IsAuthorize = true)]
        public ActionResult ExistsHaveKeFu(int serviceQqType)
        {
            string retMsg = "";
            try
            {
                List<AdminServiceQQInfo> adminSqqList = BLLServiceQQMethod.GetZhOtherStateServiceQQInfo(serviceQqType);
                if (adminSqqList == null || adminSqqList.Count == 0)
                {
                    retMsg = "当前没有相应的客服可供选择，请稍候尝试";
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("可用在线客服验证：", ex, this.GetType().FullName, "ExistsHaveKeFu");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }


        /// <summary>
        /// 提取密码订单验证
        /// </summary>
        /// <returns></returns>
        public ActionResult ExistsGetPwd(string spId, string computerName)
        {
            string retMsg = "";
            try
            {
                Shopping spModel = new BssShopping().GetModel(spId);
                if (spModel != null && (spModel.State == BssShopping.ShoppingState.交易成功.ToString() || spModel.State == BssShopping.ShoppingState.部分完成.ToString() || spModel.State == BssShopping.ShoppingState.交易取消.ToString()))
                    retMsg = "该订单已被处理过";

                if (spModel != null)
                {
                    Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                    BssModifyMembersRecording.AddCaoZuoRecording(spModel.UserID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.提取密码电脑名, spId, computerName);
                    List<ShoppingDbAutoAssembly> ShopAssmeblyList = new BssShoppingDbAutoAssembly().GetModelList(string.Format(" ShoppingId='{0}'", spId));
                    bool query = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.客服开始登陆游戏.ToString());
                    if (!query)
                    {
                        BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(spId, BssShoppingDbAutoAssembly.AssType.客服.ToString(), BssShoppingDbAutoAssembly.AssemblyInfo.客服开始登陆游戏.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("提取密码订单验证：", ex, this.GetType().FullName, "ExistsGetPwd");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 记录必要步骤
        /// </summary>
        /// <param name="spId">shopid</param>
        /// <param name="Type">步骤类型</param>
        /// <returns></returns>
        public ActionResult ExtractSteps(string spId, string Type, string State)
        {
            string retMsg = "";
            try
            {
                Shopping spModel = new BssShopping().GetModel(spId);

                if (spModel != null && !string.IsNullOrEmpty(Type) && spModel.State != BssShopping.ShoppingState.等待支付.ToString() && spModel.State != BssShopping.ShoppingState.部分完成.ToString() && spModel.State != BssShopping.ShoppingState.交易成功.ToString() && spModel.State != BssShopping.ShoppingState.交易取消.ToString())
                {
                    List<ShoppingDbAutoAssembly> ShopAssmeblyList = new BssShoppingDbAutoAssembly().GetModelList(string.Format(" ShoppingId='{0}'", spId));
                    string Assmblyinfo = Type == "1" ? BssShoppingDbAutoAssembly.AssemblyInfo.客服复制卖家所需信息.ToString() : Type == "2" ? BssShoppingDbAutoAssembly.AssemblyInfo.客服未联系到卖家.ToString() : Type == "3" ? BssShoppingDbAutoAssembly.AssemblyInfo.允许买家提取帐号信息.ToString() : "";
                    ShoppingDbAutoAssembly shopAssmebly = ShopAssmeblyList.Where(p => p.AssemblyInfo == Assmblyinfo).FirstOrDefault();
                    if (shopAssmebly == null)
                    {
                        BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(spId, BssShoppingDbAutoAssembly.AssType.客服.ToString(), Assmblyinfo);
                    }
                    else
                    {
                        if (Type == "2" && State == "noseller")
                        {
                            new BssShoppingDbAutoAssembly().Delete(shopAssmebly.ID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("记录必要步骤：", ex, this.GetType().FullName, "ExtractSteps");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 查询订单免赔条目
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="spId"></param>
        /// <returns></returns>
        public ActionResult OrderSuccessMPReason(string Type, string spId)
        {
            List<ShopFailedReasonConfig> list = new BLLAdminOrderMethod().GetSuccessMPReason(Type, spId);
            if (list == null || list.Count == 0)
            {
                return Content("");
            }
            return View(list);
        }
        /// <summary>
        /// 验证必要步骤是否执行
        /// </summary>
        /// <param name="DealType">订单类型</param>
        /// <param name="spId">shopid</param>
        /// <returns></returns>
        public ActionResult OrderStepsVerfiy(string DealType, string spId)
        {
            string reg = "";
            if (string.IsNullOrEmpty(DealType))
            {
                return Content("此订单未检查到必要步骤是否执行，是否确定提交？");
            }
            try
            {
                List<ShoppingDbAutoAssembly> ShopAssmeblyList = new BssShoppingDbAutoAssembly().GetModelList(string.Format(" ShoppingId='{0}'", spId));
                if (DealType == "1")
                {
                    bool query = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.客服开始登陆游戏.ToString());
                    reg = query ? "" : "打开游戏";
                }
                else if (DealType == "2")
                {
                    bool query = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.客服复制卖家所需信息.ToString());
                    bool virey = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.客服未联系到卖家.ToString());
                    reg = !query && virey ? "复制卖家所需信息和未联系到卖家" : !query ? "复制卖家所需信息" : virey ? "未联系到卖家" : "";
                }
                else if (DealType == "3")
                {
                    bool query = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.允许买家提取帐号信息.ToString());
                    reg = query ? "" : "允许买家提取帐号";
                }
                if (!string.IsNullOrEmpty(reg))
                {
                    reg = "此订单未执行“" + reg + "”操作，是否确定提交？";
                }
            }
            catch (Exception ex)
            {
                reg = "此订单未检查到必要步骤是否执行，是否确定提交？";
                LogExcDb.Log_AppDebug("验证必要步骤是否执行：", ex, this.GetType().FullName, "OrderStepsVerfiy");
            }
            return Content(reg);
        }
        /// <summary>
        /// 再次提取密码记录
        /// </summary>
        /// <returns></returns>
        public ActionResult ReGetPwdToDB(string spId)
        {
            try
            {
                Shopping spModel = new BssShopping().GetModel(spId);
                if (spModel != null)
                {
                    Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                    BssModifyMembersRecording.AddCaoZuoRecording(spModel.UserID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.再次提取密码, spId, spId);
                    List<ShoppingDbAutoAssembly> ShopAssmeblyList = new BssShoppingDbAutoAssembly().GetModelList(string.Format(" ShoppingId='{0}'", spId));
                    bool query = ShopAssmeblyList.Exists(p => p.AssemblyInfo == BssShoppingDbAutoAssembly.AssemblyInfo.客服开始登陆游戏.ToString());
                    if (!query)
                    {
                        BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(spId, BssShoppingDbAutoAssembly.AssType.客服.ToString(), BssShoppingDbAutoAssembly.AssemblyInfo.客服开始登陆游戏.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("再次提取密码记录：", ex, this.GetType().FullName, "ReGetPwdToDB");
            }
            return Content("");
        }

        /// <summary>
        /// 提取订单会员信息验证
        /// </summary>
        /// <returns></returns>
        [Role("提取订单会员信息验证", IsAuthorize = true)]
        public ActionResult ExistsGetUserInfo(string shoppingId, string type)
        {
            string retMsg = "";
            try
            {
                Shopping spModel = new BssShopping().GetModel(shoppingId);
                if (spModel == null)
                {
                    retMsg = "获取订单信息失败";
                    return Content(retMsg);
                }
                if (spModel.ObjectType == BssShopping.ShoppingType.会员商城.ToString() || spModel.ObjectType == BssShopping.ShoppingType.点券商城.ToString())
                {
                    if (spModel.State == BssShopping.ShoppingState.交易成功.ToString() || spModel.State == BssShopping.ShoppingState.交易取消.ToString())
                    {
                        retMsg = "该订单已被处理过";
                        return Content(retMsg);
                    }
                }
                else
                {
                    OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(spModel.ID);
                    if (orderShopModel != null && orderShopModel.DealType != (int)BssShop.EDealType.帐号)
                    {
                        if (spModel.State == BssShopping.ShoppingState.交易成功.ToString() || spModel.State == BssShopping.ShoppingState.部分完成.ToString() || spModel.State == BssShopping.ShoppingState.交易取消.ToString())
                        {
                            retMsg = "该订单已被处理过";
                            return Content(retMsg);
                        }
                    }
                }

                switch (type)
                {
                    case "SellPhone":
                        type = "获取卖家联系电话";
                        break;
                    case "SellQQ":
                        type = "获取卖家联系QQ";
                        break;
                    case "CopySellQQ":
                        type = "复制卖家联系QQ";
                        break;
                    case "BuyPhone":
                        type = "获取买家联系电话";
                        break;
                    case "BuyQQ":
                        type = "获取买家联系QQ";
                        break;
                    case "CopyBuyQQ":
                        type = "复制买家联系QQ";
                        break;
                    case "CopySellOrderPhone":
                        type = "复制卖家订单联系电话";
                        break;
                    case "CopySellOrderQQ":
                        type = "复制卖家订单联系QQ";
                        break;
                    case "BuyPwd":
                        type = "获取买家订单游戏密码";
                        break;
                    case "BuyAccount":
                        type = "获取买家订单游戏帐号";
                        break;
                    case "CopySellAccount":
                        type = "获取卖家商品游戏帐号";
                        break;
                    default:
                        break;
                }
                Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                BssModifyMembersRecording.AddCaoZuoRecording(spModel.UserID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.获取会员信息, shoppingId, "类型：" + type);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("提取订单会员信息验证：", ex, this.GetType().FullName, "ExistsGetUserInfo");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 提取订单成功或取消会员信息验证
        /// </summary>
        /// <returns></returns>
        [Role("提取订单订单成功或取消会员信息验证", IsAuthorize = true)]
        public ActionResult ExistsSuccessGetUserInfo(string shoppingId, string type)
        {
            string retMsg = "";
            try
            {
                Shopping spModel = new BssShopping().GetModel(shoppingId);
                if (spModel == null)
                {
                    retMsg = "获取订单信息失败";
                    return Content(retMsg);
                }
                switch (type)
                {
                    case "SellPhone":
                        type = "获取卖家联系电话";
                        break;
                    case "SellQQ":
                        type = "获取卖家联系QQ";
                        break;
                    case "CopySellQQ":
                        type = "复制卖家联系QQ";
                        break;
                    case "BuyPhone":
                        type = "获取买家联系电话";
                        break;
                    case "BuyQQ":
                        type = "获取买家联系QQ";
                        break;
                    case "CopyBuyQQ":
                        type = "复制买家联系QQ";
                        break;
                    case "CopySellOrderPhone":
                        type = "复制卖家订单联系电话";
                        break;
                    case "CopySellOrderQQ":
                        type = "复制卖家订单联系QQ";
                        break;
                    case "BuyPwd":
                        type = "获取买家订单游戏密码";
                        break;
                    case "BuyAccount":
                        type = "获取买家订单游戏帐号";
                        break;
                    case "CopySellAccount":
                        type = "获取卖家商品游戏帐号";
                        break;
                    default:
                        break;
                }
                Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                BssModifyMembersRecording.AddCaoZuoRecording(spModel.UserID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.获取会员信息, shoppingId, "类型：" + type);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("提取订单订单成功或取消会员信息验证：", ex, this.GetType().FullName, "ExistsSuccessGetUserInfo");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }

        #region 商品比例限制配置
        [Role("商品比例限制配置列表", IsAuthorize = true)]
        public ActionResult ShopRatioConfigList(int? Page)
        {
            DataPages<ShopRatioConfigModel> TaskList = null;
            try
            {
                TaskList = new BLLShopRatioConfig().GetShopRatioDPList(Page ?? 1, 20);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取后台积分列表出错", ex, this.GetType().FullName, "ShopRatioConfigList");
            }
            return View(TaskList);
        }
        #endregion

        [Role("第三方请求链接记录", IsAuthorize = true)]
        public ActionResult SdkPostUrlList(int? Page, string shoppingId, string StartTime, string EntTime, string Stype, string State)
        {
            DataPages<Weike.EShop.SdkPostUrl> LSa = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(shoppingId))
                {
                    where.Append(string.Format(" and OrderId='{0}'", shoppingId));
                }
                if (!string.IsNullOrEmpty(Stype))
                {
                    where.Append(string.Format(" and SdkType='{0}'", Stype));
                }
                if (!string.IsNullOrEmpty(State))
                {
                    where.Append(string.Format(" and Status='{0}'", State));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format("and createtime between '{0}' and '{1}'", STime, ETime));

                LSa = new BssSdkPostUrl().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("第三方请求链接记录出错", ex, this.GetType().FullName, "SdkPostUrlList");
            }

            return View(LSa);

        }


        /// <summary>
        /// 更新帐号信息
        /// </summary>
        /// <returns></returns>
        [Role("更新帐号信息", IsAuthorize = true)]
        public ActionResult UpdateFormValue(string fvGuid,string ShopNo)
        {
            FormValue fvModel = null;
            try
            {
                bool isChange = false;
                BssFormValueChange bssFVC = new BssFormValueChange();
                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                BssFormValue bssFv = new BssFormValue();
                fvModel = bssFv.GetModelByFvGuid(fvGuid);
                if (fvModel == null)
                {
                    fvModel = bssFVC.GetModelByFvGuid(fvGuid);
                    if (fvModel == null)
                    {
                        MsgHelper.InsertResult("没有找到该帐号信息，请重新尝试");
                        return View(fvModel);
                    }
                    isChange = true;
                }
                BssFormFields bssFF = new BssFormFields();
                FormFields ffModel = bssFF.GetModel(fvModel.FldGuid);
                if (ffModel == null)
                {
                    MsgHelper.InsertResult("没有找到该表单，请重新尝试");
                    return View(fvModel);
                }
                ViewData["FFModel"] = ffModel;
                if (IsPost)
                {
                    ShopAccount shopAccount = null;
                    BssShopAccount bssShopAccount = new BssShopAccount();

                    if (ffModel.FldType == "3")
                    {
                        string picUrl = Globals.Attachment_UploadNoWater("fvValue");
                        fvModel.FVValue = picUrl;
                    }
                    else
                    {
                        string newFvValue = Request.Form["fvValue"].ToString();
                        if (ffModel.FldName.Contains("游戏帐号") || ffModel.FldName.Contains("游戏账号"))
                        {
                            if (string.IsNullOrEmpty(newFvValue))
                            {
                                MsgHelper.InsertResult("请填写游戏帐号");
                                return View(fvModel);
                            }
                            string sZhRemark = "";
                            Weike.CMS.BLLKeyValues blk = new Weike.CMS.BLLKeyValues();
                            if (blk.Exists(Weike.CMS.BLLKeyValues.KeyType.游戏帐号, ffModel.ObjectID, newFvValue, out sZhRemark))
                            {
                                MsgHelper.InsertResult("该帐号已加入黑名单，请检查后再试！");
                                return View(fvModel);
                            }
                            Shop shopModel = new BssShop().GetModelTOShopID(ShopNo);
                            if (shopModel == null)
                            {
                                MsgHelper.InsertResult("商品已被删除！");
                                return View(fvModel);
                            }
                            if (BssShop.GameAccountReSell(ffModel.ObjectID, newFvValue, shopModel))
                            {
                                MsgHelper.Insert("SellSures", "该帐号已经在平台上出售，请不要重复发布");
                                return View(fvModel);
                            }
                            bool IsCheckAccSell = true;
                            GameCompanyInfo gciModel = new BssGameCompanyInfo().GetModel(ffModel.ObjectID);
                            if (gciModel != null && gciModel.CompanyId == 1)
                            {
                                string pattern = @"(^1\d{10}$)";
                                if (System.Text.RegularExpressions.Regex.IsMatch(newFvValue, pattern))
                                {
                                    IsCheckAccSell = false;
                                }
                            }
                            if (IsCheckAccSell)
                            {
                                if (!BssShopAccount.GameAccountSameUserSell(ffModel.ObjectID, newFvValue, shopModel.PublicUser))
                                {
                                    MsgHelper.Insert("SellSures", "该账号已被其他用户购买");
                                    return View(fvModel);
                                }
                            }
                            shopAccount = bssShopAccount.GetModelByShopNo(ShopNo);
                        }
                        fvModel.FVValue = newFvValue;
                    }

                    if (!isChange)
                        bssFv.UpdateByFVGuid(fvModel);
                    else
                        bssFVC.UpdateByFVGuid(fvModel);

                    if (shopAccount != null)
                    {
                        bssShopAccount.UpdateAccountNo(ShopNo, fvModel.FVValue);
                    }
                    MsgHelper.InsertResult("更新成功");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新FormValue表单：", ex, this.GetType().FullName, "UpdateFormValue");
            }
            return View(fvModel);
        }


        /// <summary>
        /// 增加帐号信息
        /// </summary>
        /// <returns></returns>
        [Role("增加帐号信息", IsAuthorize = true)]
        public ActionResult AddFormValue(string fldGuid, string spGUID, string ischange)
        {
            FormFields ffModel = null;
            try
            {
                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                BssFormFields bssFF = new BssFormFields();
                ffModel = bssFF.GetModel(fldGuid);
                if (ffModel == null)
                {
                    MsgHelper.InsertResult("没有找到该表单，请重新尝试");
                    return View(ffModel);
                }
                if (IsPost)
                {
                    FormValue fvModel = new FormValue();
                    fvModel.createtime = DateTime.Now;
                    fvModel.FldGuid = fldGuid;
                    fvModel.FVGuid = Guid.NewGuid().ToString();
                    fvModel.ObjectID = spGUID;

                    if (ffModel.FldType == "3")
                    {
                        string picUrl = Globals.Attachment_UploadNoWater("fvValue");
                        fvModel.FVValue = picUrl;
                    }
                    else
                    {
                        fvModel.FVValue = Request.Form["fvValue"].ToString();
                    }

                    if (ischange == "1")
                        new BssFormValueChange().Add(fvModel);
                    else
                        new BssFormValue().Add(fvModel);

                    MsgHelper.InsertResult("增加成功");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("增加FormValue表单：", ex, this.GetType().FullName, "AddFormValue");
            }
            return View(ffModel);
        }

        /// <summary>
        /// 是否允许买家提取帐号
        /// </summary>
        /// <param name="shoppingId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [Role("是否允许买家提取帐号", IsAuthorize = true)]
        public ActionResult AllowGetAccount(string shoppingId, string type)
        {
            string res = "";

            try
            {
                BssShopping bllShopping = new BssShopping();
                Shopping shopping = bllShopping.GetModel(shoppingId);
                if (shopping != null)
                {
                    BssShoppingGetZH bssGz = new BssShoppingGetZH();
                    ShoppingGetZH gzModel = bssGz.GetModel(shoppingId);
                    if (gzModel == null)
                    {
                        gzModel = new ShoppingGetZH();
                        gzModel.CreateTime = DateTime.Now;
                        gzModel.EditTime = DateTime.Now;
                        gzModel.IsEnabled = type == "y" ? 1 : 0;
                        gzModel.Remark = string.Format("更新允许买家提取帐号状态：管理员用户名[{0}],操作时间[{1}]，更新状态[{2}]。", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), type == "y" ? "允许" : "拒绝");
                        gzModel.SpId = shoppingId;
                        bssGz.Add(gzModel);

                        //插入订单备注记录
                        BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, string.Format("更新允许买家提取帐号状态：管理员用户名[{0}]，更新状态[{1}]", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, type == "y" ? "允许" : "拒绝"));
                    }
                    else
                    {
                        gzModel.EditTime = DateTime.Now;
                        gzModel.IsEnabled = type == "y" ? 1 : 0;
                        if (gzModel.Remark.Length <= 100)
                            gzModel.Remark = string.Format("更新允许买家提取帐号状态：管理员用户名[{0}],操作时间[{1}]，更新状态[{2}]。<br/>", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), type == "y" ? "允许" : "拒绝");
                        bssGz.Update(gzModel);

                        //插入订单备注记录
                        BssShoppingRemarkInfo.InsertShoppingRemarkInfo(shopping.ID, string.Format("更新允许买家提取帐号状态：管理员用户名[{0}]，更新状态[{1}]", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, type == "y" ? "允许" : "拒绝"));
                    }

                    if (type == "y")
                        res = "更新允许买家提取帐号成功";
                    else
                        res = "更新拒绝买家提取帐号成功";
                }
                else
                {
                    res = "更新允许买家提取帐号状态失败，无法获取订单信息";
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("是否允许买家提取帐号", ex, this.GetType().FullName, "AllowGetAccount");
            }

            return Content(res);
        }

        [Role("出售账号列表", IsAuthorize = true)]
        public ActionResult ShopAccountList(int? Page, string GameId, string AccountNo, string UserName)
        {
            DataPages<Weike.EShop.ShopAccount> LSa = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(GameId))
                {
                    where.Append(string.Format(" and GameId='{0}'", GameId));
                }
                if (!string.IsNullOrEmpty(AccountNo))
                {
                    where.Append(string.Format(" and AccountNo='{0}'", AccountNo));
                }
                if (!string.IsNullOrEmpty(UserName))
                {
                    where.Append(string.Format(" and M_ID=(select m_id from members where m_name='{0}')", UserName));
                }

                LSa = new BssShopAccount().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("出售账号列表出错", ex, this.GetType().FullName, "ShopAccountList");
            }

            return View(LSa);

        }

        [Role("成功出售账号列表", IsAuthorize = true)]
        public ActionResult ShopAccountSuccessList(int? Page, string GameId, string AccountNo, string SellName, string BuyName)
        {
            DataPages<Weike.EShop.ShopAccount> LSa = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(GameId))
                {
                    where.Append(string.Format(" and GameId='{0}'", GameId));
                }
                if (!string.IsNullOrEmpty(AccountNo))
                {
                    where.Append(string.Format(" and AccountNo='{0}'", AccountNo));
                }
                if (!string.IsNullOrEmpty(SellName))
                {
                    where.Append(string.Format(" and M_ID=(select m_id from members where m_name='{0}')", SellName));
                }
                if (!string.IsNullOrEmpty(BuyName))
                {
                    where.Append(string.Format(" and BuyerId=(select m_id from members where m_name='{0}')", BuyName));
                }
                where.AppendFormat(" and OrderState='{0}'", BssShopping.ShoppingState.交易成功.ToString());

                LSa = new BssShopAccount().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("成功出售账号列表", ex, this.GetType().FullName, "ShopAccountSuccessList");
            }

            return View(LSa);
        }

        /// <summary>
        /// 删除成功出售账号
        /// </summary>
        /// <returns></returns>
        [Role("删除成功出售账号", Description = "删除成功出售账号", IsAuthorize = true)]
        public ActionResult DelShopAccountSuccess(int sID)
        {
            try
            {
                new Weike.EShop.BssShopAccount().Delete(sID);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除成功出售账号", ex, this.GetType().FullName, "DelShopAccountSuccess");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("失败原因分类列表", IsAuthorize = true)]
        public ActionResult ShopFailedReasonList(int? Page, string ReasonType, string BuyType, string DealType, string Enabled)
        {
            DataPages<Weike.EShop.ShopFailedReasonConfig> LReason = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(ReasonType))
                {
                    where.Append(string.Format(" and ReasonType='{0}'", ReasonType));
                }
                if (!string.IsNullOrEmpty(BuyType))
                {
                    where.Append(string.Format(" and BuyType='{0}'", BuyType));
                }
                if (!string.IsNullOrEmpty(DealType))
                {
                    where.Append(string.Format(" and DealType='{0}'", DealType));
                }
                if (!string.IsNullOrEmpty(Enabled))
                {
                    where.Append(string.Format(" and Enabled={0}", Enabled));
                }

                LReason = new BssShopFailedReasonConfig().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("失败原因分类列表出错", ex, this.GetType().FullName, "ShopFailedReasonList");
            }

            return View(LReason);
        }

        [Role("添加失败原因分类", IsAuthorize = true)]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ShopReasonNew(string AContent, string ASortNo, string AReasonType, string ABuyType, string ADealType, string ARemark, string AEnabled, string AIsXjRole, string AXjAccount, string AIsOffline)
        {
            if (IsPost)
            {
                try
                {
                    ShopFailedReasonConfig sfmodel = new ShopFailedReasonConfig();
                    sfmodel.BuyType = ABuyType;
                    sfmodel.CreateTime = DateTime.Now;
                    sfmodel.DealType = ADealType;
                    sfmodel.EditTime = DateTime.Now;
                    sfmodel.Enabled = AEnabled.ToInt32();
                    sfmodel.ReasonContent = AContent;
                    sfmodel.ReasonType = AReasonType;
                    sfmodel.Remark = ARemark;
                    sfmodel.SortNo = ASortNo.ToInt32();
                    sfmodel.XjAccountShop = AXjAccount.ToInt32();
                    sfmodel.XjRoleShop = AIsXjRole.ToInt32();
                    sfmodel.IsOffline = AIsOffline.ToInt32();
                    new BssShopFailedReasonConfig().Add(sfmodel);
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("添加失败原因分类出错", ex, this.GetType().FullName, "ShopReasonNew");
                    return Content("添加失败原因分类出错");
                }
            }

            return Content("");
        }

        [Role("删除失败原因分类", IsAuthorize = true)]
        public ActionResult ShopReasonDel(string fID)
        {
            try
            {
                new BssShopFailedReasonConfig().Delete(fID.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除失败原因分类出错", ex, this.GetType().FullName, "ShopReasonDel");
            }
            return Content("");
        }

        [Role("修改失败原因分类", IsAuthorize = true)]
        public ActionResult ShopReasonUpdate(string fid, string AContent, string ASortNo, string AReasonType, string ABuyType, string ADealType, string ARemark, string AEnabled, string AIsXjRole, string AXjAccount, string AIsOffline)
        {
            BssShopFailedReasonConfig bssSf = new BssShopFailedReasonConfig();
            ShopFailedReasonConfig sfmodel = bssSf.GetModel(fid.ToInt32());
            try
            {
                if (IsPost)
                {
                    sfmodel.BuyType = ABuyType;
                    sfmodel.DealType = ADealType;
                    sfmodel.EditTime = DateTime.Now;
                    sfmodel.Enabled = AEnabled.ToInt32();
                    sfmodel.ReasonContent = AContent;
                    sfmodel.ReasonType = AReasonType;
                    sfmodel.Remark = ARemark;
                    sfmodel.SortNo = ASortNo.ToInt32();
                    sfmodel.XjAccountShop = AXjAccount.ToInt32();
                    sfmodel.XjRoleShop = AIsXjRole.ToInt32();
                    sfmodel.IsOffline = AIsOffline.ToInt32();
                    new BssShopFailedReasonConfig().Update(sfmodel);

                    return Content("");
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新失败原因分类出错", ex, this.GetType().FullName, "ShopReasonUpdate");
            }

            return Json(sfmodel);
        }

        /// <summary>
        /// 提交上传账号图片
        /// </summary>
        /// <returns></returns>
       [Role("提交上传账号图片", IsAuthorize = true)]
        public ActionResult SubmitShopAccountImage(string ShopId, string ShoppingId, string ImagePath, string TypeId, string Guid, string infoType)
        {
            string res = "OK";
            try
            {
                if (string.IsNullOrEmpty(ShopId) || string.IsNullOrEmpty(ShoppingId) || string.IsNullOrEmpty(ImagePath) || string.IsNullOrEmpty(TypeId))
                    res = "参数错误，请重新上传提交";
                else
                {
                    Shop shopModel = new BssShop().GetModelTOShopID(ShopId);
                    MembersMallShop mallShop = null;
                    if (shopModel == null)
                    {
                        mallShop = new BssMembersMallShop().GetModel(ShopId.ToInt32());
                        if (mallShop == null)
                        {
                            NeedReceive needModel = new BssNeedReceive().GetModel(ShopId.ToInt32());
                            if (needModel == null)
                            {
                                res = "商品不存在";
                                return Content(res);
                            }
                        }
                    }
                    ShopAccountImageInfo saiModel = new ShopAccountImageInfo();
                    saiModel.CretaeTime = DateTime.Now;
                    saiModel.ImgInfo = ImagePath.Trim('|');
                    saiModel.ShopId = ShopId;
                    saiModel.ShoppingId = ShoppingId;
                    if (!string.IsNullOrWhiteSpace(infoType)&&!Enum.IsDefined(typeof(BssShopAccountImageInfo.InfoType), infoType))
                    {
                        infoType = BssShopAccountImageInfo.InfoType.截图信息.ToString();
                    }
                    Admins admin = BLLAdmins.GetCurrentAdminUserInfo();
                    saiModel.SID = admin != null ? admin.A_ID : 0;
                    saiModel.InfoType = infoType;
                    string typeName = "";
                    int orderNo = 0;
                    if (TypeId == "0")
                    {
                        Guid = "";
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Guid))
                        {
                            res = "不能为空";
                            return Content(res);
                        }
                        ScreenShotType st = new BssScreenShotType().GetModel(TypeId);
                        if (st == null)
                        {
                            res = "截图类型不存在，请刷新页面后重试";
                            return Content(res);
                        }
                        typeName = st.Name;
                        orderNo = st.OrderNo;
                    }

                    saiModel.ScreenShotType = typeName;
                    saiModel.OrderNo = orderNo;
                    saiModel.Guid = Guid;
                    int imgId = new BssShopAccountImageInfo().Add(saiModel);
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("提交上传账号图片：", ex, this.GetType().FullName, "SubmitShopAccountImage");
            }
            return Content(res);
        }

        /// <summary>
        /// 查看帐号上传图片
        /// </summary>
        /// <returns></returns>
        [Role("查看帐号上传图片", IsAuthorize = true)]
        public ActionResult ViewShopAccountImage(int Sid)
        {
            List<ShopAccountImageInfo> list = null;
            try
            {
                BssShopAccountImageInfo bssSai = new BssShopAccountImageInfo();
                ShopAccountImageInfo saiModel = bssSai.GetModel(Sid);

                if (saiModel != null && !string.IsNullOrEmpty(saiModel.ImgInfo))
                {
                    if (string.IsNullOrEmpty(saiModel.Guid))
                    {
                        list = new List<ShopAccountImageInfo>();
                        list.Add(saiModel);
                    }
                    else
                    {
                        list = bssSai.GetModelListByGuid(saiModel.Guid);
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("查看帐号上传图片：", ex, this.GetType().FullName, "ViewShopAccountImage");
            }
            return View(list);
        }

        /// <summary>
        /// 提交账号验证信息
        /// </summary>
        /// <returns></returns>
        [Role("提交账号验证信息", IsAuthorize = true)]
        public ActionResult SubmitShopAccountYzInfo(string ShopId, string YzInfo)
        {
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(ShopId) || string.IsNullOrEmpty(YzInfo))
                    res = "参数错误，请重新提交";
                else
                {
                    Shop shopModel = new BssShop().GetModel(ShopId.ToInt32());
                    if (shopModel == null)
                    {
                        res = "商品不存在";
                    }
                    else
                    {
                        ShopAccountImageInfo saiModel = new ShopAccountImageInfo();
                        saiModel.CretaeTime = DateTime.Now;
                        saiModel.ImgInfo = YzInfo;
                        saiModel.ShopId = shopModel.ShopID;
                        saiModel.ShoppingId = "";
                        saiModel.InfoType = BssShopAccountImageInfo.InfoType.验证信息.ToString();
                        new BssShopAccountImageInfo().Add(saiModel);
                    }
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("提交账号验证信息：", ex, this.GetType().FullName, "SubmitShopAccountYzInfo");
            }
            return Content(res);
        }

        /// <summary>
        /// 提交账号认证信息
        /// </summary>
        /// <returns></returns>
        [Role("提交账号认证信息", IsAuthorize = true)]
        public ActionResult SubmitShopAccountRzInfo(string ShopId, string ShoppingId, string RzInfo)
        {
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(ShopId) || string.IsNullOrEmpty(RzInfo))
                    res = "参数错误，请重新提交";
                else
                {
                    Shop shopModel = new BssShop().GetModel(ShopId.ToInt32());
                    if (shopModel == null)
                    {
                        res = "商品不存在";
                    }
                    else
                    {
                        ShopAccountImageInfo saiModel = new ShopAccountImageInfo();
                        saiModel.CretaeTime = DateTime.Now;
                        saiModel.ImgInfo = RzInfo;
                        saiModel.ShopId = shopModel.ShopID;
                        saiModel.ShoppingId = ShoppingId;
                        saiModel.InfoType = BssShopAccountImageInfo.InfoType.认证信息.ToString();
                        new BssShopAccountImageInfo().Add(saiModel);

                        res = "提交成功";
                    }
                }

            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("提交账号认证信息：", ex, this.GetType().FullName, "SubmitShopAccountRzInfo");
            }
            return Content(res);
        }

        /// <summary>
        /// 提交账号认证信息
        /// </summary>
        /// <param name="ShopId">商品编号</param>
        /// <param name="ShoppingId">订单编号</param>
        /// <param name="RzInfo">认证信息</param>
        /// <returns></returns>
        [Role("提交账号认证信息", IsAuthorize = true)]
        public ActionResult SubmitShopAccountRzInfoNew(string ShopId, string ShoppingId, string RzInfo)
        {
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(ShopId) || string.IsNullOrEmpty(ShoppingId))
                    res = "参数错误，请重新提交";
                else
                {
                    #region 判断客服类型
                    Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                    if (BssAdmins.IsYiChangKefu(adminModel))
                    {
                        res = "不是验证客服不能进行提交";
                        return Content(res);
                    }
                    if (new BssShoppingAssembly().GetModelBySpIdAndSidAndSType(ShoppingId, adminModel.A_ID, BssShoppingAssembly.SourceType.截图完成.ToString()) == null)
                    {
                        res = "不是验证客服不能进行提交";
                        return Content(res);
                    }
                    #endregion

                    #region 判断订单状态
                    Shopping ordermodel = new BssShopping().GetModel(ShoppingId);
                    if (ordermodel == null || ordermodel.State == BssShopping.ShoppingState.交易成功.ToString() ||
                        ordermodel.State == BssShopping.ShoppingState.部分完成.ToString() || ordermodel.State == BssShopping.ShoppingState.交易取消.ToString())
                    {
                        res = "该订单状态下不能进行提交账号认证信息";
                        return Content(res);
                    }

                    #endregion
                    BssShopAuditInfo bssa = new BssShopAuditInfo();
                    BssShopAuditInfo.WriteShopAuditInfo(ShopId, ShoppingId, Guid.NewGuid().ToString());

                    res = "提交成功";
                }

            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("提交账号认证信息新版", ex, this.GetType().FullName, "SubmitShopAccountRzInfoNew");
            }
            return Content(res);
        }

        /// <summary>
        /// 允许买家查看上传账号图片
        /// </summary>
        /// <returns></returns>
        [Role("允许买家查看上传账号图片", IsAuthorize = true)]
        public ActionResult AllowBuyerViewImage(int Sid, string ShoppingId)
        {
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(ShoppingId))
                    res = "参数错误，请重新提交";
                else
                {
                    BssShopAccountImageInfo bssSai = new BssShopAccountImageInfo();
                    ShopAccountImageInfo saiModel = bssSai.GetModel(Sid);
                    BssShopping bssSp = new BssShopping();
                    Shopping spModel = bssSp.GetModel(ShoppingId);
                    if (saiModel != null && spModel != null)
                    {
                        OrderShopSnapshot orderShopModel = new BssOrderShopSnapshot().GetModelByOrderId(spModel.ID);
                        if (orderShopModel == null)
                        {
                            res = "提交失败,订单商品信息不存在";
                        }
                        else
                        {
                            ShopInfoShow ss = new ShopInfoShow();
                            ss.ShopId = orderShopModel.ShopId;
                            ss.Guid = string.IsNullOrEmpty(saiModel.Guid) ? BssShopInfoShow.GuidPerfix + saiModel.ID.ToString() : saiModel.Guid;
                            ss.UserId = spModel.UserID;
                            ss.Type = BssShopInfoShow.ShowType.截图信息.ToString();
                            ss.CreateTime = DateTime.Now;
                            ss.ShoppingId = ShoppingId;
                            ss.ID = Guid.NewGuid().ToString().Replace("-", "");

                            new BssShopInfoShow().Add(ss);

                            //插入订单备注记录
                            BssShoppingRemarkInfo.InsertShoppingRemarkInfo(spModel.ID, string.Format("更新允许买家查看截图，管理员用户名[{0}]，截图ID:[{1}]", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, ss.Guid));

                            res = "提交成功";
                        }
                    }
                    else
                    {
                        res = "提交失败";
                    }
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("允许买家查看上传账号图片：", ex, this.GetType().FullName, "AllowBuyerViewImage");
            }
            return Content(res);
        }

        /// <summary>
        /// 更新商品描述信息
        /// </summary>
        /// <returns></returns>
        [Role("更新商品描述信息", IsAuthorize = true)]
        public ActionResult UpdateShopDetail(int shopId, string changeDetail)
        {
            string res = "";
            try
            {
                BssShop bssSp = new BssShop();
                Shop spModel = bssSp.GetModel(shopId);
                if (spModel != null)
                {
                    if (IsGet)
                        res = spModel.Detail;
                    else
                    {
                        if (!string.IsNullOrEmpty(changeDetail) && changeDetail != spModel.Detail)
                        {
                            spModel.Detail = changeDetail;
                            bssSp.Update(spModel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
                LogExcDb.Log_AppDebug("允许买家查看上传账号图片：", ex, this.GetType().FullName, "AllowBuyerViewImage");
            }
            return Content(res);
        }

        #region 下架指定会员全部商品
        /// <summary>
        /// 下架指定会员全部商品
        /// </summary>
        /// <returns></returns>
        [Role("下架指定会员全部商品", Description = "下架指定会员全部商品", IsAuthorize = true)]
        public ActionResult XiajiaMemberShop(string mid)
        {
            try
            {
                BLLShopInfoMethod bllShopMethod = new BLLShopInfoMethod();
                BssShop bssSp = new BssShop();
                List<Shop> shoplist = bssSp.GetModelList(string.Format(" publicuser={0} and ShopState='审核成功' and PublicCount>0", mid.ToInt32()));
                foreach (Shop s in shoplist)
                {
                    bllShopMethod.XiajiaShop(s);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除商品属性分类值出错", ex, this.GetType().FullName, "DelGameAttributeTypeValue");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        private void ReturnCashShopMoney(Shop s)
        {
            ShopOtherInfo cashInfo = new BssShopOtherInfo().GetModelByShopNoAndConfigId(s.ShopID, (int)BssShopOtherInfo.InfoType.押金商品);
            decimal CashMoney = cashInfo == null ? 10 : cashInfo.ConfigValues.ToDecimal2();
            if (cashInfo != null && s.PublicCount > 0)
            {
                #region 资金记录
                AccMembers bllmo = new AccMembers();
                Members msmodel = bllmo.GetModel(s.PublicUser);

                msmodel.M_Money += (CashMoney * s.PublicCount);
                new BLLMoneyhistory().Insert(msmodel.M_ID, CashMoney * s.PublicCount, s.ShopID, BssMoneyHistory.HistoryType.退款记录, msmodel.M_Money.Value - CashMoney * s.PublicCount, msmodel.M_Money.Value);
                bllmo.AddMembersMoney(CashMoney * s.PublicCount, msmodel.M_ID);
                #endregion
            }
        }
        #endregion


        /// <summary>
        /// 订单首次打开增加流程
        /// </summary>
        /// <param name="spId"></param>
        private void ShoppingStartAddAssembly(string spId)
        {
            try
            {
                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                if (adminmodel != null)
                {
                    BssShoppingAssembly bssSpAss = new BssShoppingAssembly();
                    ShoppingAssembly nsaModel = bssSpAss.GetModelBySpIdAndResType(spId, BssShoppingAssembly.ResType.开始处理.ToString());
                    if (nsaModel == null)
                    {
                        ShoppingAssembly dealModel = bssSpAss.GetNoModelBySpId(spId);
                        if (dealModel != null && dealModel.SID == adminmodel.A_ID && dealModel.SortNo == 1 && dealModel.SourceType != BssShoppingAssembly.SourceType.客服截图.ToString())
                        {
                            #region 插入客服订单流水
                            nsaModel = new ShoppingAssembly();
                            nsaModel.BuyerName = dealModel.BuyerName;
                            nsaModel.CreateTime = dealModel.CreateTime;
                            nsaModel.GameAccount = dealModel.GameAccount;
                            nsaModel.ProcessTime = Weike.Common.Globals.MinDateValue;
                            nsaModel.Remark = BssShoppingAssembly.ResType.开始处理.ToString();
                            nsaModel.ResType = BssShoppingAssembly.ResType.未处理.ToString();
                            nsaModel.SellerName = dealModel.SellerName;
                            nsaModel.ShopId = dealModel.ShopId;
                            nsaModel.ShoppingId = dealModel.ShoppingId;
                            nsaModel.SID = dealModel.SID;
                            nsaModel.SortNo = dealModel.SortNo + 1;
                            nsaModel.SourceType = BssShoppingAssembly.ResType.开始处理.ToString();
                            nsaModel.ObjectType = dealModel.ObjectType;
                            #endregion

                            //更新原来流水状态
                            dealModel.ProcessTime = DateTime.Now;
                            dealModel.Remark = dealModel.Remark + "|" + BssShoppingAssembly.ResType.开始处理.ToString();
                            dealModel.ResType = BssShoppingAssembly.ResType.开始处理.ToString();
                            bssSpAss.AsyncUpdateAssembly(dealModel, nsaModel, null);

                            BLLShoppingMethod.ShoppingDbAutoAssemblyAdd(spId, BssShoppingDbAutoAssembly.AssType.客服.ToString(), BssShoppingDbAutoAssembly.AssemblyInfo.客服开始处理订单.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单首次打开增加流程", ex, this.GetType().FullName, "ShoppingStartAddAssembly");
            }
        }


        [Role("会员资金-锁定资金退回", IsAuthorize = true)]
        public ActionResult ReturnMemberMoneyLock(string mlid)
        {
            int id = Convert.ToInt32(mlid);
            BssMoneyLock bss = new BssMoneyLock();
            MoneyLock ml = bss.GetModel(id);
            if (ml != null)
            {
                try
                {
                    Members mModel = new BssMembers().GetModel(ml.UID);
                    if (mModel != null)
                    {
                        #region Add--MoneyHistory-资金收回记录
                        new Weike.EShop.BssMoneyHistory().Add(new Weike.EShop.MoneyHistory()
                        {
                            CreateDate = DateTime.Now,
                            OperaType = Weike.EShop.BssMoneyHistory.HistoryType.收款记录.ToString(),
                            OrdID = ml.ObjectGUID,
                            State = Weike.EShop.BssMoneyHistory.HistoryState.成功.ToString(),
                            UID = mModel.M_ID,
                            SumMoney = ml.Price,
                            LastMoney = mModel.M_Money.Value,
                            AfterMoney = mModel.M_Money.Value + ml.Price
                        });
                        #endregion

                        #region 会员加钱
                        new BssMembers().AddMembersMoney(ml.Price, mModel.M_ID);
                        #endregion

                        //删除记录
                        bss.Delete(ml.ID);

                        Weike.CMS.Admins adminentity = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                        BssModifyMembersRecording.AddCaoZuoRecording(ml.UID, adminentity.A_ID, (int)BssModifyMembersRecording.ECate.退回锁定资金, ml.ObjectGUID, ml.ObjectGUID + DateTime.Now.ToString());
                    }
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("会员资金-锁定资金退回", ex, this.GetType().FullName, "ReturnMoneyLock");
                }
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        #region 盛付通卡充值配置
        [Role("盛付通卡充值配置", IsAuthorize = true)]
        public ActionResult SdoCardChargeConfigList(int? page)
        {
            string where = " 1=1 ";
            DataPages<Weike.EShop.SdoCardChargeConfig> chargeList = new BssSdoCardChargeConfig().GetPageRecord(where, "OrderNo", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(chargeList);
        }

        [Role("盛付通卡充值配置删除", IsAuthorize = true)]
        public ActionResult SdoCardChargeConfigDelete(string id)
        {
            try
            {
                new BssSdoCardChargeConfig().Delete(id.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("盛付通卡充值配置删除", ex, this.GetType().FullName, "SdoCardChargeConfigDelete");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("盛付通卡充值配置新增", IsAuthorize = true)]
        public ActionResult SdoCardChargeConfigAdd(string CardName, string MianZhi, string OrderNo, string PaymentType, string InstCode, string PayChannel, string ChangeRate, string NotifyInfo, bool? Enalbed)
        {
            try
            {
                BssSdoCardChargeConfig bssSdo = new BssSdoCardChargeConfig();
                SdoCardChargeConfig sdoModel = new SdoCardChargeConfig();
                sdoModel.CardName = CardName;
                sdoModel.ChangeRate = Convert.ToDecimal(ChangeRate);
                sdoModel.EditTime = DateTime.Now;
                sdoModel.CreateTime = DateTime.Now;
                sdoModel.ImgUrl = Globals.AttachSitePic_UploadNoWater("ImgFile");
                sdoModel.InstCode = InstCode;
                sdoModel.MianZhi = MianZhi.Trim(',');
                sdoModel.OrderNo = OrderNo.ToInt32();
                sdoModel.PayChannel = PayChannel;
                sdoModel.PaymentType = PaymentType;
                sdoModel.NotifyInfo = NotifyInfo.Replace("\r\n", "<br/>").Replace(" ", "&nbsp;"); ;
                sdoModel.Remark = string.Format("管理员{0}于{1}新增", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                sdoModel.Enalbed = Enalbed.Value;
                bssSdo.Add(sdoModel);

                MsgHelper.InsertResult("OK");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("盛付通卡充值配置新增", ex, this.GetType().FullName, "SdoCardChargeConfigAdd");
            }
            return View();
        }

        [Role("盛付通卡充值配置更新", IsAuthorize = true)]
        public ActionResult SdoCardChargeConfigUpdate(string id, string CardName, string MianZhi, string OrderNo, string PaymentType, string InstCode, string PayChannel, string ChangeRate, string NotifyInfo, bool? Enalbed)
        {
            SdoCardChargeConfig sdoModel = null;
            try
            {
                BssSdoCardChargeConfig bssSdo = new BssSdoCardChargeConfig();
                sdoModel = bssSdo.GetModel(id.ToInt32());
                if (sdoModel == null)
                {
                    return RedirectToAction("SdoCardChargeConfigList");
                }

                if (IsPost)
                {
                    string newImg = Globals.AttachSitePic_UploadNoWater("ImgFile");
                    sdoModel.CardName = CardName;
                    sdoModel.ChangeRate = Convert.ToDecimal(ChangeRate);
                    sdoModel.EditTime = DateTime.Now;
                    sdoModel.ImgUrl = string.IsNullOrEmpty(newImg) && !string.IsNullOrEmpty(Request["ImgUrl"]) ? Request["ImgUrl"].ToString() : newImg;
                    sdoModel.InstCode = InstCode;
                    sdoModel.MianZhi = MianZhi.Trim(',');
                    sdoModel.OrderNo = OrderNo.ToInt32();
                    sdoModel.PayChannel = PayChannel;
                    sdoModel.PaymentType = PaymentType;
                    sdoModel.NotifyInfo = NotifyInfo.Replace("\r\n", "<br/>").Replace(" ", "&nbsp;"); ;
                    sdoModel.Remark = string.Format("管理员{0}于{1}修改<br/>", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm")) + sdoModel.Remark;
                    sdoModel.Enalbed = Enalbed.Value;
                    bssSdo.Update(sdoModel);

                    MsgHelper.InsertResult("OK");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("盛付通卡充值配置更新", ex, this.GetType().FullName, "SdoCardChargeConfigUpdate");
            }
            return View(sdoModel);
        }


        /// <summary>
        /// 盛付通卡充值查询接口
        /// </summary>
        /// <returns></returns>
        [Role("盛付通卡充值查询接口", IsAuthorize = true)]
        public ActionResult SdoCardOrderQuery(int orderId)
        {
            try
            {
                BssMoneyHistory bssMh = new BssMoneyHistory();
                MoneyHistory mhModel = bssMh.GetModel(orderId);
                if (mhModel != null && mhModel.PayType == BssMoneyHistory.PayMoneyType.盛付通卡.ToString())
                {
                    string transStatus = "";
                    string errorCode = "";
                    string errorMsg = "";
                    string transAmount = "";
                    string orderAmount = "";

                    BssShoppingPayOrder bssSpOrder = new BssShoppingPayOrder();
                    ShoppingPayOrder payInfoModel = bssSpOrder.GetModel(mhModel.OrdID);
                    if (payInfoModel != null && !string.IsNullOrEmpty(payInfoModel.PayChannelId))
                    {
                        Weike.Config.ShengPayConfig config = Weike.Config.ShengPayConfig.Instance(payInfoModel.PayChannelId);
                        string partnerId = config.SdoPartnerId;
                        string sdoKey = config.SdoKey;

                        new Weike.EShop.PayMent.SdoCardQueryOrderService().SdoCardQueryOrder(partnerId, sdoKey, mhModel.OrdID, out transStatus, out errorCode, out errorMsg, out transAmount, out orderAmount);
                        if (transStatus == "01")
                        {
                            Weike.WebGlobalMethod.BLLPayOrderMethod.PassMoney(mhModel.OrdID, transAmount.ToDecimal2(), BssShoppingPayOrder.OrderState.成功);
                        }

                        LogExcDb.Log_AppDebug("盛付通卡充值查询：" + string.Format("transStatus:{0},errorCode:{1},errorMsg:{2},transAmount:{3},orderAmount:{4}", transStatus, errorCode, errorMsg, transAmount, orderAmount), null, this.GetType().FullName, "SdoCardOrderQuery");
                    }
                    else 
                    {
                        MsgHelper.InsertResult("支付渠道不存在");
                    }
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("盛付通卡充值查询接口出错：", ex, this.GetType().FullName, "SdoCardOrderQuery");
                MsgHelper.InsertResult(ex.Message);

            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        #endregion


        #region 网上银行渠道选择配置

        [Role("网上银行渠道选择配置列表", IsAuthorize = true)]
        public ActionResult BankOnlineConfig(int? page, string SBankType)
        {
            DataPages<Weike.EShop.BankOnlineConfig> bankList = null;
            try
            {
                string where = " 1=1 ";
                if (!string.IsNullOrEmpty(SBankType))
                    where += " and BankType='" + SBankType + "'";
                bankList = new BssBankOnlineConfig().GetPageRecord(where, "OrderNo", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网上银行渠道选择配置列表", ex, this.GetType().FullName, "BankOnlineConfig");
            }

            return View(bankList);
        }

        [Role("网上银行渠道选择配置删除", IsAuthorize = true)]
        public ActionResult BankOnlineConfigDelete(string bankid)
        {
            try
            {
                new BssBankOnlineConfig().Delete(bankid.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网上银行渠道选择配置删除", ex, this.GetType().FullName, "BankOnlineConfigDelete");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("网上银行渠道选择配置更改状态", IsAuthorize = true)]
        public ActionResult BankOnlineConfigUpdateState(string bankid)
        {
            try
            {
                BssBankOnlineConfig bssBank = new BssBankOnlineConfig();
                BankOnlineConfig bank = bssBank.GetModel(bankid.ToInt32());
                if (bank != null)
                {
                    bank.Enalbed = !bank.Enalbed;
                    bssBank.Update(bank);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网上银行渠道选择配置更改状态", ex, this.GetType().FullName, "BankOnlineConfigUpdateState");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }


        [Role("网上银行渠道选择配置新增", IsAuthorize = true)]
        public ActionResult BankOnlineConfigAdd(string BankName, string BankType, string OrderNo, string CardType, string Enalbed)
        {
            try
            {
                BssBankOnlineConfig bssBank = new BssBankOnlineConfig();
                BankOnlineConfig bank = new BankOnlineConfig();
                bank.BankName = BankName;
                bank.BankType = BankType;
                bank.CardType = CardType.ToInt32();
                bank.CreateTime = DateTime.Now;
                bank.OrderNo = OrderNo.ToInt32();
                bank.Enalbed = Convert.ToBoolean(Enalbed);
                bank.Remark = string.Format("管理员{0}于{1}新增", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                bssBank.Add(bank);

                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网上银行渠道选择配置新增", ex, this.GetType().FullName, "BankOnlineConfigAdd");
            }
            return Content("");
        }

        [Role("网上银行渠道选择配置更新", IsAuthorize = true)]
        public ActionResult BankOnlineConfigUpdate(string bankid, string BankName, string BankType, string OrderNo, string CardType, string Enalbed)
        {
            BssBankOnlineConfig bssBank = new BssBankOnlineConfig();
            BankOnlineConfig bank = new BankOnlineConfig();
            try
            {
                bank = bssBank.GetModel(bankid.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网上银行渠道选择配置更新出错", ex, this.GetType().FullName, "BankOnlineConfigUpdate");
            }
            if (IsPost)
            {
                bank.BankName = BankName;
                bank.BankType = BankType;
                bank.CardType = CardType.ToInt32();
                bank.CreateTime = DateTime.Now;
                bank.OrderNo = OrderNo.ToInt32();
                bank.Enalbed = Convert.ToBoolean(Enalbed);
                bank.Remark += string.Format("管理员{0}于{1}更新", BLLAdmins.GetCurrentAdminUserInfo().A_RealName, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                bssBank.Update(bank);
                return Content("");
            }
            return Json(bank);
        }


        #endregion


        #region 游戏厂商分类
        /// <summary>
        /// 游戏厂商分类列表
        /// </summary>
        /// <returns></returns>
        [Role("游戏厂商分类列表", Description = "游戏厂商分类列表", IsAuthorize = true)]
        public ActionResult GameCompanyList(int? page, string companyName)
        {
            DataPages<GameCompany> GcList = null;
            try
            {
                string where = " 1=1";
                if (!string.IsNullOrEmpty(companyName))
                    where = where + " and ConpanyName like '%" + companyName + "%'";
                GcList = new BssGameCompany().GetPageRecord(where, "CreateTime", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏厂商分类列表", ex, this.GetType().FullName, "GameCompanyList");
            }

            return View(GcList);
        }

        /// <summary>
        /// 删除游戏厂商分类
        /// </summary>
        /// <returns></returns>
        [Role("删除游戏厂商分类", Description = "删除游戏厂商分类", IsAuthorize = true)]
        public ActionResult DelGameCompany(string gcId)
        {
            try
            {
                new BssGameCompany().Delete(gcId.ToInt32());

                new BssGameCompanyInfo().DeleteByCompanyId(gcId.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除游戏厂商分类出错", ex, this.GetType().FullName, "DelGameCompany");
            }

            return RedirectToAction("GameCompanyList");
        }
        /// <summary>
        /// 更新游戏厂商分类
        /// </summary>
        /// <param name="gcId"></param>
        /// <param name="companyName"></param>
        /// <returns></returns>
        [Role("更新游戏厂商分类", Description = "更新游戏厂商分类", IsAuthorize = true)]
        public ActionResult UpdateGameCompany(string gcId, string companyName, string AccountUrl)
        {
            GameCompany gcModel = new GameCompany();
            BssGameCompany bsGc = new BssGameCompany();
            try
            {
                gcModel = bsGc.GetModel(gcId.ToInt32());

                if (IsPost)
                {
                    gcModel.ConpanyName = companyName;
                    gcModel.AccountUrl = AccountUrl;
                    gcModel.CreateTime = DateTime.Now;
                    bsGc.Update(gcModel);
                    return Content("");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新游戏厂商分类出错", ex, this.GetType().FullName, "UpdateGameCompany");
            }

            return Json(gcModel);
        }
        /// <summary>
        /// 新增游戏厂商分类
        /// </summary>
        /// <param name="companyName"></param>
        /// <returns></returns>
        [Role("新增游戏厂商分类", Description = "新增游戏厂商分类", IsAuthorize = true)]
        public ActionResult AddGameCompany(string companyName, string AccountUrl)
        {
            try
            {
                GameCompany gcModel = new GameCompany();
                BssGameCompany bsGc = new BssGameCompany();

                gcModel.ConpanyName = companyName;
                gcModel.AccountUrl = AccountUrl;
                gcModel.CreateTime = DateTime.Now;
                bsGc.Add(gcModel);

                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("新增游戏厂商分类出错", ex, this.GetType().FullName, "AddGameCompany");
            }
            return Content("");
        }
        #endregion

        #region 订单评价配置

        [Role("订单评价配置列表", IsAuthorize = true)]
        public ActionResult ShoppingEvaluationTypeList(int? Page, string BuyType, string DealType, string Enalbed)
        {
            DataPages<Weike.EShop.ShoppingEvaluationType> LReason = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(BuyType))
                {
                    where.Append(string.Format(" and BuyType='{0}'", BuyType));
                }
                if (!string.IsNullOrEmpty(DealType))
                {
                    where.Append(string.Format(" and DealType='{0}'", DealType));
                }
                if (!string.IsNullOrEmpty(Enalbed))
                {
                    where.Append(string.Format(" and Enalbed={0}", Enalbed));
                }

                LReason = new BssShoppingEvaluationType().GetPageRecord(where.ToString(), "createtime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单评价配置列表出错", ex, this.GetType().FullName, "ShoppingEvaluationTypeList");
            }

            return View(LReason);
        }

        [Role("添加订单评价配置分类", IsAuthorize = true)]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ShoppingEvaluationTypeNew(string ATypeName, string ASortNo, string ABuyType, string ADealType, string ADescribe, string AEnabled)
        {
            if (IsPost)
            {
                try
                {
                    ShoppingEvaluationType setmodel = new ShoppingEvaluationType();
                    setmodel.BuyType = ABuyType;
                    setmodel.CreateTime = DateTime.Now;
                    setmodel.DealType = ADealType.ToInt32();
                    setmodel.SortNo = ASortNo.ToInt32();
                    setmodel.Describe = ADescribe;
                    setmodel.Enalbed = Convert.ToBoolean(AEnabled);
                    setmodel.TypeName = ATypeName;

                    new BssShoppingEvaluationType().Add(setmodel);
                }
                catch (Exception ex)
                {
                    LogExcDb.Log_AppDebug("添加订单评价配置分类出错", ex, this.GetType().FullName, "ShoppingEvaluationTypeNew");
                    return Content("添加订单评价配置分类出错");
                }
            }

            return Content("");
        }

        [Role("删除订单评价配置分类", IsAuthorize = true)]
        public ActionResult ShoppingEvaluationTypeDel(string fID)
        {
            try
            {
                new BssShoppingEvaluationType().Delete(fID.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除订单评价配置分类出错", ex, this.GetType().FullName, "ShoppingEvaluationTypeDel");
            }
            return Content("");
        }

        [Role("修改订单评价配置分类", IsAuthorize = true)]
        public ActionResult ShoppingEvaluationTypeUpdate(string fid, string ATypeName, string ASortNo, string ABuyType, string ADealType, string ADescribe, string AEnabled)
        {
            BssShoppingEvaluationType bssSet = new BssShoppingEvaluationType();
            ShoppingEvaluationType setmodel = bssSet.GetModel(fid.ToInt32());
            try
            {
                if (IsPost)
                {
                    setmodel.BuyType = ABuyType;
                    setmodel.DealType = ADealType.ToInt32();
                    setmodel.SortNo = ASortNo.ToInt32();
                    setmodel.Describe = ADescribe;
                    setmodel.Enalbed = Convert.ToBoolean(AEnabled);
                    setmodel.TypeName = ATypeName;

                    bssSet.Update(setmodel);

                    return Content("");
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新订单评价配置分类出错", ex, this.GetType().FullName, "ShoppingEvaluationTypeUpdate");
            }

            return Json(setmodel);
        }


        [Role("订单评价配置分类值列表", IsAuthorize = true)]
        public ActionResult ShoppingEvaluationTypeValueList(int? page, string typeId)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(typeId))
            {
                where += string.Format(" and EvaluationTypeId = '{0}'", typeId);
            }
            DataPages<Weike.EShop.ShoppingEvaluationTypeValue> typeList = new BssShoppingEvaluationTypeValue().GetPageRecord(where, "SortNo", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(typeList);
        }

        /// <summary>
        /// 新增订单评价配置分类
        /// </summary>
        /// <returns></returns>
        [Role("新增订单评价配置分类值", Description = "新增订单评价配置分类", IsAuthorize = true)]
        public ActionResult AddShoppingEvaluationTypeValue(string typeId, string typeName, string describe, string SortNo, string Enalbed)
        {

            try
            {
                ShoppingEvaluationTypeValue setvModel = new ShoppingEvaluationTypeValue();
                setvModel.Createtime = DateTime.Now;
                setvModel.Describe = describe;
                setvModel.Enalbed = Convert.ToBoolean(Enalbed);
                setvModel.EvaluationTypeId = typeId.ToInt32();
                setvModel.EvaluationValue = typeName;
                setvModel.SortNo = SortNo.ToInt32();
                new Weike.EShop.BssShoppingEvaluationTypeValue().Add(setvModel);

                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("新增订单评价配置分类值出错", ex, this.GetType().FullName, "AddShoppingEvaluationTypeValue");
            }
            return Content("");
        }

        /// <summary>
        /// 更新订单评价配置分类值
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        [Role("更新订单评价配置分类值", Description = "更新订单评价配置分类值", IsAuthorize = true)]
        public ActionResult UpdateShoppingEvaluationTypeValue(string tid, string typeName, string describe, string SortNo, string Enalbed)
        {
            Weike.EShop.ShoppingEvaluationTypeValue setvModel = new Weike.EShop.ShoppingEvaluationTypeValue();
            Weike.EShop.BssShoppingEvaluationTypeValue bssSetv = new Weike.EShop.BssShoppingEvaluationTypeValue();
            try
            {
                setvModel = bssSetv.GetModel(tid.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("更新订单评价配置分类值出错", ex, this.GetType().FullName, "UpdateShoppingEvaluationTypeValue");
            }
            if (IsPost)
            {
                setvModel.Describe = describe;
                setvModel.Enalbed = Convert.ToBoolean(Enalbed);
                setvModel.EvaluationValue = typeName;
                setvModel.SortNo = SortNo.ToInt32();
                bssSetv.Update(setvModel);

                return Content("");
            }
            return Json(setvModel);
        }

        /// <summary>
        /// 删除订单评价配置分类值
        /// </summary>
        /// <returns></returns>
        [Role("删除订单评价配置分类值", Description = "删除订单评价配置分类值", IsAuthorize = true)]
        public ActionResult DelShoppingEvaluationTypeValue(string tid)
        {
            try
            {
                new Weike.EShop.BssShoppingEvaluationTypeValue().Delete(tid.ToInt32());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除订单评价配置分类值出错", ex, this.GetType().FullName, "DelShoppingEvaluationTypeValue");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        #endregion


        #region 会员商城订单处理

        [Role("处理会员商城商品订单", IsAuthorize = true)]
        public ActionResult MembersMallShopUSellOrderUpload(string sid, string ordOperState, string remark, string reason, string SucMp, string SucReason, string SucRemark, string adminType, string kfId, decimal? fhNum,string delayDay, string TimeType)
        {
            Weike.EShop.Shopping model = null;
            Weike.EShop.BssShopping bll = new Weike.EShop.BssShopping();

            try
            {
                model = bll.GetModel(sid);
                if (model == null)
                {
                    if (adminType == "1")
                    {
                        RedirectToAction("USellOrder", new { t = "s" });
                    }
                    else
                    {
                        RedirectToAction("littleUSellOrderAssembly", new { DelState = "nodeal", t = new Random().Next() });
                    }
                }

                //首次打开添加流程
                ShoppingStartAddAssembly(model.ID);

                if (IsPost)
                {
                    string msgInfo = "";

                    new Weike.WebGlobalMethod.BLLAdminOrderMethod().MembersMallOrderDealMethod(model, ordOperState, remark, reason, SucMp, SucReason, SucRemark, out msgInfo, kfId.ToInt32(), fhNum, delayDay,TimeType);
                    MsgHelper.Insert("megCkSell", msgInfo);

                    model = new BssShopping().GetModel(model.ID);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("处理会员商城商品订单", ex, this.GetType().FullName, "MembersMallShopUSellOrderUpload");
            }
            return View(model);
        }


        #endregion

        #region 收货商品列表

        [Role("收货订单处理", IsAuthorize = true)]
        public ActionResult NeedReceiveOrderUpload(string sid, string whoerror, string ordOperState, string remark, string reason, decimal? fhNum, string BJSName,string delayDay = "", string TimeType = "")
        {
            Weike.EShop.Shopping model = null;
            Weike.EShop.BssShopping bll = new Weike.EShop.BssShopping();

            model = bll.GetModel(sid);
            if (IsPost)
            {
                string msgInfo = "";
                if (model != null)
                {
                    bool res = new Weike.WebGlobalMethod.BLLAdminOrderMethod().NeedReceiveOrderUpload(model, 0, ordOperState, whoerror, remark, reason, out msgInfo, BJSName, fhNum, delayDay, TimeType);
                }

                MsgHelper.Insert("megCkSell", msgInfo);
            }
            return View(model);
        }
        [Role("收货订单部分发货计算退还金额", IsAuthorize = true)]
        public ActionResult NeedOrderBFFHShowByFhnum(string ShoppingID, decimal FHNum)
        {
            BssShopping bssShoping = new BssShopping();
            try
            {
                Shopping shoping = bssShoping.GetModel(ShoppingID);
                if (shoping != null)
                {
                    NeedReceiveOrder NeedOrder = new BssNeedReceiveOrder().GetModel(shoping.ID);
                    if (NeedOrder!=null)
                    {
                        if (FHNum > 0 && FHNum < NeedOrder.Num * shoping.Count)
                        {
                            bool IsShowBfwc = Weike.WebGlobalMethod.BLLShoppingMethod.IsShowBfwcShopping(shoping);//是否普通商品
                            if (IsShowBfwc)
                            {
                                bool HanShui = false;
                                decimal QuanBuNum = NeedOrder.Num * (shoping.Count.HasValue ? shoping.Count.Value : 1);
                                string ShuiHouStr = "";
                                decimal ShaoFa = 0M;
                                decimal TuiHuanPrice = 0.00M;
                                decimal ShiShouSXF = 0.00M;//实收手续费
                                double shuihou = 0.0;

                                HanShui = BLLShoppingMethod.IsHanShui(shoping, FHNum, out ShuiHouStr,out shuihou);
                                ShaoFa = QuanBuNum - FHNum;
                                decimal FHBL = FHNum / QuanBuNum;//发货比例
                                decimal FHPrice = FHBL * shoping.Price;//发货数量价格
                                TuiHuanPrice = Math.Round(shoping.Price - FHPrice, 2);
                                shoping.Price = FHPrice;
                                string payId = shoping.ID;

                                //查找收货支付编号
                                BatchPayInfo payInfo = new BssBatchPayInfo().GetModelByTypeAndObjectIdAndState(BssBatchPayInfo.Type.收货支付.ToString(), shoping.ID, BssBatchPayInfo.State.支付成功.ToString());
                                if (payInfo != null)
                                {
                                    payId = payInfo.ID;
                                }

                                Weike.EShop.MoneyHistory buyerSXF = BssMoneyHistory.GetModelByUIDAndOrdIdAndOperaType(shoping.SellerId.Value, payId, Weike.EShop.BssMoneyHistory.HistoryType.手续费.ToString());
                                decimal buyersxf = buyerSXF == null ? 0.00M : buyerSXF.SumMoney;
                                ShiShouSXF = Math.Round(FHBL * buyersxf, 2);
                                return Json(new { HanShui = HanShui, ShuiHouStr = ShuiHouStr, ShaoFa = ShaoFa, TuiHuanPrice = TuiHuanPrice, ShiShouSXF = ShiShouSXF }, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单部分发货，计算显示内容出错：", ex, this.GetType().FullName, "OrderBFFHShowByFhnum");
            }
            return Content("");
        }
        [Role("收货商品列表", IsAuthorize = true)]
        public ActionResult NeedReceiveList(int? Page, string GameId, string GameOtherId, string userName)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(GameId))
            {
                bool isLike = true;
                string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);
                if (!string.IsNullOrEmpty(gameGUID))
                {
                    if (isLike)
                        where += string.Format(" and gameguid like '{0}%'", gameGUID);
                    else
                        where += string.Format(" and gameguid='{0}'", gameGUID);
                }
            }
            if (!string.IsNullOrEmpty(userName))
            {
                where += string.Format(" and exists(select 1 from Members where Members.M_ID = NeedReceive.M_ID and Members.M_Name like '{0}%')", userName);
            }
            DataPages<Weike.EShop.NeedReceive> sList = new BssNeedReceive().GetPageRecord(where, "ID", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(sList);
        }
        #endregion


        #region 自动铺货
        [Role("自动铺货列表", IsAuthorize = true)]
        public ActionResult AutoSaleList(int? Page, string GameId, string GameOtherId,string ShopTypeId, string userName)
        {
            string where = " 1=1 ";
            string guid = "";
            GameInfoModel infoModel = null;
            if (!string.IsNullOrEmpty(GameId))
            {
                infoModel = new BLLGame().GetGameInfoModel(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, "", false);
                if (infoModel != null && infoModel.GameModel != null)
                {
                    guid = infoModel.GameModel.GameIdentify;
                    if (infoModel.GameOtherList != null && infoModel.GameOtherList.Count > 0)
                    {
                        foreach (GameOther item in infoModel.GameOtherList)
                        {
                            guid += "|" + item.GameIdentify;
                        }
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(guid))
            {
                where += string.Format(" and gameguid like '{0}%'", guid);
            }
            if (!string.IsNullOrWhiteSpace(ShopTypeId))
            {
                where += string.Format(" and ShopType='{0}'",ShopTypeId);
            }
            if (!string.IsNullOrEmpty(userName))
            {
                where += string.Format(" and M_ID=(select M_ID from members where m_name='{0}')", userName);
            }
            DataPages<Weike.EShop.ShopAutoSale> sList = new BssShopAutoSale().GetPageRecord(where, "ID", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(sList);
        }
        [Role("自动铺货更新状态", IsAuthorize = true)]
        public ActionResult AutoSaleUpdateState(int msId)
        {
            try
            {
                BssShopAutoSale mBssShopAutoSale = new BssShopAutoSale();
                ShopAutoSale s = mBssShopAutoSale.GetModel(msId);
                if (s != null)
                {
                    s.IsSale = !s.IsSale;
                    s.EditTime = DateTime.Now;
                    mBssShopAutoSale.Update(s);
                }
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
            catch (Exception) { }
            return Content("");
        }
        #endregion


        [Role("获取网站资金统计信息", IsAuthorize = true)]
        public ActionResult WebTotalMoneyInfo()
        {
            try
            {
                BssMembers bssMembers = new BssMembers();
                BssMoneyLock bssML = new BssMoneyLock();
                BssShopping bssShopping = new BssShopping();
                BssShop bssShop = new BssShop();

                //会员账户总资金
                string totalMemberMoney = BssMembers.GetAllCountByMoney("");
                ViewData["totalMemberMoney"] = totalMemberMoney;

                //会员真实姓名以“嘟嘟”开头的总资金
                string ddMembersMoney = BssMembers.GetAllCountByMoney(string.Format(" M_RealName like '{0}%'", "嘟嘟"));
                ViewData["ddMembersMoney"] = ddMembersMoney;

                //会员真实姓名以“DD373推广”开头的总资金
                string tgMembersMoney = BssMembers.GetAllCountByMoney(string.Format(" M_RealName like '{0}%'", "DD373推广"));
                ViewData["tgMembersMoney"] = tgMembersMoney;

                //提现锁定资金
                string txLockMoney = BssMoneyLock.GetMembersLockMoney(string.Format(" LType='{0}'", BssMoneyLock.MoneyLockType.提现申请.ToString()));
                ViewData["txLockMoney"] = txLockMoney;

                //安全保障锁定资金
                string aqbzLockMoney = BssMoneyLock.GetMembersLockMoney(string.Format(" LType='{0}'", BssMoneyLock.MoneyLockType.交易审核.ToString()));
                ViewData["aqbzLockMoney"] = aqbzLockMoney;

                //vip会员押金
                string vipLockMoney = "0.00";
                ViewData["vipLockMoney"] = vipLockMoney;

                //商家认证押金
                string mallLockMoney = BssMoneyLock.GetMembersLockMoney(string.Format(" LType='{0}'", BssMoneyLock.MoneyLockType.商家认证.ToString()));
                ViewData["mallLockMoney"] = mallLockMoney;

                //客服手动锁定资金
                string kfLockMoney = BssMoneyLock.GetMembersLockMoney(string.Format(" LType='{0}'", BssMoneyLock.MoneyLockType.锁定资金.ToString()));
                ViewData["kfLockMoney"] = kfLockMoney;

                //非交易完成和交易取消的订单金额
                string spTotalMoney = bssShopping.GetToatlPrice(string.Format(" State not in('{0}','{1}','{2}','{3}')", BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.部分完成.ToString(), BssShopping.ShoppingState.等待支付.ToString()));
                ViewData["spTotalMoney"] = spTotalMoney;

                //非交易完成和交易取消的过户金额
                string spGhMoney = bssShopping.GetToatlINPrice(string.Format(" State not in('{0}','{1}','{2}','{3}') and ObjectType in('{4}','{5}','{6}','{7}')", BssShopping.ShoppingState.交易成功.ToString(), BssShopping.ShoppingState.部分完成.ToString(), BssShopping.ShoppingState.交易取消.ToString(), BssShopping.ShoppingState.等待支付.ToString(), BssShopping.ShoppingType.出售交易.ToString(), BssShopping.ShoppingType.降价交易.ToString(), BssShopping.ShoppingType.求购交易.ToString(), BssShopping.ShoppingType.代收交易.ToString()));
                ViewData["spGhMoney"] = spGhMoney;

                //审核成功的求购总资金
                string ndMoney = BssNeedDeal.GetNeedDealMoney(string.Format(" NeedState='{0}' and NeedCount>0", BssNeedDeal.NeedState.审核成功.ToString()));
                ViewData["ndMoney"] = ndMoney;

                //审核成功的无货赔付押金
                string cashMoney = BssShop.GetShopCashMoney(string.Format(" ShopState in ('{0}','{1}','{2}') and PublicCount>0 and exists(select 1 from ShopOtherInfo where ShopOtherInfo.ShopNo=ShopId and ConfigId={1})", BssShop.ShopState.审核成功.ToString(), BssShop.ShopState.卖家隐身.ToString(), BssShop.ShopState.非交易时间.ToString(), (int)BssShopOtherInfo.InfoType.押金商品));
                ViewData["cashMoney"] = cashMoney;


            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取网站资金统计信息", ex, this.GetType().FullName, "WebTotalMoneyInfo");
            }
            return View();
        }

        [Role("管理员访问记录", IsAuthorize = true)]
        public ActionResult AdminPostUrlList(int? Page, string UrlName, string UrlAction, string AdminId, string StartTime, string EntTime)
        {
            DataPages<Weike.EShop.AdminPostUrl> LSa = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(UrlName))
                {
                    where.Append(string.Format(" and UrlName='{0}'", UrlName));
                }
                if (!string.IsNullOrEmpty(UrlAction))
                {
                    where.Append(string.Format(" and UrlAction='{0}'", UrlAction));
                }
                if (!string.IsNullOrEmpty(AdminId))
                {
                    where.Append(string.Format(" and AdminId={0}", AdminId));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format("and CreateTime between '{0}' and '{1}'", STime, ETime));

                LSa = new BssAdminPostUrl().GetPageRecord(where.ToString(), "CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("管理员访问记录出错", ex, this.GetType().FullName, "AdminPostUrlList");
            }

            return View(LSa);

        }


        [Role("订单商品数据报表", IsAuthorize = true)]
        public ActionResult RecordReport(string GameId, string dealType, string ShopState, string OrderState, string StartTime, string EntTime, string timeType)
        {
            try
            {
                Game gameModel = new BssGame().GetModel(GameId, false);
                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
                EntTime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.ToString() : EntTime;

                DataSet dsShop = null;
                DataSet dsShopping = null;
                DataSet dsMembers = null;
                if (timeType == "day")
                {
                    dsShop = new BssShop().GetShopOneDayReportData(gameModel == null ? "" : gameModel.GameIdentify, dealType, ShopState, StartTime, EntTime);
                    dsShopping = new BssShopping().GetShoppingOneDayReportData(gameModel == null ? "" : gameModel.GameIdentify, dealType, OrderState, StartTime, EntTime);
                    dsMembers = new BssMembers().GetRegMembersOneDayReportData(StartTime, EntTime);
                }
                else
                {
                    dsShop = new BssShop().GetShopOneHourReportData(gameModel == null ? "" : gameModel.GameIdentify, dealType, ShopState, StartTime, EntTime);
                    dsShopping = new BssShopping().GetShoppingOneHourReportData(gameModel == null ? "" : gameModel.GameIdentify, dealType, OrderState, StartTime, EntTime);
                    dsMembers = new BssMembers().GetRegMembersOneHourReportData(StartTime, EntTime);
                }

                string shoprecords = "";
                string shoppingRecotds = "";
                string membersRecotds = "";
                string dateStr = "";
                Dictionary<string, string> recShopDic = new Dictionary<string, string>();
                Dictionary<string, string> recShoppingDic = new Dictionary<string, string>();
                Dictionary<string, string> recMembersDic = new Dictionary<string, string>();
                //遍历填充有商品的日期
                if (dsShop != null && dsShop.Tables.Count > 0 && dsShop.Tables[0] != null)
                {
                    foreach (DataRow dr in dsShop.Tables[0].Rows)
                    {
                        if (timeType == "day")
                        {
                            if (!recShopDic.ContainsKey(dr[0].ToString()))
                                recShopDic.Add(dr[0].ToString(), dr[1].ToString());
                        }
                        else
                        {
                            if (!recShopDic.ContainsKey(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0')))
                                recShopDic.Add(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0'), dr[2].ToString());
                        }
                    }
                }
                //遍历填充有订单的日期
                if (dsShopping != null && dsShopping.Tables.Count > 0 && dsShopping.Tables[0] != null)
                {
                    foreach (DataRow dr in dsShopping.Tables[0].Rows)
                    {
                        if (timeType == "day")
                        {
                            if (!recShoppingDic.ContainsKey(dr[0].ToString()))
                                recShoppingDic.Add(dr[0].ToString(), dr[1].ToString());
                        }
                        else
                        {
                            if (!recShoppingDic.ContainsKey(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0')))
                                recShoppingDic.Add(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0'), dr[2].ToString());
                        }
                    }
                }
                //遍历填充有注册会员的日期
                if (dsMembers != null && dsMembers.Tables.Count > 0 && dsMembers.Tables[0] != null)
                {
                    foreach (DataRow dr in dsMembers.Tables[0].Rows)
                    {
                        if (timeType == "day")
                        {
                            if (!recMembersDic.ContainsKey(dr[0].ToString()))
                                recMembersDic.Add(dr[0].ToString(), dr[1].ToString());
                        }
                        else
                        {
                            if (!recMembersDic.ContainsKey(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0')))
                                recMembersDic.Add(dr[0].ToString() + " " + dr[1].ToString().PadLeft(2, '0'), dr[2].ToString());
                        }
                    }
                }
                for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                {
                    if (timeType == "day")
                    {
                        //遍历填充指定日期每天商品的个数
                        if (recShopDic.ContainsKey(currTime.ToString("yyyy-MM-dd")))
                            shoprecords = shoprecords + recShopDic[currTime.ToString("yyyy-MM-dd")] + ",";
                        else
                            shoprecords = shoprecords + "0,";
                        //遍历填充指定日期每天订单的个数
                        if (recShoppingDic.ContainsKey(currTime.ToString("yyyy-MM-dd")))
                            shoppingRecotds = shoppingRecotds + recShoppingDic[currTime.ToString("yyyy-MM-dd")] + ",";
                        else
                            shoppingRecotds = shoppingRecotds + "0,";
                        //遍历填充指定日期每天注册会员的个数
                        if (recMembersDic.ContainsKey(currTime.ToString("yyyy-MM-dd")))
                            membersRecotds = membersRecotds + recMembersDic[currTime.ToString("yyyy-MM-dd")] + ",";
                        else
                            membersRecotds = membersRecotds + "0,";
                        dateStr = dateStr + "'" + currTime.ToString("MM-dd") + "',";
                        currTime = currTime.AddDays(1);
                    }
                    else
                    {
                        //遍历填充指定日期每个小时商品的个数
                        if (recShopDic.ContainsKey(currTime.ToString("yyyy-MM-dd HH")))
                            shoprecords = shoprecords + recShopDic[currTime.ToString("yyyy-MM-dd HH")] + ",";
                        else
                            shoprecords = shoprecords + "0,";
                        //遍历填充指定日期每个小时订单的个数
                        if (recShoppingDic.ContainsKey(currTime.ToString("yyyy-MM-dd HH")))
                            shoppingRecotds = shoppingRecotds + recShoppingDic[currTime.ToString("yyyy-MM-dd HH")] + ",";
                        else
                            shoppingRecotds = shoppingRecotds + "0,";
                        //遍历填充指定日期每个小时注册会员的个数
                        if (recMembersDic.ContainsKey(currTime.ToString("yyyy-MM-dd HH")))
                            membersRecotds = membersRecotds + recMembersDic[currTime.ToString("yyyy-MM-dd HH")] + ",";
                        else
                            membersRecotds = membersRecotds + "0,";
                        dateStr = dateStr + "'" + currTime.ToString("MM-dd HH") + "',";
                        currTime = currTime.AddHours(1);
                    }
                }

                ViewData["ShopYRecord"] = shoprecords.Trim(',');
                ViewData["ShoppingYRecord"] = shoppingRecotds.Trim(',');
                ViewData["MembersYRecord"] = membersRecotds.Trim(',');
                ViewData["DateXRecord"] = dateStr.Trim(',');
                ViewData["RecordDesc"] = (gameModel == null ? "" : "游戏:" + gameModel.GameName + ",") + "时间范围:" + StartTime + "-" + EntTime;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单商品数据报表出错", ex, this.GetType().FullName, "RecordReport");
            }

            return View();

        }


        [Role("订单数据报表", IsAuthorize = true)]
        public ActionResult ShoppingReport(string GameId, string dealType, string OrderState, string StartTime, string EntTime)
        {
            try
            {
                Game gameModel = new BssGame().GetModel(GameId, false);
                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
                EntTime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.ToString() : EntTime;

                string shoppingNo = "";
                string shoppingPrice = "";
                string shoppingSxf = "";
                string dateStr = "";

                BssShoppingTotalList bssSpt = new BssShoppingTotalList();
                List<ShoppingTotalList> sptList = null;
                string where = " 1=1 ";
                if (!string.IsNullOrEmpty(GameId))
                    where += " and GameID='" + GameId + "'";
                if (!string.IsNullOrEmpty(dealType))
                    where += " and DealType=" + dealType + "";
                string swhere = "";
                for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                {
                    swhere = where;
                    swhere += " and DataDate='" + currTime.ToString("yyyy-MM-dd") + "'";
                    sptList = bssSpt.GetModelList(swhere);
                    if (sptList != null && sptList.Count > 0)
                    {
                        if (string.IsNullOrEmpty(OrderState))
                        {
                            shoppingNo = shoppingNo + sptList.Sum(spt => spt.SpTotalNo) + ",";
                            shoppingPrice = shoppingPrice + sptList.Sum(spt => spt.SpTotalMoney) + ",";
                            shoppingSxf = shoppingSxf + sptList.Sum(spt => spt.SpSxfTotalMoney) + ",";
                        }
                        else if (OrderState == "0")
                        {
                            shoppingNo = shoppingNo + sptList.Sum(spt => spt.SpFailedTotalNo) + ",";
                            shoppingPrice = shoppingPrice + sptList.Sum(spt => spt.SpFailedTotalMoney) + ",";
                            shoppingSxf = shoppingSxf + "0" + ",";
                        }
                        else if (OrderState == "1")
                        {
                            shoppingNo = shoppingNo + sptList.Sum(spt => spt.SpSuccessTotalNo) + ",";
                            shoppingPrice = shoppingPrice + sptList.Sum(spt => spt.SpSuccessTotalMoney) + ",";
                            shoppingSxf = shoppingSxf + sptList.Sum(spt => spt.SpSxfTotalMoney) + ",";
                        }
                        else
                        {
                            shoppingNo = shoppingNo + "0" + ",";
                            shoppingPrice = shoppingPrice + "0" + ",";
                            shoppingSxf = shoppingSxf + "0" + ",";
                        }
                    }
                    else
                    {
                        shoppingNo = shoppingNo + "0" + ",";
                        shoppingPrice = shoppingPrice + "0" + ",";
                        shoppingSxf = shoppingSxf + "0" + ",";
                    }

                    dateStr = dateStr + "'" + currTime.ToString("MM-dd") + "',";
                    currTime = currTime.AddDays(1);
                }

                ViewData["ShoppingNo"] = shoppingNo.Trim(',');
                ViewData["ShoppingPrice"] = shoppingPrice.Trim(',');
                ViewData["ShoppingSxf"] = shoppingSxf.Trim(',');

                ViewData["DateXRecord"] = dateStr.Trim(',');
                ViewData["RecordDesc"] = (gameModel == null ? "" : "游戏:" + gameModel.GameName + ",") + "时间范围:" + StartTime + "-" + EntTime;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单数据报表出错", ex, this.GetType().FullName, "ShoppingReport");
            }

            return View();

        }

        [Role("订单数据报表（前十游戏）", IsAuthorize = true)]
        public ActionResult ShoppingRecord(string RecordType, string dealType, string OrderState, string StartTime, string EntTime)
        {
            try
            {
                StringBuilder recordSb = new StringBuilder();
                StringBuilder yAxisSb = new StringBuilder();
                string yPerInt = "元";
                if (RecordType == "SNo")
                {
                    yPerInt = "个";
                }

                //查询热门前10个游戏
                List<Game> gameList = new BssGame().GetTopModelList(10, "IsEnabled=1 and IsHot=1", "OrderNo desc", false);

                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-5).ToShortDateString() : StartTime;
                EntTime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.ToString() : EntTime;

                BssShoppingTotalList bssSpt = new BssShoppingTotalList();
                List<ShoppingTotalList> sptList = null;

                string where = " 1=1 ";
                if (!string.IsNullOrEmpty(dealType))
                    where += " and DealType=" + dealType + "";
                //遍历每一个游戏输出
                for (int i = 0; i < gameList.Count; i++)
                {
                    string gameWhere = where;
                    string shoppingRecord = "";
                    gameWhere += " and  GameID='" + gameList[i].ID + "'";
                    for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                    {
                        string swhere = gameWhere;
                        swhere += " and DataDate='" + currTime.ToString("yyyy-MM-dd") + "'";
                        sptList = bssSpt.GetModelList(swhere);
                        if (sptList != null && sptList.Count > 0)
                        {
                            if (string.IsNullOrEmpty(OrderState))
                            {
                                if (RecordType == "SMoney")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpTotalMoney) + ",";
                                }
                                else if (RecordType == "SNo")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpTotalNo) + ",";
                                }
                                else
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpSxfTotalMoney) + ",";
                                }
                            }
                            else if (OrderState == "0")
                            {
                                if (RecordType == "SMoney")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpFailedTotalMoney) + ",";
                                }
                                else if (RecordType == "SNo")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpFailedTotalNo) + ",";
                                }
                                else
                                {
                                    shoppingRecord = shoppingRecord + "0" + ",";
                                }
                            }
                            else if (OrderState == "1")
                            {
                                if (RecordType == "SMoney")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpSuccessTotalMoney) + ",";
                                }
                                else if (RecordType == "SNo")
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpSuccessTotalNo) + ",";
                                }
                                else
                                {
                                    shoppingRecord = shoppingRecord + sptList.Sum(spt => spt.SpSxfTotalMoney) + ",";
                                }
                            }
                            else
                            {
                                shoppingRecord = shoppingRecord + "0" + ",";
                            }
                        }
                        else
                        {
                            shoppingRecord = shoppingRecord + "0" + ",";
                        }
                        currTime = currTime.AddDays(1);
                    }
                    shoppingRecord = shoppingRecord.Trim(',');
                    //将查询到的游戏放入集合
                    if (!string.IsNullOrEmpty(shoppingRecord))
                    {
                        recordSb.Append("{");
                        recordSb.Append("   name: '" + gameList[i].GameName + "',");
                        recordSb.Append("   color: '" + GetRecordColor(i + 1) + "',");
                        recordSb.Append("   type: 'spline',");
                        recordSb.Append("   yAxis: 1,");
                        recordSb.Append("   data: [" + shoppingRecord + "],");
                        recordSb.Append("   tooltip: {");
                        recordSb.Append("       valueSuffix: ' " + yPerInt + "'");
                        recordSb.Append("   }");
                        recordSb.Append("},");

                        //Y轴显示
                        yAxisSb.Append("{ ");
                        yAxisSb.Append("   labels: {");
                        yAxisSb.Append("       formatter: function () {");
                        yAxisSb.Append("           return this.value + ' " + yPerInt + "';");
                        yAxisSb.Append("       },");
                        yAxisSb.Append("       style: {");
                        yAxisSb.Append("           color: '" + GetRecordColor(i + 1) + "'");
                        yAxisSb.Append("       }");
                        yAxisSb.Append("   },");
                        yAxisSb.Append("   title: {");
                        yAxisSb.Append("       text: '" + gameList[i].GameName + "',");
                        yAxisSb.Append("       style: {");
                        yAxisSb.Append("           color: '" + GetRecordColor(i + 1) + "'");
                        yAxisSb.Append("       }");
                        yAxisSb.Append("   },");
                        yAxisSb.Append("   opposite: true");
                        yAxisSb.Append("},");
                    }
                }

                string dateStr = "";
                for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                {
                    dateStr = dateStr + "'" + currTime.ToString("MM-dd") + "',";
                    currTime = currTime.AddDays(1);
                }

                ViewData["RecordList"] = recordSb.ToString().Trim(',');
                ViewData["yAxisSb"] = yAxisSb.ToString().Trim(',');

                ViewData["DateXRecord"] = dateStr.Trim(',');
                ViewData["RecordDesc"] = "时间范围:" + StartTime + "-" + EntTime;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单数据报表出错", ex, this.GetType().FullName, "ShoppingReport");
            }

            return View();

        }


        [Role("订单发货时间报表", IsAuthorize = true)]
        public ActionResult ShoppingFhTimeReport(string GameId, string dealType, string OrderState, string StartTime, string EntTime, string timeType)
        {
            try
            {
                Game gameModel = new BssGame().GetModel(GameId, false);
                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-2).ToString() : StartTime;
                EntTime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.ToString() : EntTime;

                string shoppingNo = "";
                string shoppingFhTime = "";
                int tfhTime = 0;
                int tfhNo = 0;
                string dateStr = "";

                BssShoppingFhTimeList bssSftl = new BssShoppingFhTimeList();
                List<ShoppingFhTimeList> sftlList = null;
                string where = " 1=1 ";
                if (!string.IsNullOrEmpty(GameId))
                    where += " and GameID='" + GameId + "'";
                if (!string.IsNullOrEmpty(dealType))
                    where += " and DealType=" + dealType + "";
                string swhere = "";
                for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                {
                    swhere = where;
                    if (timeType == "day")
                    {
                        swhere += " and DateDate='" + currTime.ToString("yyyy-MM-dd") + "'";
                    }
                    else
                    {
                        swhere += " and DateDate='" + currTime.ToString("yyyy-MM-dd") + "' and DateHour='" + currTime.ToString("HH") + "'";
                    }
                    sftlList = bssSftl.GetModelList(swhere);
                    if (sftlList != null && sftlList.Count > 0)
                    {
                        if (string.IsNullOrEmpty(OrderState))
                        {
                            shoppingNo = shoppingNo + sftlList.Sum(spt => spt.SpTotalNo) + ",";
                        }
                        else if (OrderState == "0")
                        {
                            shoppingNo = shoppingNo + sftlList.Sum(spt => spt.SpFailedTotalNo) + ",";
                        }
                        else if (OrderState == "1")
                        {
                            shoppingNo = shoppingNo + sftlList.Sum(spt => spt.SpSuccessTotalNo) + ",";
                        }
                        else
                        {
                            shoppingNo = shoppingNo + "0" + ",";
                        }

                        //计算平均发货时间
                        tfhTime = 0;
                        tfhNo = 0;
                        foreach (ShoppingFhTimeList sft in sftlList)
                        {
                            tfhTime = tfhTime + sft.AvgTime * sft.SpSuccessTotalNo;
                            tfhNo = tfhNo + sft.SpSuccessTotalNo;
                        }
                        if (tfhNo > 0)
                        {
                            shoppingFhTime = shoppingFhTime + (tfhTime / tfhNo).ToString() + ",";
                        }
                        else
                        {
                            shoppingFhTime = shoppingFhTime + "0" + ",";
                        }
                    }
                    else
                    {
                        shoppingNo = shoppingNo + "0" + ",";
                        shoppingFhTime = shoppingFhTime + "0" + ",";
                    }

                    if (timeType == "day")
                    {
                        dateStr = dateStr + "'" + currTime.ToString("MM-dd") + "',";
                        currTime = currTime.AddDays(1);
                    }
                    else
                    {
                        if (currTime.Hour == 0)
                        {
                            dateStr = dateStr + "'" + currTime.ToString("MM-dd HH") + "',";
                        }
                        else
                        {
                            dateStr = dateStr + "'" + currTime.ToString("HH") + "',";
                        }
                        currTime = currTime.AddHours(1);
                    }
                }

                ViewData["ShoppingNo"] = shoppingNo.Trim(',');
                ViewData["ShoppingFhTime"] = shoppingFhTime.Trim(',');

                ViewData["DateXRecord"] = dateStr.Trim(',');
                ViewData["RecordDesc"] = (gameModel == null ? "" : "游戏:" + gameModel.GameName + ",") + "时间范围:" + StartTime + "-" + EntTime;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单数据报表出错", ex, this.GetType().FullName, "ShoppingReport");
            }

            return View();

        }

        [Role("订单发货时间报表（前十游戏）", IsAuthorize = true)]
        public ActionResult ShoppingFhTimeRecord(string RecordType, string dealType, string OrderState, string StartTime, string EntTime, string timeType)
        {
            try
            {
                StartTime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-2).ToString() : StartTime;
                EntTime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.ToString() : EntTime;

                StringBuilder recordSb = new StringBuilder();
                StringBuilder yAxisSb = new StringBuilder();

                int tfhTime = 0;
                int tfhNo = 0;
                string dateStr = "";

                string yPerInt = "秒";
                if (RecordType == "SNo")
                {
                    yPerInt = "个";
                }

                //查询热门前10个游戏
                List<Game> gameList = new BssGame().GetTopModelList(10, "IsHot=1 and IsEnabled=1", "OrderNo DESC", false);

                BssShoppingFhTimeList bssSftl = new BssShoppingFhTimeList();
                List<ShoppingFhTimeList> sftlList = null;
                string where = " 1=1 ";
                if (!string.IsNullOrEmpty(dealType))
                    where += " and DealType=" + dealType + "";
                //遍历每一个游戏输出
                for (int i = 0; i < gameList.Count; i++)
                {
                    string gameWhere = where;
                    string shoppingRecord = "";
                    gameWhere += " and  GameID='" + gameList[i].ID + "'";
                    for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                    {
                        string swhere = gameWhere;
                        if (timeType == "day")
                        {
                            swhere += " and DateDate='" + currTime.ToString("yyyy-MM-dd") + "'";
                        }
                        else
                        {
                            swhere += " and DateDate='" + currTime.ToString("yyyy-MM-dd") + "' and DateHour='" + currTime.ToString("HH") + "'";
                        }
                        sftlList = bssSftl.GetModelList(swhere);
                        if (sftlList != null && sftlList.Count > 0)
                        {
                            if (RecordType == "SNo")
                            {
                                if (string.IsNullOrEmpty(OrderState))
                                {
                                    shoppingRecord = shoppingRecord + sftlList.Sum(spt => spt.SpTotalNo) + ",";
                                }
                                else if (OrderState == "0")
                                {
                                    shoppingRecord = shoppingRecord + sftlList.Sum(spt => spt.SpFailedTotalNo) + ",";
                                }
                                else if (OrderState == "1")
                                {
                                    shoppingRecord = shoppingRecord + sftlList.Sum(spt => spt.SpSuccessTotalNo) + ",";
                                }
                                else
                                {
                                    shoppingRecord = shoppingRecord + "0" + ",";
                                }
                            }
                            else
                            {
                                //计算平均发货时间
                                tfhTime = 0;
                                tfhNo = 0;
                                foreach (ShoppingFhTimeList sft in sftlList)
                                {
                                    tfhTime = tfhTime + sft.AvgTime * sft.SpSuccessTotalNo;
                                    tfhNo = tfhNo + sft.SpSuccessTotalNo;
                                }
                                if (tfhNo > 0)
                                {
                                    shoppingRecord = shoppingRecord + (tfhTime / tfhNo).ToString() + ",";
                                }
                                else
                                {
                                    shoppingRecord = shoppingRecord + "0" + ",";
                                }
                            }
                        }
                        else
                        {
                            if (RecordType == "SNo")
                            {
                                shoppingRecord = shoppingRecord + "0" + ",";
                            }
                            else
                            {
                                shoppingRecord = shoppingRecord + "0" + ",";
                            }
                        }

                        if (timeType == "day")
                        {
                            currTime = currTime.AddDays(1);
                        }
                        else
                        {
                            currTime = currTime.AddHours(1);
                        }
                    }

                    shoppingRecord = shoppingRecord.Trim(',');
                    //将查询到的游戏放入集合
                    if (!string.IsNullOrEmpty(shoppingRecord))
                    {
                        recordSb.Append("{");
                        recordSb.Append("   name: '" + gameList[i].GameName + "',");
                        recordSb.Append("   color: '" + GetRecordColor(i + 1) + "',");
                        recordSb.Append("   type: 'spline',");
                        recordSb.Append("   yAxis: 1,");
                        recordSb.Append("   data: [" + shoppingRecord + "],");
                        recordSb.Append("   tooltip: {");
                        recordSb.Append("       valueSuffix: ' " + yPerInt + "'");
                        recordSb.Append("   }");
                        recordSb.Append("},");

                        //Y轴显示
                        yAxisSb.Append("{ ");
                        yAxisSb.Append("   labels: {");
                        yAxisSb.Append("       formatter: function () {");
                        yAxisSb.Append("           return this.value + ' " + yPerInt + "';");
                        yAxisSb.Append("       },");
                        yAxisSb.Append("       style: {");
                        yAxisSb.Append("           color: '" + GetRecordColor(i + 1) + "'");
                        yAxisSb.Append("       }");
                        yAxisSb.Append("   },");
                        yAxisSb.Append("   title: {");
                        yAxisSb.Append("       text: '" + gameList[i].GameName + "',");
                        yAxisSb.Append("       style: {");
                        yAxisSb.Append("           color: '" + GetRecordColor(i + 1) + "'");
                        yAxisSb.Append("       }");
                        yAxisSb.Append("   },");
                        yAxisSb.Append("   opposite: true");
                        yAxisSb.Append("},");
                    }
                }

                //生成X轴时间数据
                for (DateTime currTime = DateTime.Parse(StartTime); currTime <= DateTime.Parse(EntTime); )
                {
                    if (timeType == "day")
                    {
                        dateStr = dateStr + "'" + currTime.ToString("MM-dd") + "',";
                        currTime = currTime.AddDays(1);
                    }
                    else
                    {
                        if (currTime.Hour == 0)
                        {
                            dateStr = dateStr + "'" + currTime.ToString("MM-dd HH") + "',";
                        }
                        else
                        {
                            dateStr = dateStr + "'" + currTime.ToString("HH") + "',";
                        }
                        currTime = currTime.AddHours(1);
                    }
                }

                ViewData["RecordList"] = recordSb.ToString().Trim(',');
                ViewData["yAxisSb"] = yAxisSb.ToString().Trim(',');

                ViewData["DateXRecord"] = dateStr.Trim(',');
                ViewData["RecordDesc"] = "时间范围:" + StartTime + "-" + EntTime;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单数据报表出错", ex, this.GetType().FullName, "ShoppingReport");
            }

            return View();

        }

        public string GetRecordColor(int gNo)
        {
            string color = "#4572A7";
            switch (gNo)
            {
                case 1:
                    color = "#AD059C";
                    break;
                case 2:
                    color = "#FA61EB";
                    break;
                case 3:
                    color = "#C3EE6D";
                    break;
                case 4:
                    color = "#7AE195";
                    break;
                case 5:
                    color = "#6669F4";
                    break;
                case 6:
                    color = "#C7C6FB";
                    break;
                case 7:
                    color = "#FDD3C4";
                    break;
                case 8:
                    color = "#F64409";
                    break;
                case 9:
                    color = "#AC3006";
                    break;
                case 10:
                    color = "#F7ED26";
                    break;
                default:
                    break;
            }
            return color;
        }


        [Role("订单银行支付记录", IsAuthorize = true)]
        public ActionResult ShoppingPayOrderList(int? Page, string OrderId, string PayOrderId, string StartTime, string EntTime, string OrderState, string BankType)
        {
            DataPages<Weike.EShop.ShoppingPayOrder> LPo = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(OrderId))
                {
                    where.Append(string.Format(" and OrderId in(select BatchPayInfo from PayOrderMoney where OrderId='{0}') ", OrderId));
                }
                if (!string.IsNullOrEmpty(PayOrderId))
                {
                    where.Append(string.Format(" and PayOrderId='{0}' ", PayOrderId));
                }
                if (!string.IsNullOrEmpty(OrderState))
                {
                    where.Append(string.Format(" and OrderState='{0}' ", OrderState));
                }
                if (!string.IsNullOrEmpty(BankType))
                {
                    where.Append(string.Format(" and BankType='{0}' ", BankType));
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
                where.Append(string.Format(" and CreateTime between '{0}' and '{1}' ", STime, ETime));

                LPo = new BssShoppingPayOrder().GetPageRecord(where.ToString(), "CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单银行支付记录出错", ex, this.GetType().FullName, "ShoppingPayOrderList");
            }

            return View(LPo);

        }

        [Role("修改订单充值支付状态", IsAuthorize = true)]
        public ActionResult UpdatePayOrderState(string orderId, string state)
        {
            try
            {
                BssShoppingPayOrder bssSpPayOrder = new BssShoppingPayOrder();
                ShoppingPayOrder payOrderMode = bssSpPayOrder.GetModel(orderId);
                if (payOrderMode != null && payOrderMode.OrderState == BssShoppingPayOrder.OrderState.处理中.ToString())
                {
                    if (payOrderMode.OrderType == BssShoppingPayOrder.OrderType.充值订单.ToString())
                    {
                        MoneyHistory mhModel = BssMoneyHistory.GetModel(payOrderMode.OrderId);
                        if (mhModel != null)
                        {
                            BssModifyMembersRecording.AddCaoZuoRecording(mhModel.ID, Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo().A_ID, (int)BssModifyMembersRecording.ECate.修改订单状态, payOrderMode.OrderId, string.Format("充值订单编号{0},状态改为{1}", orderId, state));

                            if (state == BssShoppingPayOrder.OrderState.成功.ToString())
                            {
                                Weike.WebGlobalMethod.BLLPayOrderMethod.PassMoney(orderId, payOrderMode.PayBankAllMoney, BssShoppingPayOrder.OrderState.成功);
                            }
                            else
                            {
                                Weike.WebGlobalMethod.BLLPayOrderMethod.PassMoney(orderId, 0, BssShoppingPayOrder.OrderState.失败);
                            }
                        }
                    }
                    else
                    {
                        PayOrderMoney payMoneyModel = new BssPayOrderMoney().GetModel(payOrderMode.OrderId);
                        if (payMoneyModel != null && payMoneyModel.State == BssPayOrderMoney.State.等待支付.ToString())
                        {
                            BssModifyMembersRecording.AddCaoZuoRecording(payMoneyModel.M_ID, Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo().A_ID, (int)BssModifyMembersRecording.ECate.修改订单状态, payMoneyModel.OrderId, string.Format("订单编号{0},状态改为{1}", orderId, state));

                            BLLPayMoneyMethod.PayOrderChargeNotify(orderId, "", payOrderMode.PayBankAllMoney, state == BssShoppingPayOrder.OrderState.成功.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("修改订单支付状态失败", ex, this.GetType().FullName, "UpdatePayOrderState");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        #region 会员商城游戏配置列表
        [Role("会员商城游戏配置列表", IsAuthorize = true)]
        public ActionResult MembersMallGameConfigList(int? page, string GameId)
        {
            string where =string.Format( " ConfigType={0}",(int)BssMembersMallGameConfig.GonfigType.会员商城游戏配置);
            if (!string.IsNullOrEmpty(GameId))
            {
                where += string.Format(" and GameID = '{0}' ", GameId);
            }
            where += " and exists( select 1 from Game where GameId=Game.ID  )";
            DataPages<Weike.EShop.MembersMallGameConfig> AcList = new BssMembersMallGameConfig().GetPageRecord(where, "CreateTime", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(AcList);
        }
        #endregion


        #region 商城收货配置
        [Role("会员商城收货配置列表", IsAuthorize = true)]
        public ActionResult ReceiveGoodsConfig(int? page, string GameId, string GameShopTypeId)
        {
            string where = string.Format(" ConfigType={0}", (int)BssMembersMallGameConfig.GonfigType.商城收货配置);
            if (!string.IsNullOrWhiteSpace(GameId))
            {
                where += string.Format(" and GameID = '{0}' ", GameId);
            }
            if (!string.IsNullOrWhiteSpace(GameShopTypeId))
            {
                where += string.Format(" and TypeId='{0}'", GameShopTypeId);
            }
            where +=string.Format( " and exists( select 1 from Game where GameId=Game.ID and GameType in('{0}','{1}'))",BssGame.GameType.网络游戏.ToString(),BssGame.GameType.网页游戏.ToString());
            DataPages<Weike.EShop.MembersMallGameConfig> AcList = new BssMembersMallGameConfig().GetPageRecord(where, "CreateTime", 10, page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(AcList);
        }
        #endregion

        /// <summary>
        /// 订单异常信息列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="gameId"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        [Role("订单异常信息列表", IsAuthorize = true)]
        public ActionResult OrderExceptionInfoList(int? page, string OrderId, string ExceptionType, string StartTime, string EntTime)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(OrderId))
            {
                where += string.Format(" and OrderId = '{0}'", OrderId);
            }
            if (!string.IsNullOrEmpty(ExceptionType))
            {
                where += string.Format(" and ExceptionType = {0}", ExceptionType);
            }
            string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-1).ToShortDateString() : StartTime;
            string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.MaxValue.ToShortDateString() : EntTime;
            where += string.Format(" and CreateTime between '{0}' and '{1}'", STime, ETime);
            DataPages<Weike.EShop.OrderExceptionInfo> exceptionList = new BssOrderExceptionInfo().GetPageRecord(where, "CreateTime", 20, page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(exceptionList);
        }

        #region 担保大卖家认证
        /// <summary>
        /// 订单异常信息列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="gameId"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        [Role("担保大卖家游戏配置列表", IsAuthorize = true)]
        public ActionResult MerchantsGameConfigList(int? Page, string GameId)
        {
            string where = " 1=1 ";
            if (!string.IsNullOrEmpty(GameId))
            {
                where += string.Format(" and GameId = '{0}'", GameId);
            }
            DataPages<Weike.EShop.MerchantsGameConfig> List = new BssMerchantsGameConfig().GetPageRecord(where, "CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            return View(List);
        }
        #endregion

        #region 商品列表关键词搜索
        [Role("商品列表关键词搜索记录", IsAuthorize = true)]
        public ActionResult ShopSearchKeyList(int? Page, string GameId, string SearchKey, string State)
        {
            DataPages<Weike.EShop.ShopSearchKey> LPo = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(GameId))
                {
                    where.Append(string.Format(" and GameId='{0}' ", GameId));
                }
                if (!string.IsNullOrEmpty(SearchKey))
                {
                    where.Append(string.Format(" and SearchKeyName='{0}' ", SearchKey));
                }
                if (!string.IsNullOrEmpty(State))
                {
                    where.Append(string.Format(" and State='{0}' ", State));
                }

                LPo = new BssShopSearchKey().GetPageRecord(where.ToString(), "SearchTimes desc,CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商品列表关键词搜索记录出错", ex, this.GetType().FullName, "ShopSearchKeyList");
            }

            return View(LPo);

        }

        [Role("修改商品列表关键词搜索状态", IsAuthorize = true)]
        public ActionResult UpdateShopSearchKeyState(string sid, string State)
        {
            try
            {
                BssShopSearchKey bssSkey = new BssShopSearchKey();
                ShopSearchKey sKeyModel = bssSkey.GetModel(sid.ToInt32());
                if (sKeyModel != null)
                {
                    sKeyModel.State = State;
                    sKeyModel.EditTime = DateTime.Now;
                    bssSkey.Update(sKeyModel);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("修改商品列表关键词搜索状态", ex, this.GetType().FullName, "UpdateShopSearchKeyState");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }
        #endregion

        /// <summary>
        /// 账号截图认证
        /// </summary>
        /// <returns></returns>
        [Role("账号截图认证", IsAuthorize = true)]
        public ActionResult AccountJieTuAdd(int sid)
        {
            string retMsg = "Error";
            try
            {
                Shop shopModel = new BssShop().GetModel(sid);
                if (shopModel != null && shopModel.DealType == 3)
                {
                    ShopOtherInfo soModel = new ShopOtherInfo();
                    soModel.ConfigId = (int)BssShopOtherInfo.InfoType.自动截图;
                    soModel.CreateTime = DateTime.Now;
                    soModel.ShopNo = shopModel.ShopID;
                    new BssShopOtherInfo().Add(soModel);

                    retMsg = "Success";
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("账号截图认证：", ex, this.GetType().FullName, "AccountJieTuAdd");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }

        #region 境外汇款订单

        [Role("境外汇款订单", IsAuthorize = true)]
        public ActionResult MoneyOverseaPayList(int? Page, string StartTime, string EndTime, string OrderState, string OrderId, string UserName)
        {
            #region 搜索功能

            DataPages<Weike.EShop.MoneyOverseaPay> LMop = null;

            string where = "1=1";

            if (!string.IsNullOrEmpty(StartTime))
                where = string.Format(" CreateTime >'{0}'", StartTime);
            if (!string.IsNullOrEmpty(EndTime))
                where = string.Format(" CreateTime <'{0}'", EndTime);
            if (!string.IsNullOrEmpty(OrderState))
                where += string.Format(" and OrderState='{0}'", OrderState);
            if (!string.IsNullOrEmpty(UserName))
                where += string.Format(" and M_ID=(select M_ID from members where M_Name='{0}')", UserName);
            if (!string.IsNullOrEmpty(OrderId))
                where += string.Format(" and OrderId = '{0}'", OrderId.Trim());
            try
            {
                LMop = new BssMoneyOverseaPay().GetPageRecord(where, "CreateTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("境外汇款订单", ex, this.GetType().FullName, "MoneyOverseaPayList");
            }
            #endregion

            #region 统计功能
            try
            {
                ViewData["Sum"] = new BssMoneyOverseaPay().GetSingle("select sum(AddMoney) from MoneyOverseaPay where OrderState='充值成功'");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("境外汇款订单总金额出错", ex, this.GetType().FullName, "MoneyOverseaPayList");
            }
            #endregion

            return View(LMop);
        }

        [Role("删除境外汇款订单", IsAuthorize = true)]
        public ActionResult MoneyOverseaPayDel(string orderId)
        {
            try
            {
                if (!string.IsNullOrEmpty(orderId))
                    new Weike.EShop.BssMoneyOverseaPay().Delete(orderId);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除台充值订单", ex, this.GetType().FullName, "MoneyOverseaPayDel");
            }

            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        [Role("处理境外汇款订单", IsAuthorize = true)]
        public ActionResult MoneyOverseaPayDetail(string orderId, string orderState)
        {
            Weike.EShop.MoneyOverseaPay model = null;
            Weike.EShop.BssMoneyOverseaPay bll = new Weike.EShop.BssMoneyOverseaPay();

            try
            {
                model = bll.GetModel(orderId);
                if (model == null)
                    return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());

                if (IsPost)
                {
                    if (model.OrderState == BssMoneyOverseaPay.OrderState.充值中.ToString() && (orderState == BssMoneyOverseaPay.OrderState.充值成功.ToString() || orderState == BssMoneyOverseaPay.OrderState.充值失败.ToString()))
                    {
                        bool res = new BLLMoneyHistoryMethod().UpdateMoneyOverseaPay(orderId, orderState);
                        if (res)
                        {
                            MsgHelper.Insert("megOffline", "提交成功");
                        }
                        else
                        {
                            MsgHelper.Insert("megOffline", "提交失败，请重新尝试");
                        }

                        return RedirectToAction("MoneyOverseaPayDetail", new { orderId = orderId });
                    }
                    else
                    {
                        MsgHelper.Insert("megOffline", "您在干什么呢？");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("处理境外汇款订单", ex, this.GetType().FullName, "MoneyOverseaPayDetail");
            }

            return View(model);
        }

        #endregion

        #region 客服业绩统计
        [Role("客服业绩详细列表", Description = "客服业绩详细列表", IsAuthorize = true)]
        public ActionResult ServicePerformanceDetailList(int? page, string GameId, string GameOtherId, string Server, int? sid, string StartTime, string EntTime, int? SLevel, int? SType)
        {
            DataPages<ServicePerformance> StList = null;
            try
            {
                string where = " 1=1";
                if (sid.HasValue)
                    where = where + " and SID=" + sid.Value;

                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);

                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            where += string.Format(" and GameGuid like '{0}%' ", gameGUID);
                        else
                            where += string.Format(" and GameGuid='{0}' ", gameGUID);
                    }
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);

                if (SLevel.HasValue)
                {
                    where += string.Format(" and Level = '{0}'", ((BssServicePerformanceConfig.LevelType)SLevel.Value).ToString());
                }
                if (SType.HasValue)
                {
                    where += string.Format(" and Type = '{0}'", ((BssServicePerformance.Type)SType.Value).ToString());
                }
                int pageSize = 20;
                int pageIndex = page ?? 1;
                StList = new BssServicePerformance().GetPageRecord(where, "ID", pageSize, pageIndex, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceDetailList");
            }

            return View(StList);
        }
        [Role("客服个人业绩详细列表", Description = "客服个人业绩详细列表", IsAuthorize = true)]
        public ActionResult ServicePerformanceDetailMyList(int? page, string GameId, string GameOtherId, string StartTime, string EntTime, int? SLevel, int? SType)
        {
            DataPages<ServicePerformance> StList = null;
            try
            {
                Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                string where = " SID=" + adminModel.A_ID;
                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);

                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            where += string.Format(" and GameGuid like '{0}%' ", gameGUID);
                        else
                            where += string.Format(" and GameGuid='{0}' ", gameGUID);
                    }
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);

                if (SLevel.HasValue)
                {
                    where += string.Format(" and Level = '{0}'", ((BssServicePerformanceConfig.LevelType)SLevel.Value).ToString());
                }
                if (SType.HasValue)
                {
                    where += string.Format(" and Type = '{0}'", ((BssServicePerformance.Type)SType.Value).ToString());
                }
                int pageSize = 20;
                int pageIndex = page ?? 1;
                StList = new BssServicePerformance().GetPageRecord(where, "ID", pageSize, pageIndex, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceDetailMyList");
            }

            return View(StList);
        }

        [Role("客服业绩列表", Description = "客服业绩列表", IsAuthorize = true)]
        public ActionResult ServicePerformanceList(int? page, string GameId, string GameOtherId, string sid, string StartTime, string EntTime, string Role, string Sort, string SpType, string GameCategoryProperty)
        {
            List<ServicePerformanceTongji> StList = null;
            try
            {
                string where = " 1=1";
                //选择了客服
                if (!string.IsNullOrEmpty(sid))
                {
                    sid = sid.Remove(sid.LastIndexOf(","), 1);
                    where = where + " and SID in(" + sid + ")";
                }
                else
                {
                    //按角色 all代表所有部门
                    if (Role != "all" && !string.IsNullOrEmpty(Role))
                    {
                        string Rsid = "";
                        string strwhere = string.Format(" R_ID='{0}' and  A_Islock=0 order by A_RealName asc", Role);
                        List<Admins> Adminlist = new BssAdmins().GetAdminsModelList(strwhere);
                        foreach (Admins admins in Adminlist)
                        {
                            Rsid += admins.A_ID + ",";
                        }
                        if (Adminlist.Count > 0)
                        {
                            Rsid = Rsid.Remove(Rsid.LastIndexOf(","), 1);
                        }
                        where = where + " and SID in(" + Rsid + ")";

                    }
                }
                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);

                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            where += string.Format(" and GameGuid like '{0}%' ", gameGUID);
                        else
                            where += string.Format(" and GameGuid='{0}' ", gameGUID);
                    }
                }
                if (!string.IsNullOrEmpty(SpType))
                {
                    where += string.Format(" and ObjectType='{0}'", SpType);
                }
                if (!string.IsNullOrEmpty(GameCategoryProperty))
                {
                    where += string.Format(" and ShopType in (select ID from EshopGame.dbo.GameShopType where Property ='{0}')", GameCategoryProperty);
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);
                int pageSize = 10;
                int pageIndex = page ?? 1;
                System.Collections.ArrayList Arrylist = new BssServicePerformance().GetPageRecordOfTj(where, pageSize, pageIndex, "GameId", Sort);
                StList = Arrylist[0] as List<ServicePerformanceTongji>;
                ViewData["General"] = Arrylist[1];
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceList");
            }

            return View(StList);
        }
        [Role("客服业绩列表导出", Description = "客服业绩列表导出", IsAuthorize = true)]
        public ActionResult ServicePerformanceListImport(string Game, string Qu, string Server, string sid, string StartTime, string EntTime, string Role, string Sort, string SpType, string GameCategoryProperty, string ImportType)
        {
            List<ServicePerformanceTongji> StList = null;
            try
            {
                string where = " 1=1";
                //选择了客服
                if (!string.IsNullOrEmpty(sid))
                {
                    sid = sid.Remove(sid.LastIndexOf(","), 1);
                    where = where + " and SID in(" + sid + ")";
                }
                else
                {
                    //按角色 all代表所有部门
                    if (Role != "all" && !string.IsNullOrEmpty(Role))
                    {
                        string Rsid = "";
                        string strwhere = string.Format(" R_ID='{0}' and  A_Islock=0 order by A_RealName asc", Role);
                        List<Admins> Adminlist = new BssAdmins().GetAdminsModelList(strwhere);
                        foreach (Admins admins in Adminlist)
                        {
                            Rsid += admins.A_ID + ",";
                        }
                        if (Adminlist.Count > 0)
                        {
                            Rsid = Rsid.Remove(Rsid.LastIndexOf(","), 1);
                        }
                        where = where + " and SID in(" + Rsid + ")";

                    }
                }
                BssGameOther bssGameOther = new BssGameOther();
                string guid = "";
                if (!string.IsNullOrEmpty(Game))
                {
                    Game tmp = new BssGame().GetModel(Game, false);
                    if (tmp != null)
                    {
                        guid += tmp.GameIdentify + "|";
                    }
                }
                if (!string.IsNullOrEmpty(Qu))
                {
                    GameOther tmp = bssGameOther.GetModel(Qu,false);
                    if (tmp != null)
                    {
                        guid += tmp.GameIdentify + "|";
                    }
                }
                if (!string.IsNullOrEmpty(Server))
                {
                    GameOther tmp = bssGameOther.GetModel(Server,false);
                    if (tmp != null)
                    {
                        guid += tmp.GameIdentify + "|";
                    }
                }
                if (!string.IsNullOrEmpty(guid))
                {
                    guid = guid.TrimEnd('|');
                    where += string.Format(" and gameguid like '{0}%'", guid);
                }
                if (!string.IsNullOrEmpty(SpType))
                {
                    where += string.Format(" and ObjectType='{0}'", SpType);
                }
                if (!string.IsNullOrEmpty(GameCategoryProperty))
                {
                    where += string.Format(" and ShopType in (select ID from EshopGame.dbo.GameShopType where Property ='{0}')", GameCategoryProperty);
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);
                string Filed = "GameId";
                if (ImportType == "new")
                {
                    Filed = "SID";
                }
                System.Collections.ArrayList Arrylist = new BssServicePerformance().GetPageRecordOfTj(where, 100, 1, Filed, Sort);
                StList = Arrylist[0] as List<ServicePerformanceTongji>;

                if (StList != null)
                {
                    HSSFWorkbook hssfworkbook = new HSSFWorkbook();
                    ISheet sheet = hssfworkbook.CreateSheet("客服业绩");
                    NPOI.HPSF.DocumentSummaryInformation dsi = NPOI.HPSF.PropertySetFactory.CreateDocumentSummaryInformation();
                    dsi.Company = "DD373 Team";
                    NPOI.HPSF.SummaryInformation si = NPOI.HPSF.PropertySetFactory.CreateSummaryInformation();
                    si.Subject = "http://www.dd373.com/";
                    hssfworkbook.DocumentSummaryInformation = dsi;
                    hssfworkbook.SummaryInformation = si;

                    IRow rowtop = sheet.CreateRow(0);

                    IFont font = hssfworkbook.CreateFont();
                    font.FontName = "宋体";
                    font.FontHeightInPoints = 11;

                    ICellStyle style = hssfworkbook.CreateCellStyle();
                    style.SetFont(font);

                    //生成标题
                    string[] tits = new string[] { "序号", ImportType != "new" ? "游戏" : "客服", "平均时间", "总单量", "成交率", "A数量/比例", "B数量/比例", "C数量/比例", "D数量/比例", "好评数", "总金额" };
                    for (int i = 0; i < tits.Length; i++)
                    {
                        sheet.SetColumnWidth(i, 18 * 200);

                        ICell cell = rowtop.CreateCell(i);
                        cell.SetCellValue(tits[i]);
                        cell.CellStyle = style;
                    }

                    for (int i = 0; i < StList.Count; i++)
                    {
                        ServicePerformanceTongji recordModel = StList[i];

                        IRow row = sheet.CreateRow(i + 1);

                        string name = "";
                        if (ImportType != "new")
                        {
                            if (!string.IsNullOrEmpty(recordModel.GameId))
                            {
                                Game gc = new BssGame().GetModel(recordModel.GameId, false);
                                if (gc != null)
                                {
                                    name = gc.GameName;
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(recordModel.SId))
                            {
                                Weike.CMS.Admins Admins = new BssAdmins().GetModel(Convert.ToInt32(recordModel.SId));
                                if (Admins != null)
                                {
                                    name = Admins.A_RealName;
                                }
                            }
                        }

                        //序号
                        ICell cell = row.CreateCell(0);
                        cell.SetCellValue(i + 1);
                        cell.CellStyle = style;

                        //游戏?客服
                        cell = row.CreateCell(1);
                        cell.SetCellValue(name);
                        cell.CellStyle = style;

                        //平均时间
                        cell = row.CreateCell(2);
                        cell.SetCellValue(string.Format("{0}分{1}秒", recordModel.FhTime / 60, recordModel.FhTime % 60));
                        cell.CellStyle = style;

                        //总单量
                        cell = row.CreateCell(3);
                        cell.SetCellValue(recordModel.Count.ToString());
                        cell.CellStyle = style;

                        //成交率
                        cell = row.CreateCell(4);
                        cell.SetCellValue((recordModel.SucRate * 100).ToString("f2") + "%");
                        cell.CellStyle = style;

                        //A数量/比例
                        cell = row.CreateCell(5);
                        cell.SetCellValue(recordModel.ACount.ToString() + "/" + (recordModel.ARate * 100).ToString("f2") + "%");
                        cell.CellStyle = style;

                        //B数量/比例
                        cell = row.CreateCell(6);
                        cell.SetCellValue(recordModel.BCount.ToString() + "/" + (recordModel.BRate * 100).ToString("f2") + "%");
                        cell.CellStyle = style;

                        //C数量/比例
                        cell = row.CreateCell(7);
                        cell.SetCellValue(recordModel.CCount.ToString() + "/" + (recordModel.CRate * 100).ToString("f2") + "%");
                        cell.CellStyle = style;

                        //D数量/比例
                        cell = row.CreateCell(8);
                        cell.SetCellValue(recordModel.DCount.ToString() + "/" + (recordModel.DRate * 100).ToString("f2") + "%");
                        cell.CellStyle = style;

                        //好评数
                        cell = row.CreateCell(9);
                        cell.SetCellValue(recordModel.PosCount);
                        cell.CellStyle = style;

                        //总金额
                        cell = row.CreateCell(10);
                        cell.SetCellValue(recordModel.TotalMoney.ToString("f2"));
                        cell.CellStyle = style;
                    }

                    string fileName = string.Format("客服业绩_{0}", Guid.NewGuid().ToString().Replace("-", ""));
                    string excelFileName = string.Format("{0}.xls", fileName);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        hssfworkbook.Write(ms);

                        FileInfo FI = new FileInfo(System.Web.HttpContext.Current.Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)));
                        if (!Directory.Exists(FI.DirectoryName))
                            Directory.CreateDirectory(FI.DirectoryName);
                        FileStream fileUpload = new FileStream(System.Web.HttpContext.Current.Server.MapPath(string.Format("~/ExcelFile/{0}", excelFileName)), FileMode.Create);
                        ms.WriteTo(fileUpload);
                        fileUpload.Close();
                        fileUpload = null;
                    }

                    //Excel文件路径
                    string excelFile = System.Web.HttpContext.Current.Server.MapPath(string.Format("~/ExcelFile/{0}.xls", fileName));
                    //Excel的Zip文件路径
                    string excelZipFile = System.Web.HttpContext.Current.Server.MapPath(string.Format("~/ExcelFile/{0}.zip", fileName));
                    //Excel的Zip文件下载路径
                    string excelZipPath = string.Format("/ExcelFile/{0}.zip", fileName);

                    //将文件压缩
                    string errMsg = "";
                    bool retZip = Globals.ZipFile(excelFile, excelZipFile, out errMsg);
                    if (retZip)
                    {
                        //压缩成功删除文件
                        FileInfo fi = new FileInfo(excelFile);
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }
                    }

                    return Redirect(excelZipPath);
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩详情导出", ex, this.GetType().FullName, "ServicePerformanceList");
            }

            return RedirectToAction("ServicePerformanceList");
        }
        [Role("客服个人业绩列表", Description = "客服个人业绩列表", IsAuthorize = true)]
        public ActionResult ServicePerformanceMyList(int? page, string GameId, string GameOtherId, string StartTime, string EntTime, string Sort, string SpType, string GameCategoryProperty)
        {
            List<ServicePerformanceTongji> StList = null;
            try
            {
                Weike.CMS.Admins adminModel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                string where = " SID=" + adminModel.A_ID;
                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);

                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            where += string.Format(" and GameGuid like '{0}%' ", gameGUID);
                        else
                            where += string.Format(" and GameGuid='{0}' ", gameGUID);
                    }
                }
                if (!string.IsNullOrEmpty(SpType))
                {
                    where += string.Format(" and ObjectType='{0}'", SpType);
                }
                if (!string.IsNullOrEmpty(GameCategoryProperty))
                {
                    where += string.Format(" and ShopType in (select ID from EshopGame.dbo.GameShopType where Property ='{0}')", GameCategoryProperty);
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);

                int pageSize = 10;
                int pageIndex = page ?? 1;
                System.Collections.ArrayList Arrylist = new BssServicePerformance().GetPageRecordOfTj(where, pageSize, pageIndex, "GameId", Sort);
                StList = Arrylist[0] as List<ServicePerformanceTongji>;
                ViewData["General"] = Arrylist[1];
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceMyList");
            }

            return View(StList);
        }
        [Role("客服业绩配置", Description = "客服业绩配置", IsAuthorize = true)]
        public ActionResult ServicePerformanceConfigList(int? page, string GameId, string SType, int? SLevel)
        {
            DataPages<ServicePerformanceConfig> StList = null;
            try
            {
                string where = " 1=1";
                if (Globals.isInt(SType))
                    where = where + " and ServiceType=" + SType;
                if (!string.IsNullOrEmpty(GameId))
                    where = where + string.Format(" and gameid='{0}'", GameId);
                if (SLevel.HasValue)
                    where = where + string.Format(" and level='{0}'", ((BssServicePerformanceConfig.LevelType)SLevel).ToString());
                StList = new BssServicePerformanceConfig().GetPageRecord(where, "ID desc,GameId,ServiceType,Level", 10, page ?? 1, PagesOrderTypeDesc.升序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceConfigList");
            }

            return View(StList);
        }
        [Role("客服业绩配置添加", Description = "客服业绩配置添加", IsAuthorize = true)]
        public ActionResult ServicePerformanceConfigAdd(string GameId, string ShopType, int Level, int ServiceType, int CaculateType, decimal CaculateValue, int MinTime, int MaxTime, int IndexTime)
        {
            try
            {
                BssServicePerformanceConfig mBssServicePerformanceConfig = new BssServicePerformanceConfig();
                string SLevel = ((BssServicePerformanceConfig.LevelType)Level).ToString();
                if (mBssServicePerformanceConfig.Exists(GameId, ShopType, SLevel, ServiceType))
                {
                    return Content("此游戏已存在该类型客服的绩效配置");
                }
                ServicePerformanceConfig config = new ServicePerformanceConfig();
                config.GameId = GameId;
                config.ShopType = ShopType;
                config.Level = SLevel;
                config.ServiceType = ServiceType;
                config.CaculateType = CaculateType;
                config.CaculateValue = CaculateValue;
                config.MinTime = MinTime;
                config.MaxTime = MaxTime;
                config.IndexTime = IndexTime;
                config.CreateTime = DateTime.Now;
                config.EditTime = DateTime.Now;
                mBssServicePerformanceConfig.Add(config);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceConfigList");
            }

            return Content("");
        }
        [Role("客服业绩配置更新", Description = "客服业绩配置更新", IsAuthorize = true)]
        public ActionResult ServicePerformanceConfigUpdate(int cid, string GameId, string Level, int? CaculateType, decimal? CaculateValue, int? MinTime, int? MaxTime, int? IndexTime)
        {
            try
            {
                BssServicePerformanceConfig mBssServicePerformanceConfig = new BssServicePerformanceConfig();
                ServicePerformanceConfig config = mBssServicePerformanceConfig.GetModel(cid);
                if (config != null)
                {
                    if (IsGet)
                    {
                        return Json(config);
                    }
                    else
                    {
                        config.CaculateType = CaculateType.Value;
                        config.CaculateValue = CaculateValue.Value;
                        config.MinTime = MinTime.Value;
                        config.MaxTime = MaxTime.Value;
                        config.IndexTime = IndexTime.Value;
                        config.EditTime = DateTime.Now;
                        mBssServicePerformanceConfig.Update(config);
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceConfigList");
            }

            return Content("");
        }
        [Role("客服业绩配置删除", Description = "客服业绩配置删除", IsAuthorize = true)]
        public ActionResult DelServicePerformanceConfig(int cid)
        {
            new BssServicePerformanceConfig().Delete(cid);
            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("ServicePerformanceConfigList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }
        [Role("获取客服列表", Description = "获取客服列表", IsAuthorize = true)]
        public ActionResult GetAdminsByGuid(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string where = string.Format(" R_ID='{0}' and  A_Islock=0 order by A_RealName asc", guid);
                if (guid == "all")
                {
                    where = string.Format("A_Islock=0 order by A_RealName asc");
                }
                List<Admins> Adminlist = new BssAdmins().GetAdminsModelList(where);
                return Json(Adminlist);
            }
            return Json("");
        }
        [Role("客服业绩列表客服统计", Description = "客服业绩列表客服统计", IsAuthorize = true)]
        public ActionResult ServicePerformanceNewList(int? page, string GameId, string GameOtherId, string sid, string StartTime, string EntTime, string Role, string Sort, string SpType, string GameCategoryProperty)
        {
            List<ServicePerformanceTongji> StList = null;
            try
            {
                string where = " 1=1";
                //选择了客服
                if (!string.IsNullOrEmpty(sid))
                {
                    sid = sid.Remove(sid.LastIndexOf(","), 1);
                    where = where + " and SID in(" + sid + ")";
                }
                else
                {
                    //按角色 all代表所有部门
                    if (Role != "all" && !string.IsNullOrEmpty(Role))
                    {
                        string Rsid = "";
                        string strwhere = string.Format(" R_ID='{0}' and  A_Islock=0 order by A_RealName asc", Role);
                        List<Admins> Adminlist = new BssAdmins().GetAdminsModelList(strwhere);
                        foreach (Admins admins in Adminlist)
                        {
                            Rsid += admins.A_ID + ",";
                        }
                        if (Adminlist.Count > 0)
                        {
                            Rsid = Rsid.Remove(Rsid.LastIndexOf(","), 1);
                        }
                        where = where + " and SID in(" + Rsid + ")";

                    }
                }

                if (!string.IsNullOrEmpty(GameId))
                {
                    bool isLike = true;
                    string gameGUID = new BLLGame().GetGameInfoGameIdentifyStrByGameGUID(string.IsNullOrWhiteSpace(GameOtherId) ? GameId : GameOtherId, out isLike);

                    if (!string.IsNullOrEmpty(gameGUID))
                    {
                        if (isLike)
                            where += string.Format(" and GameGuid like '{0}%' ", gameGUID);
                        else
                            where += string.Format(" and GameGuid='{0}' ", gameGUID);
                    }
                }
                if (!string.IsNullOrEmpty(SpType))
                {
                    where += string.Format(" and ObjectType='{0}'", SpType);
                }
                if (!string.IsNullOrEmpty(GameCategoryProperty))
                {
                    where += string.Format(" and ShopType in (select ID from EshopGame.dbo.GameShopType where Property ='{0}')", GameCategoryProperty);
                }
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);
                int pageSize = 10;
                int pageIndex = page ?? 1;
                System.Collections.ArrayList Arrylist = new BssServicePerformance().GetPageRecordOfTj(where, pageSize, pageIndex, "SID", Sort);
                StList = Arrylist[0] as List<ServicePerformanceTongji>;
                ViewData["General"] = Arrylist[1];
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("客服业绩配置列表", ex, this.GetType().FullName, "ServicePerformanceNewList");
            }

            return View(StList);
        }
        #endregion

        #region 网站资金统计
        [Role("网站资金统计列表", Description = "网站资金统计列表", IsAuthorize = true)]
        public ActionResult WebTotalMoneyInfoList(int? page, string StartTime, string EntTime)
        {
            DataPages<WebTotalMoneyInfo> wtList = null;
            try
            {
                string where = " 1=1";
                string STime = string.IsNullOrEmpty(StartTime) ? DateTime.Now.AddDays(-7).ToShortDateString() : StartTime;
                string ETime = string.IsNullOrEmpty(EntTime) ? DateTime.Now.AddDays(1).ToShortDateString() : EntTime;
                where += string.Format(" and createtime between '{0}' and '{1}'", STime, ETime);

                int pageSize = 20;
                int pageIndex = page ?? 1;
                wtList = new BssWebTotalMoneyInfo().GetPageRecord(where, "ID", pageSize, pageIndex, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("网站资金统计列表", ex, this.GetType().FullName, "ServicePerformanceDetailList");
            }

            return View(wtList);
        }
        #endregion

        #region 账号截图类型

        [Role("账号截图类型列表", Description = "账号截图类型列表", IsAuthorize = true)]
        public ActionResult ScreenShotTypeList(int? page)
        {
            DataPages<ScreenShotType> StList = null;
            try
            {
                int pageSize = 20;
                int pageIndex = page ?? 1;
                StList = new BssScreenShotType().GetPageRecord("1=1", "OrderNo", pageSize, pageIndex, PagesOrderTypeDesc.升序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("账号截图类型列表", ex, this.GetType().FullName, "ScreenShotTypeList");
            }

            return View(StList);
        }

        /// <summary>
        /// 账号截图类型添加
        /// </summary>
        /// <returns></returns>
        [Role("账号截图类型添加", IsAuthorize = true)]
        public ActionResult ScreenShotTypeAdd(string Name, string Remark, string OrderNo)
        {
            string retMsg = "Error";
            try
            {
                if (IsPost)
                {
                    ScreenShotType sst = new ScreenShotType();
                    sst.Name = Name;
                    sst.Remark = Remark;
                    sst.OrderNo = OrderNo.ToInt32();
                    sst.CreateTime = DateTime.Now;
                    sst.EditTime = DateTime.Now;
                    sst.ID = Guid.NewGuid().ToString().Replace("-", "");

                    new BssScreenShotType().Add(sst);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("账号截图认证：", ex, this.GetType().FullName, "AccountJieTuAdd");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 账号截图类型编辑
        /// </summary>
        /// <returns></returns>
        [Role("账号截图类型编辑", IsAuthorize = true)]
        public ActionResult ScreenShotTypeEdit(string sid, string Name, string Remark, string OrderNo)
        {
            string retMsg = "Error";
            ScreenShotType sst = null;
            try
            {
                sst = new BssScreenShotType().GetModel(sid);
                if (sst == null)
                {
                    return Content("");
                }
                if (IsPost)
                {
                    sst.Name = Name;
                    sst.Remark = Remark;
                    sst.OrderNo = OrderNo.ToInt32();
                    sst.EditTime = DateTime.Now;

                    new BssScreenShotType().Update(sst);
                    retMsg = "";
                }
                else
                {
                    retMsg = "{\"Name\":\"" + sst.Name + "\",\"Remark\":\"" + sst.Remark + "\",\"OrderNo\":\"" + sst.OrderNo + "\"}";
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("账号截图类型编辑：", ex, this.GetType().FullName, "ScreenShotTypeEdit");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 账号截图类型删除
        /// </summary>
        /// <returns></returns>
        [Role("账号截图类型删除", IsAuthorize = true)]
        public ActionResult ScreenShotTypeDelete(string sid)
        {
            string retMsg = "";
            try
            {
                new BssScreenShotType().Delete(sid);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("账号截图类型删除：", ex, this.GetType().FullName, "ScreenShotTypeDelete");
                retMsg = ex.Message;
            }
            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("ScreenShotTypeList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }
        /// <summary>
        /// 游戏截图类型列表
        /// </summary>
        /// <returns></returns>
        [Role("游戏截图类型列表", IsAuthorize = true)]
        public ActionResult GameScreenShotConfigList(int? page, string Game, bool? HasConf)
        {
            DataPages<Game> StList = null;
            try
            {
                int pageSize = 20;
                int pageIndex = page ?? 1;
                string where = "IsEnabled=1 ";
                if (!string.IsNullOrEmpty(Game))
                {
                    where += string.Format(" and ID='{0}'", Game);
                }
                else if (HasConf.HasValue && HasConf.Value)
                {
                    where += " and exists(select 1 from GameScreenShotConfig where Game.ID =GameScreenShotConfig.GameId)";
                }
                StList = new BssGame().GetPageRecord(where, "IsEnabled desc,ishot desc,OrderNo", pageSize, pageIndex, PagesOrderTypeDesc.升序, "ID,GameName");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏截图类型列表", ex, this.GetType().FullName, "GameScreenShotConfigList");
            }

            return View(StList);
        }

        /// <summary>
        /// 游戏截图类型更新
        /// </summary>
        /// <returns></returns>
        [Role("游戏截图类型更新", IsAuthorize = true)]
        public ActionResult GameScreenShotConfigEdit(string Guid)
        {
            string msg = "";
            try
            {
                BssScreenShotType bsst = new BssScreenShotType();
                BssGameScreenShotConfig bssc = new BssGameScreenShotConfig();
                if (string.IsNullOrEmpty(Guid) || !new BssGame().Exists(Guid))
                {
                    return Content(msg);
                }
                if (IsGet)
                {
                    List<GameScreenShotConfig> clist = new BssGameScreenShotConfig().GetModelList(string.Format("Gameid='{0}'", Guid));
                    if (clist.Count > 0)
                    {
                        foreach (GameScreenShotConfig t in clist)
                        {
                            msg += t.ScreenShotTypeId + ",";
                        }
                        msg = msg.TrimEnd(',');
                    }
                }
                else
                {
                    string ids = Request["ids"];
                    bssc.ResetGameScreenShotType(Guid, ids);
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏截图类型更新", ex, this.GetType().FullName, "GameScreenShotConfigEdit");
            }

            return Content(msg);
        }

        /// <summary>
        /// 账号截图类型删除
        /// </summary>
        /// <returns></returns>
        [Role("游戏截图类型删除", IsAuthorize = true)]
        public ActionResult GameScreenShotConfigDelete(string Guid)
        {
            string msg = "";
            try
            {
                BssGameScreenShotConfig bssc = new BssGameScreenShotConfig();
                if (string.IsNullOrEmpty(Guid) || !new BssGame().Exists(Guid))
                {
                    return Content(msg);
                }
                bssc.DeleteByGameId(Guid);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏截图类型删除", ex, this.GetType().FullName, "GameScreenShotConfigDelete");
            }

            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("GameScreenShotConfigList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }

        #endregion

        #region 商品审核配置
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Role("商品审核配置", IsAuthorize = true)]
        public ActionResult ShopAuditConfigList(int? Page, int cid)
        {
            List<ShopAuditConfig> datas = null;
            try
            {
                GameCompany gc = new BssGameCompany().GetModel(cid);
                if (gc == null)
                {
                    return Content("游戏运营商已经删除");
                }
                datas = new BssShopAuditConfig().GetModelListByCompany(cid);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商品审核配置", ex, this.GetType().FullName, "ShopAuditConfigList");
            }

            return View(datas);
        }


        /// <summary>
        /// 账号截图类型添加
        /// </summary>
        /// <returns></returns>
        [Role("商品审核配置添加", IsAuthorize = true)]
        public ActionResult ShopAuditConfigAdd(int cid, string Name, string FldType, string Attr, string AttrAdd, string Require, string OrderNo)
        {
            string retMsg = "Error";
            try
            {
                if (IsPost)
                {
                    ShopAuditConfig sst = new ShopAuditConfig();

                    sst.FldGuid = Guid.NewGuid().ToString();
                    sst.CompanyId = cid;
                    sst.CreateTime = DateTime.Now;

                    sst.Name = Name;
                    sst.OrderNo = OrderNo.ToInt32();
                    sst.FldType = FldType.ToString();
                    sst.Require = !string.IsNullOrEmpty(Require) && Require.ToUpper() == "TRUE";
                    sst.AttrAdd = AttrAdd;
                    sst.Attr = Attr;

                    new BssShopAuditConfig().Add(sst);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商品审核配置添加：", ex, this.GetType().FullName, "AccountJieTuAdd");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 商品审核配置编辑
        /// </summary>
        /// <returns></returns>
        [Role("商品审核配置编辑", IsAuthorize = true)]
        public ActionResult ShopAuditConfigUpdate(string fid, string Name, string FldType, string Attr, string AttrAdd, string Require, string OrderNo)
        {
            string retMsg = "Error";
            ShopAuditConfig sst = null;
            try
            {
                sst = new BssShopAuditConfig().GetModel(fid);
                if (sst == null)
                {
                    return Content("");
                }
                if (IsPost)
                {
                    sst.Name = Name;
                    sst.OrderNo = OrderNo.ToInt32();
                    sst.FldType = FldType.ToString();
                    sst.Require = !string.IsNullOrEmpty(Require) && Require.ToUpper() == "TRUE";
                    sst.AttrAdd = AttrAdd;
                    sst.Attr = Attr;

                    new BssShopAuditConfig().Update(sst);
                    retMsg = "";
                }
                else
                {
                    return Json(sst);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商品审核配置编辑：", ex, this.GetType().FullName, "ShopAuditConfigUpdate");
                retMsg = ex.Message;
            }
            return Content(retMsg);
        }
        /// <summary>
        /// 商品审核信息
        /// </summary>
        /// <returns></returns>
        [Role("商品审核配置删除", IsAuthorize = true)]
        public ActionResult ShopAuditConfigDel(string fid)
        {
            string retMsg = "";
            try
            {
                new BssShopAuditConfig().Delete(fid);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商品审核配置删除：", ex, this.GetType().FullName, "ShopAuditConfigDel");
                retMsg = ex.Message;
            }
            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("GameCompanyList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }
        #endregion

        #region 上传图片
        /// <summary>
        /// 上传图片页面
        /// </summary>
        /// <returns></returns>
        [Role("上传图片页面", IsAuthorize = true)]
        public ActionResult ShopAccountImage(string shopId)
        {
            Shop shop = new BssShop().GetModelTOShopID(shopId);
            string gameType = string.Empty;
            MembersMallShop mallShop = null;
            if (shop == null)
            {
                mallShop = new BssMembersMallShop().GetModel(shopId.ToInt32());
                if (mallShop == null)
                {
                    NeedReceive needModel = new BssNeedReceive().GetModel(shopId.ToInt32());
                    if (needModel == null)
                    {
                        return Content("商品不存在");
                    }
                    else
                    {
                        gameType = needModel.Server;
                    }
                }
                else
                {
                    gameType = mallShop.GameType;
                }
            }
            else
            {
                gameType = shop.GameType;
            }
            GameInfoModel infoModel = new BLLGame().GetGameInfoModel(gameType, "", false);
            if (infoModel == null || infoModel.GameModel == null)
            {
                return Content("游戏不存在");
            }

            ViewData["gameModel"] = infoModel.GameModel;
            return View();
        }
        [Role("截图信息删除", IsAuthorize = true)]
        public ActionResult AccountImageDel(int sid)
        {
            string msg = "";
            try
            {
                new BssShopAccountImageInfo().Delete(sid);
            }
            catch (Exception)
            {
                msg = "删除图片出错";
            }
            return Content(msg);
        }
        /// <summary>
        /// 上传图片到服务器
        /// </summary>
        /// <returns></returns>
        public ActionResult UploadAccountImage()
        {
            string msg = "";
            try
            {
                if (Request.Files.Count > 0)
                {
                    var file = Request.Files[0];
                    if (file.ContentLength != 0)
                    {
                        //上传图片
                        string filename = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
                        string FileDirectory = DateTime.Now.ToString("yyyy-MM-dd");
                        string path = "Upload/AccountImg/" + FileDirectory + "/" + filename;

                        //移动图片
                        Weike.Config.AliyunConfig instance = Weike.Config.AliyunConfig.Instance();
                        bool res = Weike.Common.Helper.UploadImg.UpLoadImagAliyun(instance.endpoint, instance.accessKeyId, instance.accessKeySecret, path, "aimg-dd373", file.InputStream);

                        msg = "//aimg.dd373.com/" + path;
                    }
                }
            }
            catch (Exception)
            {

            }
            return Content(msg);
        }

        #endregion

        #region 增值服务管理

        /// <summary>
        /// 游戏截图类型列表
        /// </summary>
        /// <returns></returns>
        [Role("游戏增值服务列表", IsAuthorize = true)]
        public ActionResult GameAddedServiceList(int? page, string Game, bool? HasConf)
        {
            DataPages<GameAddedService> StList = null;
            try
            {
                int pageSize = 20;
                int pageIndex = page ?? 1;
                string where = "1=1";
                if (!string.IsNullOrWhiteSpace(Game))
                {
                    where +=string.Format( " and GUID='{0}'",Game);
                }
                StList = new BssGameAddedService().GetPageRecord(where, "IsHot desc,OrderNo", pageSize, pageIndex, PagesOrderTypeDesc.升序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏增值服务列表", ex, this.GetType().FullName, "GameAddedServiceList");
            }

            return View(StList);
        }

        /// <summary>
        /// 游戏截图类型列表
        /// </summary>
        /// <returns></returns>
        [Role("游戏增值服务修改图片", IsAuthorize = true)]
        public ActionResult GameAddedServiceUploadImg(string Game)
        {
            try
            {
                BssGameAddedService bssGS = new BssGameAddedService();
                GameAddedService ga = new BssGameAddedService().GetModel(Game);
                if (ga != null)
                {
                    MsgHelper.InsertResult("游戏没有开通增值服务");
                }
                if (Request.Files.Count > 0)
                {
                    string path = Request.Files["ImgPath"].FileName == "" ? "" : Globals.AttachSitePic_UploadNoWater("ImgPath");//图片名称
                    ga.ImgPath = path;
                    bssGS.Update(ga);
                    MsgHelper.InsertResult("图片修改成功");
                }
                else
                { MsgHelper.InsertResult("请选择图片"); }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏增值服务修改图片", ex, this.GetType().FullName, "GameAddedServiceUploadImg");
            }

            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("GameAddedServiceList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }

        /// <summary>
        /// 游戏增值服务添加
        /// </summary>
        /// <returns></returns>
        [Role("游戏增值服务添加", IsAuthorize = true)]
        public ActionResult GameAddedServiceAdd(string Guid, bool? IsHot, int? OrderNo, int? State)
        {
            string msg = "";
            try
            {
                if (IsPost)
                {
                    if (new BssGameAddedService().Equals(Guid))
                    {
                        msg = "已经添加过此游戏";
                    }
                    else
                    {
                        GameAddedService ga = new GameAddedService();
                        ga.CreateTime = DateTime.Now;
                        ga.GUID = Guid;
                        ga.ImgPath = "";
                        ga.IsHot = IsHot.Value;
                        ga.OrderNo = OrderNo.Value;
                        ga.State = State.Value;
                        new BssGameAddedService().Add(ga);
                    }
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏增值服务添加", ex, this.GetType().FullName, "GameAddedServiceAdd");
                msg = "系统出错，请稍后再试";
            }

            return Content(msg);
        }
        /// <summary>
        /// 游戏增值服务更新
        /// </summary>
        /// <returns></returns>
        [Role("游戏增值服务更新", IsAuthorize = true)]
        public ActionResult GameAddedServiceUpdate(string Guid, bool? IsHot, int? OrderNo, int? State)
        {
            string msg = "";
            GameAddedService ga = null;
            try
            {
                ga = new BssGameAddedService().GetModel(Guid);
                if (ga == null)
                {
                    return Content(msg);
                }
                if (IsGet)
                {
                    string res = string.Format("\"GUID\":\"{0}\",\"IsHot\":\"{1}\",\"OrderNo\":\"{2}\",\"State\":\"{3}\"", Guid, ga.IsHot, ga.OrderNo, ga.State);
                    msg = "{" + res + "}";
                }
                else
                {
                    ga.IsHot = IsHot.Value;
                    ga.OrderNo = OrderNo.Value;
                    ga.State = State.Value;
                    new BssGameAddedService().Update(ga);
                }

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏增值服务更新", ex, this.GetType().FullName, "GameAddedServiceUpdate");
            }

            return Content(msg);
        }

        /// <summary>
        /// 账号截图类型删除
        /// </summary>
        /// <returns></returns>
        [Role("游戏增值服务删除", IsAuthorize = true)]
        public ActionResult GameAddedServiceDelete(string Guid)
        {
            string msg = "";
            try
            {
                BssGameAddedService bssc = new BssGameAddedService();
                if (string.IsNullOrEmpty(Guid) || !new BssGame().Exists(Guid))
                {
                    return Content(msg);
                }
                bssc.Delete(Guid);

            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("游戏增值服务删除", ex, this.GetType().FullName, "GameAddedServiceDelete");
            }

            if (Request.UrlReferrer == null)
            {
                return RedirectToAction("GameAddedServiceList");
            }
            else
            {
                return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
            }
        }

        #endregion


        #region 订单预警配置
        [Role("订单预警配置", IsAuthorize = true)]
        public ActionResult ShoppingWarningConfig(string gameId, int? Page)
        {
            if (!string.IsNullOrEmpty(gameId))
            {
                if (!Globals.isrightString(gameId))
                {
                    MsgHelper.InsertResult("游戏id错误！");
                    return View();
                }
            }
            ShoppingWarningConfigList model = new ShoppingWarningConfigList();
            model.DPList = BLLShoppingWarning.GetShoppingWarningConfigByDataPages(gameId, Page ?? 1, 20);
            model.GameId = gameId;
            model.GameList = new BssGame().GetModelList(string.Format(" GameType='{0}' order by GameName asc,GameNameInitials asc", BssGame.GameType.网络游戏.ToString()), false);
            return View(model);
        }
        [HttpPost]
        [Role("新增订单预警配置", IsAuthorize = true)]
        public ActionResult AddShoppingWarningConfig(string gameId, int dealType, int warningValue)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                MsgHelper.InsertResult("游戏不能为空！");
                return Content("");
            }
            Game  game = new BssGame().GetModel(gameId);
            if (game == null)
            {
                MsgHelper.InsertResult("选择游戏错误！");
                return Content("");
            }
            if (game.GameType != BssGame.GameType.网络游戏.ToString())
            {
                MsgHelper.InsertResult("预警只支持网络游戏！");
                return Content("");
            }
            if (!Enum.IsDefined(BssShoppingWarningConfig.EDealType.寄售.GetType(), dealType))
            {
                MsgHelper.InsertResult("交易类型错误！");
                return Content("");
            }
            if (warningValue < 1)
            {
                MsgHelper.InsertResult("预警值必须大于0！");
                return Content("");
            }
            try
            {
                if (new BssShoppingWarningConfig().GetRecordCount(string.Format("1=1 and GameGuid='{0}' and DealType={1}", gameId, dealType)) > 0)
                {
                    MsgHelper.InsertResult("已经添加过该游戏预警！");
                    return Content("");
                }
                ShoppingWarningConfig config = new ShoppingWarningConfig();
                config.ID = System.Guid.NewGuid().ToString().Replace("-", "");
                config.GameGuid = gameId;
                config.DealType = dealType;
                config.WarningValue = warningValue;
                config.CreateTime = DateTime.Now;
                config.EditTime = DateTime.Now;
                new BssShoppingWarningConfig().Add(config);
                MsgHelper.InsertResult("操作成功！");
                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("新增预警配置", ex, this.GetType().FullName, "AddShoppingWarningConfig");
            }
            MsgHelper.InsertResult("操作失败！");
            return Content("");
        }
        [Role("获取订单预警配置", IsAuthorize = true)]
        public ActionResult GetShoppingWarningConfig(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            ShoppingWarningConfig config = new BssShoppingWarningConfig().GetModel(Id);
            return Json(config, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [Role("修改订单预警配置", IsAuthorize = true)]
        public ActionResult EditShoppingWarningConfig(string Id, int warningValue)
        {
            if (string.IsNullOrEmpty(Id))
            {
                MsgHelper.InsertResult("Id不能为空！");
                return Content("");
            }
            if (warningValue < 1)
            {
                MsgHelper.InsertResult("预警值必须大于0！");
                return Content("");
            }
            ShoppingWarningConfig config = new BssShoppingWarningConfig().GetModel(Id);
            if (config == null)
            {
                MsgHelper.InsertResult("预警配置id错误，找不到数据！");
                return Content("");
            }
            try
            {
                config.WarningValue = warningValue;
                config.EditTime = DateTime.Now;
                new BssShoppingWarningConfig().Update(config);
                MsgHelper.InsertResult("操作成功！");
                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("修改预警配置", ex, this.GetType().FullName, "EditShoppingWarningConfig");
            }
            MsgHelper.InsertResult("操作失败！");
            return Content("");
        }
        [Role("删除订单预警配置", IsAuthorize = true)]
        public ActionResult DelShoppingWarningConfig(string Id)
        {
            try
            {
                if (string.IsNullOrEmpty(Id))
                {
                    MsgHelper.InsertResult("Id不能为空！");
                    return Content("");
                }
                if (new BssShoppingWarningConfig().Delete(Id))
                {
                    MsgHelper.InsertResult("操作成功！");
                    return Content("");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除预警配置", ex, this.GetType().FullName, "DelShoppingWarningConfig");
            }
            MsgHelper.InsertResult("操作失败！");
            return Content("");
        }
        [Role("订单预警管理", IsAuthorize = true)]
        public ActionResult ShoppingWarning(string gameId, int? dealType)
        {
            if (!string.IsNullOrEmpty(gameId))
            {
                if (!Globals.isrightString(gameId))
                {
                    MsgHelper.InsertResult("游戏错误！");
                    return View();
                }
            }
            if (dealType.HasValue && !Enum.IsDefined(BssShoppingWarningConfig.EDealType.寄售.GetType(), dealType.Value))
            {
                MsgHelper.InsertResult("交易类型错误！");
                return View();
            }
            ShoppingWarningList model = null;

            #region 数据查询（先判断缓存，不存在则查询数据库）
            string md5Key = "manage.dd373.com.Controllers/AdminEShopController/ShoppingWarning:{gameId:" + gameId + ",dealType:" + (dealType.HasValue ? dealType.Value.ToString() : "") + "}";
            string mcKeyName = Weike.Common.Globals.MD5(md5Key.ToUpper());
            Weike.Common.RedisHelp.RedisHelper redisHelper = new Weike.Common.RedisHelp.RedisHelper();
            model = redisHelper.StringGet<ShoppingWarningList>(mcKeyName);

            if (model == null)
            {
                model = new ShoppingWarningList();
                model.GameId = gameId;
                model.DealType = dealType;
                model.WarningList = BLLShoppingWarning.GetShoppingWarningByList(gameId, dealType);
                model.GameList = new BssGame().GetModelList(string.Format(" GameType='{0}' order by GameName asc,GameNameInitials asc", BssGame.GameType.网络游戏.ToString()));

                //将数据写入缓存
                redisHelper.StringSet<ShoppingWarningList>(mcKeyName, model, TimeSpan.FromMinutes(1));
            }
            #endregion
            
            return View(model);
        }
        #endregion

        #region 订单审核
        /// <summary>
        /// 商城订单审核
        /// </summary>
        /// <param name="OrderId">订单编号</param>
        /// <returns></returns>
        [Role("商城订单审核", IsAuthorize = true)]
        public ActionResult MallExceptionOrderSh(string OrderId)
        {
            try
            {
                Shopping spModel = new BssShopping().GetModel(OrderId);
                if (spModel != null && (spModel.ObjectType == BssShopping.ShoppingType.会员商城.ToString() || spModel.ObjectType == BssShopping.ShoppingType.点券商城.ToString()) && spModel.State == BssShopping.ShoppingState.等待审核.ToString())
                {
                    bool res = new BLLAdminOrderMethod().MallOrderShDeal(spModel);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("商城订单审核", ex, this.GetType().FullName, "MallOrderSh");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        /// <summary>
        /// 普通订单审核
        /// </summary>
        /// <param name="OrderId">订单编号</param>
        /// <returns></returns>
        [Role("普通订单审核", IsAuthorize = true)]
        public ActionResult ExceptionOrderSh(string OrderId)
        {
            try
            {
                Shopping spModel = new BssShopping().GetModel(OrderId);
                if (spModel != null && spModel.ObjectType == BssShopping.ShoppingType.出售交易.ToString() && spModel.State == BssShopping.ShoppingState.等待审核.ToString())
                {
                    bool res = new BLLAdminOrderMethod().OrderShDeal(spModel);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("普通订单审核", ex, this.GetType().FullName, "ExceptionOrderSh");
            }
            return Redirect(Request.UrlReferrer.ToUriStringNoProtocol());
        }

        #endregion

        #region 交易证明申请
        /// <summary>
        /// 订单详情 交易证明申请
        /// </summary>
        /// <param name="Page">页码</param>
        /// <param name="KeyType">关键字类型</param>
        /// <param name="Keywords">搜索关键字</param>
        /// <param name="Status">审核状态</param>
        /// <returns></returns>
        [Role("交易证明申请列表", IsAuthorize = true)]
        public ActionResult ShoppingZmApplyList(int? Page, string KeyType, string Keywords, string Status)
        {
            try
            {
                StringBuilder s_where = new StringBuilder("1=1");
                if (!string.IsNullOrEmpty(Keywords))
                {
                    if (KeyType == "username")
                    {
                        Members m = new BssMembers().GetModelByName(Keywords);
                        s_where.Append(string.Format(" and UserID={0}", m.M_ID));
                    }
                    else if (KeyType == "orderid")
                    {
                        s_where.Append(string.Format(" and OrderId='{0}'", Keywords));
                    }
                }
                if (!string.IsNullOrEmpty(Status))
                {
                    s_where.Append(string.Format(" and Status='{0}'", Status));
                }

                DataPages<ShoppingZmApply> ShoppingZmAList = new BssShoppingZmApply().GetPageRecord(s_where.ToString(), "Status asc,ApplyTime", 20, Page ?? 1, PagesOrderTypeDesc.降序, "ID,OrderId,UserID,ApplyReason,ApplyTime,DealTime,Status,DealAdmin,FailReason");
                ViewData["ShoppingZmAList"] = ShoppingZmAList;
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("分页获取交易证明申请出错", ex, this.GetType().FullName, "ShoppingZmApplyList");
            }
            return View();
        }

        /// <summary>
        /// 交易证明申请详情
        /// </summary>
        /// <param name="ID">申请ID</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Get)]
        [Role("交易证明申请详情", IsAuthorize = true)]
        public ActionResult ShoppingZmApplyDetails(string ID)
        {
            BssShoppingZmApply BssZma = new BssShoppingZmApply();
            ShoppingZmApply Shopzma = new ShoppingZmApply();
            try
            {
                if (!string.IsNullOrEmpty(ID))
                {
                    Shopzma = BssZma.GetModel(ID);
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("交易证明申请详情出错", ex, this.GetType().FullName, "ShoppingZmApplyDetails");
            }
            return View(Shopzma);
        }

        /// <summary>
        /// 交易证明申请详情提交
        /// </summary>
        /// <param name="ID">申请ID</param>
        /// <param name="Status">状态</param>
        /// <param name="FailReason">失败原因</param>
        /// <returns></returns>
        [AcceptVerbs(HttpVerbs.Post)]
        [Role("交易证明申请详情提交", IsAuthorize = true)]
        public ActionResult ShoppingZmApplyDetails(string ID, int Status, string FailReason)
        {
            BssShoppingZmApply BssZma = new BssShoppingZmApply();
            ShoppingZmApply Shopzma = BssZma.GetModel(ID);
            try
            {
                #region 验证
                if (Status == (int)BssShoppingZmApply.Dealstatus.审核失败 && string.IsNullOrEmpty(FailReason))
                {
                    MsgHelper.Insert("FailReason", "审核失败原因必填");
                }
                if (!MsgHelper.IsVaild)
                {
                    MsgHelper.InsertResult("验证失败");
                    return View(Shopzma);
                }
                #endregion

                #region 实体赋值
                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                Shopzma.Status = Status;
                Shopzma.DealAdmin = adminmodel.A_ID;
                Shopzma.DealTime = DateTime.Now;
                Shopzma.FailReason = FailReason;
                #endregion

                #region 逻辑处理
                if (BssZma.Update(Shopzma))
                {
                    MsgHelper.InsertResult("操作成功");
                    return RedirectToAction("ShoppingZmApplyList");
                }
                else
                {
                    MsgHelper.InsertResult("操作失败");
                }
                #endregion
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("审核交易证明申请出错：", ex, this.GetType().FullName, "ShoppingZmApplyDetails");
                MsgHelper.InsertResult("审核交易证明申请出错");
            }
            return View(Shopzma);
        }

        /// <summary>
        /// 交易证明申请审核通过
        /// </summary>
        /// <param name="ID">申请ID</param>
        /// <returns></returns>
        [Role("交易证明申请审核通过", IsAuthorize = true)]
        public ActionResult ShoppingZmApplySucc(string ID)
        {
            BssShoppingZmApply BssZma = new BssShoppingZmApply();
            ShoppingZmApply Shopzma = BssZma.GetModel(ID);
            try
            {
                #region 实体赋值
                Weike.CMS.Admins adminmodel = Weike.CMS.BLLAdmins.GetCurrentAdminUserInfo();
                Shopzma.Status = (int)BssShoppingZmApply.Dealstatus.审核成功;
                Shopzma.DealAdmin = adminmodel.A_ID;
                Shopzma.DealTime = DateTime.Now;
                #endregion

                #region 逻辑处理
                if (BssZma.Update(Shopzma))
                {
                    MsgHelper.InsertResult("操作成功");
                    return RedirectToAction("ShoppingZmApplyList");
                }
                else
                {
                    MsgHelper.InsertResult("操作失败");
                }
                #endregion
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("审核交易证明申请出错：", ex, this.GetType().FullName, "ShoppingZmApplySucc");
                MsgHelper.InsertResult("审核交易证明申请出错");
            }
            return RedirectToAction("ShoppingZmApplyList");
        }

        #endregion

        #region 页顶导航条菜单设置

        /// <summary>
        /// 页顶导航条菜单列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parentId">上级菜单ID</param>
        /// <param name="level">菜单层级</param>
        /// <returns></returns>
        [Role("页顶导航条菜单列表", IsAuthorize = true)]
        public ActionResult TopbarMenuInfoList(int? page, string parentId, string level)
        {
            DataPages<TopbarMenuInfo> MList = null;
            try
            {
                StringBuilder s_where = new StringBuilder("Enable=1");
                if (!string.IsNullOrEmpty(parentId))
                {
                    s_where.Append(string.Format(" and ParentId='{0}'", parentId));
                }
                else
                {
                    s_where.Append(string.Format(" and ParentId='{0}'", 0));
                }
                if (!string.IsNullOrEmpty(level))
                {
                    s_where.Append(string.Format(" and MenuLevel={0}", level));
                }
                else
                {
                    s_where.Append(string.Format(" and MenuLevel={0}", 1));
                }
                MList = new BssTopbarMenuInfo().GetPageRecord(s_where.ToString(), "OrderNo", 15, page ?? 1, PagesOrderTypeDesc.升序, "ID,MenuName,MenuUrl,ParentId,MenuLevel,OrderNo,ClassName,Qrimg,CreateTime,EditTime");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("页顶导航条菜单列表：", ex, this.GetType().FullName, "TopbarMenuInfoList");
            }
            return View(MList);
        }

        /// <summary>
        /// 新增页顶导航条菜单
        /// </summary>
        /// <param name="ParentId">上级菜单ID</param>
        /// <param name="Level">菜单层级</param>
        /// <param name="MenuName">菜单名称</param>
        /// <param name="MenuUrl">菜单链接</param>
        /// <param name="Target">链接打开方式</param>
        /// <param name="Order">菜单排序</param>
        /// <param name="ClassName">样式名</param>
        /// <returns></returns>
        [Role("添加页顶导航条菜单", IsAuthorize = true)]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TopbarMenuInfoAdd(string ParentId, string Level, string MenuName, string MenuUrl, string Target, string Order, string ClassName)
        {
            TopbarMenuInfo menuModel = new TopbarMenuInfo();
            BssTopbarMenuInfo bssmenu = new BssTopbarMenuInfo();
            try
            {
                if (string.IsNullOrEmpty(MenuName))
                {
                    MsgHelper.InsertResult("菜单名必填");
                }
                if (string.IsNullOrEmpty(Order))
                {
                    MsgHelper.InsertResult("排序必填");
                }
                string strpicname = Request.Files["Img"].FileName;
                if (!string.IsNullOrEmpty(strpicname))
                {
                    if (!Weike.Config.BasicSettingsConfig.Instance().ImageExtendName.Contains(Globals.GetExtension(strpicname).ToLower()))
                        MsgHelper.InsertResult("图片格式错误");
                }
                if (!MsgHelper.IsVaild)
                {
                    return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
                }
                menuModel.ID = Globals.GetPrimaryID();
                menuModel.MenuName = MenuName;
                menuModel.MenuUrl = MenuUrl;
                menuModel.Target = Target;
                menuModel.ParentId = ParentId;
                menuModel.MenuLevel = Level.ToInt32();
                menuModel.OrderNo = Order.ToInt32();
                menuModel.ClassName = ClassName == "0" ? "" : ClassName;
                if (!string.IsNullOrEmpty(strpicname))
                {
                    menuModel.Qrimg = Globals.AttachSitePic_UploadNoWater("Img");
                }
                menuModel.Enable = true;
                menuModel.CreateTime = DateTime.Now;
                menuModel.EditTime = DateTime.Now;
                if (bssmenu.Add(menuModel))
                {
                    MsgHelper.InsertResult("添加成功");
                    return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
                }
                else
                {
                    MsgHelper.InsertResult("添加失败");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("新增页顶导航条菜单出错", ex, this.GetType().FullName, "TopbarMenuInfoAdd");
            }
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
        }

        /// <summary>
        /// 删除页顶导航条菜单
        /// </summary>
        /// <param name="mid">菜单ID</param>
        /// <returns></returns>
        [Role("删除页顶导航条菜单", IsAuthorize = true)]
        public ActionResult TopbarMenuInfoDel(string ID)
        {
            try
            {
                BssTopbarMenuInfo bssmenu = new BssTopbarMenuInfo();
                TopbarMenuInfo menuModel = bssmenu.GetModel(ID); ;
                if (menuModel != null)
                {
                    menuModel.Enable = false;
                    if (bssmenu.Update(menuModel))
                    {
                        MsgHelper.InsertResult("删除 [" + menuModel.MenuName + "] 菜单成功");
                    }
                    else
                    {
                        MsgHelper.InsertResult("删除 [" + menuModel.MenuName + "] 菜单失败");
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("删除页顶导航条菜单", ex, this.GetType().FullName, "TopbarMenuInfoDel");
            }
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
        }

        /// <summary>
        /// 编辑页顶导航条菜单get方法
        /// </summary>
        /// <param name="ID">菜单ID</param>
        /// <returns></returns>
        [Role("编辑页顶导航条菜单", IsAuthorize = true)]
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult TopbarMenuInfoUpdate(string ID)
        {
            TopbarMenuInfo menuModel = new TopbarMenuInfo();
            BssTopbarMenuInfo bssmenu = new BssTopbarMenuInfo();
            try
            {
                menuModel = bssmenu.GetModel(ID);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取页顶导航条菜单信息出错", ex, this.GetType().FullName, "TopbarMenuInfoUpdate");
            }
            return Json(menuModel);
        }

        /// <summary>
        /// 编辑页顶导航条菜单post方法
        /// </summary>
        /// <param name="ID">菜单ID</param>
        /// <param name="MenuName">菜单名</param>
        /// <param name="MenuUrl">菜单链接</param>
        /// <param name="Target">链接打开方式</param>
        /// <param name="Order">菜单排序</param>
        /// <param name="ClassName">菜单样式</param>
        /// <returns></returns>
        [Role("编辑页顶导航条菜单", IsAuthorize = true)]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TopbarMenuInfoUpdate(string ID, string MenuName, string MenuUrl, string Target, string Order, string ClassName)
        {
            TopbarMenuInfo menuModel = new TopbarMenuInfo();
            BssTopbarMenuInfo bssmenu = new BssTopbarMenuInfo();
            try
            {
                string strpicname = Request.Files["Img"].FileName;
                if (!string.IsNullOrEmpty(strpicname))
                {
                    if (!Weike.Config.BasicSettingsConfig.Instance().ImageExtendName.Contains(Globals.GetExtension(strpicname).ToLower()))
                        MsgHelper.InsertResult("图片格式错误");
                }
                if (!MsgHelper.IsVaild)
                {
                    MsgHelper.InsertResult("验证失败");
                    return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
                }

                menuModel = bssmenu.GetModel(ID);
                menuModel.MenuName = MenuName;
                menuModel.MenuUrl = MenuUrl;
                menuModel.Target = Target;
                menuModel.OrderNo = Order.ToInt32();
                menuModel.ClassName = ClassName;
                if (!string.IsNullOrEmpty(strpicname))
                {
                    menuModel.Qrimg = Globals.AttachSitePic_UploadNoWater("Img");
                }
                menuModel.EditTime = DateTime.Now;
                if (bssmenu.Update(menuModel))
                {
                    MsgHelper.InsertResult("修改成功");
                    return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
                }
                else
                {
                    MsgHelper.InsertResult("操作失败");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取页顶导航条菜单信息出错", ex, this.GetType().FullName, "TopbarMenuInfoUpdate");
            }
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
        }

        #endregion

        #region 担保自动发货配置
        /// <summary>
        /// 担保自动发货配置列表
        /// </summary>
        /// <param name="Page">页码</param>
        /// <param name="GameId">游戏ID</param>
        /// <param name="TypeId">商品类型ID</param>
        /// <param name="Enalbed">是否可用（0或者1或者空）</param>
        /// <returns></returns>
        [Role("担保自动发货配置列表", IsAuthorize = true)]
        public ActionResult ShopAutoFhConfigList(int? Page, string GameId, string Enalbed)
        {
            DataPages<ShopAutoFhConfig> configList = null;
            try
            {
                StringBuilder where = new StringBuilder("1=1 ");
                if (!string.IsNullOrEmpty(GameId))
                {
                    where.Append(string.Format(" and GameId='{0}'", GameId));
                }
                if (!string.IsNullOrEmpty(Enalbed))
                {
                    where.Append(string.Format(" and Enalbed={0}", Enalbed));
                }
                configList = new BssShopAutoFhConfig().GetPageRecord(where.ToString(), "EditTime", 15, Page ?? 1, PagesOrderTypeDesc.降序, "*");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("担保自动发货配置列表", ex, this.GetType().FullName, "ShopAutoFhConfigList");
            }

            return View(configList);
        }

        /// <summary>
        /// 担保自动发货配置状态更新
        /// </summary>
        /// <param name="ID">主键ID</param>
        /// <returns></returns>
        [Role("担保自动发货配置状态更新", IsAuthorize = true)]
        public ActionResult ShopAutoFhConfigUpdateState(string ID)
        {
            BssShopAutoFhConfig bssConfig = new BssShopAutoFhConfig();
            ShopAutoFhConfig configModel = bssConfig.GetModel(ID);
            if (configModel != null)
            {
                configModel.Enalbed = !configModel.Enalbed;
                configModel.EditTime = DateTime.Now;
                bssConfig.Update(configModel);
            }

            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToUriStringNoProtocol() : Request.Url.ToUriStringNoProtocol());
        }

        #endregion

        #region 系统账户

        [Role("系统账户列表", IsAuthorize = true)]
        public ActionResult PaySystemAccountList(int? Page)
        {
            DataPages<Weike.EShop.PaySystemAccount> Lmoney = null;
            try
            {
                Lmoney = new BssPaySystemAccount().GetPageRecord("1=1", "id", 20, Page ?? 1, PagesOrderTypeDesc.升序, "ID,Name,Money,CreateTime");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("系统账户列表出错", ex, this.GetType().FullName, "PaySystemAccountList");
            }

            return View(Lmoney);

        }

        [Role("系统账户资金记录", IsAuthorize = true)]
        public ActionResult PaySystemMoneyHistoryList(int? Page, string M_ID, int? FlowType, int? OperaType, int? AccountType, string OrderId, DateTime? StartTime, DateTime? EntTime)
        {
            DataPages<Weike.EShop.PaySystemMoneyHistory> Lmoney = null;
            try
            {
                int? mid = null;
                if (!string.IsNullOrEmpty(M_ID))
                {
                    Members mModel = new BssMembers().GetModelByName(M_ID);
                    mid = mModel == null ? -1 : mModel.M_ID;
                }
                Lmoney = new BssPaySystemMoneyHistory().GetPageRecord(Page, mid, FlowType, OperaType, AccountType, OrderId, StartTime, EntTime);
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("系统账户明细列表出错", ex, this.GetType().FullName, "PaySystemMoneyHistoryList");
            }

            return View(Lmoney);

        }
        #endregion

        #region 后台订单详情页获取客服
        /// <summary>
        /// 订单详情页获取该订单类型客服
        /// </summary>
        /// <param name="OrderId">订单编号</param>
        /// <returns></returns>
        [Role("订单详情页获取该订单类型客服", IsAuthorize = true)]
        public ActionResult GetOrderZdInfo(string OrderId)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");

                List<AdminServiceQQInfo> adminSqqList = BLLServiceQQMethod.GetAdminServiceQQInfo(OrderId);
                if (adminSqqList != null && adminSqqList.Count > 0)
                {
                    foreach (AdminServiceQQInfo adminSqq in adminSqqList)
                    {
                        sb.Append("{\"AdminId\":\"" + adminSqq.AdminId + "\",\"AdminRealName\":\"" + adminSqq.AdminRealName + "\",\"DealOrderCount\":\"" + adminSqq.DealOrderCount + "\"},");
                    }
                }
                sb.Append("{\"AdminId\":\"68\",\"AdminRealName\":\"装备\",\"DealOrderCount\":\"\"}");

                sb.Append("]");

                return Content(sb.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单详情页获取该订单类型客服：", ex, this.GetType().FullName, "GetOrderZdInfo");
            }
            return Content("");
        }

        /// <summary>
        /// 订单转单其他客服获取其他客服
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [Role("订单转单其他客服获取其他客服", IsAuthorize = true)]
        public ActionResult GetOrderZdOtherKfInfo(string OrderId)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");

                List<AdminServiceQQInfo> adminSqqList = BLLServiceQQMethod.GetOtherAdminServiceQQInfo(OrderId);
                if (adminSqqList != null && adminSqqList.Count > 0)
                {
                    foreach (AdminServiceQQInfo adminSqq in adminSqqList)
                    {
                        sb.Append("{\"AdminId\":\"" + adminSqq.AdminId + "\",\"AdminRealName\":\"" + adminSqq.AdminRealName + "\",\"DealOrderCount\":\"" + adminSqq.DealOrderCount + "\"},");
                    }
                }
                sb.Append("{\"AdminId\":\"68\",\"AdminRealName\":\"装备\",\"DealOrderCount\":\"\"}");

                sb.Append("]");

                return Content(sb.ToString());
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("订单转单其他客服获取其他客服", ex, this.GetType().FullName, "GetOrderZdOtherKfInfo");
            }
            return Content("");
        }
        #endregion

        /// <summary>
        /// 获取订单相同处理客服信息
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <returns></returns>
        [Role("获取订单相同处理客服信息", IsAuthorize = true)]
        public ActionResult GetOrderJySameInfo(string orderId)
        {
            try
            {
                Shopping orderModel = new BssShopping().GetModel(orderId);
                if (orderModel != null)
                {
                    BssOrderShopSnapshot bssOrderShop = new BssOrderShopSnapshot();
                    OrderShopSnapshot orderShopModel = bssOrderShop.GetModelByOrderId(orderId);
                    if (orderShopModel != null)
                    {
                        List<Shopping> spList = null;
                        string noticeInfo = "";
                        int orderNo = 0;

                        if (orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.寄售 || orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.担保)
                        {
                            if (orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.担保)
                            {
                                spList = BssShopping.GetListByBuyer(orderModel.UserID);
                                noticeInfo = "当前买家正在处理的担保订单";
                            }
                            else
                            {
                                Members sellerModel = new BssMembers().GetModel(orderShopModel.PublicUser);
                                if (sellerModel != null)
                                {
                                    spList = BssShopping.GetListBySeller(sellerModel.M_Name);
                                }
                                noticeInfo = "当前卖家正在处理的寄售订单";
                            }

                        }
                        else if (orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.账号)
                        {
                            noticeInfo = "当前商品同时被购买次数";
                            spList = BssShopping.GetListByShop(orderModel);
                        }

                        if (spList != null && spList.Count > 0)
                        {
                            bool isYzKf = false;
                            bool isJtKf = false;

                            string sourceType = "";
                            Admins adminCurrModel = BLLAdmins.GetCurrentAdminUserInfo();
                            ShoppingAssembly spaModel = null;
                            BssShoppingAssembly bssSpA = new BssShoppingAssembly();

                            if (orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.账号)
                            {
                                bool isYcKf = Weike.CMS.BssAdmins.IsYiChangKefu(adminCurrModel);
                                isYzKf = !isYcKf && new BssShoppingAssembly().GetModelBySpIdAndSidAndSType(orderModel.ID, adminCurrModel.A_ID, BssShoppingAssembly.SourceType.截图完成.ToString()) != null ? true : false;
                                isJtKf = Weike.CMS.BssAdmins.IsJtKefu(adminCurrModel);

                                if (isJtKf || isYzKf)
                                {
                                    spaModel = bssSpA.GetModelBySpIdAndSid(orderModel.ID, adminCurrModel.A_ID);
                                    sourceType = isJtKf ? BssShoppingAssembly.SourceType.客服截图.ToString() : BssShoppingAssembly.SourceType.截图完成.ToString();
                                }
                                else
                                {
                                    spaModel = bssSpA.GetNoModelBySpId(orderModel.ID);
                                }
                            }

                            string gameInfo = "";
                            bool canZd = false;
                            BLLGame bllGame = new BLLGame();
                            Admins adminModel = null;
                            BssAdmins bssAdmin = new BssAdmins();
                            OrderShopSnapshot orderShopInfoModel = null;
                            ShoppingAssembly spaTempModel = null;
                            BssServiceQQ bssServiceQQ = new BssServiceQQ();

                            StringBuilder orderInfoSb = new StringBuilder();
                            orderInfoSb.Append("{");
                            orderInfoSb.AppendFormat("\"NoticeInfo\":\"{0}\",", noticeInfo);

                            orderInfoSb.Append("\"OrderInfo\":[");
                            foreach (Shopping shoppingModel in spList)
                            {
                                canZd = false;
                                adminModel = null;

                                orderShopInfoModel = bssOrderShop.GetModelByOrderId(shoppingModel.ID);
                                if (orderShopInfoModel != null && orderShopInfoModel.DealType == orderShopModel.DealType)
                                {
                                    if (orderShopInfoModel.DealType != (int)BssOrderShopSnapshot.DealType.账号)
                                    {
                                        gameInfo = bllGame.GetGameInfoModelByGameGuid(orderShopInfoModel.GameGuid, "");
                                    }

                                    if (orderShopInfoModel.DealType == (int)BssOrderShopSnapshot.DealType.寄售 || orderShopInfoModel.DealType == (int)BssOrderShopSnapshot.DealType.担保)
                                    {
                                        if (shoppingModel.SID != 0)
                                        {
                                            adminModel = bssAdmin.GetModel(shoppingModel.SID);

                                            if (shoppingModel.SID == orderModel.SID)
                                            {
                                                canZd = false;
                                            }
                                            else if (orderModel.SID == adminCurrModel.A_ID)
                                            {
                                                canZd = true;
                                            }
                                            else
                                            {
                                                canZd = false;
                                            }
                                        }
                                        else
                                        {
                                            adminModel = null;
                                            canZd = false;
                                        }
                                    }
                                    else if (orderShopInfoModel.DealType == (int)BssOrderShopSnapshot.DealType.账号)
                                    {
                                        spaTempModel = null;
                                        bool otherZd = false;//其他转单，不改变订单客服
                                        if (isJtKf || isYzKf)
                                        {
                                            spaTempModel = bssSpA.GetModelBySpIdAndSourceType(shoppingModel.ID, sourceType);
                                            otherZd = true;
                                        }
                                        else
                                        {
                                            spaTempModel = spaModel;
                                        }
                                        if (spaModel != null && spaTempModel != null)
                                        {
                                            if (spaTempModel != null && spaTempModel.SID > 0)
                                            {
                                                adminModel = bssAdmin.GetAdminsModel(otherZd ? spaTempModel.SID : shoppingModel.SID);
                                            }
                                            else
                                            {
                                                adminModel = null;
                                            }

                                            if (adminModel != null && spaTempModel != null)
                                            {
                                                if ((!otherZd && shoppingModel.SID == orderModel.SID) || (otherZd && adminModel.A_ID == spaTempModel.SID) || !bssServiceQQ.IsSameCategory(adminModel.A_ID, spaTempModel.SID))
                                                {
                                                    canZd = false;
                                                }
                                                else if (orderModel.SID == adminCurrModel.A_ID)
                                                {
                                                    canZd = true;
                                                }
                                                else
                                                {
                                                    canZd = false;
                                                }
                                            }
                                        }
                                    }

                                    string AdminId = adminModel == null ? "" : adminModel.A_ID.ToString();
                                    string AdminName = adminModel == null ? "" : adminModel.A_RealName;
                                    string DataOther = orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.账号 ? isJtKf || isYzKf ? "1" : "0" : "";
                                    string SType = orderShopModel.DealType == (int)BssOrderShopSnapshot.DealType.账号 ? isJtKf ? "jt" : "yz" : "";

                                    orderInfoSb.Append("{\"GameInfo\":\"" + gameInfo + "\",\"OrderId\":\"" + shoppingModel.ID + "\",\"AdminId\":\"" + AdminId + "\",\"AdminName\":\"" + AdminName + "\",\"DataOther\":\"" + DataOther + "\",\"SType\":\"" + SType + "\",\"CanZd\":\"" + (canZd ? "1" : "0") + "\"},");

                                    orderNo++;
                                }
                            }
                            orderInfoSb = orderInfoSb.Remove(orderInfoSb.ToString().LastIndexOf(','), 1);
                            orderInfoSb.Append("]");
                            orderInfoSb.AppendFormat(",\"OrderNo\":\"{0}\"", orderNo);
                            orderInfoSb.Append("}");

                            return Content(orderInfoSb.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取订单相同处理客服信息", ex, this.GetType().FullName, "GetOrderJySameInfo");
            }
            return Content("");
        }

        /// <summary>
        /// 获取订单信息并创建Tab
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [Role("获取订单信息并创建Tab", IsAuthorize = true)]
        public ActionResult ShoppingInfoJump(string orderId)
        {
            try
            {
                Shopping shopping = new BssShopping().GetModel(orderId);
                if (shopping != null && shopping.State != BssShopping.ShoppingState.等待支付.ToString() && shopping.State != BssShopping.ShoppingState.等待审核.ToString() && shopping.State != BssShopping.ShoppingState.部分完成.ToString() && shopping.State != BssShopping.ShoppingState.交易成功.ToString() && shopping.State != BssShopping.ShoppingState.交易取消.ToString())
                {
                    BssOrderShopSnapshot bssOrderShop = new BssOrderShopSnapshot();
                    OrderShopSnapshot orderShopModel = bssOrderShop.GetModelByOrderId(orderId);
                    int dealType = 0;
                    if (orderShopModel != null)
                    {
                        dealType = orderShopModel.DealType;
                    }
                    if (dealType != 0)
                    {
                        if (dealType == (int)BssOrderShopSnapshot.DealType.收货)
                        {
                            return RedirectToAction("NeedReceiveOrderUpload", new { sid = orderId, adminType = 0, t = new Random().Next() });
                        }
                        else if (dealType == (int)BssOrderShopSnapshot.DealType.商城)
                        {
                            return RedirectToAction("MembersMallShopUSellOrderUpload", new { sid = orderId, adminType = 0, t = new Random().Next() });
                        }
                        else if (dealType == (int)BssOrderShopSnapshot.DealType.手游)
                        {
                            return RedirectToAction("OrderDetail", "AdminMobileGame", new { orderId = orderId, PageType = "manage", t = new Random().Next() });
                        }
                        else
                        {
                            return RedirectToAction("OrderDetail", new { orderId = orderId, t = new Random().Next() });
                        }
                    }
                }
                else
                {
                    //阿里云MQ消息订阅发送
                    Weike.WebGlobalMethod.BLLAliyunMQMethod.SendMessage("KfMsgStateChangeBluck", orderId, 10, false, 0, "ChatConsumeTag");
                }
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("获取订单信息并创建Tab", ex, this.GetType().FullName, "ShoppingInfoJump");
            }
            return Content("该订单因为交易完成前卖家或者买家发送消息您没有阅读，弹出此页面后之前消息已全部更新为已读，请直接关闭选项卡即可");
        }


        /// <summary>
        /// 发送订单信息给聊天系统
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <param name="receivetype">接收系统消息的聊天类型:0买家卖家,1买家客服，2卖家客服，3所有人</param>
        /// <param name="msgInfo">消息内容</param>
        /// <returns></returns>
        [Role("发送订单信息给聊天系统", IsAuthorize = true)]
        public ActionResult SendOrderInfoToChat(string orderId, int receivetype, string msgInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return Content("发送订单信息给聊天系统失败，订单编号不能为空");
                }
                if (receivetype != 1 && receivetype != 2 && receivetype != 3)
                {
                    return Content("接收系统消息的聊天类型错误");
                }

                BLLChatSystemMsg.SendOrderInfoToChat(orderId, receivetype, 3, msgInfo, "系统消息", false, false);

                return Content("");
            }
            catch (Exception ex)
            {
                LogExcDb.Log_AppDebug("发送订单信息给聊天系统", ex, this.GetType().FullName, "SendOrderInfoToChat");
            }
            return Content("发送订单信息给聊天系统失败");
        }
    }
}
