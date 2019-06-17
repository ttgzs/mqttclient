using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Formatter;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private MqttFactory factory = null;
        private IMqttClient mqttclient = null;
        private MqttConfig mqttConfig = null;
        private MqttClientOptions options = null;
        private int SummaryMsgCount = 0;

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            mqttConfig = new MqttConfig();
            this.factory = new MqttFactory();
            this.mqttclient = factory.CreateMqttClient();
            this.mqttclient.ConnectedHandler = new MqttClientConnectedHandlerDelegate((e) =>
                 {
                     switch (e.AuthenticateResult.ResultCode)
                     {
                         case MqttClientConnectResultCode.Success:

                             this.lb_currentstate.Text = "连接成功";
                             insertLog($"连接成功{e.AuthenticateResult.ResultCode}");
                             break;
                         default:

                             this.lb_currentstate.Text = "连接异常";
                             insertLog($"连接异常{e.AuthenticateResult.ResultCode}");
                             break;
                     }
                     this.btn_disconnection.Enabled = true;
                     this.btn_connection.Enabled = false;
                     this.btn_getServerTopic.Enabled = true;

                 });
            this.mqttclient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate((e) =>
                  {
                      this.lb_currentstate.Text = "连接断开";
                      insertLog($"连接断开:{e.Exception?.Message}");
                      this.btn_disconnection.Enabled = false;
                      this.btn_connection.Enabled = true;
                  });
            this.mqttclient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate((e) =>
            {

                insertLog($"clientId:{e.ClientId}:topic:{e.ApplicationMessage.Topic}:Payload:{Encoding.GetEncoding("GB2312").GetString(e.ApplicationMessage.Payload)}");
                SummaryMsgCount++;
                lb_summaryReceiveMsg.Text = $"累计收到消息({SummaryMsgCount})";
            });

        }



        private void insertLog(string msg)
        {
            if (this.listBox_Msg.Items.Count > 1000)
            {
                this.listBox_Msg.Items.Clear();
            }
            else
            {
                this.listBox_Msg.Items.Insert(0, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:FFF")}:{msg}");
            }
        }

        private void btn_createclientid_Click(object sender, EventArgs e)
        {
            this.mtb_clientid.Text = Guid.NewGuid().ToString();
        }

        private void btn_connection_Click(object sender, EventArgs e)
        {
            this.mqttConfig.ServerAddress = this.mtb_serverIP.Text.Trim();
            int port = 0;
            int.TryParse(mtb_port.Text.Trim(), out port);
            this.mqttConfig.Port = port;
            this.mqttConfig.UserName = this.mtb_username.Text.Trim();
            this.mqttConfig.Password = this.mtb_password.Text.Trim();
            this.mqttConfig.ClientId = this.mtb_clientid.Text.Trim();
            if (string.IsNullOrEmpty(mqttConfig.ClientId))
            {
                MessageBox.Show("请确认客户端ID是否填写","系统提示");
                this.mtb_clientid.Focus();
                return;
                
            }

            this.mqttConfig.UserAuthentication = this.cb_userpwd.Checked;
            this.mqttConfig.HeatSpan = new TimeSpan(0, 0, int.Parse(this.numericUpDown1.Value.ToString()));
            this.mqttConfig.IsCleanSession = this.cb_clearsession.Checked;
            this.options = new MqttClientOptions
            {
                ClientId = this.mqttConfig.ClientId,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = this.mqttConfig.ServerAddress,
                    Port = this.mqttConfig.Port
                },
                CleanSession = mqttConfig.IsCleanSession,
                ProtocolVersion = MqttProtocolVersion.V311


            };
            if (this.mqttConfig.UserAuthentication)
            {
                //sha256
                
                this.options.Credentials = new MqttClientCredentials() { Password = this.mqttConfig.Password, Username = this.mqttConfig.UserName };
            }
            this.mqttclient.ConnectAsync(options);
        }

        private void btn_disconnection_ClickAsync(object sender, EventArgs e)
        {
            this.mqttclient.DisconnectAsync();
           // this.mqttclient.DisconnectAsync(new MqttClientDisconnectOptions { ReasonString = "手工断开" }).Wait();
            insertLog($"手工断开");
        }

        private void btn_addSub_Click(object sender, EventArgs e)
        {
            if (this.mqttclient.IsConnected)
            {
                this.mqttclient.SubscribeAsync(this.mtb_addsubtopic.Text.Trim());
                this.listbox_subTopic.Items.Insert(0, $"{this.mtb_addsubtopic.Text.Trim()}");
                this.lb_subtopiccount.Text = $"({this.listbox_subTopic.Items.Count})";
            }
            else
            {
                MessageBox.Show("当前连接已断开，请连接后重试", "系统提示");
            }

        }

        private void btn_unsub_Click(object sender, EventArgs e)
        {
            if (this.mqttclient.IsConnected)
            {
                if (this.listbox_subTopic.SelectedItems.Count > 0)
                {
                    List<string> topList = new List<string>();
                    for (int i = 0; i < this.listbox_subTopic.SelectedItems.Count; i++)
                    {
                        topList.Add(listbox_subTopic.SelectedItems[i].ToString());
                        listbox_subTopic.Items.Remove(listbox_subTopic.SelectedItems[i].ToString());
                    }

                    this.mqttclient.UnsubscribeAsync(new MqttClientUnsubscribeOptions() { TopicFilters = topList });
                    insertLog($"取消订阅:{string.Join(";", topList.ToArray())}");

                }
                else
                {
                    MessageBox.Show("请先选择已订阅的主题", "系统提示");
                }
            }
            else
            {
                MessageBox.Show("当前连接已断开，请连接后重试", "系统提示");
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            if (this.mqttclient.IsConnected)
            {
                for (int i = 0; i < int.Parse(this.nud_sendNum.Value.ToString()); i++)
                {
                    MqttApplicationMessageBuilder builder = new MqttApplicationMessageBuilder();
                    builder.WithPayload(this.mtb_sendmsg.Text.Trim())
                        .WithTopic(this.mtb_sendTopic.Text.Trim());
                    switch (this.cb_sendQos.SelectedText)
                    {
                        case "0":
                            builder.WithAtMostOnceQoS();
                            break;
                        case "1":
                            builder.WithAtLeastOnceQoS();
                            break;
                        case "2":
                            builder.WithExactlyOnceQoS();
                            break;
                    }

                    if (this.nud_sendDelay.Value > 0)
                    {
                        Task.Delay(new TimeSpan(0, 0, 0,0, int.Parse(this.nud_sendDelay.Value.ToString()))).Wait();
                    }


                    this.mqttclient.PublishAsync(builder.Build());
                    insertLog($"topic:{this.mtb_sendTopic.Text.Trim()},value:{this.mtb_sendmsg.Text.Trim()}");
                }

            }
            else
            {
                MessageBox.Show("当前连接已断开，请连接后重试", "系统提示");
            }
        }

        private void btn_getServerTopic_Click(object sender, EventArgs e)
        {
            //            this.listBox_serverTopic.Items.Add()
            //            Topic / flexem / fbox / 300015080059 / system / MDPCS
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / GetInfo
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / Reboot
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / MDataPubNow
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / MDataPubCycle
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / Pause
            //            emqx@127.0.0.1
            //            Topic / flexem / fbox / 300015080059 / system / WriteData
        }

        

        private void btn_clean_Click(object sender, EventArgs e)
        {
            this.listBox_Msg.Items.Clear();
            
        }

        private void btn_clean_summsgcount_Click(object sender, EventArgs e)
        {
            this.SummaryMsgCount = 0;
        }
    }

    public class MqttConfig
    {
        /// <summary>
        /// 服务地址
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// MQTT 默认端口号 1883
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 设备ID，不可重复
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 是否启用用户验证
        /// </summary>
        public bool UserAuthentication { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        public TimeSpan HeatSpan { get; set; }
        public bool IsCleanSession { get; set; }


    }
}
