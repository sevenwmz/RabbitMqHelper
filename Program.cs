
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

RabbitMq rabbitMq = new RabbitMq(new RabbitMqRegisterModel
{
    HostName = "127.0.0.1", //ip
    Port = 5672, // 端口
    UserName = "guest", // 账户
    Password = "guest", // 密码
    VirtualHost = "/"   // 虚拟主机
});
IModel channel = rabbitMq.CreateChannel();

#region 生产者

#region 点对点
//for (int i = 0; i < 5; i++)
//{
//    rabbitMq.NomarlSend(s =>
//    {
//        s.QueueName = "Nomal";
//        s.RoutingKey = "Nomal";
//        s.Data = "send message" + i;
//        s.PublisherConfirmType = PublisherConfirmType.ListenerConfirm;
//        s.ErrorConfirmCallback = (code, info, msg) => Console.WriteLine($"{code},{info},{Encoding.UTF8.GetString(msg)}");
//    });
//    Console.WriteLine("send message");
//}
#endregion

#region workSend
//for (int i = 0; i < 10; i++)
//{
//    rabbitMq.WokerSend(s =>
//    {
//        s.QueueName = "worker";
//        s.RoutingKey = "worker";
//        s.Data = "send message" + i;
//        s.QueueDurable = true;
//        //死信队列过期时间5秒
//        s.DLXTTL = 5;
//        s.DLXPublisher = new RabbitMqPublisher
//        {
//            ExchangeName = "dlx.exchange",
//            ExchangeDurable = true,
//            RoutingKey = "dlx.queue",
//            DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("dlx_queue1", "") },
//            DlxOrBackExchangeMQType = DlxOrBackExchangeMQType.FanOut
//        };
//    });
//    Thread.Sleep(100);
//    Console.WriteLine("send message" + i);
//}
//Console.WriteLine("Start work send");
#endregion

#region FunoutSend
//for (int i = 0; i < 30; i++)
//{
//    rabbitMq.FanoutSend(s =>
//    {
//        s.ExchangeName = "fanout_exchange";
//        s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("fanout_queue1", ""), ("fanout_queue2", ""), ("fanout_queue3", "") };
//        s.Data = "funout send message" + i;
//        s.PublisherConfirmType = PublisherConfirmType.ListenerConfirm;

//        ////死信队列过期时间5秒
//        //DLXTTL = 5,
//        //DlxOrBackExchangeMQType = DlxOrBackExchangeMQType.FanOut,
//        //DLXPublisher = new RabbitMqPublisher
//        //{
//        //    ExchangeName = "dlx.exchange",
//        //    RoutingKey = "dlx.queue",
//        //    DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("dlx.route","")},
//        //    ErrorConfirmCallback = (code, info, msg) => Console.WriteLine($"{code},{info},{Encoding.UTF8.GetString(msg)}"),
//        //}
//    });
//}
//Console.WriteLine("Start FunoutSend send");
#endregion

#region Direct
//for (int i = 0; i < 10; i++)
//{
//    string data = "routing green  send message" + i;
//    rabbitMq.DirectRoutingSend(s =>
//    {
//        s.ExchangeName = "direct_exchange";
//        s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("direct_queue1", "red"), ("direct_queue2", "yellow"), ("direct_queue3", "green") };
//        s.Data = data;
//        s.RoutingKey = "green";
//        s.PublisherConfirmType = PublisherConfirmType.ListenerConfirm;
//        s.ErrorConfirmCallback = (code, info, msg) => Console.WriteLine($"{code},{info},{Encoding.UTF8.GetString(msg)}");
//    });
//    Console.WriteLine(data);
//}
//Console.WriteLine("Start RoutingSend send");
#endregion

