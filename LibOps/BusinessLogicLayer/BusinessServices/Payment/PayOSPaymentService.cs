using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibOps.DataModels.DataTransferObjects;
using Newtonsoft.Json.Linq;

namespace LibOps.BusinessLogicLayer.BusinessServices.Payment
{
    /// <summary>
    /// Triển khai tích hợp Cổng thanh toán VietQR PayOS Auto-Banking trực tiếp qua chuẩn REST API v2
    /// </summary>
    public class PayOSPaymentService : IPaymentGatewayService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;

        public PayOSPaymentService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            _clientId = ConfigurationManager.AppSettings["PayOS_ClientId"] ?? "e600df83-9a68-44f6-bb42-bb266dd3b8e7";
            _apiKey = ConfigurationManager.AppSettings["PayOS_ApiKey"] ?? "74231360-e0d2-4346-a931-bcddc6ef734d";
            _checksumKey = ConfigurationManager.AppSettings["PayOS_ChecksumKey"] ?? "c0ce9bbf702764f4cb2687d95a38ad9b9b8229ad734babd527be2842c116e975";
        }

        public PayOSPaymentService(string clientId, string apiKey, string checksumKey)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            _clientId = clientId;
            _apiKey = apiKey;
            _checksumKey = checksumKey;
        }

        /// <summary>
        /// Tạo đơn thanh toán trực tiếp qua Cổng PayOS REST API v2
        /// </summary>
        public async Task<PaymentOrderDto> CreatePaymentOrderAsync(
            int memberId, 
            string memberCardCode, 
            string memberFullName, 
            decimal amount, 
            string actionType, 
            string customNote = null)
        {
            try
            {
                // 1. Sinh mã đơn hàng duy nhất tuyệt đối (Unix Timestamp ms + 3 số ngẫu nhiên) <= 9007199254740991
                long orderCode = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1700000000L) * 1000L + new Random().Next(100, 999);

                // 2. Nội dung chuyển khoản không dấu (Max 25 ký tự theo quy định PayOS)
                string actionPrefix = actionType == "RENEW" ? "GIAHAN" : (actionType == "FINE_PAYMENT" ? "PHAT" : "NAP");
                string desc = $"{memberCardCode.Trim()} {actionPrefix}";
                desc = Regex.Replace(desc, @"[^a-zA-Z0-9 ]", "");
                if (desc.Length > 25) desc = desc.Substring(0, 25);

                string cancelUrl = "https://libops.vn/cancel";
                string returnUrl = "https://libops.vn/success";

                // 3. Tính chữ ký HMAC-SHA256 theo chuẩn PayOS (sắp xếp theo thứ tự bảng chữ cái: amount, cancelUrl, description, orderCode, returnUrl)
                string rawSignature = $"amount={(long)amount}&cancelUrl={cancelUrl}&description={desc}&orderCode={orderCode}&returnUrl={returnUrl}";
                string signature = ComputeHmacSha256(rawSignature, _checksumKey);

                var payload = new JObject
                {
                    ["orderCode"] = orderCode,
                    ["amount"] = (long)amount,
                    ["description"] = desc,
                    ["cancelUrl"] = cancelUrl,
                    ["returnUrl"] = returnUrl,
                    ["signature"] = signature,
                    ["items"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = actionType == "RENEW" ? "Phi gia han the thu vien" : "Nap tien coc the thu vien",
                            ["quantity"] = 1,
                            ["price"] = (long)amount
                        }
                    }
                };

                using (var req = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests"))
                {
                    req.Headers.Add("x-client-id", _clientId);
                    req.Headers.Add("x-api-key", _apiKey);
                    req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
                    string respBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = JObject.Parse(respBody);
                        if (json["code"]?.ToString() == "00" && json["data"] != null)
                        {
                            var data = json["data"];
                            string bin = data["bin"]?.ToString();
                            string bankName = bin == "970422" ? "MBBank (Quân Đội)" : (bin == "970415" ? "VietinBank" : "Ngân Hàng Liên Kết");
                            return new PaymentOrderDto
                            {
                                OrderCode = orderCode,
                                MemberId = memberId,
                                MemberCardCode = memberCardCode,
                                MemberFullName = memberFullName,
                                Amount = amount,
                                Description = desc,
                                ActionType = actionType,
                                QrCodeText = data["qrCode"]?.ToString(),
                                AccountNumber = data["accountNumber"]?.ToString() ?? "00150818092005",
                                AccountName = data["accountName"]?.ToString() ?? "LE MANH DUNG",
                                BankName = bankName,
                                PaymentLinkId = data["paymentLinkId"]?.ToString(),
                                Status = "PENDING",
                                CreatedAt = DateTime.Now
                            };
                        }
                        else
                        {
                            throw new Exception(json["desc"]?.ToString() ?? "Lỗi từ PayOS API");
                        }
                    }
                    else
                    {
                        throw new Exception($"PayOS HTTP {(int)response.StatusCode}: {respBody}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể kết nối với Cổng PayOS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đơn thanh toán qua PayOS REST API (Polling)
        /// </summary>
        public async Task<string> CheckPaymentStatusAsync(long orderCode)
        {
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}"))
                {
                    req.Headers.Add("x-client-id", _clientId);
                    req.Headers.Add("x-api-key", _apiKey);

                    var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        string respBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var json = JObject.Parse(respBody);
                        if (json["code"]?.ToString() == "00" && json["data"] != null)
                        {
                            return json["data"]["status"]?.ToString().ToUpper() ?? "PENDING";
                        }
                    }
                }
            }
            catch
            {
            }

            return "PENDING";
        }

        /// <summary>
        /// Hủy đơn thanh toán khi hết thời gian chờ
        /// </summary>
        public async Task<bool> CancelPaymentOrderAsync(long orderCode, string cancellationReason = "Người dùng hủy giao dịch")
        {
            try
            {
                var payload = new JObject
                {
                    ["cancellationReason"] = cancellationReason
                };

                using (var req = new HttpRequestMessage(HttpMethod.Post, $"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}/cancel"))
                {
                    req.Headers.Add("x-client-id", _clientId);
                    req.Headers.Add("x-api-key", _apiKey);
                    req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data ?? string.Empty);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(dataBytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
