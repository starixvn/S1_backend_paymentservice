using Newtonsoft.Json;
using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Payment.Fuli
{
    class PayHelper : PayHelperBase
    {
        Dictionary<string, string> failReasonTypeDic = new Dictionary<string, string>() {
            {"200200000","网络异常，请致电客服" },
            {"200300000","网络异常，请致电客服" },
            {"200300162","账户余额不足，请及时充值" },
            {"200300288","银行处理失败" },
            {"209900000","网络异常，请致电客服" },
            {"209999999","IP地址沒有加入白名单，请致电客服" } ,
            {"310060309","单笔金额超限" } ,
            {"310060310","单日金额超限" },
            {"310060312","金额有误" },
            {"310060313","无效业务" },
            {"310060314","通讯失败" },
            {"310060315","外部系统错误" },
            {"310060316","其他错误" },
            {"310060317","账号有误" }
        };
        private class CallBackWithdrawalReq
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string sum { get; set; }
            public string totalSum { get; set; }
            public string balance { get; set; }
            public string canUsedBalance { get; set; }
            public string frozenBalance { get; set; }
            public string unsettledBalance { get; set; }
            public string payType { get; set; }
            public string bankAccount { get; set; }
            public string bankName { get; set; }
            public string name { get; set; }
            public string requestTime { get; set; }
            public string dealTime { get; set; }
            public string noticeType { get; set; }
            public string failReason { get; set; }
            public string signature { get; set; }
        }

        public override NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackWithdrawalReq>(ToJson(formData));
            var p = $"{data.requestId}{data.result}{data.sum}{data.balance}";
            var sign = ToHMACSMD5(p, payResource["key"]);

            var oNotify = new NotifyWithdrawResult();
            oNotify.WithdrawId = data.requestId;
            oNotify.ThirdId = data.requestId;
            oNotify.Message = data.result;
            if (sign == data.signature)
            {
                if (data.result == "00000")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.sum);
                    oNotify.Response = "success";
                }
                else
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                    var failReason = "";
                    try
                    {
                        //failReason = System.Web.HttpUtility.UrlDecode(data.failReason, System.Text.Encoding.GetEncoding("GB2312"));
                        failReason = GBKToUTF8(data.failReason);
                    }
                    catch(Exception ex)
                    {
                        failReason = data.failReason;
                    }

                    oNotify.Message = failReason;
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Message = "Signature error";
                oNotify.Response = "Signature error";
            }

            return oNotify;
        }

        private class CallBackDebitReq
        {
            public string requestId { get; set; }
            public string description { get; set; }
            public string tradeId { get; set; }
            public string fiscalDate { get; set; }
            public string payType { get; set; }
            public string bankCode { get; set; }
            public string totalPrice { get; set; }
            public string tradeAmount { get; set; }
            public string tradeFee { get; set; }
            public string status { get; set; }
            public string endTime { get; set; }
            public string userIdIdentity { get; set; }
            public string couponFe { get; set; }
            public string feeType { get; set; }
            public string bankType { get; set; }
            public string resultSignature { get; set; }
        }

        public override NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource)
        {
            var data = FromJson<CallBackDebitReq>(ToJson(formData));
            var p = $"{data.requestId}{data.tradeId}{data.fiscalDate}{data.description}{data.totalPrice}{data.tradeAmount}{data.tradeFee}";
            var sign = ToHMACSMD5(p, payResource["key"]);

            var oNotify = new NotifyDepositResult();
            oNotify.DepositId = data.requestId;
            oNotify.ThirdId = data.tradeId;
            oNotify.Comment = data.description;
            oNotify.Message = data.status;

            if (sign == data.resultSignature)
            {
                if (data.status == "2")
                {
                    oNotify.TransStatus = TransStatus.Success;
                    oNotify.RequestAmount = decimal.Parse(data.totalPrice);
                    oNotify.ActualAmount = decimal.Parse(data.totalPrice);
                    oNotify.Comment = data.status;
                    oNotify.Response = "success";
                }
                else 
                {
                    oNotify.TransStatus = TransStatus.Fail;
                    oNotify.Response = "success";
                }
            }
            else
            {
                oNotify.TransStatus = TransStatus.Pending;
                oNotify.Comment = "Signature error";
                oNotify.Message = "Signature error";
                oNotify.Response = "failed";
            }

            return oNotify;
        }

        public override Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }


        private class WithdrawalRes
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string sum { get; set; }
            public string signature { get; set; }
        }

        public override WithdrawResult Credit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["PayOut"];
            var formData = new Dictionary<string, string>();
            formData.Add("requestId", inputs["TransID"]);
            formData.Add("merchantCode", inputs["merchant_no"]);
            formData.Add("transferType", "1");
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("sum", amount.ToString("#"));
            formData.Add("accountType", "1");
            formData.Add("unionBankNum", "000000");
            formData.Add("branchBankName", "无");
            formData.Add("openBankName", "无");
            formData.Add("openBankProvince", "无");
            formData.Add("openBankCity", "无");
            formData.Add("accountName", inputs["AccountName"]);
            formData.Add("bankCode", inputs["BankCode"]);
            formData.Add("bankAccount", inputs["AccountNo"]);
            formData.Add("reason", "无");
            formData.Add("noticeUrl", inputs["CallBackUrl"]);

            var p = $"{formData["requestId"]}{formData["merchantCode"]}{formData["transferType"]}{formData["sum"]}{formData["accountType"]}{formData["unionBankNum"]}" +
                    $"{formData["branchBankName"]}{formData["openBankName"]}{formData["openBankProvince"]}{formData["openBankCity"]}{formData["accountName"]}{formData["bankCode"]}" +
                    $"{formData["bankAccount"]}{formData["reason"]}{formData["noticeUrl"]}";
            formData.Add("signature", ToHMACSMD5(p, inputs["key"]));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPostGBK(sUrl, sParam, "application/x-www-form-urlencoded", formData["requestId"]);

            var withdrawResult = new WithdrawResult();
            var withdrawalRes = new WithdrawalRes();
            withdrawalRes = JsonConvert.DeserializeObject<WithdrawalRes>(sResponse);

            if (withdrawalRes.result == "00000" || withdrawalRes.result == "00001")
            {
                withdrawResult.Success = true;
                withdrawResult.Message = withdrawalRes.result;
            }
            else
            {
                withdrawResult.Success = false;
                withdrawResult.Message = withdrawalRes.result;
            }

            return withdrawResult;
        }

        private class DepositRes
        {
            public string requestId { get; set; }
            public string result { get; set; }
            public string codeUrl { get; set; }
            public string codeImgString { get; set; }
            public string codeImgUrl { get; set; }
            public string payId { get; set; }
            public string passThrough { get; set; }
            public string resultSignature { get; set; }
        }

        public override DepositResult Debit(Dictionary<string, string> inputs)
        {
            var sUrl = inputs["host"];
            var formData = new Dictionary<string, string>();
            formData.Add("tradeCode", "IAZ1002");
            formData.Add("requestId", inputs["TransID"]);
            formData.Add("merchantCode", inputs["merchant_no"]);
            formData.Add("totalBizType", "BIZ01100");
            var amount = decimal.Parse(inputs["Amount"]);
            formData.Add("totalPrice", amount.ToString("#"));
            formData.Add("goodsDesc", "无");
            formData.Add("rePayTimeOut", "0");
            formData.Add("noticeurl", inputs["CallBackUrl"]);
            formData.Add("encode", "GBK");
            formData.Add("remoteIp", inputs["ClientIP"]);
            formData.Add("payType", inputs["PayCode"]);
            formData.Add("productId", "无");
            formData.Add("productName", "无");;
            formData.Add("fund", formData["totalPrice"]);
            formData.Add("merAcct", inputs["TransID"]);
            formData.Add("bizType", "BIZ01100");
            formData.Add("productNumber", "1");
            var p = $"{formData["requestId"]}{formData["merchantCode"]}{formData["totalBizType"]}{formData["totalPrice"]}{formData["noticeurl"]}{formData["remoteIp"]}{formData["payType"]}";
            formData.Add("mersignature", ToHMACSMD5(p, inputs["key"]));

            var sParam = string.Join("&", formData.Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var sResponse = HttpPostGBK(sUrl, sParam, "application/x-www-form-urlencoded", formData["requestId"]);

            var depositRes = new DepositRes();
            depositRes = JsonConvert.DeserializeObject<DepositRes>(sResponse);
            var oDepositResult = new DepositResult();
            if (depositRes.result == "00000")
            {
                oDepositResult.Url = depositRes.codeUrl;
                oDepositResult.Success = true;
                oDepositResult.FormData = null;
                oDepositResult.Message = "";
            }
            else
            {
                oDepositResult.Success = false;
            }

            return oDepositResult;
        }

        public override EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
    }
}
