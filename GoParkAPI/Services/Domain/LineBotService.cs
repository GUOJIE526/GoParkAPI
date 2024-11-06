using GoParkAPI.Controllers;
using GoParkAPI.DTO.Actions;
using GoParkAPI.DTO.Messages.Request;
using GoParkAPI.DTO.Messages;
using GoParkAPI.DTO.Webhook;
using GoParkAPI.DTO;
using GoParkAPI.Enum;
using GoParkAPI.Providers;
using static GoParkAPI.DTO.Messages.BaseMessageDto;
using static GoParkAPI.Enum.MessageEnum;
using System.Net.Http.Headers;
using System.Text;

namespace GoParkAPI.Services.Domain
{
    public class LineBotService
    {
        //messaging api channel 中的 accessToken & secret
        private readonly string channelAccessToken = "ryqtZiA6xa3TwMai/8Xqrgd7u8BRaPuw2fa/XhjG3Ij+contVfz60Uv8yuBXt4XTALlsRe2JUcTluWuSQlOhXkqvmWG27IoO8zsmdtSDa7iPOeKhh+hG5aS1Vcy5DFqQT4uaziHnsQHL8wiAoKbZ5wdB04t89/1O/w1cDnyilFU=";
        private readonly string channelSecret = "50d7c5c553b96a588eb086a7215d898d";

        //回復用戶訊息的api
        private readonly string replyMessageUri = "https://api.line.me/v2/bot/message/reply";
        //發送廣播的api
        private readonly string broadcastMessageUri = "https://api.line.me/v2/bot/message/broadcast";
        private static HttpClient client = new HttpClient(); // 負責處理HttpRequest
        private readonly JsonProvider _jsonProvider = new JsonProvider();
        private int userId = 1;


        private readonly ILogger<LineBotController> _logger;
        private readonly HttpClient _httpClient;

        public LineBotService(ILogger<LineBotController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        //解決傳送文字過長問題
        private string GetTruncatedText(EntryExitManagementDTO record)
        {
            var text = $"車牌： {record.licensePlate}\n進場 / 離場：{record.entryTime.ToString("HH:mm")}/{record.exitTime?.ToString("HH:mm")}\n停車時間：{record.totalMins}分\n費用：{record.amount}";

            // 如果字數超過 57，截斷並加上省略號
            return text.Length > 57 ? text.Substring(0, 57) + "..." : text;
        }

        //處理Line平台傳回的 webhook 事件資料。
        public void ReceiveWebhook(WebhookRequestBodyDto requestBody)
        {
            foreach (var eventObject in requestBody.Events)
            {
                switch (eventObject.Type)
                {
                    case WebhookEventTypeEnum.Message:
                        _logger.LogInformation("收到使用者傳送訊息");
                        //如果是發送訊息-------
                        if (eventObject.Message.Type == MessageTypeEnum.Text)
                        {
                            HandleTextMessage(eventObject);   //★處理文字訊息(當用戶輸入指令)
                            //ReceiveMessageWebhookEvent(eventObject);
                        }
                        break;
                    //這個先暫時不需要
                    case WebhookEventTypeEnum.Unsend:
                        Console.WriteLine($"使用者{eventObject.Source.UserId}在聊天室收回訊息");
                        break;
                    case WebhookEventTypeEnum.Follow:
                        Console.WriteLine($"使用者{eventObject.Source.UserId}將我們新增為好友");
                        break;
                    //這個暫時不需要
                    case WebhookEventTypeEnum.UnFollow:
                        Console.WriteLine($"使用者{eventObject.Source.UserId}封鎖了我們");
                        break;
                    //不需要
                    case WebhookEventTypeEnum.Join:
                        Console.WriteLine("我們被邀請進入聊天室了");
                        break;
                    //不需要
                    case WebhookEventTypeEnum.Leave:
                        Console.WriteLine("我們被聊天室踢出了");
                        break;
                    // ★當用戶選擇選單的某項目會返回此事件資料(假如action type為postback)
                    case WebhookEventTypeEnum.Postback:
                        HandlePostback(eventObject);
                        _logger.LogInformation("測試導航功能");
                        break;
                }
            }
        }

        //當用戶輸入訊息符合特定關鍵字
        private void HandleTextMessage(WebhookEventDto eventDto)
        {
            if (eventDto.Message.Text == "功能選單")
            {
                ShowMainMenu(eventDto.ReplyToken);
            }
            //之後可擴增

        }


        //★顯示功能選單(查詢預訂、月租查詢)
        private void ShowMainMenu(string replyToken)
        {
            var replyMessage = new ReplyMessageRequestDto<TemplateMessageDto<ButtonsTemplateDto>>
            {
                ReplyToken = replyToken,
                Messages = new List<TemplateMessageDto<ButtonsTemplateDto>>
                {
                    new TemplateMessageDto<ButtonsTemplateDto>
                    {
                        AltText = "請選擇服務項目",
                        Template = new ButtonsTemplateDto
                        {
                            ThumbnailImageUrl = "https://i.imgur.com/jTqLkmN.png",
                            ImageAspectRatio = TemplateImageAspectRatioEnum.Rectangle,
                            ImageSize = TemplateImageSizeEnum.Contain,
                            Title = "親愛的用戶您好，歡迎來到MyGoParking!🚗",
                            Text = "請選擇服務項目。",
                            Actions = new List<ActionDto>
                            {
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=booking_query",
                                    Label = "車位預訂查詢",
                                    DisplayText = "車位預訂查詢"
                                },
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=record_query",
                                    Label = "停車紀錄查詢",
                                    DisplayText = "停車紀錄查詢"
                                },
                                new ActionDto
                                {
                                    Type = ActionTypeEnum.Postback,
                                    Data = "action=monthly_rent_query",
                                    Label = "車位月租查詢",
                                    DisplayText = "車位月租查詢"
                                }
                            }
                        }
                    }
                }
            };
            ReplyMessageHandler(replyMessage);
        }


