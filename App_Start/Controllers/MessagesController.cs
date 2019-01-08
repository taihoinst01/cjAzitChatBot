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
        public static string luisQuery = "";
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

                            //2018-11-26:KSO:INIT Carousel 만드는부분 추가
                            if (tempcard.card_order_no > 1)
                            {
                                initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            }
                        }
                    }
                    else
                    {

                        tempAttachment = dbutil.getAttachmentFromDialog(dialogs, activity);
                        initReply.Attachments.Add(tempAttachment);

                    }
                    await connector.Conversations.SendToConversationAsync(initReply);
                }

               
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
                        luisQuery = orgMent;
                        orgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                        orgMent = orgMent.Replace(" ", "").ToLower();
                        queryStr = orgMent;
                        cacheList = db.CacheChk(orgMent.Replace(" ", ""));                     // 캐시 체크 (TBL_QUERY_ANALYSIS_RESULT 조회..)
                        //cacheList.luisIntent 초기화
                        //cacheList.luisIntent = null;

                        //smalltalk 문자 확인                        
                        String smallTalkSentenceConfirm = db.SmallTalkSentenceConfirm;

                        //smalltalk 답변이 있을경우
                        if (!string.IsNullOrEmpty(smallTalkSentenceConfirm))
                        {
                            luisId = "";
                        }
                        //luis 호출
                        else if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                        {
                            DButil.HistoryLog("cache none : " + orgMent);
                            Debug.WriteLine("cache none : " + orgMent);
                            //루이스 체크(intent를 루이스를 통해서 가져옴)
                            //cacheList.luisId = dbutil.GetMultiLUIS(orgMent);
                            //Debug.WriteLine("cacheList.luisId : " + cacheList.luisId);

                            cacheList.luisIntent = dbutil.GetMultiLUIS(luisQuery);
                            Debug.WriteLine("cacheList.luisIntent : " + cacheList.luisIntent);
                            //Debug.WriteLine("cacheList.luisEntitiesValue : " + cacheList.luisEntitiesValue);
                            cacheList = db.CacheDataFromIntent(cacheList.luisIntent);

                            luisId = cacheList.luisId;
                            luisIntent = cacheList.luisIntent;
                            luisEntities = cacheList.luisEntities;
                            luisIntentScore = cacheList.luisScore;

                        }
                        else
                        {
                            luisId = cacheList.luisId;
                            luisIntent = cacheList.luisIntent;
                            luisEntities = cacheList.luisEntities;
                            luisIntentScore = cacheList.luisScore;
                        }

                        string smallTalkConfirm = "";

                        if (!string.IsNullOrEmpty(luisIntent))
                        {
                            relationList = db.DefineTypeChkSpare(cacheList.luisIntent);
                        }
                        else
                        {
                            relationList = null;
                            //smalltalk 답변가져오기
                            if (orgMent.Length < 9)
                            {
                                smallTalkConfirm = db.SmallTalkConfirm;
                            }
                            else
                            {
                                smallTalkConfirm = "";
                            }

                        }
                       
                        if (relationList != null)
                        {
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

                                        tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity);

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

                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);
                                }

                                if (commonReply.Attachments.Count > 0)
                                {
                                    SetActivity(commonReply);
                                    replyresult = "H";
                                }
                            }
                        }
                        //SMALLTALK 확인
                        else if (!string.IsNullOrEmpty(smallTalkConfirm))
                        {
                            Debug.WriteLine("smalltalk dialogue-------------");

                            Random rand = new Random();

                            //SMALLTALK 구분
                            string[] smallTalkConfirm_result = smallTalkConfirm.Split('$');

                            int smallTalkConfirmNum = rand.Next(0, smallTalkConfirm_result.Length);

                            Activity smallTalkReply = activity.CreateReply();
                            smallTalkReply.Recipient = activity.From;
                            smallTalkReply.Type = "message";
                            smallTalkReply.Attachments = new List<Attachment>();

                            HeroCard plCard = new HeroCard()
                            {
                                Title = "",
                                Text = smallTalkConfirm_result[smallTalkConfirmNum]
                            };

                            Attachment plAttachment = plCard.ToAttachment();
                            smallTalkReply.Attachments.Add(plAttachment);

                            SetActivity(smallTalkReply);
                            replyresult = "S";

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
                            //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

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

                        int dbResult = db.insertUserQuery();

                        //}
                        //history table insert
                        db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", replyresult);
                        replyresult = "";
                        luisIntent = "";
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    //int sorryMessageCheck = db.SelectUserQueryErrorMessageCheck(activity.Conversation.Id, MessagesController.chatBotID);



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