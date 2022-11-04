using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQ;
public class RabbitMqBase
{
    /// <summary>
    /// 交换机
    /// </summary>
    public string ExchangeName { get; set; } = "";
    /// <summary>
    /// 交换机和队列持久化，默认false
    /// </summary>
    public bool ExchangeDurable { get; set; } = false;
    public bool ExchangeExclusive { get; set; } = false;
    public bool ExchangeAutoDelete { get; set; } = false;
    public IDictionary<string, object> ExchangeArguments { get; set; } = null;

    /// <summary>
    /// 路由Key，发送接收使用
    /// </summary>
    public string RoutingKey { get; set; } = "";
    /// <summary>
    /// 队列名称
    /// </summary>
    public string QueueName { get; set; } = "";
    /// <summary>
    /// 该处为声明队列绑定使用，不做实际发送使用
    /// 元组第一位为QueueName，第二位为RoutingKey使用
    /// </summary>
    public IEnumerable<(string queueName, string bindingRoutingKey)> DeclareQueuesAndRoutingKeys { get; set; }
    /// <summary>
    /// 队列持久化，默认false
    /// </summary>
    public bool QueueDurable { get; set; } = false;
    public bool QueueExclusive { get; set; } = false;
    public bool QueueAutoDelete { get; set; } = false;
    /// <summary>
    /// 此队列的使用是否应仅限于其声明连接？当此类队列的声明连接关闭时，将删除该队列。
    /// </summary>
    public IDictionary<string, object> QueueArguments { get; set; } = null;

    /// <summary>
    /// 消息类型，死信队列和备用交换机模式下配置该项生效
    /// </summary>
    public DlxOrBackExchangeMQType DlxOrBackExchangeMQType { get; set; }
   
    /// <summary>
    /// 消息优先级，只有在消息堆积情况下才有效，默认10不开启
    /// 优先级范围0-9
    /// </summary>
    public byte Priority { get; set; } = 10;
    /// <summary>
    /// 设置路由到死信队列消息的存活时间,即过期时间,单位秒，默认10秒
    /// </summary>
    public int DLXTTL { get; set; } = 10;

    /// <summary>
    /// 备用交换机声明，默认为null
    /// 不为null即开启
    /// </summary>
    public RabbitMqPublisher BackExchangePublisher { get; set; } = null;

    /// <summary>
    /// 设置过期队列的存活时间,即过期时间,单位秒,默认-1不生效,大于0秒即为开启
    /// </summary>
    public int Expires { get; set; } = -1;
    /// <summary>
    /// 死信队列交换机和队列声明。 默认为null
    /// 不为null即开启
    /// </summary>
    public RabbitMqPublisher DLXPublisher { get; set; } = null;
}
/// <summary>
/// 生产者
/// </summary>
public class RabbitMqPublisher : RabbitMqBase
{
    /// <summary>
    /// 只做队列初始化，不发送消息，默认不开启
    /// </summary>
    public bool OnlyInitNoSend { get; set; } = false;
    /// <summary>
    /// 幂等消息ID
    /// </summary>
    public string IdempotentMessageId { get; set; } = "";
    /// <summary>
    /// 消息持久化，默认false
    /// </summary>
    public bool Persistent { get; set; } = false;
    /// <summary>
    /// 生产者消息确认机制，默认不开启。
    /// 开启推荐使用ListenerConfirm，需注册回调事件
    /// ListenerConfirm模式下注册回调后Mandatory会默认设置为true，只有Mandatory为true，才会触发事件回调
    /// </summary>
    public PublisherConfirmType PublisherConfirmType { get; set; } = PublisherConfirmType.Close;
    /// <summary>
    /// 消息失败回调（ReturnModel），只有PublisherConfirmType是ListenerConfirm的时候才会给出异常Code，失败原因和失败消息
    /// 其他模式下只会返回失败
    /// 不注册不会回调,
    /// ListenerConfirm模式下只有Funout、Direct和Topic会返回失败信息内容，其他不会回复
    /// </summary>
    public Action<ushort , string , byte[]> ErrorConfirmCallback { get; set; } = null;
    /// <summary>
    /// 开启强制消息投递（mandatory为设置为true），但消息未被路由至任何一个queue，则回退一条消息到ErrorConfirmCallback
    /// 如果开启了ListenerConfirm模式，这里自动设置为true
    /// </summary>
    public bool Mandatory { get; set; } = false;
    /// <summary>
    /// 数据，默认使用UTF-8发送，如果修改发送类型，需要设置EncodingType
    /// </summary>
    public string Data { get; set; }
    /// <summary>
    /// 默认使用UTF8转码发送
    /// </summary>
    public Encoding EncodingType { get; set; } = Encoding.UTF8;
}
/// <summary>
/// 消费者
/// </summary>
public class RabbitMqConsumer : RabbitMqBase
{

    /// <summary>
    /// 默认0
    /// 最多传输的内容的大小的限制，0为不限制，但据说prefetchSize参数，RabbitMQ暂未对其没有实现。
    /// </summary>
    public uint QosPrefetchSize { get; set; } = 0;
    /// <summary>
    /// 默认1
    /// 会告诉RabbitMQ不要同时给一个消费者推送多于N个消息，即一旦有N个消息还没有ack，
    /// 则该消费者Consumer将阻塞block掉，直到有消息进行ack确认
    /// </summary>
    public ushort QosPrefetchCount { get; set; } = 1;
    /// <summary>
    /// 默认false
    /// 表示是否将上面设置应用于channel，简单点说，就是上面限制是信道channel级别的还是消费者consumer级别
    /// </summary>
    public bool QosGlobal { get; set; } = false;
    /// <summary>
    /// 是否开启自动签收机制，默认为手动
    /// </summary>
    public bool AutoAck { get; set; } = false;
    /// <summary>
    /// 开启批量手动签收，false为单条签收，批量手动签收可以提高处理性能
    /// 默认开启批量签收
    /// </summary>
    public bool Multiple { get; set; } = true;
    /// <summary>
    /// 开启接收优先级
    /// 如果在Arguments已经设置过，就无需在这里单独开启
    /// </summary>
    public bool OpenPriority { get; set; } = false;
    /// <summary>
    /// 仅有消息内容的回调，不注册，不生效
    /// </summary>
    public Action<byte[]> ReceiveByteCallback { get; set; } = null;
    /// <summary>
    /// 带有全部回调事件内容的回调，不注册，不生效
    /// </summary>
    public Action<BasicDeliverEventArgs> ReceiverCallback { get; set; } = null;
}
/// <summary>
/// 生产者的消息确认机制发送模式
/// </summary>
public enum PublisherConfirmType
{
    /// <summary>
    /// 不使用消息确认模式
    /// </summary>
    Close,
    /// <summary>
    /// 事件模式
    /// </summary>
    Transaction,
    /// <summary>
    /// 普通确认模式
    /// </summary>
    NormalConfirm,
    /// <summary>
    /// 批量确认模式
    /// </summary>
    BatchConfirm,
    /// <summary>
    /// ReturnModel，带有失败消息回调的方式
    /// </summary>
    ListenerConfirm
}
/// <summary>
/// 死信和备用交换机使用这里
/// </summary>
public enum DlxOrBackExchangeMQType
{
    Nomal,
    Worker,
    FanOut,
    Direct,
    Topic
}
public class RabbitMqRegisterModel
{
    public string HostName { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public bool UseCluster { get; set; } = false;
    public List<AmqpTcpEndpoint> ClusterNodes { get; set; }
}