        // Postback 事件處理
        private void HandlePostback(WebhookEventDto eventDto)
        {
            // 解析 Postback 數據  
            //Split function會返回一個字串陣列(以下例子為以=分隔)
            try
            {
                // 記錄完整的 Postback Data 內容
                _logger.LogInformation("Postback Data: {Data}", eventDto.Postback.Data);

                // 確保 Postback.Data 不為空
                if (string.IsNullOrEmpty(eventDto.Postback.Data))
                {
                    _logger.LogError("Postback Data 為空");
                    return;
                }

                // 使用 '=' 進行分割並檢查 actionType
                string[] dataPartsEqual = eventDto.Postback.Data.Split('=');
                if (dataPartsEqual.Length > 1)
                {
                    string actionType = dataPartsEqual[1];
                    //點擊預訂查詢出現的quick reply
                    if (actionType == "booking_query")
                    {                        
                        ShowReservationQuickReply(eventDto.ReplyToken);
                        return;
                    }
                    //當前預訂查詢
                    else if (actionType == "currentReservation")
                    {
                        _logger.LogInformation("有觸發 currentReservation");
                        ShowCurrentReservation(eventDto.ReplyToken, userId);
                        return;
                    }
                    //點擊停車紀錄查詢的quick reply
                    else if (actionType == "record_query")
                    {
                        ShowRecordQuickReply(eventDto.ReplyToken);
                        return;
                    }
                    //查詢特定日期的停車紀錄
                    else if(actionType == "selectDateForRecord")
                    {
                        string date = eventDto.Postback.Params.Date;
                        _logger.LogInformation($"有從postback事件獲取{date}");

                        ShowRecordByDate(eventDto.ReplyToken, userId, date);
                        return;
                    }
                    //月租查詢
                    else if (actionType == "monthly_rent_query")
                    {
                        // ShowMonthlyRentOptions(eventDto.ReplyToken);
                        return;
                    }
                    
                }

                // 使用 '$' 進行分割並檢查 "navigate" 的導航指令
                string[] dataPartsDollar = eventDto.Postback.Data.Split('$');
                if (dataPartsDollar[0] == "navigate" && dataPartsDollar.Length >= 5)
                {
                    string latitude = dataPartsDollar[1];
                    string longitude = dataPartsDollar[2];
                    string lotName = dataPartsDollar[3];
                    string address = dataPartsDollar[4];

                    _logger.LogInformation("有觸發導航功能");
                    ShowLocation(eventDto.ReplyToken, latitude, longitude, lotName, address);
                    return;
                }
                else
                {
                    _logger.LogError("Postback Data 格式錯誤或不足以導航。Data: {Data}", eventDto.Postback.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Postback 時發生錯誤");
                throw;
            }
        }


        //當點預訂查詢會出現的Quick Reply(當前預訂、返回選單)
        private void ShowReservationQuickReply(string replyToken)
        {
            var today = DateTime.Now;
            var oneYearAgo = today.AddYears(-1);

            // 格式化日期为只包含日期的格式 (yyyy-MM-dd)
            var max = today.ToString("yyyy-MM-dd");
            var min = oneYearAgo.ToString("yyyy-MM-dd");
            var initial = today.ToString("yyyy-MM-dd");

            var replyMessage = new ReplyMessageRequestDto<TextMessageDto>
            {
                ReplyToken = replyToken,
                Messages = new List<TextMessageDto>
                {
                    new TextMessageDto
                    {
                        Text = "請選擇以下選項：",
                        QuickReply = new QuickReplyItemDto
                        {
                            Items = new List<QuickReplyButtonDto>
                            {
                                // postback action:查看當前預訂
                                new QuickReplyButtonDto {
                                    Action = new ActionDto {
                                        Type = ActionTypeEnum.Postback,
                                        Label = "當前預訂" ,
                                        Data = "action=currentReservation" ,
                                        DisplayText = "當前預訂",
                                    }
                                },
                                // message :返回功能選單
                                new QuickReplyButtonDto
                                {
                                    Action = new ActionDto
                                    {
                                        Type= ActionTypeEnum.Message,
                                        Label= "返回功能選單",
                                        Text ="功能選單"
                                    }
                                }

                            }
                        }
                    }
                }
            };
            ReplyMessageHandler(replyMessage);
        }

        //當點停車紀錄查詢會出現的quick reply(選擇日期、返回選單)
        private void ShowRecordQuickReply(string replyToken)
        {
            var today = DateTime.Now;
            var oneYearAgo = today.AddYears(-1);

            // 格式化日期为只包含日期的格式 (yyyy-MM-dd)
            var max = today.ToString("yyyy-MM-dd");
            var min = oneYearAgo.ToString("yyyy-MM-dd");
            var initial = today.ToString("yyyy-MM-dd");


            var replyMessage = new ReplyMessageRequestDto<TextMessageDto>
            {
                ReplyToken = replyToken,
                Messages = new List<TextMessageDto>
                {
                    new TextMessageDto
                    {
                        Text = "請選擇以下選項：",
                        QuickReply = new QuickReplyItemDto
                        {
                            Items = new List<QuickReplyButtonDto>
                            {                                
                                // datetime picker action:選擇日期
                                new QuickReplyButtonDto {
                                    Action = new ActionDto {
                                        Type = ActionTypeEnum.DatetimePicker,
                                        Label = "選擇日期",
                                        Data = "action=selectDateForRecord",
                                        Mode = DatetimePickerModeEnum.Date,
                                        Initial = initial,
                                        Max = max,
                                        Min = min
                                    }
                                },
                                // message :返回功能選單
                                new QuickReplyButtonDto
                                {
                                    Action = new ActionDto
                                    {
                                        Type= ActionTypeEnum.Message,
                                        Label= "返回功能選單",
                                        Text ="功能選單"
                                    }
                                }

                            }
                        }
                    }
                }
            };
            ReplyMessageHandler(replyMessage);
        }

        //顯示預訂資料
        public async Task ShowCurrentReservation(string replyToken, int userId)
        {
            try
            {
                // Step 1: 獲取當前預訂資料
                var response = await _httpClient.GetAsync($"https://localhost:7077/api/Reservations/CurrentReservations?userId={userId}");
                response.EnsureSuccessStatusCode(); // 確保狀態碼為 200

                var jsonString = await response.Content.ReadAsStringAsync(); // 先讀取內容為字符串
                var reservations = _jsonProvider.Deserialize<IEnumerable<ReservationDTO>>(jsonString); // 使用 JsonProvider解析 JSON(反序列化)

                //如果沒有資料(返回訊息:目前沒有進行中預訂)
                if (reservations == null || !reservations.Any())
                {
                    var noResultMessage = new ReplyMessageRequestDto<TextMessageDto>
                    {
                        ReplyToken = replyToken,
                        Messages = new List<TextMessageDto>
                        {
                            new TextMessageDto
                            {
                                Text = "目前沒有進行中的預訂"
                            }
                        }
                    };
                    ReplyMessageHandler(noResultMessage);
                    return;
                }


                // 如果有資料，建立輪播模板消息
                var carouselColumns = reservations.Select(res => new CarouselColumnObjectDto
                {
                    ThumbnailImageUrl = "https://i.imgur.com/o1SHCuG.png", // 每個輪播物件的圖片
                    Title = $"預訂ID: {res.resId} {res.lotName}", // 替換為實際的預訂ID
                    Text = $"車牌： {res.licensePlate}\n預訂進場時間：{res.startTime.ToString("yyyy-MM-dd HH:mm")}\n最遲進場時間：{res.validUntil.ToString("yyyy-MM-dd HH:mm")}",
                    Actions = new List<ActionDto>
                    {
                        new ActionDto
                        {
                            Type = ActionTypeEnum.Uri,
                            Label = "查看詳情",
                            Uri = $"https://medium.com/appxtech/day-15-%E8%AE%93-c-%E4%B9%9F%E5%8F%AF%E4%BB%A5%E5%BE%88-social-net-6-c-%E8%88%87-line-services-api-%E9%96%8B-flex-message-d149f20a7df6" // 替換為適合的詳情頁面URL
                        },
                        new ActionDto
                        {
                            Type = ActionTypeEnum.Postback,
                            Label = "導航到停車場",
                            Data = $"navigate${res.latitude}${res.longitude}${res.lotName}${res.location}", // 傳送導航所需的經緯度
                            DisplayText ="導航到停車場"
                        }



                    }
                }).ToList();

                var replyMessage = new ReplyMessageRequestDto<TemplateMessageDto<CarouselTemplateDto>>
                {
                    ReplyToken = replyToken,
                    Messages = new List<TemplateMessageDto<CarouselTemplateDto>>
                    {
                        new TemplateMessageDto<CarouselTemplateDto>
                        {
                            AltText = "查看當前預訂",
                            Template = new CarouselTemplateDto
                            {
                                Columns = carouselColumns
                            }
                        }
                    }
                };

                ReplyMessageHandler(replyMessage); // 發送消息
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "無法獲取當前預訂資料並發送輪播消息。");
                //await SendReplyMessage(new
                //{
                //    replyToken = replyToken,
                //    messages = new[]
                //    {
                //    new
                //    {
                //        type = "text",
                //        text = "獲取預訂資料時發生錯誤，請稍後再試。"
                //    }
                //}
                //});
            }

        }
        //點擊特定預訂可以開啟map
        public void ShowLocation(string replyToken, string latitude, string longitude, string lotName, string address)
        {
            double latitudeValue, longitudeValue;
            if (double.TryParse(latitude, out latitudeValue) && double.TryParse(longitude, out longitudeValue))
            {
                var message = new ReplyMessageRequestDto<LocationMessageDto>
                {
                    ReplyToken = replyToken,
                    Messages = new List<LocationMessageDto>
                    {
                        new LocationMessageDto
                        {
                            Title = lotName,
                            Address =address,
                            Latitude =latitudeValue,
                            Longitude= longitudeValue
                        }
                    }
                };
                ReplyMessageHandler(message);
            };
        }

        //顯示特定日期停車紀錄
        public async Task ShowRecordByDate(string replyToken, int userId, string date)
        {
            

            try
            {
                // Step 1: 獲取特定日期停車紀錄
                var response = await _httpClient.GetAsync($"https://localhost:7077/api/EntryExitManagements/RecordByDate?userId={userId}&dateString={date}");
                response.EnsureSuccessStatusCode(); // 確保狀態碼為 200

                var jsonString = await response.Content.ReadAsStringAsync(); // 先讀取內容為字符串
                _logger.LogInformation(jsonString);
                var parkingRecords = _jsonProvider.Deserialize<IEnumerable<EntryExitManagementDTO>>(jsonString); // 使用 JsonProvider解析 JSON(反序列化)

                //如果沒有資料(返回訊息:目前沒有進行中預訂)
                if (parkingRecords == null || !parkingRecords.Any())
                {
                    var noResultMessage = new ReplyMessageRequestDto<TextMessageDto>
                    {
                        ReplyToken = replyToken,
                        Messages = new List<TextMessageDto>
                        {
                            new TextMessageDto
                            {
                                Text = $"{date}沒有停車紀錄"
                            }
                        }
                    };
                    ReplyMessageHandler(noResultMessage);
                    return;
                }


                // 如果有資料，建立輪播模板消息
                var carouselColumns = parkingRecords.Take(10).Select(record => new CarouselColumnObjectDto
                {
                    ThumbnailImageUrl = "https://i.imgur.com/cdj6a0F.png", // 每個輪播物件的圖片
                    Title = $"#{record.entryexitId} {record.lotName}", 
                    Text = GetTruncatedText(record),
                    Actions = new List<ActionDto>
                    {   //目前未設定
                        new ActionDto
                        {
                            Type = ActionTypeEnum.Uri,
                            Label = "查看詳情",
                            Uri = $"https://medium.com/appxtech/day-15-%E8%AE%93-c-%E4%B9%9F%E5%8F%AF%E4%BB%A5%E5%BE%88-social-net-6-c-%E8%88%87-line-services-api-%E9%96%8B-flex-message-d149f20a7df6" // 替換為適合的詳情頁面URL
                        }

                    }
                }).ToList();
                _logger.LogInformation($"Generated {carouselColumns.Count} carousel items.");
                

                var replyMessage = new ReplyMessageRequestDto<TemplateMessageDto<CarouselTemplateDto>>
                {
                    ReplyToken = replyToken,
                    Messages = new List<TemplateMessageDto<CarouselTemplateDto>>
                    {
                        new TemplateMessageDto<CarouselTemplateDto>
                        {
                            AltText = $"查看{date}之停車紀錄",
                            Template = new CarouselTemplateDto
                            {
                                Columns = carouselColumns
                            }
                        }
                    }
                };
                _logger.LogInformation("Sending reply message...");
                ReplyMessageHandler(replyMessage); //發送消息
                _logger.LogInformation("Reply message sent.");


                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "無法獲取停車紀錄資料並發送輪播消息。");
                //await SendReplyMessage(new
                //{
                //    replyToken = replyToken,
                //    messages = new[]
                //    {
                //    new
                //    {
                //        type = "text",
                //        text = "獲取預訂資料時發生錯誤，請稍後再試。"
                //    }
                //}
                //});
            }

        }


