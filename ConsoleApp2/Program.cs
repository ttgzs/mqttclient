using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ;
using MQTTnet;
using MQTTnet.Client;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RunAsync().GetAwaiter();
            Console.ReadLine();
        }

        public static async Task RunAsync()
        {
            try
            {
                var factory = new MqttFactory();
                var mqttclient = factory.CreateMqttClient();

                var options = new MqttClientOptions
                {
                    ClientId = "yuanjun_ai_wangtao_tttgzs", //、、$"ttgzs:{Guid.NewGuid().ToString()}",
                    CleanSession = true,
                   // Credentials = new MqttClientCredentials() { Password = "flexem", Username = "flexem" },
                    ChannelOptions = new MqttClientTcpOptions
                    {
                       // Server = "192.168.50.92",
                        Server="liangxidong.xyz",
                        Port = 1883
                    }

                };

                mqttclient.Connected += async (sender, e) =>
                {
                    Console.WriteLine($"Connected,{e.IsSessionPresent}");
                    await mqttclient.SubscribeAsync(new TopicFilterBuilder().WithTopic("Topic/flexem/fbox/300115070033/system/MonitorData").Build());
                    //                    await mqttclient.SubscribeAsync(new TopicFilterBuilder().WithTopic("Topic/flexem/fbox/327618050578/system/MonitorData").Build());

//                    for (int i = 0; i < 5; i++)
//                    {
//                        //int i = 1;
//                        var cs = "{\"Version\":10, \"Data\":[ {\"name\":\"temp3" + i + "\",\"value\":\"" + (i + 1) + "\"}]}";
//                        await mqttclient.PublishAsync(new MqttApplicationMessageBuilder()
//                            .WithTopic("Topic/flexem/fbox/300015080059/system/WriteData")
//                            .WithPayload(cs)
//                            .WithExactlyOnceQoS()
//                            .Build());
//                    }
                    //await mqttclient.PublishAsync(new MqttApplicationMessageBuilder()
                    //    .WithTopic("Topic/flexem/fbox/300015080059/system/WriteData")
                    //    .WithPayload("Topiclist")
                    //    .WithExactlyOnceQoS()
                    //    .Build());


                    //                    await mqttclient.SubscribeAsync(new TopicFilterBuilder()
                    //                        .WithTopic("Topic/flexem/fbox/300015080059/system/Topiclist").Build());
                    //                    await mqttclient.PublishAsync(new MqttApplicationMessageBuilder()
                    //                        .WithTopic("Topic/flexem/fbox/300015080059/system/GetInfo")
                    //                        .WithPayload("Topiclist")
                    //                        .WithExactlyOnceQoS()
                    //                        .Build());
                };
                mqttclient.Disconnected += async (sender, e) =>
                {
                    Console.WriteLine($"Disconnected,{e.ClientWasConnected}");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        await mqttclient.ConnectAsync(options);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"reconnection error {exception}");

                    }
                };
                int i = 0;
                mqttclient.ApplicationMessageReceived += (sender, e) =>
                {
                    
                    Console.WriteLine($"receive msg->clientid:{e.ClientId},topic:{e.ApplicationMessage.Topic},payload:{Encoding.GetEncoding("GB2312").GetString(e.ApplicationMessage.Payload)},Qos:{e.ApplicationMessage.QualityOfServiceLevel.ToString()},Retain:{e.ApplicationMessage.Retain}");

                    Console.WriteLine($"current i is {i}");
                  
                    var cs = "{\"Version\":10, \"Data\":[ {\"name\":\"temp30\",\"value\":\"" + i + "\"}]}";
//                    mqttclient.PublishAsync(new MqttApplicationMessageBuilder()
//                       .WithTopic("Topic/flexem/fbox/327618050578/system/WriteData")
//                       .WithPayload(cs)
//                       .WithExactlyOnceQoS()
//                       .Build());
                    Task.Delay(1000);
                    mqttclient.PublishAsync(new MqttApplicationMessageBuilder()
                        .WithTopic("Topic/flexem/fbox/300115070033/system/WriteData")
                        .WithPayload(cs)
                        .WithExactlyOnceQoS()
                        .Build());
                    i++;

                };
                await mqttclient.ConnectAsync(options);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }

        }




    }
}
