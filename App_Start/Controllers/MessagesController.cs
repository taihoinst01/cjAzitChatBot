using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using cjAzitChatBot.DB;
using cjAzitChatBot.Models;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Web.Configuration;
using cjAzitChatBot.Dialogs;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.ConnectorEx;

namespace cjAzitChatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //MessagesController
        public static readonly string TEXTDLG = "2";
        public static readonly string CARDDLG = "3";
        public static readonly string MEDIADLG = "4";
        //public static readonly int MAXFACEBOOKCARDS = 10;

        public static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string chatBotAppID = "appID";
        public static int appID = Convert.ToInt32(rootWebConfig.ConnectionStrings.ConnectionStrings[chatBotAppID].ToString());

        //config 변수 선언
        static public string[] LUIS_NM = new string[10];        //루이스 이름
        static public string[] LUIS_APP_ID = new string[10];    //루이스 app_id
        static public string LUIS_SUBSCRIPTION = "";            //루이스 구독키
        static public int LUIS_TIME_LIMIT;                      //루이스 타임 체크
        static public string QUOTE = "";                        //견적 url
        static public string TESTDRIVE = "";                    //시승 url
        static public string BOT_ID = "";                       //bot id
        static public string MicrosoftAppId = "";               //app id
        static public string MicrosoftAppPassword = "";         //app password
        static public string LUIS_SCORE_LIMIT = "";             //루이스 점수 체크

        public static int sorryMessageCnt = 0;
        public static int chatBotID = 0;

        //public static int pagePerCardCnt = 10;
        //public static int pageRotationCnt = 0;
        //public static int fbLeftCardCnt = 0;
        //public static int facebookpagecount = 0;
        //public static string FB_BEFORE_MENT = "";

        //public static List<DeliveryData> deliveryData = new List<DeliveryData>();
        //public static List<DeliveryTypeList> deliveryTypeList = new List<DeliveryTypeList>();
        public static List<RelationList> relationList = new List<RelationList>();
        public static string luisId = "";
        public static string luisIntent = "";
        public static string luisEntities = "";
        public static string luisIntentScore = "";
        public static string dlgId = "";        
        public static string queryStr = "";
        public static DateTime startTime;

        public static CacheList cacheList = new CacheList();
        //페이스북 페이지용
        //public static ConversationHistory conversationhistory = new ConversationHistory();
        //추천 컨텍스트 분석용
        public static Dictionary<String, String> recommenddic = new Dictionary<string, String>();
        //결과 플레그 H : 정상 답변, S : SMALLTALK 답변, D : 답변 실패, F : 건의사항
        public static String replyresult = "";
        //API 플레그 COMMON : 일반
        public static String apiFlag = "";
        //public static String recommendResult = "";

        public static string channelID = "";

        public static DbConnect db = new DbConnect();
        public static DButil dbutil = new DButil();

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            //string cashOrgMent = "";

            //DbConnect db = new DbConnect();
            //DButil dbutil = new DButil();
            DButil.HistoryLog("db connect !! ");
            //HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            HttpResponseMessage response;

            Activity reply1 = activity.CreateReply();
            Activity reply2 = activity.CreateReply();
            Activity reply3 = activity.CreateReply();
            Activity reply4 = activity.CreateReply();

            // Activity 값 유무 확인하는 익명 메소드
            Action<Activity> SetActivity = (act) =>
            {
                if (!(reply1.Attachments.Count != 0 || reply1.Text != ""))
                {
                    reply1 = act;
                }
                else if (!(reply2.Attachments.Count != 0 || reply2.Text != ""))
                {
                    reply2 = act;
                }
                else if (!(reply3.Attachments.Count != 0 || reply3.Text != ""))
                {
                    reply3 = act;
                }
                else if (!(reply4.Attachments.Count != 0 || reply4.Text != ""))
                {
                    reply4 = act;
                }
                else
                {

                }
            };

            if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                startTime = DateTime.Now;
                //activity.ChannelId = "facebook";
                //파라메터 호출
                if (LUIS_NM.Count(s => s != null) > 0)
                {
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    //string[] LUIS_APP_ID = new string[10];
                    Array.Clear(LUIS_APP_ID, 0, LUIS_APP_ID.Length);
                }
                //Array.Clear(LUIS_APP_ID, 0, 10);
                DButil.HistoryLog("db SelectConfig start !! ");
                List<ConfList> confList = db.SelectConfig();
                DButil.HistoryLog("db SelectConfig end!! ");

                for (int i = 0; i < confList.Count; i++)
                {
                    switch (confList[i].cnfType)
                    {
                        case "LUIS_APP_ID":
                            LUIS_APP_ID[LUIS_APP_ID.Count(s => s != null)] = confList[i].cnfValue;
                            LUIS_NM[LUIS_NM.Count(s => s != null)] = confList[i].cnfNm;
                            break;
                        case "LUIS_SUBSCRIPTION":
                            LUIS_SUBSCRIPTION = confList[i].cnfValue;
                            break;
                        case "BOT_ID":
                            BOT_ID = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppId":
                            MicrosoftAppId = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppPassword":
                            MicrosoftAppPassword = confList[i].cnfValue;
                            break;
                        case "LUIS_SCORE_LIMIT":
                            LUIS_SCORE_LIMIT = confList[i].cnfValue;
                            break;
                        case "LUIS_TIME_LIMIT":
                            LUIS_TIME_LIMIT = Convert.ToInt32(confList[i].cnfValue);
                            break;
                        default: //미 정의 레코드
                            Debug.WriteLine("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            DButil.HistoryLog("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            break;
                    }
                }

                Debug.WriteLine("* DB conn : " + activity.Type);
                DButil.HistoryLog("* DB conn : " + activity.Type);

                //초기 다이얼로그 호출
                List<DialogList> dlg = db.SelectInitDialog(activity.ChannelId);

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                foreach (DialogList dialogs in dlg)
                {
                    Activity initReply = activity.CreateReply();
                    initReply.Recipient = activity.From;
                    initReply.Type = "message";
                    initReply.Attachments = new List<Attachment>();
                    //initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    Attachment tempAttachment;

                    if (dialogs.dlgType.Equals(CARDDLG))
                    {
                        foreach (CardList tempcard in dialogs.dialogCard)
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                            initReply.Attachments.Add(tempAttachment);
                        }
                    }
                    else
                    {
                        //if (activity.ChannelId.Equals("facebook") && string.IsNullOrEmpty(dialogs.cardTitle) && dialogs.dlgType.Equals(TEXTDLG))
                        //{
                        //    Activity reply_facebook = activity.CreateReply();
                        //    reply_facebook.Recipient = activity.From;
                        //    reply_facebook.Type = "message";
                        //    DButil.HistoryLog("facebook  card Text : " + dialogs.cardText);
                        //    reply_facebook.Text = dialogs.cardText;
                        //    var reply_ment_facebook = connector.Conversations.SendToConversationAsync(reply_facebook);
                        //    //SetActivity(reply_facebook);

                        //}
                        //else
                        //{
                            tempAttachment = dbutil.getAttachmentFromDialog(dialogs, activity);
                            initReply.Attachments.Add(tempAttachment);
                        //}
                    }
                    await connector.Conversations.SendToConversationAsync(initReply);
                }

                //현재위치사용승인 테스트
                //Activity replyLocation = activity.CreateReply();
                //replyLocation.Recipient = activity.From;
                //replyLocation.Type = "message";
                //replyLocation.Attachments = new List<Attachment>();
                //replyLocation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                //replyLocation.Attachments.Add(
                //    GetHeroCard_facebookMore(
                //    "", "",
                //    "현재 위치 사용 승인",
                //    new CardAction(ActionTypes.ImBack, "현재 위치 사용 승인", value: MessagesController.queryStr))
                //);
                //await connector.Conversations.SendToConversationAsync(replyLocation);

                DateTime endTime = DateTime.Now;
                Debug.WriteLine("프로그램 수행시간 : {0}/ms", ((endTime - startTime).Milliseconds));
                Debug.WriteLine("* activity.Type : " + activity.Type);
                Debug.WriteLine("* activity.Recipient.Id : " + activity.Recipient.Id);
                Debug.WriteLine("* activity.ServiceUrl : " + activity.ServiceUrl);

                DButil.HistoryLog("* activity.Type : " + activity.ChannelData);
                DButil.HistoryLog("* activity.Recipient.Id : " + activity.Recipient.Id);
                DButil.HistoryLog("* activity.ServiceUrl : " + activity.ServiceUrl);
            }
            else if (activity.Type == ActivityTypes.Message)
            {
                //activity.ChannelId = "facebook";
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                try
                {
                    Debug.WriteLine("* activity.Type == ActivityTypes.Message ");
                    channelID = activity.ChannelId;
                    string orgMent = activity.Text;

                    //현재위치사용승인
                    if (orgMent.Contains("current location") || orgMent.Equals("현재위치사용승인"))
                    {
                        if (!orgMent.Contains(':'))
                        {
                            //첫번쨰 메세지 출력 x
                            response = Request.CreateResponse(HttpStatusCode.OK);
                            return response;
                        }
                        else
                        {
                            //위도경도에 따른 값 출력
                            try
                            {
                                string location = orgMent.Replace("current location:", "");
                                //테스트용
                                //string location = "129.0929788:35.2686635";
                                string[] location_result = location.Split(':');
                                //regionStr = db.LocationValue(location_result[1], location_result[2]);
                                DButil.HistoryLog("*regionStr : " + location_result[0] + " " + location_result[1]);
                                Debug.WriteLine("*regionStr : " + location_result[0] + " " + location_result[1]);
                                DButil.mapSave(location_result[0], location_result[1]);
                                Activity reply_brach = activity.CreateReply();
                                reply_brach.Recipient = activity.From;
                                reply_brach.Type = "message";
                                reply_brach.Attachments = new List<Attachment>();
                                reply_brach.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                reply_brach.Attachments.Add(
                                    DButil.GetHeroCard_Map(
                                    "타이호인스트",
                                    "연락처",
                                    "주소",
                                    new CardImage(url: "https://cjAzitChatBot.azurewebsites.net/image/map/"+ location_result[1] + ","+ location_result[0] + ".png"),
                                    new CardAction(ActionTypes.OpenUrl, "타이호인스트", value: "http://www.taihoinst.com/"),
                                    location_result[1],
                                    location_result[0])
                                    );
                                var reply_brach1 = await connector.Conversations.SendToConversationAsync(reply_brach);
                                response = Request.CreateResponse(HttpStatusCode.OK);
                                return response;
                            }
                            catch
                            {
                                queryStr = "서울 시승센터";
                            }
                        }
                    }

                    apiFlag = "COMMON";

                    //대화 시작 시간
                    startTime = DateTime.Now;
                    long unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                    DButil.HistoryLog("orgMent : " + orgMent);
                    //금칙어 체크
                    CardList bannedMsg = db.BannedChk(orgMent);
                    Debug.WriteLine("* bannedMsg : " + bannedMsg.cardText);//해당금칙어에 대한 답변

                    if (bannedMsg.cardText != null)
                    {
                        Activity reply_ment = activity.CreateReply();
                        reply_ment.Recipient = activity.From;
                        reply_ment.Type = "message";
                        reply_ment.Text = bannedMsg.cardText;

                        var reply_ment_info = await connector.Conversations.SendToConversationAsync(reply_ment);
                        response = Request.CreateResponse(HttpStatusCode.OK);
                        return response;
                    }
                    else
                    {
                        //정규화
                        orgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                        orgMent = orgMent.Replace(" ", "");
                        queryStr = orgMent;
                        //cacheList = db.CacheChk(cashOrgMent.Replace(" ", ""));                     // 캐시 체크 (TBL_QUERY_ANALYSIS_RESULT 조회..)
                        //cacheList.luisIntent 초기화
                        cacheList.luisIntent = null;
                        cacheList.luisIntent = dbutil.GetMultiLUIS(orgMent);
                        //캐시에 없을 경우
                        //if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                        //{
                        //    DButil.HistoryLog("cache none : " + orgMent);
                        //    Debug.WriteLine("cache none : " + orgMent);
                        //    //루이스 체크(intent를 루이스를 통해서 가져옴)
                        //    //cacheList.luisId = dbutil.GetMultiLUIS(orgMent);
                        //    //Debug.WriteLine("cacheList.luisId : " + cacheList.luisId);

                        //    cacheList.luisIntent = dbutil.GetMultiLUIS(orgMent);
                        //    Debug.WriteLine("cacheList.luisIntent : " + cacheList.luisIntent);
                        //    //Debug.WriteLine("cacheList.luisEntitiesValue : " + cacheList.luisEntitiesValue);
                        //    cacheList = db.CacheDataFromIntent(cacheList.luisIntent);


                        //}
                        // TBL_HISTORY_QUERY 로그인정보, SMALL TALK 여부 컬럼 추가
                        // SMALL TALK 확인
                        // 1. 루이스에서 intent가 null OR intent score 낮을경우  small talk 연결
                        // 2. small talk 결과값 result s로 입력
                        // 3. 답변 3개등록, 랜덤으로 출력 


                        // 테스트용
                        // small Talk Answer 랜덤
                        //string test = "111|222|333";
                        //string[] smallTalkAnswer = test.Split('|');

                        //Random r = new Random();

                        //int smallTalkAnswerOrder = r.Next(0, smallTalkAnswer.Length);

                        //Debug.WriteLine("smallTalkAnswer[smallTalkAnswerOrder] = " + smallTalkAnswer[smallTalkAnswerOrder]);

                        luisId = cacheList.luisId;
                        luisIntent = cacheList.luisIntent;
                        luisEntities = cacheList.luisEntities;
                        luisIntentScore = cacheList.luisScore;

                        DButil.HistoryLog("luisId : " + luisId);
                        DButil.HistoryLog("luisIntent : " + luisIntent);
                        DButil.HistoryLog("luisEntities : " + luisEntities);

                        //String fullentity = db.SearchCommonEntities;
                        //DButil.HistoryLog("fullentity : " + fullentity);
                        if (apiFlag.Equals("COMMON"))
                        {
                            //relationList = db.DefineTypeChkSpare(cacheList.luisIntent, cacheList.luisEntities);
                            // INTENT 만으로 RELATION 찾기, 추후에 ENTITY와 함께 찾기
                            relationList = db.DefineTypeChkSpare(cacheList.luisIntent);
                        }
                        else
                        {
                            relationList = null;
                        }
                        //if (relationList != null)
                        ////if (relationList.Count > 0)
                        //{
                        //    DButil.HistoryLog("relationList 조건 in ");
                        //    if (relationList.Count > 0 && relationList[0].dlgApiDefine != null)
                        //    {
                        //        if (relationList[0].dlgApiDefine.Equals("api testdrive"))
                        //        {
                        //            apiFlag = "TESTDRIVE";
                        //        }
                        //        else if (relationList[0].dlgApiDefine.Equals("api quot"))
                        //        {
                        //            apiFlag = "QUOT";
                        //        }
                        //        else if (relationList[0].dlgApiDefine.Equals("api recommend"))
                        //        {
                        //            apiFlag = "RECOMMEND";
                        //        }
                        //        else if (relationList[0].dlgApiDefine.Equals("D"))
                        //        {
                        //            apiFlag = "COMMON";
                        //        }
                        //        DButil.HistoryLog("relationList[0].dlgApiDefine : " + relationList[0].dlgApiDefine);
                        //    }

                        //}
                        //else
                        //{

                        //    if (MessagesController.cacheList.luisIntent == null || apiFlag.Equals("COMMON"))
                        //    {
                        //        apiFlag = "";
                        //    }
                        //    else if (MessagesController.cacheList.luisId.Equals("cjAzitChatBot_luis_01") && MessagesController.cacheList.luisIntent.Contains("quot"))
                        //    {
                        //        apiFlag = "QUOT";
                        //    }
                        //    DButil.HistoryLog("apiFlag : " + apiFlag);
                        //}


                        if (apiFlag.Equals("COMMON") && relationList.Count > 0)
                        {

                            //context.Call(new CommonDialog("", MessagesController.queryStr), this.ResumeAfterOptionDialog);
                            dlgId = "";
                            for (int m = 0; m < MessagesController.relationList.Count; m++)
                            {
                                DialogList dlg = db.SelectDialog(MessagesController.relationList[m].dlgId);
                                dlgId += Convert.ToString(dlg.dlgId) + ",";
                                Activity commonReply = activity.CreateReply();
                                Attachment tempAttachment = new Attachment();
                                DButil.HistoryLog("dlg.dlgType : " + dlg.dlgType);

                                if (dlg.dlgType.Equals(CARDDLG))
                                {
                                    foreach (CardList tempcard in dlg.dialogCard)
                                    {
                                        //DButil.HistoryLog("tempcard.card_order_no : " + tempcard.card_order_no);
                                        //if (conversationhistory.facebookPageCount > 0)
                                        //{
                                        //    if (tempcard.card_order_no > (MAXFACEBOOKCARDS * facebookpagecount) && tempcard.card_order_no <= (MAXFACEBOOKCARDS * (facebookpagecount + 1)))
                                        //    {
                                        //        tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                        //    }
                                        //    else if (tempcard.card_order_no > (MAXFACEBOOKCARDS * (facebookpagecount + 1)))
                                        //    {
                                        //        fbLeftCardCnt++;
                                        //        tempAttachment = null;
                                        //    }
                                        //    else
                                        //    {
                                        //        fbLeftCardCnt = 0;
                                        //        tempAttachment = null;
                                        //    }
                                        //}
                                        //else if (activity.ChannelId.Equals("facebook"))
                                        //{
                                        //    DButil.HistoryLog("facebook tempcard.card_order_no : " + tempcard.card_order_no);
                                        //    if (tempcard.card_order_no <= MAXFACEBOOKCARDS && fbLeftCardCnt == 0)
                                        //    {
                                        //        tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                        //    }
                                        //    else
                                        //    {
                                        //        fbLeftCardCnt++;
                                        //        tempAttachment = null;
                                        //    }
                                        //}
                                        //else
                                        //{
                                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);
                                        //}



                                        if (tempAttachment != null)
                                        {
                                            commonReply.Attachments.Add(tempAttachment);
                                        }

                                        //2018-04-19:KSO:Carousel 만드는부분 추가
                                        if (tempcard.card_order_no > 1)
                                        {
                                            commonReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        }

                                    }
                                }
                                else
                                {
                                    //DButil.HistoryLog("* facebook dlg.dlgId : " + dlg.dlgId);
                                    DButil.HistoryLog("* activity.ChannelId : " + activity.ChannelId);


                                    //if (activity.ChannelId.Equals("facebook") && string.IsNullOrEmpty(dlg.cardTitle) && dlg.dlgType.Equals(TEXTDLG))
                                    //{
                                    //    commonReply.Recipient = activity.From;
                                    //    commonReply.Type = "message";
                                    //    DButil.HistoryLog("facebook card Text : " + dlg.cardText);
                                    //    commonReply.Text = dlg.cardText;
                                    //}
                                    //else
                                    //{                                        
                                        tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                        commonReply.Attachments.Add(tempAttachment);
                                    //}

                                }

                                if (commonReply.Attachments.Count > 0)
                                {
                                    SetActivity(commonReply);
                                    //conversationhistory.commonBeforeQustion = orgMent;
                                    replyresult = "H";

                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("no dialogue-------------");
                            string newUserID = activity.Conversation.Id;
                            //string beforeUserID = "";
                            //string beforeMessgaeText = "";
                            //string messgaeText = "";

                            Activity intentNoneReply = activity.CreateReply();

                            //if (beforeUserID != newUserID)
                            //{
                            //    beforeUserID = newUserID;
                            //    MessagesController.sorryMessageCnt = 0;
                            //}

                            var message = MessagesController.queryStr;
                            //beforeMessgaeText = message.ToString();

                            //Debug.WriteLine("SERARCH MESSAGE : " + message);

                            Activity sorryReply = activity.CreateReply();
                            sorryReply.Recipient = activity.From;
                            sorryReply.Type = "message";
                            sorryReply.Attachments = new List<Attachment>();
                            sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                            List<TextList> text = new List<TextList>();
                            text = db.SelectSorryDialogText("5");
                            for (int i = 0; i < text.Count; i++)
                            {
                                HeroCard plCard = new HeroCard()
                                {
                                    Title = text[i].cardTitle,
                                    Text = text[i].cardText
                                };

                                Attachment plAttachment = plCard.ToAttachment();
                                sorryReply.Attachments.Add(plAttachment);
                            }

                            SetActivity(sorryReply);
                            replyresult = "D";

                        }

                        DateTime endTime = DateTime.Now;
                        //analysis table insert
                        //if (rc != null)
                        //{
                        int dbResult = db.insertUserQuery();

                        //}
                        //history table insert
                        db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), luisIntent, luisEntities, luisIntentScore, dlgId, replyresult);
                        replyresult = "";
                        //recommendResult = "";
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    //int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);

                    //++MessagesController.sorryMessageCnt;

                    Activity sorryReply = activity.CreateReply();

                    sorryReply.Recipient = activity.From;
                    sorryReply.Type = "message";
                    sorryReply.Attachments = new List<Attachment>();
                    //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    List<TextList> text = new List<TextList>();
                    //if (sorryMessageCnt == 1)
                    //{
                        text = db.SelectSorryDialogText("5");
                    //}
                    //else
                    //{
                    //    text = db.SelectSorryDialogText("6");
                    //}

                    for (int i = 0; i < text.Count; i++)
                    {
                        HeroCard plCard = new HeroCard()
                        {
                            Title = text[i].cardTitle,
                            Text = text[i].cardText
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        sorryReply.Attachments.Add(plAttachment);
                    }

                    SetActivity(sorryReply);

                    DateTime endTime = DateTime.Now;
                    int dbResult = db.insertUserQuery();
                    db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), luisIntent, luisEntities, luisIntentScore, "", "E");
                    replyresult = "";
                    //recommendResult = "";
                }
                finally
                {
                    if (reply1.Attachments.Count != 0 || reply1.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply1);
                    }
                    if (reply2.Attachments.Count != 0 || reply2.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply2);
                    }
                    if (reply3.Attachments.Count != 0 || reply3.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply3);
                    }
                    if (reply4.Attachments.Count != 0 || reply4.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply4);
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            response = Request.CreateResponse(HttpStatusCode.OK);
            return response;

        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.Typing)
            {
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            return null;
        }


    }
}