        //---------以下為傳訊機制(Reply、BroadCast)

        //---------建立Reply(發送訊息-自動回復)機制
        /// 接收到回覆請求時，將請求傳至 Line 前多一層處理
        /// <param name="messageType"></param>
        /// <param name="requestBody"></param>
        public void ReplyMessageHandler<T>(ReplyMessageRequestDto<T> requestBody)
        {
            ReplyMessage(requestBody);
        }

        /// <summary>
        /// 將回覆訊息請求送到 Line
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>    
        public async void ReplyMessage<T>(ReplyMessageRequestDto<T> request)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken); //帶入 channel access token
            var json = _jsonProvider.Serialize(request);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(replyMessageUri),
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await client.SendAsync(requestMessage);

                // 檢查 HTTP 回應狀態碼
                if (response.IsSuccessStatusCode)
                {
                    // 如果回應成功，輸出成功訊息
                    _logger.LogInformation("Reply message sent successfully.");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    // 如果回應失敗，輸出錯誤訊息
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error sending message. Status code: {response.StatusCode}. Response: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // 捕捉並處理任何異常
                _logger.LogError($"Exception occurred while sending message: {ex.Message}");
                Console.WriteLine($"Exception: {ex.Message}");
            }

        }




        //---------建立Broadcast(廣播) 機制
        /// 接收到廣播請求時，將請求傳至 Line 前多一層處理，依據收到的 messageType 將 messages 轉換成正確的型別，這樣 Json 轉換時才能正確轉換。
        /// <param name="messageType"></param>
        /// <param name="requestBody"></param>
        public void BroadcastMessageHandler(string messageType, object requestBody)
        {
            //確保後續反序列化操作，先將內容轉為字串(統一格式)
            string strBody = requestBody.ToString();
            dynamic messageRequest = new BroadcastMessageRequestDto<BaseMessageDto>();
            //*dynamic 是 C# 中的一種特殊型別，允許在編譯時跳過型別檢查，改為在執行期動態決定物件的型別

            switch (messageType)
            {
                //若廣播訊息為文字
                case MessageTypeEnum.Text:
                    messageRequest = _jsonProvider.Deserialize<BroadcastMessageRequestDto<TextMessageDto>>(strBody);
                    break;
                //若廣播訊息為貼圖
                case MessageTypeEnum.Sticker:
                    messageRequest = _jsonProvider.Deserialize<BroadcastMessageRequestDto<StickerMessageDto>>(strBody);
                    break;
                //若廣播訊息為連結
                case MessageTypeEnum.Image:
                    messageRequest = _jsonProvider.Deserialize<BroadcastMessageRequestDto<ImageMessageDto>>(strBody);
                    break;
                //若廣播訊息為位置
                case MessageTypeEnum.Location:
                    messageRequest = _jsonProvider.Deserialize<BroadcastMessageRequestDto<LocationMessageDto>>(strBody);
                    break;
            }
            BroadcastMessage(messageRequest);

        }

        /// 將廣播訊息請求送到 Line
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        public async void BroadcastMessage<T>(BroadcastMessageRequestDto<T> request)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken); //帶入 channel access token
            var json = _jsonProvider.Serialize(request);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(broadcastMessageUri),
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(requestMessage);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