#region Topic
//for (int i = 0; i < 10; i++)
//{
//    string data = "routing user.data.update 9999999999999999  send message" + i;
//    rabbitMq.TopicSend(s =>
//    {
//        s.ExchangeName = "topic_exchange";
//        s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("topic_queue1", "user.data.#"), ("topic_queue2", "user.data.delete") };
//        s.Data = data;
//        s.RoutingKey = "user.data.update";
//        //优先级
//        s.Priority = 9;
//        //消息确认模式
//        s.PublisherConfirmType = PublisherConfirmType.ListenerConfirm;
//        //消息确认模式回调
//        s.ErrorConfirmCallback = (code, info, msg) => Console.WriteLine($"{code},{info},{Encoding.UTF8.GetString(msg)}");
//    });
//    Console.WriteLine(data);
//}
//Console.WriteLine("Start Topic send");
#endregion


#region DelaySend
//for (int i = 0; i < 10; i++)
//{
//    rabbitMq.NomarlSend(s =>
//    {
//        s.QueueName = "delay_queue";
//        s.RoutingKey = "delay_queue";
//        s.Data = "send message" + i;

//        //死信队列过期时间5秒
//        s.DLXTTL = 12;
//        s.Expires = 25;
//        s.DLXPublisher = new RabbitMqPublisher
//        {
//            ExchangeName = "exchange-direct",
//            RoutingKey = "routing-delay",
//            QueueName = "recv_delay_queue",
//        };
//    });
//    Thread.Sleep(100);
//    Console.WriteLine("send message" + i);
//}
//Console.WriteLine("Start work send");
#endregion

#region BackExchangeSend
//for (int i = 0; i < 10; i++)
//{
//    string data = "routing green  send message" + i;

//    rabbitMq.DirectRoutingSend(s =>
//    {
//        s.ExchangeName = "STEVEN_EXCHANGE";
//        s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("STEVEN_QUEUE", "STEVEN_ROUTEKEY"), ("STEVEN_QUEUE1", "STEVEN_ROUTEKEY1"), ("STEVEN_QUEUE2", "STEVEN_ROUTEKEY2") };
//        s.Data = data;
//        s.RoutingKey = "STEVEN_ROUTEKEY2";
//        if (i % 2 == 0)
//        {
//            s.RoutingKey = "asdadsa";//错误RotingKey
//        }
//        //PublisherConfirmType = PublisherConfirmType.ListenerConfirm,
//        //ErrorConfirmCallback = (code, info, msg) => Console.WriteLine($"{code},{info},{Encoding.UTF8.GetString(msg)}"),
//        s.BackExchangePublisher = new RabbitMqPublisher
//        {
//            ExchangeName = "BACKUP_EXCHNAGE",
//            DlxOrBackExchangeMQType = DlxOrBackExchangeMQType.FanOut,
//            DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("BACKUP_QUEUE", ""), ("BACKUP_QUEUE1", ""), ("BACKUP_QUEUE2", "") },
//        };
//    });
//    Console.WriteLine(data);
//}
//Console.WriteLine("Start RoutingSend send");
#endregion

#endregion

#region 消费者

#region 点对点
//{
//    // 消费者消费是队列中消息
//    string queueName = "Nomal";
//    var connection = factory.CreateConnection();
//    {
//        var channel = connection.CreateModel();
//        {
//            channel.QueueDeclare(queueName, false, false, false, null);
//            var consumer = new EventingBasicConsumer(channel);
//            consumer.Received += (model, ea) => {
//                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
//                Console.WriteLine(" Normal Received => {0}", message);
//            };
//            channel.BasicConsume(queueName, true, consumer);
//        }
//    }
//}
//rabbitMq.NomarReceive(s =>
//{
//    s.QueueName = "Nomal";
//    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
//});
//Console.WriteLine("Start NomarReceive");
#endregion

#region 工作模式
//rabbitMq.WorkerReceive(s =>
//{
//    s.QueueDurable = true;
//    s.QueueName = "worker";
//    s.RoutingKey = "worker";
//    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));

//    //死信队列过期时间5秒
//    s.DLXTTL = 5;
//    s.DLXPublisher = new RabbitMqPublisher
//    {
//        ExchangeName = "dlx.exchange",
//        ExchangeDurable = true,
//        RoutingKey = "dlx.queue",
//        DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("dlx_queue1", "") },
//        DlxOrBackExchangeMQType = DlxOrBackExchangeMQType.FanOut
//    };


