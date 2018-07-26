﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Huobi.Market.WebSocketAPI
{
    public static class HuobiMarket
    {
        #region 私有属性
        private static WebSocket websocket;
        private static Dictionary<string, string> topicDic = new Dictionary<string, string>();
        private static bool isOpened = false;
        private const string HUOBI_WEBSOCKET_API = "wss://api.huobi.pro/ws";
        #endregion

        public const string MARKET_KLINE = "market.{0}.kline.{1}";
        public const string MARKET_DEPTH = "market.{0}.depth.{1}";
        public const string MARKET_TRADE_DETAIL = "market.{0}.trade.detail";
        public const string MARKET_DETAIL = "market.{0}.detail";
        public static event EventHandler<HuoBiMessageReceivedEventArgs> OnMessage;
        public static bool IsHandClose = false;
        /// <summary>
        /// 初始化WebSocket
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            try
            {
                if (websocket == null)
                {
                    websocket = new WebSocket(HUOBI_WEBSOCKET_API);

                    websocket.Error += (sender, e) =>
                    {
                        Console.WriteLine("Error:" + e.Exception.Message.ToString());
                    };
                    websocket.DataReceived += (ReceviedMsg);
                    websocket.Opened += OnOpened;
                    //websocket.Closed += OnClosed;
                }

                websocket.Open();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message);
            }
            return true;
        }
        public static void OnClosed(object sender, EventArgs e)
        {
            if (!IsHandClose)
            {
                Thread.Sleep(10000);
                Init();
            }
            IsHandClose = false;
        }

        #region Opened&心跳响应&触发消息事件
        public static void OnOpened(object sender, EventArgs e)
        {
            Console.WriteLine($"OnOpened Topics Count:{topicDic.Count}");
            isOpened = true;
            foreach (var item in topicDic)
            {
                SendSubscribeTopic(item.Value);
            }

        }
        public static void Send(string msg)
        {
            websocket.Send(msg);
        }
        /// <summary>
        /// 响应心跳包
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void ReceviedMsg(object sender, DataReceivedEventArgs args)
        {
            var msg = GZipHelper.GZipDecompressString(args.Data);
            //if (msg.IndexOf("ping") != -1) //响应心跳包
            //{
            //    var reponseData = msg.Replace("ping", "pong");
            //    websocket.Send(reponseData);
            //}
            //else//接收消息
            //{
            //    OnMessage?.Invoke(null, new HuoBiMessageReceivedEventArgs(msg));
            //}
            OnMessage?.Invoke(null, new HuoBiMessageReceivedEventArgs(msg));

        }
        #endregion


        #region 订阅相关
        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="id"></param>
        public static void Subscribe(string topic, string id)
        {
            if (topicDic.ContainsKey(topic))
                return;
            var msg = $"{{\"sub\":\"{topic}\",\"id\":\"{id}\"}}";
            topicDic.Add(topic, msg);
            if (isOpened)
            {
                SendSubscribeTopic(msg);
            }
        }


        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="id"></param>

        public static void UnSubscribe(string topic, string id)
        {
            if (!topicDic.ContainsKey(topic) || !isOpened)
                return;
            var msg = $"{{\"unsub\":\"{topic}\",\"id\":\"{id}\"}}";
            topicDic.Remove(topic);
            SendSubscribeTopic(msg);
            Console.WriteLine($"UnSubscribed, Topics Count:{topicDic.Count}");

        }
        private static void SendSubscribeTopic(string msg)
        {
            websocket.Send(msg);
            Console.WriteLine(msg);
        }
        #endregion

   
    }
    public class HuoBiMessageReceivedEventArgs : EventArgs
    {
        public HuoBiMessageReceivedEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; set; }
    }
}
