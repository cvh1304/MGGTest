using Domain.Common;

namespace Domain.Interfaces;

public interface IMessageBus
{
    PublishErrorHandler PublishErrorHandler { get; set; }

    void Publish(Message message);

    void Subscribe(Action<Message> proceedAction);
}