//});
//Console.WriteLine("Start work received");
#endregion

#region 扇形模式
//var connection = factory.CreateConnection(list);
//{
//    var channel = connection.CreateModel();
//    {
//        string fanoutExchange = "fanout_exchange";
//        //申明exchange
//        channel.ExchangeDeclare(exchange: fanoutExchange, type: "fanout", true);
//        // 创建队列
//        string queueName1 = "fanout_queue1";
//        channel.QueueDeclare(queueName1, true, false, false, null);
//        string queueName2 = "fanout_queue2";
//        channel.QueueDeclare(queueName2, true, false, false, null);
//        string queueName3 = "fanout_queue3";
//        channel.QueueDeclare(queueName3, true, false, false, null);
//        // 绑定到交互机
//        channel.QueueBind(queue: queueName1, exchange: fanoutExchange, routingKey: "");
//        channel.QueueBind(queue: queueName2, exchange: fanoutExchange, routingKey: "");
//        channel.QueueBind(queue: queueName3, exchange: fanoutExchange, routingKey: "");


//        //声明consumer
//        var consumer = new EventingBasicConsumer(channel);
//        //绑定消息接收后的事件委托
//        consumer.Received += (model, ea) =>
//        {
//            var body = ea.Body;
//            var message = Encoding.UTF8.GetString(body.ToArray());
//            Console.WriteLine("[x] {0}", message);
//        };
//        channel.BasicConsume(queue: queueName1, autoAck: true, consumer: consumer);
//    }
//}
//Console.WriteLine("Start 扇形队列模式 received");

rabbitMq.FanoutReceive(s =>
{
    s.ExchangeName = "fanout_exchange";
    s.QueueName = "fanout_queue1";
    s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("fanout_queue1", ""), ("fanout_queue2", ""), ("fanout_queue3", "") };
    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
});
Console.WriteLine("Start FunoutReceive received");
#endregion

#region 直连路由模式
//var connection = factory.CreateConnection();
//var channel = connection.CreateModel();
//channel.ExchangeDeclare(exchange: "direct_exchange", type: "direct");
//var queueName = "direct_queue1";
//channel.QueueDeclare(queueName, false, false, false, null);
//channel.QueueBind(queue: queueName,
//                          exchange: "direct_exchange",
//                          routingKey: "red");
//channel.QueueBind(queue: queueName,
//                          exchange: "direct_exchange",
//                          routingKey: "yellow");
//channel.QueueBind(queue: queueName,
//                          exchange: "direct_exchange",
//                          routingKey: "green");

////消息接收方，是只关注队列，不关心 路由 key
//var consumer = new EventingBasicConsumer(channel);
//consumer.Received += (model, ea) =>
//{
//    var body = ea.Body;
//    var message = Encoding.UTF8.GetString(body.ToArray());
//    var routingKey = ea.RoutingKey;
//    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);

//    // 消费完成后需要手动签收消息，如果不写该代码就容易导致重复消费问题
//    channel.BasicAck(ea.DeliveryTag, true); // 可以降低每次签收性能损耗
//};

//// 消息签收模式
//// 手动签收 保证正确消费，不会丢消息(基于客户端而已)
//// 自动签收 容易丢消息 
//// 签收：意味着消息从队列中删除
//channel.BasicConsume(queue: queueName,
//                     autoAck: false,
//                     consumer: consumer);

//rabbitMq.DirectRoutingReceive(s =>
//{
//    s.ExchangeName = "direct_exchange";
//    s.QueueName = "direct_queue3";
//    s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("direct_queue1", "red"), ("direct_queue2", "yellow"), ("direct_queue3", "green") };
//    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
//    s.AutoAck = false;
//});
//Console.WriteLine("Start RoutingReceive received");
#endregion

#region 主题模式
//var connection = factory.CreateConnection();
//var channel = connection.CreateModel();
//var queueName = "topic_queue1";
//channel.ExchangeDeclare(exchange: "topic_exchange", type: "topic");
//channel.QueueDeclare(queueName, false, false, false, null);
//// 有个bug
//channel.QueueBind(queue: queueName,
//                          exchange: "topic_exchange",
//                          routingKey: "user.data.insert");


