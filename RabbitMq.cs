using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ;

public class RabbitMq
{
    #region Field Area
    /// <summary>
    /// 工厂对象
    /// </summary>
    private ConnectionFactory _connectionFactory;
    /// <summary>
    /// 连接对象
    /// </summary>
    private IConnection _connection;
    #endregion

    #region CTOR

    /// <summary>
    /// 建议单例
    /// </summary>
    /// <param name="mqModel"></param>
    public RabbitMq(RabbitMqRegisterModel mqModel)
    {
        //单例
        _connectionFactory = new ConnectionFactory()
        {
            HostName = mqModel.HostName,
            Port = mqModel.Port,
            UserName = mqModel.UserName,
            Password = mqModel.Password,
            VirtualHost = mqModel.VirtualHost,
        };
        _connection = mqModel.UseCluster
                ? _connectionFactory.CreateConnection(mqModel.ClusterNodes)
                : _connectionFactory.CreateConnection()
                ;
    }
    #endregion

    #region 通用方法

    #region 队列交换机等基础
    /// <summary>
    /// 通过连接对象获取Channel对象
    /// </summary>
    /// <returns></returns>
    public IModel CreateChannel() => _connection.CreateModel();
    /// <summary>
    /// 交换机声明
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitBasic"></param>
    /// <param name="mqType"></param>
    /// <returns></returns>
    private IModel ExchangeDeclare(IModel channel, RabbitMqBase rabbitBasic, string mqType)
    {
        // 声明Direct交换机
        channel.ExchangeDeclare(exchange: rabbitBasic.ExchangeName,
                                type: mqType,
                                durable: rabbitBasic.ExchangeDurable,
                                autoDelete: rabbitBasic.ExchangeAutoDelete,
                                arguments: rabbitBasic.ExchangeArguments);
        return channel;
    }
    /// <summary>
    /// 队列声明
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitBasic"></param>
    /// <returns></returns>
    private IModel QueueDeclare(IModel channel, RabbitMqBase rabbitBasic)
    {
        channel.QueueDeclare(queue: rabbitBasic.DeclareQueuesAndRoutingKeys.First().queueName,
                               durable: rabbitBasic.QueueDurable,
                               exclusive: rabbitBasic.QueueExclusive,
                               autoDelete: rabbitBasic.QueueAutoDelete,
                               arguments: rabbitBasic.QueueArguments);
        return channel;
    }
    /// <summary>
    /// 队列绑定
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitBasic"></param>
    /// <returns></returns>
    private IModel QueueBind(IModel channel, RabbitMqBase rabbitBasic)
    {
        channel.QueueBind(queue: rabbitBasic.DeclareQueuesAndRoutingKeys.First().queueName,
                              exchange: rabbitBasic.ExchangeName,
                              routingKey: rabbitBasic.RoutingKey);
        return channel;
    }
    /// <summary>
    /// 接收处理
    /// </summary>
    /// <param name="rabbitMqConsumer"></param>
    /// <param name="channel"></param>
    private void Received(RabbitMqConsumer rabbitMqConsumer, IModel channel)
    {
        //设置prefetchCount : 1来告知RabbitMQ，在未收到消费端的消息确认时，不再分发消息，
        //也就确保了当消费端处于忙碌状态时，不再分配任务。
        channel.BasicQos(prefetchSize: rabbitMqConsumer.QosPrefetchSize,
                        prefetchCount: rabbitMqConsumer.QosPrefetchCount,
                        global: rabbitMqConsumer.QosGlobal); //能者多劳
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            if (rabbitMqConsumer.ReceiveByteCallback != null)
            {
                rabbitMqConsumer.ReceiveByteCallback(ea.Body.ToArray());
            }
            if (rabbitMqConsumer.ReceiverCallback != null)
            {
                rabbitMqConsumer.ReceiverCallback(ea);
            }
            if (!rabbitMqConsumer.AutoAck)
            {
                // 消费完成后需要手动签收消息，如果不写该代码就容易导致重复消费问题
                // 可以降低每次签收性能损耗
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: rabbitMqConsumer.Multiple);
            }
        };
        // 消息签收模式
        // 手动签收 保证正确消费，不会丢消息(基于客户端而已)
        // 自动签收 容易丢消息 
        // 签收：意味着消息从队列中删除
        channel.BasicConsume(queue: rabbitMqConsumer.QueueName,
                             autoAck: rabbitMqConsumer.AutoAck,
                             consumer: consumer);
    }

    /// <summary>
    /// 仅发送，如果没有交换机队列的情况下会失败
    /// 建议注册失败回调后操作
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool BasicPublish(IModel channel, RabbitMqPublisher rabbitMqPublisher)
    {
        try
        { 
            //事务
            switch (rabbitMqPublisher.PublisherConfirmType)
            {
                case PublisherConfirmType.Close:
                    break;
                case PublisherConfirmType.Transaction:
                    channel.TxSelect();
                    break;
                case PublisherConfirmType.NormalConfirm:
                case PublisherConfirmType.BatchConfirm:
                case PublisherConfirmType.ListenerConfirm:
                    rabbitMqPublisher.Mandatory = true;
                    channel.ConfirmSelect();// 开启消息确认模式
                    break;
                default:
                    throw new ArgumentException("意外的生产者Confirm类型模式");
            }
            ///持久化
            var properties = channel.CreateBasicProperties();
            properties.Persistent = rabbitMqPublisher.Persistent;
            //设置消息优先级,仅在消息堆积时有用
            properties.Priority = rabbitMqPublisher.Priority;
            //设置幂等Id
            properties.MessageId = rabbitMqPublisher.IdempotentMessageId;
            channel.BasicPublish(exchange: rabbitMqPublisher.ExchangeName,
                            routingKey: rabbitMqPublisher.RoutingKey,
                            mandatory: rabbitMqPublisher.Mandatory,
                            basicProperties: properties,
                            body: rabbitMqPublisher.EncodingType.GetBytes(rabbitMqPublisher.Data));
            switch (rabbitMqPublisher.PublisherConfirmType)
            {
                case PublisherConfirmType.Close:
                    break;
                case PublisherConfirmType.Transaction:
                    channel.TxCommit();
                    break;
                case PublisherConfirmType.NormalConfirm:
                case PublisherConfirmType.BatchConfirm:
                    // 消息到达服务端队列中才返回结果
                    if (!channel.WaitForConfirms() && rabbitMqPublisher.ErrorConfirmCallback is not null)
                    {
                        rabbitMqPublisher.ErrorConfirmCallback(0, "", Encoding.UTF8.GetBytes("消息发送失败了"));
                    }
                    break;
                case PublisherConfirmType.ListenerConfirm:
                    /*-------------Return机制：不可达的消息消息监听--------------*/
                    //这个事件就是用来监听我们一些不可达的消息的内容的：比如某些情况下交换机没有绑定到队列的情况下
                    EventHandler<BasicReturnEventArgs> evreturn = new((o, basic) =>
                    {
                        //basic.ReplyCode; //消息失败的code
                        //basic.ReplyText; //描述返回原因的文本。
                        //basic.Body.Span; //失败消息的内容
                        //返回绑定事件我们可能要对这条不可达消息做处理，比如是否重发这条不可达的消息呀，或者这条消息发送到其他的路由中等等
                        if (rabbitMqPublisher.ErrorConfirmCallback is not null)
                        {
                            rabbitMqPublisher.ErrorConfirmCallback(basic.ReplyCode, basic.ReplyText, basic.Body.Span.ToArray());
                        }
                    });
                    channel.BasicReturn += evreturn;
                    //消息发送成功的时候进入到这个事件：即RabbitMq服务器告诉生产者，我已经成功收到了消息
                    //EventHandler<BasicAckEventArgs> BasicAcks = new((o, basic) =>
                    //{
                    //    Console.WriteLine("RabbitMq服务器告诉生产者，我已经成功收到了消息");
                    //});
                    //channel.BasicAck += BasicAcks;
                    break;
                default:
                    throw new ArgumentException("意外的生产者Confirm类型模式");
            }
            return true;
        }
        catch (Exception e)
        {
            if (channel.IsOpen)
            {
                if (rabbitMqPublisher.ErrorConfirmCallback is not null)
                {
                    rabbitMqPublisher.ErrorConfirmCallback(0, "", Encoding.UTF8.GetBytes($"消息发送失败了,抛出了异常：异常：{e.Message}"));
                }
                channel.TxRollback();
            }
            return false;
        }
    }
    #endregion

    /// <summary>
    /// 优先级设置
    /// </summary>
    /// <param name="rabbitMqBase"></param>
    /// <returns></returns>
    private RabbitMqBase SetPriority(RabbitMqBase rabbitMqBase)
    {
        if (rabbitMqBase.QueueArguments is null)
        {
            rabbitMqBase.QueueArguments = new Dictionary<string, object> { { "x-max-priority", 10 } };
        }
        else
        {
            if (!rabbitMqBase.QueueArguments.ContainsKey("x-max-priority"))
            {
                rabbitMqBase.QueueArguments.Add("x-max-priority", 10);
            }
        }
        return rabbitMqBase;
    }
    /// <summary>
    /// 消费者基础设置
    /// </summary>
    /// <param name="rabbitConsumer"></param>
    /// <returns></returns>
    private RabbitMqConsumer GetRabbitMqConsumer(Action<RabbitMqConsumer> rabbitConsumer)
    {
        RabbitMqConsumer rabbitMqConsumer = new RabbitMqConsumer();
        rabbitConsumer(rabbitMqConsumer);
        return SetMqArguments<RabbitMqConsumer>(rabbitMqConsumer);
    }
    /// <summary>
    /// 生产者基础设置
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    private RabbitMqPublisher GetRabbitMqPublisher(Action<RabbitMqPublisher> rabbitPublisher,IModel channel)
    {
        RabbitMqPublisher rabbitMqPublisher = new RabbitMqPublisher();
        rabbitPublisher(rabbitMqPublisher);
        rabbitMqPublisher = SetMqArguments<RabbitMqPublisher>(rabbitMqPublisher);
        channel = SetIfConfigDLX(channel, rabbitMqPublisher);
        channel = SetIfConfigBackExchange(channel, rabbitMqPublisher);
        return rabbitMqPublisher;
    }
    private T SetMqArguments<T>(RabbitMqBase rabbitMqBase)where T : RabbitMqBase
    {
        if (rabbitMqBase.Priority < 10)//优先级设置
        {
            rabbitMqBase = SetPriority(rabbitMqBase);
        }
        if (rabbitMqBase.DLXPublisher is not null)//死信交换机设置
        {
            rabbitMqBase = SetDLXQueueArguments(rabbitMqBase);
        }
        if (rabbitMqBase.BackExchangePublisher is not null)//备用交换机设置
        {
            rabbitMqBase = SetBackExchange(rabbitMqBase);
        }
        if (rabbitMqBase.Expires > -1)//过期队列设置
        {
            rabbitMqBase = SeExpiresQueue(rabbitMqBase);
        }
        return (T)rabbitMqBase;
    }
    /// <summary>
    /// 设置过期队列
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    private RabbitMqBase SeExpiresQueue(RabbitMqBase rabbitMqPublisher)
    {
        if (rabbitMqPublisher.QueueArguments is null)
        {
            rabbitMqPublisher.QueueArguments = new Dictionary<string, object> { { "x-expires", rabbitMqPublisher.Expires * 1000 } };
        }
        else
        {
            rabbitMqPublisher.QueueArguments.Add("x-expires", rabbitMqPublisher.Expires * 1000); // 到时间后队列自动干掉
        }
        return rabbitMqPublisher;
    }
    /// <summary>
    /// 设置备用交换机
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private RabbitMqBase SetBackExchange(RabbitMqBase rabbitMqPublisher)
    {
        if (rabbitMqPublisher.ExchangeArguments is null)
        {
            rabbitMqPublisher.ExchangeArguments = new Dictionary<string, object> { { "alternate-exchange", rabbitMqPublisher.BackExchangePublisher.ExchangeName } };
        }
        else
        {
            rabbitMqPublisher.ExchangeArguments.Add("alternate-exchange", rabbitMqPublisher.BackExchangePublisher.ExchangeName); // 到时间后队列自动干掉
        }
        return rabbitMqPublisher;
    }

    private IModel SetIfConfigBackExchange(IModel channel, RabbitMqPublisher rabbitMqPublisher)
    {
        if (rabbitMqPublisher.BackExchangePublisher is not null)
        {
            switch (rabbitMqPublisher.BackExchangePublisher.DlxOrBackExchangeMQType)
            {
                case DlxOrBackExchangeMQType.Nomal:
                case DlxOrBackExchangeMQType.Worker: 
                    channel = QueueDeclare(channel, rabbitMqPublisher.BackExchangePublisher);
                    channel = QueueBind(channel, rabbitMqPublisher.BackExchangePublisher);
                    break;
                case DlxOrBackExchangeMQType.FanOut:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.BackExchangePublisher, ExchangeType.Fanout);
                    break;
                case DlxOrBackExchangeMQType.Direct:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.BackExchangePublisher, ExchangeType.Direct);
                    break;
                case DlxOrBackExchangeMQType.Topic:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.BackExchangePublisher, ExchangeType.Topic);
                    break;
                default:
                    break;
            }
        }
        return channel;
    }
    /// <summary>
    /// 设置死信队列的队列参数
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private RabbitMqBase SetDLXQueueArguments(RabbitMqBase rabbitMqPublisher)
    {
        if (rabbitMqPublisher.DLXTTL < 0)
        {
            throw new ArgumentException("死信队列过期时间不能小于0");
        }
        if (rabbitMqPublisher.DLXPublisher is null)
        {
            throw new ArgumentNullException("需要设置死信队列交换机！");
        }
        #region 局部方法
        RabbitMqBase AddDlxRoutingKey(RabbitMqBase rabbitMqPublisher)
        {
            //设置DLX的路由key，DLX会根据该值去找到死信消息存放的队列
            switch (rabbitMqPublisher.DlxOrBackExchangeMQType)
            {
                case DlxOrBackExchangeMQType.Nomal:
                case DlxOrBackExchangeMQType.Worker:
                case DlxOrBackExchangeMQType.FanOut:
                    rabbitMqPublisher.QueueArguments.Add("x-dead-letter-routing-key", rabbitMqPublisher.DLXPublisher.RoutingKey);
                    break;
                case DlxOrBackExchangeMQType.Direct:
                case DlxOrBackExchangeMQType.Topic:
                    string binding = string.Empty;
                    foreach (var item in rabbitMqPublisher.DLXPublisher.DeclareQueuesAndRoutingKeys)
                    {
                        binding += item.bindingRoutingKey + ',';
                    }
                    rabbitMqPublisher.QueueArguments.Add("x-dead-letter-routing-key", binding.TrimEnd(','));
                    break;
                default:
                    break;
            }
            return rabbitMqPublisher;
        }
        #endregion
        if (rabbitMqPublisher.QueueArguments is null)
        {
            rabbitMqPublisher.QueueArguments = new Dictionary<string, object>
            {
                 { "x-dead-letter-exchange",rabbitMqPublisher.DLXPublisher.ExchangeName}, //设置当前队列的DLX
                 { "x-message-ttl",rabbitMqPublisher.DLXTTL * 1000} //设置消息的存活时间，即过期时间
            };
            //设置DLX的路由key，DLX会根据该值去找到死信消息存放的队列
            rabbitMqPublisher = AddDlxRoutingKey(rabbitMqPublisher);
        }
        else
        {
            if (!rabbitMqPublisher.QueueArguments.ContainsKey("x-dead-letter-exchange"))
            {
                rabbitMqPublisher.QueueArguments.Add("x-dead-letter-exchange", rabbitMqPublisher.DLXPublisher.ExchangeName);
            }
            if (!rabbitMqPublisher.QueueArguments.ContainsKey("x-message-ttl"))
            {
                rabbitMqPublisher.QueueArguments.Add("x-message-ttl", rabbitMqPublisher.DLXTTL * 1000);
            }
            if (!rabbitMqPublisher.QueueArguments.ContainsKey("x-dead-letter-routing-key"))
            {
                //设置DLX的路由key，DLX会根据该值去找到死信消息存放的队列
                rabbitMqPublisher = AddDlxRoutingKey(rabbitMqPublisher);
            }
        }
        return rabbitMqPublisher;
    }
    /// <summary>
    /// 设置配置死信模式的交换机和队列
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private IModel SetIfConfigDLX(IModel channel, RabbitMqPublisher rabbitMqPublisher)
    {
        if (rabbitMqPublisher.DLXPublisher is not null)
        {
            switch (rabbitMqPublisher.DlxOrBackExchangeMQType)
            {
                case DlxOrBackExchangeMQType.Nomal:
                case DlxOrBackExchangeMQType.Worker:
                    if (rabbitMqPublisher.DLXPublisher.DeclareQueuesAndRoutingKeys is null)
                    {
                        rabbitMqPublisher.DLXPublisher.DeclareQueuesAndRoutingKeys = new List<(string queueName, string bindingRoutingKey)> { (rabbitMqPublisher.DLXPublisher.QueueName, rabbitMqPublisher.DLXPublisher.RoutingKey) };
                    }
                    channel = QueueDeclare(channel, rabbitMqPublisher.DLXPublisher);
                    break;
                case DlxOrBackExchangeMQType.FanOut:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.DLXPublisher, ExchangeType.Fanout);
                    break;
                case DlxOrBackExchangeMQType.Direct:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.DLXPublisher, ExchangeType.Direct);
                    break;
                case DlxOrBackExchangeMQType.Topic:
                    channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher.DLXPublisher, ExchangeType.Topic);
                    break;
                default:
                    throw new ArgumentException("意外的类型");
            }
        }
        return channel;
    }

    /// <summary>
    /// 扇形/路由和主题模式生产者队列声明和绑定
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqBase"></param>
    /// <param name="mqType"></param>
    /// <returns></returns>
    private IModel FanoutDirectTopicCreateExchangeQueue(IModel channel, RabbitMqBase rabbitMqBase, string mqType)
    {
        // 声明Fanout/Direct/Topic交换机
        channel = ExchangeDeclare(channel, rabbitMqBase, mqType);
        
        // 创建队列
        foreach (var queue in rabbitMqBase.DeclareQueuesAndRoutingKeys)
        {
            channel.QueueDeclare(queue: queue.queueName,
                               durable: rabbitMqBase.QueueDurable,
                               exclusive: rabbitMqBase.QueueExclusive,
                               autoDelete: rabbitMqBase.QueueAutoDelete,
                               arguments: rabbitMqBase.QueueArguments);
            // 绑定到交换机
            channel.QueueBind(queue: queue.queueName,
                              exchange: rabbitMqBase.ExchangeName,
                              routingKey: queue.bindingRoutingKey);
        }
        return channel;
    }
    /// <summary>
    /// 路由和主题模式消费者队列声明和绑定
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    /// <param name="mqType"></param>
    /// <returns></returns>
    private void FanoutDirectTopicReceive(Action<RabbitMqConsumer> rabbitConsumer, string mqType)
    {
        RabbitMqConsumer rabbitMqConsumer = GetRabbitMqConsumer(rabbitConsumer);
        if (string.IsNullOrWhiteSpace(rabbitMqConsumer.QueueName))
        {
            throw new ArgumentNullException($"{mqType}:需要具体指定消费的 QueueName 不能为空");
        }
        IModel channel = CreateChannel();
        channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqConsumer, mqType);
        Received(rabbitMqConsumer, channel);
    }
    private void FanoutDirectTopicSend(IModel channel, Action<RabbitMqPublisher> rabbitPublisher, string mqType)
    {
        RabbitMqPublisher rabbitMqPublisher = GetRabbitMqPublisher(rabbitPublisher, channel);
        channel = FanoutDirectTopicCreateExchangeQueue(channel, rabbitMqPublisher, mqType);
        if (!rabbitMqPublisher.OnlyInitNoSend)
        {
            BasicPublish(channel, rabbitMqPublisher);
        }
    }
    #endregion

    #region 自定义生产者和消费者
    public void CustomerSend(Action<IModel> customerSend) => customerSend(CreateChannel());
    public void CustomerReceive(Action<IModel> customerReceive) => customerReceive(CreateChannel());
    #endregion

    #region 点对点消息队列

    #region --生产者
    /// <summary>
    /// 发送一条普通队列的MQ消息，消息发送后channel通道立即关闭
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    public void NomarlSend(Action<RabbitMqPublisher> rabbitMqPublisher) => CustomerSend(channel =>
    {
        using (channel)
        {
            NomarlSend(channel, rabbitMqPublisher);
        }
    });
    /// <summary>
    /// 这里不会释放Channel，交由外部释放与否。
    /// 使用场景，在确定发送量多的情况下可以开启一次channel快速发送多条消息
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    public void NomarlSend(IModel channel, Action<RabbitMqPublisher> rabbitPublisher)
    {
        RabbitMqPublisher rabbitMqPublisher = GetRabbitMqPublisher(rabbitPublisher,channel);
        channel = SetIfConfigDLX(channel, rabbitMqPublisher);
        channel = SetIfConfigBackExchange(channel, rabbitMqPublisher);

        if (rabbitMqPublisher.DeclareQueuesAndRoutingKeys is null)
        {
            rabbitMqPublisher.DeclareQueuesAndRoutingKeys = new List<(string queueName, string bindingRoutingKey)> { (rabbitMqPublisher.QueueName, rabbitMqPublisher.RoutingKey) };
        }
        channel = QueueDeclare(channel, rabbitMqPublisher);
        if (!rabbitMqPublisher.OnlyInitNoSend)
        {
            BasicPublish(channel, rabbitMqPublisher);
        }
    } 
    #endregion

    #region --消费者
    public void NomarReceive(Action<RabbitMqConsumer> rabbitConsumer)
    {
        RabbitMqConsumer rabbitMqConsumer = GetRabbitMqConsumer(rabbitConsumer);
        if (rabbitMqConsumer.DeclareQueuesAndRoutingKeys is null)
        {
            rabbitMqConsumer.DeclareQueuesAndRoutingKeys = new List<(string queueName, string bindingRoutingKey)> { (rabbitMqConsumer.QueueName, rabbitMqConsumer.RoutingKey) };
        }
        var channel = QueueDeclare(CreateChannel(), rabbitMqConsumer);
        Received(rabbitMqConsumer, channel);
    }
    #endregion

    #endregion

    #region 工作模式消息队列
    #region --生产者
    /// <summary>
    /// 发送一条普通队列的MQ消息，消息发送后channel通道立即关闭
    /// </summary>
    /// <param name="rabbitMqPublisher"></param>
    public void WokerSend(Action<RabbitMqPublisher> rabbitMqPublisher) => NomarlSend(rabbitMqPublisher);
    /// <summary>
    /// 这里不会释放Channel，交由外部释放与否。
    /// 使用场景，在确定发送量多的情况下可以开启一次channel快速发送多条消息
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    public void WokerSend(IModel channel, Action<RabbitMqPublisher> rabbitMqPublisher) => NomarlSend(channel, rabbitMqPublisher);
    #endregion

    #region 消费者
    public void WorkerReceive(Action<RabbitMqConsumer> rabbitConsumer) => NomarReceive(rabbitConsumer);
    #endregion

    #endregion

    #region 扇形消息队列

    #region --生产者
    /// <summary>
    /// 扇形发送一条普通队列的MQ消息，消息发送后channel通道立即关闭
    /// </summary>
    /// <param name="rabbitMqModel"></param>
    public void FanoutSend(Action<RabbitMqPublisher> rabbitMqModel) => CustomerSend(channel =>
    {
        using (channel)
        {
            FanoutSend(channel, rabbitMqModel);
        }
    });
    /// <summary>
    /// 这里不会释放Channel，交由外部释放与否。
    /// 使用场景，在确定发送量多的情况下可以开启一次channel快速发送多条消息,
    /// 该方法慎用ListenerConfirm事件注册回调,channel交由外部释放，会注册多次失败事件。
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    public void FanoutSend(IModel channel , Action<RabbitMqPublisher> rabbitPublisher) => FanoutDirectTopicSend(channel, rabbitPublisher, ExchangeType.Fanout);
    #endregion

    #region --消费者
    /// <summary>
    /// 扇形模式消费，消费只关心队列，不关心路由Key
    /// </summary>
    /// <param name="rabbitConsumer"></param>
    public void FanoutReceive(Action<RabbitMqConsumer> rabbitConsumer) => FanoutDirectTopicReceive(rabbitConsumer, ExchangeType.Fanout);
    #endregion

    #endregion

    #region 路由模式消息队列

    #region --生产者
    /// <summary>
    /// 路由模式发送一条普通队列的MQ消息，消息发送后channel通道立即关闭
    /// </summary>
    /// <param name="rabbitMqModel"></param>
    public void DirectRoutingSend(Action< RabbitMqPublisher> rabbitMqModel) => CustomerSend(channel =>
    {
        using (channel)
        {
            DirectRoutingSend(channel, rabbitMqModel);
        }
    });
    public void DirectRoutingSend(IModel channel, Action<RabbitMqPublisher> rabbitPublisher) => FanoutDirectTopicSend(channel, rabbitPublisher, ExchangeType.Direct);
    #endregion

    #region --消费者
    /// <summary>
    /// 路由模式消费，消费只关心队列，不关心路由Key
    /// </summary>
    /// <param name="rabbitConsumer"></param>
    public void DirectRoutingReceive(Action<RabbitMqConsumer> rabbitConsumer) => FanoutDirectTopicReceive(rabbitConsumer, ExchangeType.Direct);
    #endregion

    #endregion

    #region 主题模式消息队列

    #region --生产者
    /// <summary>
    /// 主题模式发送一条普通队列的MQ消息，消息发送后channel通道立即关闭
    /// 发送只关心路由Key，不关心队列
    /// rabbitMq.TopicSend(new RabbitMqPublisher
    ///{
    ///    Exchange = "topic_exchange",
    ///    DTRouting = new List<(string, string)> { ("topic_queue1","user.data.#"), ("topic_queue2", "user.data.delete"), 
    ///                                             ("topic_queue3", "user.data.update") },
    ///    Durable = true,
    ///    Data = data--string,
    ///    RoutingKey = "user.data.update",
    ///});
    /// </summary>
    /// <param name="rabbitMqModel"></param>
    public void TopicSend(Action<RabbitMqPublisher> rabbitMqModel) => CustomerSend(channel =>
    {
        using (channel)
        {
            TopicSend(channel, rabbitMqModel);
        }
    });
    /// <summary>
    /// 发送只关心路由Key，不关心队列
    /// rabbitMq.TopicSend(channel,new RabbitMqPublisher
    ///{
    ///    Exchange = "topic_exchange",
    ///    DTRouting = new List<(string, string)> { ("topic_queue1","user.data.#"), ("topic_queue2", "user.data.delete"), 
    ///                                             ("topic_queue3", "user.data.update") },
    ///    Durable = true,
    ///    Data = data--string,
    ///    RoutingKey = "user.data.update",
    ///});
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="rabbitMqPublisher"></param>
    public void TopicSend(IModel channel, Action<RabbitMqPublisher> rabbitPublisher) => FanoutDirectTopicSend(channel, rabbitPublisher, ExchangeType.Topic);
    #endregion

    #region --消费者
    /// <summary>
    /// 主题模式消费，消费只关心队列，不关心路由Key
    /// rabbitMq.TopicReceive(s =>
    ///{
    ///    s.Exchange = "topic_exchange";
    ///    s.QueuesName = new List<string> { "topic_queue2" };
    ///    s.Durable = true;
    ///    s.ReceiveByteCallback = s => Console.WriteLine("receive--------------0" + Encoding.UTF8.GetString(s));
    ///    s.AutoAck = false;
    ///});
    /// </summary>
    /// <param name="rabbitConsumer"></param>
    public void TopicReceive(Action<RabbitMqConsumer> rabbitConsumer) => FanoutDirectTopicReceive(rabbitConsumer, ExchangeType.Topic);
    #endregion

    #endregion

}