//var consumer = new EventingBasicConsumer(channel);
//consumer.Received += (model, ea) =>
//{
//    var body = ea.Body;
//    var message = Encoding.UTF8.GetString(body.ToArray());
//    var routingKey = ea.RoutingKey;
//    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);
//};

//channel.BasicConsume(queue: queueName,
//                     autoAck: true,
//                     consumer: consumer);




rabbitMq.TopicReceive(s =>
{
    s.ExchangeName = "topic_exchange";
    s.QueueName = "topic_queue1";
    s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("topic_queue1","user.data.#"), ("topic_queue2", "user.data.delete"),
                                                 ("topic_queue3", "user.data.update") };
    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
    s.AutoAck = false;
    s.Priority = 9;
});
Console.WriteLine("Start Topic Receive received");
#endregion

#region 消息确认机制

//Console.WriteLine("消息发送确认机制");

//var conn = factory.CreateConnection();
//{
//    var channel = conn.CreateModel();
//    {
//        channel.ExchangeDeclare("confirm-exchange", ExchangeType.Direct, true);
//        channel.QueueDeclare("confirm-queue", true, false, false, null);
//        channel.QueueBind("confirm-queue", "confirm-exchange", ExchangeType.Direct, null);
//        var properties = channel.CreateBasicProperties();
//        //properties.DeliveryMode = 2;
//        properties.Persistent = true;
//        byte[] message = Encoding.UTF8.GetBytes("测试、Confirm");
//        //方式1：普通confirm 
//        //NormalConfirm(channel, null, message);
//        //方式2：批量confirm
//        //BatchConfirm(channel, null, message);
//        //方式3：异步确认Ack
//        ListenerConfirm(channel, properties, message);
//    }
//}


///// <summary>
///// 方式1：普通confirm模式
///// 每发送一条消息后，调用waitForConfirms()方法，等待服务器端confirm。实际上是一种串行confirm了。
///// </summary>
///// <param name="channel"></param>
///// <param name="properties"></param>
///// <param name="message"></param>
//static void NormalConfirm(IModel channel, IBasicProperties properties, byte[] message)
//{
//    channel.ConfirmSelect(); // 开启消息确认模式
//    channel.BasicPublish("confirm-exchange", ExchangeType.Fanout, properties, message);
//    // 消息到达服务端队列中才返回结果
//    if (!channel.WaitForConfirms())
//    {
//        Console.WriteLine("消息发送失败了。");
//        return;
//    }
//    Console.WriteLine("消息发送成功！");
//    channel.Close();
//}

///// <summary>
///// 方式2：批量confirm模式
///// 每发送一批消息后，调用waitForConfirms()方法，等待服务器端confirm。
///// </summary>
///// <param name="channel"></param>
///// <param name="properties"></param>
///// <param name="message"></param>
//static void BatchConfirm(IModel channel, IBasicProperties properties, byte[] message)
//{
//    channel.ConfirmSelect();
//    for (int i = 0; i < 10; i++)
//    {
//        channel.BasicPublish("confirm-exchange", ExchangeType.Fanout, properties, message);
//    }
//    if (!channel.WaitForConfirms())
//    {
//        Console.WriteLine("消息发送失败了。");
//    }
//    Console.WriteLine("消息发送成功！");
//    channel.Close();
//}

///// <summary>
///// 使用异步回调方式监听消息是否正确送达
///// </summary>
///// <param name="channel"></param>
///// <param name="properties"></param>
///// <param name="message"></param>
//static void ListenerConfirm(IModel channel, IBasicProperties properties, byte[] message)
//{
//    // properties.DeliveryMode = 2;
//    channel.ConfirmSelect();//开启消息确认模式
//    /*-------------Return机制：不可达的消息消息监听--------------*/
//    //这个事件就是用来监听我们一些不可达的消息的内容的：比如某些情况下交换机没有绑定到队列的情况下
//    EventHandler<BasicReturnEventArgs> evreturn = new((o, basic) =>
//    {
//        var rc = basic.ReplyCode; //消息失败的code
//        var rt = basic.ReplyText; //描述返回原因的文本。
//        var msg = Encoding.UTF8.GetString(basic.Body.Span); //失败消息的内容
//                                                            //在这里我们可能要对这条不可达消息做处理，比如是否重发这条不可达的消息呀，或者这条消息发送到其他的路由中等等
//        System.IO.File.AppendAllText("d:/return.txt", "调用了Return;ReplyCode:" + rc + ";ReplyText:" + rt + ";Body:" + msg);
//        Console.WriteLine("send message failed,不可达的消息消息监听.");
//    });
//    channel.BasicReturn += evreturn;
//    //消息发送成功的时候进入到这个事件：即RabbitMq服务器告诉生产者，我已经成功收到了消息
//    EventHandler<BasicAckEventArgs> BasicAcks = new((o, basic) =>
//    {
//        Console.WriteLine("abbitMq服务器告诉生产者，我已经成功收到了消息");
//    });
//    /*//消息发送失败的时候进入到这个事件：即RabbitMq服务器告诉生产者，你发送的这条消息我没有成功的投递到Queue中，或者说我没有收到这条消息。
//    EventHandler<BasicNackEventArgs> BasicNacks = new ((o, basic) =>
//    {
//        //MQ服务器出现了异常，可能会出现Nack的情况
//        Console.WriteLine("send message fail,Nacks.");
//    });*/
//    channel.BasicAcks += BasicAcks;
//    /* channel.BasicNacks += BasicNacks;*/

//    channel.BasicPublish("confirm-exchange", ExchangeType.Direct, true, properties, message);
//}
#endregion

#region 优先级队列
//Console.WriteLine("优先级队列接收");
//string exchange = "pro.exchange";
//string queueName = "pro.queue";
//var connection = factory.CreateConnection();
//{
//    var channel = connection.CreateModel();
//    {
//        channel.ExchangeDeclare(exchange, type: ExchangeType.Fanout, durable: true, autoDelete: false);
//        //x-max-priority属性必须设置，否则消息优先级不生效
//        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> { { "x-max-priority", 10 } });
//        channel.QueueBind(queueName, exchange, queueName);

//        channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);
//        EventingBasicConsumer consumer = new EventingBasicConsumer(channel);

//        consumer.Received += (model, ea) =>
//        {
//            string msgId = ea.BasicProperties.MessageId;
//            byte[] body = ea.Body.ToArray();
//            string message = Encoding.UTF8.GetString(body);
//            Console.WriteLine(message);
//            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
//        };

//        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
//    }
//}
#endregion

#region 延迟队列模式
//var connection = factory.CreateConnection();
//{
//    var channel = connection.CreateModel();
//    {
//        channel.ExchangeDeclare(exchange: "exchange-direct", type: "direct");
//        // 通过api自动产生队列名字
//        string name = channel.QueueDeclare().QueueName;
//        channel.QueueBind(queue: name, exchange: "exchange-direct", routingKey: "routing-delay");

//        //回调，当consumer收到消息后会执行该函数
//        var consumer = new EventingBasicConsumer(channel);
//        consumer.Received += (model, ea) =>
//        {
//            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
//            Console.WriteLine(ea.RoutingKey);
//            Console.WriteLine(" [x] Received {0}", message);
//        };

//        //消费队列"hello"中的消息
//        channel.BasicConsume(queue: name,
//                             autoAck: true,
//                             consumer: consumer);
//    }
//}


//rabbitMq.DirectRoutingReceive(s =>
//{
//    s.ExchangeName = "exchange-direct";
//    s.DeclareQueuesAndRoutingKeys = new List<(string, string)> { ("recv_delay_queue", "routing-delay") };
//    s.QueueName = "recv_delay_queue";
//    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
//    s.AutoAck = false;
//});
//Console.WriteLine("Start 延迟队列模式 received");
#endregion
#endregion

Console.ReadLine();